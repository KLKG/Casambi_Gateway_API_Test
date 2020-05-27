using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Navigation;

namespace UDP_WPF
{
    public class IPAddressConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is IPAddress ip)
                return ip.ToString();
            else
                return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string txt && IPAddress.TryParse(txt, out var ip))
                return ip;
            else 
                return DependencyProperty.UnsetValue;
        }
    }
}
