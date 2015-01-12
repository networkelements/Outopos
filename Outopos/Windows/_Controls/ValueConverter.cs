using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Outopos.Properties;
using Library;
using Library.Collections;
using Library.Net.Outopos;
using A = Library.Net.Amoeba;
using O = Library.Net.Outopos;

namespace Outopos.Windows
{
    [ValueConversion(typeof(A.Seed), typeof(string))]
    class SeedToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var item = value as A.Seed;
            if (item == null) return null;

            return A.AmoebaConverter.ToSeedString(item);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    [ValueConversion(typeof(bool), typeof(System.Windows.Visibility))]
    class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var item = value as bool?;
            if (item == null) return null;

            return item.Value ? System.Windows.Visibility.Visible : System.Windows.Visibility.Hidden;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
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

            StringBuilder sb = new StringBuilder(item, item.Length);
            sb.Replace('\r', ' ');
            sb.Replace('\n', ' ');
            sb.Replace('\uFFFD', ' ');

            return sb.ToString();
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

            if (double.IsNaN(item.Value))
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

            if (item.Value == GridLength.Auto)
            {
                return double.NaN;
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
            if (value is A.Seed)
            {
                return MessageConverter.ToInfoMessage((A.Seed)value);
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

            return OutoposConverter.ToNodeString(item);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    [ValueConversion(typeof(byte[]), typeof(string))]
    class BytesToBase64StringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var item = value as byte[];
            if (item == null) return null;

            return NetworkConverter.ToBase64UrlString(item);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    [ValueConversion(typeof(Wiki), typeof(string))]
    class WikiToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var item = value as Wiki;
            if (item == null) return null;

            return MessageConverter.ToWikiString(item);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    [ValueConversion(typeof(Chat), typeof(string))]
    class ChatToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var item = value as Chat;
            if (item == null) return null;

            return MessageConverter.ToChatString(item);
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

    class ChatMessageWrapperToBorderBrushConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var wrapper = values[0] as ChatMessageWrapper;
            if (wrapper == null) return null;

            var isSelect = values[1] as bool?;
            if (isSelect == null) return null;

            System.Windows.Media.Color color;

            if (isSelect.HasValue && isSelect.Value)
            {
                color = App.Colors.Message_Select;
            }
            else if (wrapper.State.HasFlag(ChatMessageState.IsUnread))
            {
                color = App.Colors.Message_New;
            }
            else
            {
                color = App.Colors.Message;
            }

            return new SolidColorBrush(color);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
