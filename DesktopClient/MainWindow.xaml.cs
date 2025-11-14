using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using DesktopClient.VM;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using MaterialDesignThemes.Wpf;
using DesktopClient.Services;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;

namespace DesktopClient
{
    /// <summary>
    /// Логика взаимодействия для Window1.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void MainWindow_Closing(object? sender, CancelEventArgs e)
        {
            var result = MessageBox.Show(
                "Вы действительно хотите закрыть приложение?",
                "Подтверждение выхода",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.No);

            if (result != MessageBoxResult.Yes)
            {
                e.Cancel = true;   // отменили закрытие
                return;
            }

            // (опционально) остановить фоновые вещи перед выходом
            try { App.Services.GetRequiredService<IPollingService>().Stop(); } catch { /* ignore */ }
        }
    }
}
