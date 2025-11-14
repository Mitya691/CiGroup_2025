using DesktopClient.Config;
using DesktopClient.Helpers;
using DesktopClient.Model;
using DesktopClient.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace DesktopClient.VM
{
    public class HomeDialogVM : ViewModelBase, IAsyncDisposable
    {
        private readonly MainWindowVM _shell;

        private readonly ISQLRepository _repository;
        private IPollingService _polling;
        private ISettingsStore _settings;

        private DateTime? _filterStart;
        public DateTime? FilterStart
        {
            get { return _filterStart; }
            set
            {
                if(Set(ref _filterStart, value))
                    ApplyFilterCommand?.RaiseCanExecuteChanged();
            }
        }

        private DateTime? _filterEnd;
        public DateTime? FilterEnd
        {
            get { return _filterEnd; }
            set
            {
                if (Set(ref _filterEnd, value))
                    ApplyFilterCommand?.RaiseCanExecuteChanged();
            }
        }

        private int _convergenceTime = 300;
        public int ConvergenceTime
        {
            get { return _convergenceTime; }
            set
            {
                if(Set(ref _convergenceTime, value))
                {
                    _polling.LagSeconds = value;          // для текущей сессии
                    _settings.Settings.LagSeconds = value; // для перезапуска
                    _settings.SaveSettings();
                }
            }
        }

        public RelayCommand NavigateToReportsCommand { get; }
        public AsyncRelayCommand ApplyFilterCommand { get; }
        public AsyncRelayCommand ResetFilterCommand { get; }
        public AsyncRelayCommand LogoutCommand { get; }

        private readonly List<string> targetSilosM1 = new List<string>() { "SL201", "SL202", "SL203", "SL204", "SL205", "SL206" };
        private readonly List<string> targetSilosM2 = new List<string>() { "SL1201", "SL1202", "SL1203", "SL1204", "SL1205", "SL1206" };

        private CancellationTokenSource? _cts;

        // маркер последнего закрытого интервала
        private DateTime _lastEndTs = new DateTime(1000, 1, 1);

        public ObservableCollection<HomeCardVM> Cards { get; } = new();

        public HomeDialogVM(MainWindowVM shell, ISQLRepository repository, IPollingService polling, ISettingsStore settings)
        {
            _shell = shell;
            _repository = repository;
            _polling = polling;
            _settings = settings;
            _polling.CardsCreated += OnCardsCreated;

            ApplyFilterCommand = new AsyncRelayCommand(GetCardsForFilter, CanGetCardsForFilter);
            ResetFilterCommand = new AsyncRelayCommand(ResetFilter);
            NavigateToReportsCommand = new RelayCommand(() => _shell.NavigateToAsync(App.Services.GetRequiredService<ReportDialogVM>()));
            LogoutCommand = new AsyncRelayCommand(DoLogoutAsync);

            _convergenceTime = _polling.LagSeconds;
        }

        public async ValueTask DisposeAsync()
        {
            _polling.CardsCreated -= OnCardsCreated;
            await ValueTask.CompletedTask;
        }

        /// <summary>
        /// Первичная загрузка 30 карточек и запуск опроса
        /// </summary>
        public async Task InitializeAsync()
        {
            var cards = await _repository.GetLast30CardsAsync();
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                Cards.Clear();
                foreach (var c in cards)    
                    Cards.Add(MakeCardVM(c));
            });
        }

        private HomeCardVM MakeCardVM(Card c)
        {
            bool isM1 = string.Equals(c.Direction, "М1", StringComparison.OrdinalIgnoreCase)
                     || string.Equals(c.Direction, "M1", StringComparison.OrdinalIgnoreCase);
            var options = isM1 ? targetSilosM1 : targetSilosM2;

            return c.TargetSilo != null
                ? new HomeCardVM(_repository, c, options, "Сохранено")
                : new HomeCardVM(_repository, c, options);
        }

        private async void OnCardsCreated(IReadOnlyList<Card> fresh)
        {
            if (fresh == null || fresh.Count == 0) return;

            var snapshot = fresh.ToList(); // нежелательно трогать пришедшую коллекцию

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                foreach (var c in snapshot) // уже в порядке EndTime ASC
                    Cards.Insert(0, MakeCardVM(c));

                while (Cards.Count > 30)
                    Cards.RemoveAt(Cards.Count - 1);
            });
        }

        private async Task GetCardsForFilter()
        {
            var cards = await _repository.GetCardsForInterval(FilterStart, FilterEnd);
            if (cards.Count > 0)
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    Cards.Clear();
                    foreach (var c in cards)
                        if (c.Direction == "М1")
                        {
                            if (c.TargetSilo != null)
                                Cards.Add(new HomeCardVM(_repository, c, targetSilosM1, "Сохранено"));
                            else
                                Cards.Add(new HomeCardVM(_repository, c, targetSilosM1));
                        }
                        else if (c.Direction == "М2")
                        {
                            if (c.TargetSilo != null)
                                Cards.Add(new HomeCardVM(_repository, c, targetSilosM2, "Сохранено"));
                            else
                                Cards.Add(new HomeCardVM(_repository, c, targetSilosM2));
                        }
                });
            }
            else
            {
                MessageBox.Show("Нет карточек за выбранный период");
            }
        }

        private bool CanGetCardsForFilter() =>
           _filterStart != null && _filterEnd != null;

        private async Task ResetFilter()
        {
            FilterStart = null;
            FilterEnd = null;

            var cards = await _repository.GetLast30CardsAsync();
            App.Current.Dispatcher.Invoke(() =>
            {
                Cards.Clear();
                foreach (var c in cards)
                    if (c.Direction == "М1")
                    {
                        if (c.TargetSilo != null)
                            Cards.Add(new HomeCardVM(_repository, c, targetSilosM1, "Сохранено"));
                        else
                            Cards.Add(new HomeCardVM(_repository, c, targetSilosM1));
                    }
                    else if (c.Direction == "М2")
                    {
                        if (c.TargetSilo != null)
                            Cards.Add(new HomeCardVM(_repository, c, targetSilosM2, "Сохранено"));
                        else
                            Cards.Add(new HomeCardVM(_repository, c, targetSilosM2));
                    }
                if (Cards.Count > 0)
                    _lastEndTs = Cards[0].StopTime;
            });
        }

        private async Task DoLogoutAsync()
        {
            // 1) Обнулить текущего пользователя (через ресурс, как ты хочешь)
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                ((CurrentUserStore)Application.Current.Resources["CurrentUser"]).CurrentUser = null;
               
                Cards.Clear();
                FilterStart = null;
                FilterEnd = null;
            });

            // 3) Переход на окно авторизации
            var authVm = App.Services.GetRequiredService<AutorizationDialogVM>();
            await _shell.NavigateToAsync(authVm);
        }
    }
}
