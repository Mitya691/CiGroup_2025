using DesktopClient.Helpers;
using DesktopClient.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using DesktopClient.Services;
using DocumentFormat.OpenXml.Office.CoverPageProps;
using MimeKit.Encodings;
using System.Windows;
using System.Diagnostics;
using System.Collections.ObjectModel;
using DocumentFormat.OpenXml.Math;
using DocumentFormat.OpenXml.Drawing.Charts;
using Microsoft.Extensions.Logging;

namespace DesktopClient.VM
{
    public class ReportDialogVM : ViewModelBase
    {
        private readonly ILogger<ReportDialogVM> _logger;
        private readonly MainWindowVM _shell;
        private readonly ISQLRepository _repository;

        public IReportService _reportService;

        private DateTime? _dateStart = DateTime.Today;
        public DateTime? DateStart
        {
            get { return _dateStart; }
            set
            {
                if (Set(ref _dateStart, value))
                    GenerateReport?.RaiseCanExecuteChanged();
            }
        }

        private string _startTime = "8:00";
        public string StartTime
        {
            get { return _startTime; }
            set 
            {
                if (Set(ref _startTime, value))
                    GenerateReport?.RaiseCanExecuteChanged();
            }
        }

        private DateTime? _dateStop = DateTime.Today;
        public DateTime? DateStop
        {
            get { return _dateStop; }
            set
            {
                if (Set(ref _dateStop, value))
                    GenerateReport?.RaiseCanExecuteChanged();
            }
        }

        private string _stopTime = "20:00";
        public string StopTime
        {
            get { return _stopTime; }
            set
            {
                if (Set(ref _stopTime, value))
                    GenerateReport?.RaiseCanExecuteChanged();
            }
        }

        private DateTime? _dayDateStart = DateTime.Today.AddDays(-1);
        public DateTime? DayDateStart
        {
            get { return _dayDateStart; }
            set
            {
                if (Set(ref _dayDateStart, value))
                    GenerateDayReport?.RaiseCanExecuteChanged();
            }
        }

        private DateTime? _dayDateStop = DateTime.Today;
        public DateTime? DayDateStop
        {
            get { return _dayDateStop; }
            set
            {
                if (Set(ref _dayDateStop, value))
                    GenerateDayReport?.RaiseCanExecuteChanged();
            }
        }

        private string _dayStartTime = "8:00";
        public string DayStartTime
        {
            get { return _dayStartTime; }
            set
            {
                if (Set(ref _dayStartTime, value))
                    GenerateDayReport?.RaiseCanExecuteChanged();
            }
        }

        private string _dayStopTime = "8:00";
        public string DayStopTime
        {
            get { return _dayStopTime; }
            set
            {
                if (Set(ref _dayStopTime, value))
                    GenerateDayReport?.RaiseCanExecuteChanged();
            }
        }

        public ObservableCollection<string> Operators { get; } = new ObservableCollection<string>();

        private string _shiftOperator;
        public string ShiftOperator
        {
            get { return _shiftOperator; }
            set
            {
                if (Set(ref _shiftOperator, value))
                    GenerateReport?.RaiseCanExecuteChanged();
            }
        }

        private string _firstOperator;
        public string FirstOperator
        {
            get { return _firstOperator; }
            set
            {
                if (Set(ref _firstOperator, value))
                    GenerateDayReport?.RaiseCanExecuteChanged();
            }
        }

        private string _secondOperator;
        public string SecondOperator
        {
            get { return _secondOperator; }
            set
            {
                if (Set(ref _secondOperator, value))
                    GenerateDayReport?.RaiseCanExecuteChanged();
            }
        }

        private TimeSpan _beginTime;
        private TimeSpan _endTime;

        private TimeSpan _beginTimeForDayReport;
        private TimeSpan _endTimeForDayReport;

        public AsyncRelayCommand NavigateToHomeCommand { get; }
        public AsyncRelayCommand GenerateReport {  get; }
        public AsyncRelayCommand GenerateDayReport { get; }
        public RelayCommand SetFirstShift { get; }
        public RelayCommand SetSecondShift { get; }
        public RelayCommand SetStandardPeriod { get; }

        public AsyncRelayCommand LogoutCommand { get; }

        public ReportDialogVM(MainWindowVM shell, IReportService reportService, ISQLRepository repository, ILogger<ReportDialogVM> logger)
        {
            _shell = shell;
            _reportService = reportService;
            _logger = logger;
            _repository = repository;

            NavigateToHomeCommand = new AsyncRelayCommand(NavigateHomeAsync);
            GenerateReport = new AsyncRelayCommand(GenerateNewReport, CanGenerate);
            GenerateDayReport = new AsyncRelayCommand(GenerateNewDayReport, CanGenerateDayReport);
            SetFirstShift = new RelayCommand(SetShift1);
            SetSecondShift = new RelayCommand(SetShift2);
            SetStandardPeriod = new RelayCommand(SetStandartInterval);

            LogoutCommand = new AsyncRelayCommand(DoLogoutAsync);

            LoadOperatorsAsync();
        }

        private async Task NavigateHomeAsync()
        {
            var HomePage = App.Services.GetRequiredService<HomeDialogVM>();
            await HomePage.InitializeAsync();
            _shell.NavigateTo(HomePage);
        }

        private void SetShift1()
        {
            DateStart = DateTime.Today;
            DateStop = DateTime.Today;

            StartTime = "08:00";
            StopTime = "20:00";
        }

        private void SetShift2()
        {
            DateStart = DateTime.Today;
            DateStop = DateTime.Today.AddDays(1);

            StartTime = "20:00";
            StopTime = "08:00";
        }

        private void SetStandartInterval()
        {
            DayDateStart = DateTime.Today.AddDays(-1);
            DayDateStop = DateTime.Today;

            DayStartTime = "8:00";
            DayStopTime = "8:00";
        }

        private async Task GenerateNewReport()
        {
            _beginTime = Convert.ToDateTime(StartTime).TimeOfDay;
            _endTime = Convert.ToDateTime(StopTime).TimeOfDay;
            try
            {
                var path = await _reportService.NewReport(DateStart + _beginTime, DateStop + _endTime, ShiftOperator);

                if (path is null)
                {
                    MessageBox.Show("Нет карточек за выбранный период");
                    return;
                }

                await _reportService.SendReportAsync(path, DateStart+_beginTime, DateStop+_endTime);
                MessageBox.Show("Отчёт сформирован и отправлен.");
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Ошибка при формировании отчёта за период {Start} - {Stop}", DateStart + _beginTime, DateStop + _endTime);
                MessageBox.Show("Ошибка при формировании отчёта. Подробности в лог-файле.");
            }
        }

        private async Task GenerateNewDayReport()
        {
            _beginTimeForDayReport = Convert.ToDateTime(DayStartTime).TimeOfDay;
            _endTimeForDayReport = Convert.ToDateTime(DayStopTime).TimeOfDay;
            try
            {
                var path = await _reportService.NewDailyReport(DayDateStart + _beginTimeForDayReport, DayDateStop + _endTimeForDayReport, FirstOperator, SecondOperator);
                if (path is null)
                {
                    MessageBox.Show("Нет карточек за выбранный период");
                    return;
                }
                await _reportService.SendReportAsync(path, DayDateStart + _beginTimeForDayReport, DayDateStop + _endTimeForDayReport);
                MessageBox.Show("Отчёт сформирован и отправлен.");
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Ошибка при формировании отчёта за период {Start} - {Stop}", DateStart + _beginTime, DateStop + _endTime);
                MessageBox.Show("Ошибка при формировании отчёта. Подробности в лог-файле.");
            }
        }

        private bool CanGenerate()
        {
            return (ShiftOperator != null);
        }
        private bool CanGenerateDayReport()
        {
            return (FirstOperator != null) && (SecondOperator != null) ;
        }

        private async Task DoLogoutAsync()
        {
            // 1) Обнулить текущего пользователя (через ресурс, как ты хочешь)
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                ((CurrentUserStore)Application.Current.Resources["CurrentUser"]).CurrentUser = null;
            });

            // 3) Переход на окно авторизации
            var authVm = App.Services.GetRequiredService<AutorizationDialogVM>();
            _shell.NavigateToAsync(authVm);
        }

        private async void LoadOperatorsAsync()
        {
            try
            {
                var list = await _repository.GetOperators();

                Operators.Clear();
                foreach (var fio in list)
                    Operators.Add(fio);
            }
            catch (Exception ex)
            {
                
            }
        }
    }
}
