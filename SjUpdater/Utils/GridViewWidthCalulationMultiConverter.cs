using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;

//Code taken from: http://stackoverflow.com/questions/5573152/how-to-resize-a-certain-control-based-on-window-size-in-wpf#5573895

namespace SjUpdater.Utils
{
    public class GridViewWidthCalulationMultiConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType,
                          object parameter, CultureInfo culture)
        {
            // do some sort of calculation
            double totalWindowWidth;
            double otherColumnsTotalWidth = 0;
            double.TryParse(values[0].ToString(), out totalWindowWidth);
            var arrayOfColumns = values[1] as IList<GridViewColumn>;

            for (int i = 0; i < arrayOfColumns.Count - 1; i++)
            {
                otherColumnsTotalWidth += arrayOfColumns[i].ActualWidth;
            }

            return (totalWindowWidth - otherColumnsTotalWidth) < 0 ?
                         0 : (totalWindowWidth - otherColumnsTotalWidth);
        }

        public object[] ConvertBack(object value, Type[] targetTypes,
                                    object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
