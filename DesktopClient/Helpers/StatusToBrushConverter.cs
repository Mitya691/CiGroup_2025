using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace DesktopClient.Helpers
{
    public class StatusToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var status = value as string;
            return status switch
            {
                "Сохранено" => Brushes.Green,
                "Ошибка" => Brushes.Red,
                "Ожидание" => Brushes.Orange,
                _ => Brushes.Black
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}

