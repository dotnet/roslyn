using System;
using System.Globalization;
using System.Windows.Data;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Utilities
{
    internal class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type _, object _1, CultureInfo _2)
        {
            return !(bool)value;
        }

        public object ConvertBack(object value, Type _, object _1, CultureInfo _2)
        {
            return !(bool)value;
        }
    }
}
