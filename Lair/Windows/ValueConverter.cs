using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Lair.Properties;
using Library;
using Library.Net.Lair;

namespace Lair.Windows
{
    [ValueConversion(typeof(bool), typeof(SolidColorBrush))]
    class MessageExToBorderBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var item = value as MessageState?;
            if (item == null) return null;

            if (item.Value.HasFlag(MessageState.IsLock))
            {
                return new SolidColorBrush(System.Windows.Media.Color.FromArgb(0xFF, 0x90, 0x90, 0x90));
            }
            else
            {
                if (item.Value.HasFlag(MessageState.IsNew))
                {
                    return new SolidColorBrush(System.Windows.Media.Color.FromArgb(0x90, 0xDF, 0xa0, 0xDF));
                }
                else
                {
                    return new SolidColorBrush(System.Windows.Media.Color.FromArgb(0xFF, 0x00, 0x00, 0x00));
                }
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    [ValueConversion(typeof(ListView), typeof(double))]
    public class ListViewWidthConverter : IValueConverter
    {
        public object Convert(object o, Type type, object parameter, CultureInfo culture)
        {
            ListView l = o as ListView;
            GridView g = l.View as GridView;
            double total = 0;
            for (int i = 0; i < g.Columns.Count - 1; i++)
            {
                total += g.Columns[i].Width;
            }
            return (l.ActualWidth - total);
        }

        public object ConvertBack(object o, Type type, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    [ValueConversion(typeof(string), typeof(string))]
    class StringRegularizationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var item = value as string;
            if (item == null) return null;

            return item.Replace('\r', ' ').Replace('\n', ' ');
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    [ValueConversion(typeof(string), typeof(string))]
    class StringRegularization2Converter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var item = value as string;
            if (item == null) return null;

            return item;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    [ValueConversion(typeof(double), typeof(GridLength))]
    class DoubleToGridLengthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var item = value as double?;
            if (item == null) return null;

            if (item.Value < 0)
            {
                return GridLength.Auto;
            }
            else
            {
                return new GridLength(item.Value);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var item = value as GridLength?;
            if (item == null) return null;

            if (GridLength.Auto == item.Value)
            {
                return -1;
            }
            else
            {
                return item.Value.Value;
            }
        }
    }

    public delegate double GetDoubleEventHandler(object sender);

    [ValueConversion(typeof(double), typeof(double))]
    class TopRelativeDoubleConverter : IValueConverter
    {
        public static GetDoubleEventHandler GetDoubleEvent;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var item = value as double?;
            if (item == null) return null;

            return item + this.OnGetDoubleEvent();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var item = value as double?;
            if (item == null) return null;

            return item - this.OnGetDoubleEvent();
        }

        protected virtual double OnGetDoubleEvent()
        {
            if (GetDoubleEvent != null)
            {
                return GetDoubleEvent(this);
            }

            return 0;
        }
    }

    [ValueConversion(typeof(double), typeof(double))]
    class LeftRelativeDoubleConverter : IValueConverter
    {
        public static GetDoubleEventHandler GetDoubleEvent;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var item = value as double?;
            if (item == null) return null;

            return item + this.OnGetDoubleEvent();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var item = value as double?;
            if (item == null) return null;

            return item - this.OnGetDoubleEvent();
        }

        protected virtual double OnGetDoubleEvent()
        {
            if (GetDoubleEvent != null)
            {
                return GetDoubleEvent(this);
            }

            return 0;
        }
    }

    [ValueConversion(typeof(object), typeof(string))]
    class ObjectToInfoStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Library.Net.Amoeba.Seed)
            {
                return MessageConverter.ToInfoMessage((Library.Net.Amoeba.Seed)value);
            }
            else if (value is Channel)
            {
                return MessageConverter.ToInfoMessage((Channel)value);
            }
            else if (value is Message)
            {
                return MessageConverter.ToInfoMessage((Message)value);
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    [ValueConversion(typeof(Node), typeof(string))]
    class NodeToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var item = value as Node;
            if (item == null) return null;

            return LairConverter.ToNodeString(item);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    [ValueConversion(typeof(byte[]), typeof(string))]
    class BytesToHexStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var item = value as byte[];
            if (item == null) return null;

            return NetworkConverter.ToHexString(item);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    [ValueConversion(typeof(IEnumerable<string>), typeof(string))]
    class StringsToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var items = value as IEnumerable<string>;
            if (items == null) return null;

            return String.Join(", ", items);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    [ValueConversion(typeof(long), typeof(string))]
    class LongToSizeStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var item = value as long?;
            if (item == null) return null;

            return NetworkConverter.ToSizeString(item.Value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    [ValueConversion(typeof(bool), typeof(string))]
    class BoolToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var item = value as bool?;
            if (item == null) return null;

            return item.Value ? "＋" : "－";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    [ValueConversion(typeof(DateTime), typeof(string))]
    class DateTimeToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var item = value as DateTime?;
            if (item == null) return null;

            return item.Value.ToLocalTime().ToString(LanguagesManager.Instance.DateTime_StringFormat, System.Globalization.DateTimeFormatInfo.InvariantInfo);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    [ValueConversion(typeof(string), typeof(System.Windows.Media.FontFamily))]
    class StringToFontFamilyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var item = value as string;
            if (item == null) return null;

            return new System.Windows.Media.FontFamily(item);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    [ValueConversion(typeof(string), typeof(double))]
    class StringToDoubleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var item = value as string;
            if (item == null) return null;

            return double.Parse(item);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    [ValueConversion(typeof(ConnectionType), typeof(string))]
    class ConnectionTypeToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var item = value as ConnectionType?;
            if (item == null) return LanguagesManager.Instance.ConnectionType_None;

            if (item.Value == ConnectionType.None)
            {
                return LanguagesManager.Instance.ConnectionType_None;
            }
            else if (item.Value == ConnectionType.Tcp)
            {
                return LanguagesManager.Instance.ConnectionType_Tcp;
            }
            else if (item.Value == ConnectionType.Socks4Proxy)
            {
                return LanguagesManager.Instance.ConnectionType_Socks4Proxy;
            }
            else if (item.Value == ConnectionType.Socks4aProxy)
            {
                return LanguagesManager.Instance.ConnectionType_Socks4aProxy;
            }
            else if (item.Value == ConnectionType.Socks5Proxy)
            {
                return LanguagesManager.Instance.ConnectionType_Socks5Proxy;
            }
            else if (item.Value == ConnectionType.HttpProxy)
            {
                return LanguagesManager.Instance.ConnectionType_HttpProxy;
            }

            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var item = value as string;
            if (item == null) return ConnectionType.None;

            if (item == LanguagesManager.Instance.ConnectionType_None)
            {
                return ConnectionType.None;
            }
            else if (item == LanguagesManager.Instance.ConnectionType_Tcp)
            {
                return ConnectionType.Tcp;
            }
            else if (item == LanguagesManager.Instance.ConnectionType_Socks4Proxy)
            {
                return ConnectionType.Socks4Proxy;
            }
            else if (item == LanguagesManager.Instance.ConnectionType_Socks4aProxy)
            {
                return ConnectionType.Socks4aProxy;
            }
            else if (item == LanguagesManager.Instance.ConnectionType_Socks5Proxy)
            {
                return ConnectionType.Socks5Proxy;
            }
            else if (item == LanguagesManager.Instance.ConnectionType_HttpProxy)
            {
                return ConnectionType.HttpProxy;
            }

            return 0;
        }
    }

    [ValueConversion(typeof(int), typeof(bool))]
    class ExpanderToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((string)value == (string)parameter);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (System.Convert.ToBoolean(value)) return (string)parameter;
            return null;
        }
    }
}
