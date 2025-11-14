using DesktopClient.Helpers;
using DesktopClient.Model;
using DesktopClient.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DesktopClient.VM
{
    public class HomeCardVM : ViewModelBase
    {
        private readonly ISQLRepository _repository;
        private readonly CancellationToken _ct;
        private Card _card;

        public long Id => _card.Id;
        public DateTime StartTime => _card.StartTime;
        public DateTime StopTime => _card.EndTime;
        public string? SourceSilo => _card.SourceSilo;
        public string? Direction => _card.Direction;
        public decimal? Weight1 => _card.Weight1;
        public decimal? Weight2 => _card.Weight2;
        public decimal? TotalWeight => _card.TotalWeight;

        private string? _originalTargetSilo;

        private string? _targetSilo;
        public string? TargetSilo
        {
            get => _targetSilo;
            set
            {
                if (_targetSilo != value)
                {
                    _targetSilo = value;
                    OnPropertyChanged();
                    SaveCardCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public bool IsDirty => !StringsEqual(_targetSilo, _originalTargetSilo);

        public ObservableCollection<string> AvailableTargetSilos { get; } = new();

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            private set { _isBusy = value; OnPropertyChanged(); SaveCardCommand.RaiseCanExecuteChanged(); }
        }

        private string? _status;
        public string? Status
        {
            get => _status;
            private set { _status = value; OnPropertyChanged(); }
        }

        public AsyncRelayCommand SaveCardCommand { get; }

        public HomeCardVM(ISQLRepository repository, Card card,
                          List<string> targetSilos,
                          string status = null, CancellationToken ct = default)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _card = card ?? throw new ArgumentNullException(nameof(card));
            _ct = ct;
            _status = status;
            // Инициализируем редактируемое поле из модели
            _targetSilo = _card.TargetSilo;

            if (targetSilos != null)
                foreach (var s in targetSilos)
                    AvailableTargetSilos.Add(s);

            SaveCardCommand = new AsyncRelayCommand(DoSaveAsync, CanSave);
        }

        private async Task DoSaveAsync()
        {
            if (_repository == null) return; 
            try
            {
                IsBusy = true;
                Status = "Сохранение";

                // простая валидация перед сохранением
                if (string.IsNullOrWhiteSpace(TargetSilo))
                {
                    Status = "Выберите целевой силос";
                    return;
                }

                await _repository.UpdateCardTargetSiloAsync(Id, TargetSilo!, _ct);

                // локально обновим модель (если где-то ещё используется)
                _card.TargetSilo = TargetSilo;

                Status = "Сохранено";
            }
            catch (OperationCanceledException)  
            {
                Status = "Отменено";
            }
            catch (Exception ex)
            {
                Status = "Ошибка: " + ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private bool CanSave()
        {
            // нельзя сохранять, если идёт операция или не выбран силос
            return !IsBusy && !string.IsNullOrWhiteSpace(TargetSilo);
        }

        private static bool StringsEqual(string? a, string? b)
            => string.Equals(a?.Trim(), b?.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}
    