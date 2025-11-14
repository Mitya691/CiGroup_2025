using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DesktopClient.Model;
using Microsoft.Extensions.Logging;

namespace DesktopClient.Services
{
    public class PollingService : IPollingService
    {
        private readonly ILogger<PollingService> _logger;
        private readonly ISQLRepository _repo;
        private readonly TimeSpan _period; // фиксированный период опроса

        private CancellationTokenSource? _cts;
        private Task? _loop;

        // актуальное значение лага; читаем/пишем атомарно
        private int _lagSeconds;

        public event Action<IReadOnlyList<Card>>? CardsCreated;

        public PollingService(ISQLRepository repo, TimeSpan period, int lagSeconds, ILogger<PollingService> logger)
        {
            _repo = repo;
            _period = period;
            _lagSeconds = Math.Max(0, lagSeconds);
            _logger = logger;
        }

        public void UpdateLagSeconds(int lagSeconds)
            => Interlocked.Exchange(ref _lagSeconds, Math.Max(0, lagSeconds));

        public Task StartAsync(CancellationToken ct = default)
        {
            // уже запущен — выходим
            if (_loop is { IsCompleted: false }) return Task.CompletedTask;

            _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            _loop = Task.Run(() => RunAsync(_cts.Token));
            return Task.CompletedTask;
        }

        public void Stop()
        {
            try { _cts?.Cancel(); } catch { /* ignore */ }
            _cts = null;
            _loop = null;
        }

        private async Task RunAsync(CancellationToken ct)
        {
            // стартовый чекпоинт берём из БД (чтобы не потерять события)
            DateTime lastEnd = await _repo.GetMaxCardEndAsync(ct).ConfigureAwait(false);
            _logger.LogInformation("Start polling from {lastEnd}", lastEnd);

            using var timer = new PeriodicTimer(_period);
            try
            {
                while (await timer.WaitForNextTickAsync(ct).ConfigureAwait(false))
                {
                    try
                    {
                        int lagSec = Volatile.Read(ref _lagSeconds);

                        // есть ли созревшие закрытия после lastEnd?
                        bool hasNew = await _repo
                            .CheckCompletedIntervalAsync(lastEnd, lagSec, ct)
                            .ConfigureAwait(false);
                        if (!hasNew) continue;

                        // вставляем созревшие карточки
                        await _repo.InsertNewCard(lagSec, lastEnd, ct).ConfigureAwait(false);

                        // забираем все, что закрыто после lastEnd
                        var fresh = await _repo
                            .GetCardsClosedAfterAsync(lastEnd, take: 100, ct)
                            .ConfigureAwait(false);
                        if (fresh.Count == 0)
                        {
                            _logger.LogWarning("Detected closed interval but no rows were returned by GetCardsClosedAfterAsync");
                            continue;
                        }

                        // двигаем чекпоинт
                        lastEnd = fresh.Max(c => c.EndTime);

                        // слепок (чтобы подписчики не модифицировали наш список) + по возрастанию времени
                        var payload = fresh.OrderBy(c => c.EndTime)
                                           .ToList()
                                           .AsReadOnly();

                        CardsCreated?.Invoke(payload); // событие с фонового потока
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogInformation("Polling cancelled");
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error during polling iteration");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Polling loop cancelled");
            }
        }
    }
}


