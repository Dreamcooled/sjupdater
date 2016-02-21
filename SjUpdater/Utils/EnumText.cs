using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Windows.Data;

namespace SjUpdater.Utils
{
    [AttributeUsage(AttributeTargets.Field)]
    public class EnumText : Attribute
    {
        public static string[] GetStringValues(Type EnumType)
        {
            if (!EnumType.IsEnum)
                throw new Exception("This type is not an enumeration!");

            List<string> results = new List<string>();

            foreach (FieldInfo f in EnumType.GetFields())
            {
                object[] attributes = f.GetCustomAttributes(typeof(EnumText), true);

                if (attributes.Length > 0)
                {
                    results.Add(((EnumText)attributes[0]).String);
                }
            }

            return results.ToArray();
        }

        public static string GetStringValue(Enum Value)
        {
            Type enumType = Value.GetType();

            foreach (FieldInfo f in enumType.GetFields())
            {
                if (!f.IsStatic) 
                    continue;

                object[] attributes = f.GetCustomAttributes(typeof(EnumText), true);

                if (Object.Equals(Value, f.GetValue(f)))
                {
                    if (attributes.Length > 0)
                    {
                        return ((EnumText)attributes[0]).String;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            throw new Exception("No StringValue found for this enumeration-value!");
        }

        public static Enum GetEnumValue(string StringValue, Type EnumType)
        {
            if (!EnumType.IsEnum)
                throw new Exception("This type is not an enumeration!");

            foreach (FieldInfo f in EnumType.GetFields())
            {
                object[] attributes = f.GetCustomAttributes(typeof(EnumText), true);

                if (attributes.Length > 0)
                {
                    if (((EnumText)attributes[0]).String == StringValue)
                    {
                        return (Enum)f.GetValue(f);
                    }
                }
            }

            throw new Exception("StringValue not found!");
        }

        public EnumText(string StringValue)
        {
            String = StringValue;
        }

        string String { get; set; }


        public override string ToString()
        {
            return String;
        }
    }

    public class EnumTextValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            if (targetType != typeof(String) || !value.GetType().IsEnum)
            {
                 throw new ArgumentException();
            }
            return EnumText.GetStringValue(value as Enum);
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            if (!targetType.IsEnum || value.GetType() != typeof(String))
            {
                throw new ArgumentException();
            }
            return EnumText.GetEnumValue(value as String, targetType);
        }
    }
}
