using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace InImArchiverService
{
    public partial class InImArchiverService : ServiceBase
    {
        [DllImport("advapi32.dll")]
        private static extern bool SetServiceStatus(IntPtr hServiceStatus, ref ServiceStatus lpServiceStatus);

        /// <summary>
        /// Потоковая блокировка для функции SetServiceStatus.
        /// </summary>
        private object _lockSetServiceStatusInvoker;

        /// <summary>
        /// Потокобезопасный запуск функции SetServiceStatus.
        /// </summary>
        /// <param name="hServiceStatus"></param>
        /// <param name="lpServiceStatus"></param>
        /// <returns></returns>
        private bool SetServiceStatusInvoker(IntPtr hServiceStatus, ref ServiceStatus lpServiceStatus)
        {
            lock (_lockSetServiceStatusInvoker)
            {
                try
                {
                    return SetServiceStatus(hServiceStatus, ref lpServiceStatus);
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Потоковая блокировка для функции записи в журнал событий.
        /// </summary>
        private object _lockEventLog;

        /// <summary>
        /// Потокобезопасная запись в журнал событий.
        /// </summary>
        /// <param name="message"></param>
        private void EventLogWriteEntry(string message)
        {
            lock (_lockEventLog)
            {
                MainEventLog.WriteEntry(message);
            }
        }
        
        private Server _server;

        public InImArchiverService()
        {
            InitializeComponent();
            Globals.Init();
            //инициализация объектов потоковых блокировок
            _lockSetServiceStatusInvoker = new object();
            _lockEventLog = new object();
            //инициализация журнала событий
            MainEventLog = new EventLog();
            if (!EventLog.SourceExists("InImArchiverSource")) EventLog.CreateEventSource("InImArchiverSource", "InImArchiverEventLog");
            MainEventLog.Source = "InImArchiverSource";
            MainEventLog.Log = "InImArchiverEventLog";
        }

        protected override void OnStart(string[] args)
        {
            //обновление состояния службы на "Запускается"
            ServiceStatus serviceStatus = new ServiceStatus
            {
                dwCurrentState = ServiceState.SERVICE_START_PENDING, dwWaitHint = 100000
            };
            SetServiceStatusInvoker(ServiceHandle, ref serviceStatus);

            EventLogWriteEntry("Служба InImArchiverService запускается.");
            //запуск службы
            _server = new Server(Globals.LogFile);
            Globals.Log.Register(_server);
            _server.EventStarted += ServerOnEventStarted;
            _server.EventNotStarted += ServerOnEventNotStarted;
            //запустить поток запуска службы
            _server.Start();
        }

        /// <summary>
        /// Обработка события успешного запуска сервера.
        /// </summary>
        private void ServerOnEventStarted(string logfile, string title, string mes)
        {
            //служба успешно запущена
            //обновление состояния службы на "Запущено"
            ServiceStatus serviceStatus = new ServiceStatus
            {
                dwCurrentState = ServiceState.SERVICE_RUNNING, dwWaitHint = 100000
            };
            SetServiceStatusInvoker(ServiceHandle, ref serviceStatus);
            EventLogWriteEntry("Служба InImArchiverService запущена.");
        }

        /// <summary>
        /// Обработка события неуспешного запуска сервера.
        /// </summary>
        private void ServerOnEventNotStarted(string logfile, string title, string mes)
        {
            //ошибка запуска службы
            //обновление состояния службы на "Остановлено"
            ServiceStatus serviceStatus = new ServiceStatus
            {
                dwCurrentState = ServiceState.SERVICE_STOPPED, dwWaitHint = 100000
            };
            SetServiceStatusInvoker(ServiceHandle, ref serviceStatus);
            EventLogWriteEntry("Ошибка запуска службы InImArchiverService.");
        }

        protected override void OnStop()
        {
            //обновление состояния службы на "Останавливается"
            ServiceStatus serviceStatus = new ServiceStatus
            {
                dwCurrentState = ServiceState.SERVICE_STOP_PENDING,
                dwWaitHint = 100000
            };
            SetServiceStatusInvoker(ServiceHandle, ref serviceStatus);

            EventLogWriteEntry("Служба InImArchiverService останавливается.");
            //остановка службы
            _server.Stop();
            //ждать окончания остановки службы
            while (_server.IsWork) Thread.Sleep(1000);
            EventLogWriteEntry("Служба InImArchiverService остановлена.");

            //обновление состояния службы на "Остановлено"
            serviceStatus.dwCurrentState = ServiceState.SERVICE_STOPPED;
            SetServiceStatusInvoker(ServiceHandle, ref serviceStatus);
        }
    }
}
