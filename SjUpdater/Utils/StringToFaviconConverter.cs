using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace SjUpdater.Utils
{
    public class StringToFaviconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            if (targetType != typeof(ImageSource) || value.GetType() != typeof(string))
            {
                throw new ArgumentException();
            }
            return FavIcon.Get(value as String);
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
