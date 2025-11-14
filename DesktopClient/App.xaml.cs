using DesktopClient.Config;
using DesktopClient.Helpers;
using DesktopClient.Logging;
using DesktopClient.Services;
using DesktopClient.VM;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Configuration;
using System.Data;
using System.Windows;
using Microsoft.Extensions.Logging;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;

namespace DesktopClient
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static ServiceProvider Services { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var serviceCollection = new ServiceCollection();

            string CS = "Server=localhost;Database=elevatordb;Uid=root;Pwd=Sd$#5186;SslMode=None;";
            // Сервисы

            serviceCollection.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.AddProvider(new SimpleFileLoggerProvider("logs/app.log")); // в файл
                builder.SetMinimumLevel(LogLevel.Information);
            });

            serviceCollection.AddSingleton<IMailSettingsStore, MailSettingsStore>();

            serviceCollection.AddSingleton<IPollingService>(sp =>
            {
                var st = sp.GetRequiredService<ISettingsStore>();
                st.LoadSettings();

                var ps = new PollingService(sp.GetRequiredService<ISQLRepository>(),
                    period: TimeSpan.FromSeconds(10), sp.GetRequiredService<ILogger<PollingService>>());
                ps.LagSeconds = Math.Max(0, st.Settings.LagSeconds);
                return ps;
            });

            serviceCollection.AddSingleton<ISettingsStore, SettingsStore>();


            serviceCollection.AddSingleton<ISQLRepository>(sp =>
            {
                var st = sp.GetRequiredService<ISettingsStore>();
                st.LoadSettings(); // при желании можно убрать сюда инициализацию

                var logger = sp.GetRequiredService<ILogger<SQLRepository>>();

                return new SQLRepository(st.Settings.DbConnectionString, logger, CS);
            });

            serviceCollection.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
            serviceCollection.AddSingleton<CurrentUserStore>();

            serviceCollection.AddSingleton<IAuthService>(sp =>
                 new AuthService(CS, sp.GetRequiredService<IPasswordHasher>()));

            serviceCollection.AddSingleton<IRegistrationService>(sp =>
                 new RegistrationService(CS, sp.GetRequiredService<IPasswordHasher>()));
            
            serviceCollection.AddSingleton<IReportService>(sp =>
                new ReportService(sp.GetRequiredService<ISQLRepository>(), 
                sp.GetRequiredService<CurrentUserStore>(), 
                sp.GetRequiredService<IMailSettingsStore>(),
                sp.GetRequiredService<ILogger<ReportService>>()));

            // VM
            serviceCollection.AddSingleton<MainWindowVM>();
            serviceCollection.AddTransient<AutorizationDialogVM>();
            serviceCollection.AddTransient<RegisterDialogVM>();
            serviceCollection.AddTransient<HomeDialogVM>();
            serviceCollection.AddTransient<ReportDialogVM>();

            Services = serviceCollection.BuildServiceProvider();

            Resources["CurrentUser"] = Services.GetRequiredService<CurrentUserStore>();

            _ = Services.GetRequiredService<IPollingService>()
            .StartAsync();

            // Запускаем главное окно
            var mainWindow = new MainWindow
            {
                DataContext = Services.GetRequiredService<MainWindowVM>()
            };
            mainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Services.GetRequiredService<IPollingService>().Stop();
            base.OnExit(e);
        }
    }

}

