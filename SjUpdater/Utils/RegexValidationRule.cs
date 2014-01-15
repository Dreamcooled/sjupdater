using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Controls;

namespace SjUpdater.Utils
{
    public class RegexValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            String s = (String) value;
            if (!string.IsNullOrWhiteSpace(s))
            {

                try
                {
                    Regex.Match("", s);
                }
                catch (ArgumentException ex)
                {
                    return new ValidationResult(false, ex.Message);
                }

            }
            return new ValidationResult(true, "Ok");

        }
    }
}
