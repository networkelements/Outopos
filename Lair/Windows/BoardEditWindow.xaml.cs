using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
using Library.Net;
using Library.Net.Lair;
using Library.Security;

namespace Lair.Windows
{
    /// <summary>
    /// BoardEditWindow.xaml の相互作用ロジック
    /// </summary>
    partial class BoardEditWindow : Window
    {
        private Board _board;
        private List<SearchContains<string>> _searchWordCollection;
        private List<SearchContains<SearchRegex>> _searchRegexCollection;
        private List<SearchContains<SearchRegex>> _searchSignatureCollection;
        private List<SearchContains<Message>> _searchMessageCollection;
        private List<SearchContains<SearchRange<DateTime>>> _searchCreationTimeRangeCollection;

        public BoardEditWindow(ref Board board)
        {
            _board = board;

            var digitalSignatureCollection = new List<object>();
            digitalSignatureCollection.Add(new ComboBoxItem() { Content = "" });
            digitalSignatureCollection.AddRange(Settings.Instance.Global_DigitalSignatureCollection.Select(n => new DigitalSignatureComboBoxItem(n)).ToArray());

            InitializeComponent();

            {
                var icon = new BitmapImage();

                icon.BeginInit();
                icon.StreamSource = new FileStream(Path.Combine(App.DirectoryPaths["Icons"], "Lair.ico"), FileMode.Open, FileAccess.Read, FileShare.Read);
                icon.EndInit();
                if (icon.CanFreeze) icon.Freeze();

                this.Icon = icon;
            }

            lock (_board.ThisLock)
            {
                _nameTextBox.Text = MessageConverter.ToChannelString(_board.Channel);

                _searchWordCollection = _board.SearchWordCollection.Select(n => n.DeepClone()).ToList();
                _searchRegexCollection = _board.SearchRegexCollection.Select(n => n.DeepClone()).ToList();
                _searchSignatureCollection = _board.SearchSignatureCollection.Select(n => n.DeepClone()).ToList();
                _searchCreationTimeRangeCollection = _board.SearchCreationTimeRangeCollection.Select(n => n.DeepClone()).ToList();
                _searchMessageCollection = _board.SearchMessageCollection.Select(n => n.DeepClone()).ToList();
            }

            _wordContainsCheckBox.IsChecked = true;
            _regexContainsCheckBox.IsChecked = true;
            _signatureContainsCheckBox.IsChecked = true;
            _creationTimeRangeContainsCheckBox.IsChecked = true;
            _messageContainsCheckBox.IsChecked = true;

            _wordListView.ItemsSource = _searchWordCollection;
            _regexListView.ItemsSource = _searchRegexCollection;
            _signatureListView.ItemsSource = _searchSignatureCollection;
            _creationTimeRangeListView.ItemsSource = _searchCreationTimeRangeCollection;
            _messageListView.ItemsSource = _searchMessageCollection;

            var maxDateTime = DateTime.Now.AddDays(1);

            _creationTimeRangeMinTextBox.Text = new DateTime(DateTime.Now.Year, 1, 1, 0, 0, 0, DateTimeKind.Local).ToLocalTime().ToString(LanguagesManager.Instance.DateTime_StringFormat, System.Globalization.DateTimeFormatInfo.InvariantInfo);
            _creationTimeRangeMaxTextBox.Text = new DateTime(maxDateTime.Year, maxDateTime.Month, maxDateTime.Day, 0, 0, 0, DateTimeKind.Local).ToLocalTime().ToString(LanguagesManager.Instance.DateTime_StringFormat, System.Globalization.DateTimeFormatInfo.InvariantInfo);
        }

        #region _wordListView

        private void _wordTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                _wordAddButton_Click(null, null);

                e.Handled = true;
            }
        }

        private void _wordListViewUpdate()
        {
            _wordListView_SelectionChanged(this, null);
        }

        private void _wordListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var selectIndex = _wordListView.SelectedIndex;

                if (selectIndex == -1)
                {
                    _wordUpButton.IsEnabled = false;
                    _wordDownButton.IsEnabled = false;
                }
                else
                {
                    if (selectIndex == 0)
                    {
                        _wordUpButton.IsEnabled = false;
                    }
                    else
                    {
                        _wordUpButton.IsEnabled = true;
                    }

                    if (selectIndex == _searchWordCollection.Count - 1)
                    {
                        _wordDownButton.IsEnabled = false;
                    }
                    else
                    {
                        _wordDownButton.IsEnabled = true;
                    }
                }

                _wordListView_PreviewMouseLeftButtonDown(this, null);
            }
            catch (Exception)
            {

            }
        }

        private void _wordListView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var selectIndex = _wordListView.SelectedIndex;
            if (selectIndex == -1)
            {
                _wordContainsCheckBox.IsChecked = true;
                _wordTextBox.Text = "";
                return;
            }

            var item = _wordListView.SelectedItem as SearchContains<string>;
            if (item == null) return;

            _wordContainsCheckBox.IsChecked = item.Contains;
            _wordTextBox.Text = item.Value;
        }

        private void _wordListView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var selectItems = _wordListView.SelectedItems;

            _wordListViewDeleteMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);
            _wordListViewCopyMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);
            _wordListViewCutMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);

            {
                var line = Clipboard.GetText().Split('\r', '\n');

                if (line.Length != 0)
                {
                    Regex regex = new Regex("^([\\+-]) \"(.*)\"$");

                    _wordListViewPasteMenuItem.IsEnabled = regex.IsMatch(line[0]);
                }
            }
        }

        private void _wordListViewDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _wordDeleteButton_Click(null, null);
        }

        private void _wordListViewCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var sb = new StringBuilder();

            foreach (var item in _wordListView.SelectedItems.OfType<SearchContains<string>>())
            {
                sb.AppendLine(string.Format("{0} \"{1}\"", (item.Contains == true) ? "+" : "-", item.Value));
            }

            Clipboard.SetText(sb.ToString());
        }

        private void _wordListViewCutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _wordListViewCopyMenuItem_Click(null, null);
            _wordDeleteButton_Click(null, null);
        }

        private void _wordListViewPasteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Regex regex = new Regex("^([\\+-]) \"(.*)\"$");

            foreach (var line in Clipboard.GetText().Split('\r', '\n'))
            {
                try
                {
                    var match = regex.Match(line);
                    if (!match.Success) continue;

                    var item = new SearchContains<string>()
                    {
                        Contains = (match.Groups[1].Value == "+") ? true : false,
                        Value = match.Groups[2].Value,
                    };

                    if (_searchWordCollection.Contains(item)) continue;
                    _searchWordCollection.Add(item);
                }
                catch (Exception)
                {
                    continue;
                }
            }

            _wordTextBox.Text = "";
            _wordListView.SelectedIndex = _searchWordCollection.Count - 1;

            _wordListView.Items.Refresh();
            _wordListViewUpdate();
        }

        private void _wordUpButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _wordListView.SelectedItem as SearchContains<string>;
            if (item == null) return;

            var selectIndex = _wordListView.SelectedIndex;
            if (selectIndex == -1) return;

            _searchWordCollection.Remove(item);
            _searchWordCollection.Insert(selectIndex - 1, item);
            _wordListView.Items.Refresh();

            _wordListViewUpdate();
        }

        private void _wordDownButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _wordListView.SelectedItem as SearchContains<string>;
            if (item == null) return;

            var selectIndex = _wordListView.SelectedIndex;
            if (selectIndex == -1) return;

            _searchWordCollection.Remove(item);
            _searchWordCollection.Insert(selectIndex + 1, item);
            _wordListView.Items.Refresh();

            _wordListViewUpdate();
        }

        private void _wordAddButton_Click(object sender, RoutedEventArgs e)
        {
            if (_wordTextBox.Text == "") return;

            var item = new SearchContains<string>()
            {
                Contains = _wordContainsCheckBox.IsChecked.Value,
                Value = _wordTextBox.Text,
            };

            if (_searchWordCollection.Contains(item)) return;
            _searchWordCollection.Add(item);

            _wordTextBox.Text = "";
            _wordListView.SelectedIndex = _searchWordCollection.Count - 1;

            _wordListView.Items.Refresh();
            _wordListViewUpdate();
        }

        private void _wordEditButton_Click(object sender, RoutedEventArgs e)
        {
            if (_wordTextBox.Text == "") return;

            var uitem = new SearchContains<string>()
            {
                Contains = _wordContainsCheckBox.IsChecked.Value,
                Value = _wordTextBox.Text,
            };

            if (_searchWordCollection.Contains(uitem)) return;

            var item = _wordListView.SelectedItem as SearchContains<string>;
            if (item == null) return;

            item.Contains = _wordContainsCheckBox.IsChecked.Value;
            item.Value = _wordTextBox.Text;

            _wordListView.Items.Refresh();
            _wordListViewUpdate();
        }

        private void _wordDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            int selectIndex = _wordListView.SelectedIndex;
            if (selectIndex == -1) return;

            _wordTextBox.Text = "";

            foreach (var item in _wordListView.SelectedItems.OfType<SearchContains<string>>().ToArray())
            {
                _searchWordCollection.Remove(item);
            }

            _wordListView.Items.Refresh();
            _wordListView.SelectedIndex = selectIndex;
            _wordListViewUpdate();
        }

        #endregion

        #region _regexListView

        private void _regexTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                _regexAddButton_Click(null, null);

                e.Handled = true;
            }
        }

        private void _regexListViewUpdate()
        {
            _regexListView_SelectionChanged(this, null);
        }

        private void _regexListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var selectIndex = _regexListView.SelectedIndex;

                if (selectIndex == -1)
                {
                    _regexUpButton.IsEnabled = false;
                    _regexDownButton.IsEnabled = false;
                }
                else
                {
                    if (selectIndex == 0)
                    {
                        _regexUpButton.IsEnabled = false;
                    }
                    else
                    {
                        _regexUpButton.IsEnabled = true;
                    }

                    if (selectIndex == _searchRegexCollection.Count - 1)
                    {
                        _regexDownButton.IsEnabled = false;
                    }
                    else
                    {
                        _regexDownButton.IsEnabled = true;
                    }
                }

                _regexListView_PreviewMouseLeftButtonDown(this, null);
            }
            catch (Exception)
            {

            }
        }

        private void _regexListView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var selectIndex = _regexListView.SelectedIndex;
            if (selectIndex == -1)
            {
                _regexContainsCheckBox.IsChecked = true;
                _regexIsIgnoreCaseCheckBox.IsChecked = false;
                _regexTextBox.Text = "";
                return;
            }

            var item = _regexListView.SelectedItem as SearchContains<SearchRegex>;
            if (item == null) return;

            _regexContainsCheckBox.IsChecked = item.Contains;
            _regexIsIgnoreCaseCheckBox.IsChecked = item.Value.IsIgnoreCase;
            _regexTextBox.Text = item.Value.Value;
        }

        private void _regexListView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var selectItems = _regexListView.SelectedItems;

            _regexListViewDeleteMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);
            _regexListViewCopyMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);
            _regexListViewCutMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);

            {
                var line = Clipboard.GetText().Split('\r', '\n');

                if (line.Length != 0)
                {
                    Regex regex = new Regex("^([\\+-]) ([\\+-]) \"(.*)\"$");

                    _regexListViewPasteMenuItem.IsEnabled = regex.IsMatch(line[0]);
                }
            }
        }

        private void _regexListViewDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _regexDeleteButton_Click(null, null);
        }

        private void _regexListViewCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var sb = new StringBuilder();

            foreach (var item in _regexListView.SelectedItems.OfType<SearchContains<SearchRegex>>())
            {
                sb.AppendLine(string.Format("{0} {1} \"{2}\"", (item.Contains == true) ? "+" : "-", (item.Value.IsIgnoreCase == true) ? "+" : "-", item.Value.Value));
            }

            Clipboard.SetText(sb.ToString());
        }

        private void _regexListViewCutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _regexListViewCopyMenuItem_Click(null, null);
            _regexDeleteButton_Click(null, null);
        }

        private void _regexListViewPasteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Regex regex = new Regex("^([\\+-]) ([\\+-]) \"(.*)\"$");

            foreach (var line in Clipboard.GetText().Split('\r', '\n'))
            {
                try
                {
                    var match = regex.Match(line);
                    if (!match.Success) continue;

                    try
                    {
                        new Regex(match.Groups[3].Value);
                    }
                    catch (Exception)
                    {
                        return;
                    }

                    var item = new SearchContains<SearchRegex>()
                    {
                        Contains = (match.Groups[1].Value == "+") ? true : false,
                        Value = new SearchRegex()
                        {
                            IsIgnoreCase = (match.Groups[2].Value == "+") ? true : false,
                            Value = match.Groups[3].Value
                        },
                    };

                    if (_searchRegexCollection.Contains(item)) continue;
                    _searchRegexCollection.Add(item);
                }
                catch (Exception)
                {
                    continue;
                }
            }

            _regexTextBox.Text = "";
            _regexListView.SelectedIndex = _searchRegexCollection.Count - 1;

            _regexListView.Items.Refresh();
            _regexListViewUpdate();
        }

        private void _regexUpButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _regexListView.SelectedItem as SearchContains<SearchRegex>;
            if (item == null) return;

            var selectIndex = _regexListView.SelectedIndex;
            if (selectIndex == -1) return;

            _searchRegexCollection.Remove(item);
            _searchRegexCollection.Insert(selectIndex - 1, item);
            _regexListView.Items.Refresh();

            _regexListViewUpdate();
        }

        private void _regexDownButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _regexListView.SelectedItem as SearchContains<SearchRegex>;
            if (item == null) return;

            var selectIndex = _regexListView.SelectedIndex;
            if (selectIndex == -1) return;

            _searchRegexCollection.Remove(item);
            _searchRegexCollection.Insert(selectIndex + 1, item);
            _regexListView.Items.Refresh();

            _regexListViewUpdate();
        }

        private void _regexAddButton_Click(object sender, RoutedEventArgs e)
        {
            if (_regexTextBox.Text == "") return;

            try
            {
                var item = new SearchContains<SearchRegex>()
                {
                    Contains = _regexContainsCheckBox.IsChecked.Value,
                    Value = new SearchRegex()
                    {
                        IsIgnoreCase = _regexIsIgnoreCaseCheckBox.IsChecked.Value,
                        Value = _regexTextBox.Text
                    },
                };

                if (_searchRegexCollection.Contains(item)) return;
                _searchRegexCollection.Add(item);
            }
            catch (Exception)
            {
                return;
            }

            _regexTextBox.Text = "";
            _regexListView.SelectedIndex = _searchRegexCollection.Count - 1;

            _regexListView.Items.Refresh();
            _regexListViewUpdate();
        }

        private void _regexEditButton_Click(object sender, RoutedEventArgs e)
        {
            if (_regexTextBox.Text == "") return;

            try
            {
                var uitem = new SearchContains<SearchRegex>()
                {
                    Contains = _regexContainsCheckBox.IsChecked.Value,
                    Value = new SearchRegex()
                    {
                        IsIgnoreCase = _regexIsIgnoreCaseCheckBox.IsChecked.Value,
                        Value = _regexTextBox.Text
                    },
                };

                if (_searchRegexCollection.Contains(uitem)) return;

                var item = _regexListView.SelectedItem as SearchContains<SearchRegex>;
                if (item == null) return;

                item.Contains = _regexContainsCheckBox.IsChecked.Value;
                item.Value = new SearchRegex() { IsIgnoreCase = _regexIsIgnoreCaseCheckBox.IsChecked.Value, Value = _regexTextBox.Text };
            }
            catch (Exception)
            {
                return;
            }

            _regexListView.Items.Refresh();
            _regexListViewUpdate();
        }

        private void _regexDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            int selectIndex = _regexListView.SelectedIndex;
            if (selectIndex == -1) return;

            _regexTextBox.Text = "";

            foreach (var item in _regexListView.SelectedItems.OfType<SearchContains<SearchRegex>>().ToArray())
            {
                _searchRegexCollection.Remove(item);
            }

            _regexListView.Items.Refresh();
            _regexListView.SelectedIndex = selectIndex;
            _regexListViewUpdate();
        }

        #endregion

        #region _signatureListView

        private void _signatureTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                _signatureAddButton_Click(null, null);

                e.Handled = true;
            }
        }

        private void _signatureListViewUpdate()
        {
            _signatureListView_SelectionChanged(this, null);
        }

        private void _signatureListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var selectIndex = _signatureListView.SelectedIndex;

                if (selectIndex == -1)
                {
                    _signatureUpButton.IsEnabled = false;
                    _signatureDownButton.IsEnabled = false;
                }
                else
                {
                    if (selectIndex == 0)
                    {
                        _signatureUpButton.IsEnabled = false;
                    }
                    else
                    {
                        _signatureUpButton.IsEnabled = true;
                    }

                    if (selectIndex == _searchSignatureCollection.Count - 1)
                    {
                        _signatureDownButton.IsEnabled = false;
                    }
                    else
                    {
                        _signatureDownButton.IsEnabled = true;
                    }
                }

                _signatureListView_PreviewMouseLeftButtonDown(this, null);
            }
            catch (Exception)
            {

            }
        }

        private void _signatureListView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var selectIndex = _signatureListView.SelectedIndex;
            if (selectIndex == -1)
            {
                _signatureContainsCheckBox.IsChecked = true;
                _signatureIsIgnoreCaseCheckBox.IsChecked = false;
                _signatureTextBox.Text = "";
                return;
            }

            var item = _signatureListView.SelectedItem as SearchContains<SearchRegex>;
            if (item == null) return;

            _signatureContainsCheckBox.IsChecked = item.Contains;
            _signatureIsIgnoreCaseCheckBox.IsChecked = item.Value.IsIgnoreCase;
            _signatureTextBox.Text = item.Value.Value;
        }

        private void _signatureListView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var selectItems = _signatureListView.SelectedItems;

            _signatureListViewDeleteMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);
            _signatureListViewCopyMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);
            _signatureListViewCutMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);

            {
                var line = Clipboard.GetText().Split('\r', '\n');

                if (line.Length != 0)
                {
                    Regex regex = new Regex("^([\\+-]) ([\\+-]) \"(.*)\"$");

                    _signatureListViewPasteMenuItem.IsEnabled = regex.IsMatch(line[0]);
                }
            }
        }

        private void _signatureListViewDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _signatureDeleteButton_Click(null, null);
        }

        private void _signatureListViewCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var sb = new StringBuilder();

            foreach (var item in _signatureListView.SelectedItems.OfType<SearchContains<SearchRegex>>())
            {
                sb.AppendLine(string.Format("{0} {1} \"{2}\"", (item.Contains == true) ? "+" : "-", (item.Value.IsIgnoreCase == true) ? "+" : "-", item.Value.Value));
            }

            Clipboard.SetText(sb.ToString());
        }

        private void _signatureListViewCutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _signatureListViewCopyMenuItem_Click(null, null);
            _signatureDeleteButton_Click(null, null);
        }

        private void _signatureListViewPasteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Regex regex = new Regex("^([\\+-]) ([\\+-]) \"(.*)\"$");

            foreach (var line in Clipboard.GetText().Split('\r', '\n'))
            {
                try
                {
                    var match = regex.Match(line);
                    if (!match.Success) continue;

                    try
                    {
                        new Regex(match.Groups[3].Value);
                    }
                    catch (Exception)
                    {
                        return;
                    }

                    var item = new SearchContains<SearchRegex>()
                    {
                        Contains = (match.Groups[1].Value == "+") ? true : false,
                        Value = new SearchRegex()
                        {
                            IsIgnoreCase = (match.Groups[2].Value == "+") ? true : false,
                            Value = match.Groups[3].Value
                        },
                    };

                    if (_searchSignatureCollection.Contains(item)) continue;
                    _searchSignatureCollection.Add(item);
                }
                catch (Exception)
                {
                    continue;
                }
            }

            _signatureTextBox.Text = "";
            _signatureListView.SelectedIndex = _searchSignatureCollection.Count - 1;

            _signatureListView.Items.Refresh();
            _signatureListViewUpdate();
        }

        private void _signatureUpButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _signatureListView.SelectedItem as SearchContains<SearchRegex>;
            if (item == null) return;

            var selectIndex = _signatureListView.SelectedIndex;
            if (selectIndex == -1) return;

            _searchSignatureCollection.Remove(item);
            _searchSignatureCollection.Insert(selectIndex - 1, item);
            _signatureListView.Items.Refresh();

            _signatureListViewUpdate();
        }

        private void _signatureDownButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _signatureListView.SelectedItem as SearchContains<SearchRegex>;
            if (item == null) return;

            var selectIndex = _signatureListView.SelectedIndex;
            if (selectIndex == -1) return;

            _searchSignatureCollection.Remove(item);
            _searchSignatureCollection.Insert(selectIndex + 1, item);
            _signatureListView.Items.Refresh();

            _signatureListViewUpdate();
        }

        private void _signatureAddButton_Click(object sender, RoutedEventArgs e)
        {
            if (_signatureTextBox.Text == "") return;

            try
            {
                var item = new SearchContains<SearchRegex>()
                {
                    Contains = _signatureContainsCheckBox.IsChecked.Value,
                    Value = new SearchRegex()
                    {
                        IsIgnoreCase = _signatureIsIgnoreCaseCheckBox.IsChecked.Value,
                        Value = _signatureTextBox.Text
                    },
                };

                if (_searchSignatureCollection.Contains(item)) return;
                _searchSignatureCollection.Add(item);
            }
            catch (Exception)
            {
                return;
            }

            _signatureTextBox.Text = "";
            _signatureListView.SelectedIndex = _searchSignatureCollection.Count - 1;

            _signatureListView.Items.Refresh();
            _signatureListViewUpdate();
        }

        private void _signatureEditButton_Click(object sender, RoutedEventArgs e)
        {
            if (_signatureTextBox.Text == "") return;

            try
            {

                var uitem = new SearchContains<SearchRegex>()
                {
                    Contains = _signatureContainsCheckBox.IsChecked.Value,
                    Value = new SearchRegex()
                    {
                        IsIgnoreCase = _signatureIsIgnoreCaseCheckBox.IsChecked.Value,
                        Value = _signatureTextBox.Text
                    },
                };

                if (_searchSignatureCollection.Contains(uitem)) return;

                var item = _signatureListView.SelectedItem as SearchContains<SearchRegex>;
                if (item == null) return;

                item.Contains = _signatureContainsCheckBox.IsChecked.Value;
                item.Value = new SearchRegex() { IsIgnoreCase = _signatureIsIgnoreCaseCheckBox.IsChecked.Value, Value = _signatureTextBox.Text };
            }
            catch (Exception)
            {
                return;
            }

            _signatureListView.Items.Refresh();
            _signatureListViewUpdate();
        }

        private void _signatureDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            int selectIndex = _signatureListView.SelectedIndex;
            if (selectIndex == -1) return;

            _signatureTextBox.Text = "";

            foreach (var item in _signatureListView.SelectedItems.OfType<SearchContains<SearchRegex>>().ToArray())
            {
                _searchSignatureCollection.Remove(item);
            }

            _signatureListView.Items.Refresh();
            _signatureListView.SelectedIndex = selectIndex;
            _signatureListViewUpdate();
        }

        #endregion

        #region _creationTimeRangeListView

        private void _creationTimeRangeMinTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                _creationTimeRangeMaxTextBox.Focus();

                e.Handled = true;
            }
        }

        private void _creationTimeRangeMaxTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                _creationTimeRangeAddButton_Click(null, null);
                _creationTimeRangeMinTextBox.Focus();

                e.Handled = true;
            }
        }

        private void _creationTimeRangeListViewUpdate()
        {
            _creationTimeRangeListView_SelectionChanged(this, null);
        }

        private void _creationTimeRangeListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var selectIndex = _creationTimeRangeListView.SelectedIndex;

                if (selectIndex == -1)
                {
                    _creationTimeRangeUpButton.IsEnabled = false;
                    _creationTimeRangeDownButton.IsEnabled = false;
                }
                else
                {
                    if (selectIndex == 0)
                    {
                        _creationTimeRangeUpButton.IsEnabled = false;
                    }
                    else
                    {
                        _creationTimeRangeUpButton.IsEnabled = true;
                    }

                    if (selectIndex == _searchCreationTimeRangeCollection.Count - 1)
                    {
                        _creationTimeRangeDownButton.IsEnabled = false;
                    }
                    else
                    {
                        _creationTimeRangeDownButton.IsEnabled = true;
                    }
                }

                _creationTimeRangeListView_PreviewMouseLeftButtonDown(this, null);
            }
            catch (Exception)
            {

            }
        }

        private void _creationTimeRangeListView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var selectIndex = _creationTimeRangeListView.SelectedIndex;
            if (selectIndex == -1)
            {
                _creationTimeRangeContainsCheckBox.IsChecked = true;

                var maxDateTime = DateTime.Now.AddDays(1);

                _creationTimeRangeMinTextBox.Text = new DateTime(DateTime.Now.Year, 1, 1, 0, 0, 0, DateTimeKind.Local).ToLocalTime().ToString(LanguagesManager.Instance.DateTime_StringFormat, System.Globalization.DateTimeFormatInfo.InvariantInfo);
                _creationTimeRangeMaxTextBox.Text = new DateTime(maxDateTime.Year, maxDateTime.Month, maxDateTime.Day, 0, 0, 0, DateTimeKind.Local).ToLocalTime().ToString(LanguagesManager.Instance.DateTime_StringFormat, System.Globalization.DateTimeFormatInfo.InvariantInfo);
                return;
            }

            var item = _creationTimeRangeListView.SelectedItem as SearchContains<SearchRange<DateTime>>;
            if (item == null) return;

            _creationTimeRangeContainsCheckBox.IsChecked = item.Contains;
            _creationTimeRangeMinTextBox.Text = item.Value.Min.ToLocalTime().ToString(LanguagesManager.Instance.DateTime_StringFormat, System.Globalization.DateTimeFormatInfo.InvariantInfo);
            _creationTimeRangeMaxTextBox.Text = item.Value.Max.ToLocalTime().ToString(LanguagesManager.Instance.DateTime_StringFormat, System.Globalization.DateTimeFormatInfo.InvariantInfo);
        }

        private void _creationTimeRangeListView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var selectItems = _creationTimeRangeListView.SelectedItems;

            _creationTimeRangeListViewDeleteMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);
            _creationTimeRangeListViewCopyMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);
            _creationTimeRangeListViewCutMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);

            {
                var line = Clipboard.GetText().Split('\r', '\n');

                if (line.Length != 0)
                {
                    Regex regex = new Regex(@"^([\+-]) (.*), (.*)$");

                    _creationTimeRangeListViewPasteMenuItem.IsEnabled = regex.IsMatch(line[0]);
                }
            }
        }

        private void _creationTimeRangeListViewDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _creationTimeRangeDeleteButton_Click(null, null);
        }

        private void _creationTimeRangeListViewCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var sb = new StringBuilder();

            foreach (var item in _creationTimeRangeListView.SelectedItems.OfType<SearchContains<SearchRange<DateTime>>>())
            {
                sb.AppendLine(string.Format("{0} {1}, {2}", (item.Contains == true) ? "+" : "-",
                    item.Value.Min.ToUniversalTime().ToString("yyyy/MM/dd HH:mm:ss"),
                    item.Value.Max.ToUniversalTime().ToString("yyyy/MM/dd HH:mm:ss")));
            }

            Clipboard.SetText(sb.ToString());
        }

        private void _creationTimeRangeListViewCutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _creationTimeRangeListViewCopyMenuItem_Click(null, null);
            _creationTimeRangeDeleteButton_Click(null, null);
        }

        private void _creationTimeRangeListViewPasteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Regex regex = new Regex(@"^([\+-]) (.*), (.*)$");

            foreach (var line in Clipboard.GetText().Split('\r', '\n'))
            {
                try
                {
                    var match = regex.Match(line);
                    if (!match.Success) continue;

                    var item = new SearchContains<SearchRange<DateTime>>()
                    {
                        Contains = (match.Groups[1].Value == "+") ? true : false,
                        Value = new SearchRange<DateTime>()
                        {
                            Max = DateTime.ParseExact(match.Groups[3].Value, LanguagesManager.Instance.DateTime_StringFormat, System.Globalization.DateTimeFormatInfo.InvariantInfo, System.Globalization.DateTimeStyles.AssumeLocal).ToUniversalTime(),
                            Min = DateTime.ParseExact(match.Groups[2].Value, LanguagesManager.Instance.DateTime_StringFormat, System.Globalization.DateTimeFormatInfo.InvariantInfo, System.Globalization.DateTimeStyles.AssumeLocal).ToUniversalTime(),
                        },
                    };

                    if (_searchCreationTimeRangeCollection.Contains(item)) continue;
                    _searchCreationTimeRangeCollection.Add(item);
                }
                catch (Exception)
                {
                    continue;
                }
            }

            var maxDateTime = DateTime.Now.AddDays(1);

            _creationTimeRangeMinTextBox.Text = new DateTime(DateTime.Now.Year, 1, 1, 0, 0, 0, DateTimeKind.Local).ToLocalTime().ToString(LanguagesManager.Instance.DateTime_StringFormat, System.Globalization.DateTimeFormatInfo.InvariantInfo);
            _creationTimeRangeMaxTextBox.Text = new DateTime(maxDateTime.Year, maxDateTime.Month, maxDateTime.Day, 0, 0, 0, DateTimeKind.Local).ToLocalTime().ToString(LanguagesManager.Instance.DateTime_StringFormat, System.Globalization.DateTimeFormatInfo.InvariantInfo);
            _creationTimeRangeListView.SelectedIndex = _searchCreationTimeRangeCollection.Count - 1;

            _creationTimeRangeListView.Items.Refresh();
            _creationTimeRangeListViewUpdate();
        }

        private void _creationTimeRangeUpButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _creationTimeRangeListView.SelectedItem as SearchContains<SearchRange<DateTime>>;
            if (item == null) return;

            var selectIndex = _creationTimeRangeListView.SelectedIndex;
            if (selectIndex == -1) return;

            _searchCreationTimeRangeCollection.Remove(item);
            _searchCreationTimeRangeCollection.Insert(selectIndex - 1, item);
            _creationTimeRangeListView.Items.Refresh();

            _creationTimeRangeListViewUpdate();
        }

        private void _creationTimeRangeDownButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _creationTimeRangeListView.SelectedItem as SearchContains<SearchRange<DateTime>>;
            if (item == null) return;

            var selectIndex = _creationTimeRangeListView.SelectedIndex;
            if (selectIndex == -1) return;

            _searchCreationTimeRangeCollection.Remove(item);
            _searchCreationTimeRangeCollection.Insert(selectIndex + 1, item);
            _creationTimeRangeListView.Items.Refresh();

            _creationTimeRangeListViewUpdate();
        }

        private void _creationTimeRangeAddButton_Click(object sender, RoutedEventArgs e)
        {
            if (_creationTimeRangeMinTextBox.Text == "") return;
            if (_creationTimeRangeMaxTextBox.Text == "") return;

            try
            {
                var item = new SearchContains<SearchRange<DateTime>>()
                {
                    Contains = _creationTimeRangeContainsCheckBox.IsChecked.Value,
                    Value = new SearchRange<DateTime>()
                    {
                        Max = DateTime.ParseExact(_creationTimeRangeMaxTextBox.Text, LanguagesManager.Instance.DateTime_StringFormat, System.Globalization.DateTimeFormatInfo.InvariantInfo, System.Globalization.DateTimeStyles.AssumeLocal).ToUniversalTime(),
                        Min = DateTime.ParseExact(_creationTimeRangeMinTextBox.Text, LanguagesManager.Instance.DateTime_StringFormat, System.Globalization.DateTimeFormatInfo.InvariantInfo, System.Globalization.DateTimeStyles.AssumeLocal).ToUniversalTime(),
                    }
                };

                if (_searchCreationTimeRangeCollection.Contains(item)) return;
                _searchCreationTimeRangeCollection.Add(item);
            }
            catch (Exception)
            {
                return;
            }

            var maxDateTime = DateTime.Now.AddDays(1);

            _creationTimeRangeMinTextBox.Text = new DateTime(DateTime.Now.Year, 1, 1, 0, 0, 0, DateTimeKind.Local).ToLocalTime().ToString(LanguagesManager.Instance.DateTime_StringFormat, System.Globalization.DateTimeFormatInfo.InvariantInfo);
            _creationTimeRangeMaxTextBox.Text = new DateTime(maxDateTime.Year, maxDateTime.Month, maxDateTime.Day, 0, 0, 0, DateTimeKind.Local).ToLocalTime().ToString(LanguagesManager.Instance.DateTime_StringFormat, System.Globalization.DateTimeFormatInfo.InvariantInfo);
            _creationTimeRangeListView.SelectedIndex = _searchCreationTimeRangeCollection.Count - 1;

            _creationTimeRangeListView.Items.Refresh();
            _creationTimeRangeListViewUpdate();
        }

        private void _creationTimeRangeEditButton_Click(object sender, RoutedEventArgs e)
        {
            if (_creationTimeRangeMinTextBox.Text == "") return;
            if (_creationTimeRangeMaxTextBox.Text == "") return;

            try
            {
                var uitem = new SearchContains<SearchRange<DateTime>>()
                {
                    Contains = _creationTimeRangeContainsCheckBox.IsChecked.Value,
                    Value = new SearchRange<DateTime>()
                    {
                        Max = DateTime.ParseExact(_creationTimeRangeMaxTextBox.Text, LanguagesManager.Instance.DateTime_StringFormat, System.Globalization.DateTimeFormatInfo.InvariantInfo, System.Globalization.DateTimeStyles.AssumeLocal).ToUniversalTime(),
                        Min = DateTime.ParseExact(_creationTimeRangeMinTextBox.Text, LanguagesManager.Instance.DateTime_StringFormat, System.Globalization.DateTimeFormatInfo.InvariantInfo, System.Globalization.DateTimeStyles.AssumeLocal).ToUniversalTime(),
                    }
                };

                if (_searchCreationTimeRangeCollection.Contains(uitem)) return;

                var item = _creationTimeRangeListView.SelectedItem as SearchContains<SearchRange<DateTime>>;
                if (item == null) return;

                item.Contains = _creationTimeRangeContainsCheckBox.IsChecked.Value;
                item.Value.Max = DateTime.ParseExact(_creationTimeRangeMaxTextBox.Text, LanguagesManager.Instance.DateTime_StringFormat, System.Globalization.DateTimeFormatInfo.InvariantInfo, System.Globalization.DateTimeStyles.AssumeLocal).ToUniversalTime();
                item.Value.Min = DateTime.ParseExact(_creationTimeRangeMinTextBox.Text, LanguagesManager.Instance.DateTime_StringFormat, System.Globalization.DateTimeFormatInfo.InvariantInfo, System.Globalization.DateTimeStyles.AssumeLocal).ToUniversalTime();
            }
            catch (Exception)
            {
                return;
            }

            _creationTimeRangeListView.Items.Refresh();
            _creationTimeRangeListViewUpdate();
        }

        private void _creationTimeRangeDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            int selectIndex = _creationTimeRangeListView.SelectedIndex;
            if (selectIndex == -1) return;

            var maxDateTime = DateTime.Now.AddDays(1);

            _creationTimeRangeMinTextBox.Text = new DateTime(DateTime.Now.Year, 1, 1, 0, 0, 0, DateTimeKind.Local).ToLocalTime().ToString(LanguagesManager.Instance.DateTime_StringFormat, System.Globalization.DateTimeFormatInfo.InvariantInfo);
            _creationTimeRangeMaxTextBox.Text = new DateTime(maxDateTime.Year, maxDateTime.Month, maxDateTime.Day, 0, 0, 0, DateTimeKind.Local).ToLocalTime().ToString(LanguagesManager.Instance.DateTime_StringFormat, System.Globalization.DateTimeFormatInfo.InvariantInfo);

            foreach (var item in _creationTimeRangeListView.SelectedItems.OfType<SearchContains<SearchRange<DateTime>>>().ToArray())
            {
                _searchCreationTimeRangeCollection.Remove(item);
            }

            _creationTimeRangeListView.Items.Refresh();
            _creationTimeRangeListView.SelectedIndex = selectIndex;
            _creationTimeRangeListViewUpdate();
        }

        #endregion

        #region _messageListView

        private void _messageTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                _messageAddButton_Click(null, null);

                e.Handled = true;
            }
        }

        private void _messageListViewUpdate()
        {
            _messageListView_SelectionChanged(this, null);
        }

        private void _messageListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                var selectIndex = _messageListView.SelectedIndex;

                if (selectIndex == -1)
                {
                    _messageUpButton.IsEnabled = false;
                    _messageDownButton.IsEnabled = false;
                }
                else
                {
                    if (selectIndex == 0)
                    {
                        _messageUpButton.IsEnabled = false;
                    }
                    else
                    {
                        _messageUpButton.IsEnabled = true;
                    }

                    if (selectIndex == _searchMessageCollection.Count - 1)
                    {
                        _messageDownButton.IsEnabled = false;
                    }
                    else
                    {
                        _messageDownButton.IsEnabled = true;
                    }
                }

                _messageListView_PreviewMouseLeftButtonDown(this, null);
            }
            catch (Exception)
            {

            }
        }

        private void _messageListView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var selectIndex = _messageListView.SelectedIndex;
            if (selectIndex == -1)
            {
                _messageContainsCheckBox.IsChecked = true;
                _messageTextBox.Text = "";
                return;
            }

            var item = _messageListView.SelectedItem as SearchContains<Message>;
            if (item == null) return;

            _messageContainsCheckBox.IsChecked = item.Contains;
            _messageTextBox.Text = LairConverter.ToMessageString(item.Value);
        }

        private void _messageListView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var selectItems = _messageListView.SelectedItems;

            _messageListViewDeleteMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);
            _messageListViewCopyMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);
            _messageListViewCutMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);

            {
                var line = Clipboard.GetText().Split('\r', '\n');

                if (line.Length != 0)
                {
                    Regex regex = new Regex(@"^([\+-]) (.*)$");

                    _messageListViewPasteMenuItem.IsEnabled = regex.IsMatch(line[0]);
                }
            }
        }

        private void _messageListViewDeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _messageDeleteButton_Click(null, null);
        }

        private void _messageListViewCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var sb = new StringBuilder();

            foreach (var item in _messageListView.SelectedItems.OfType<SearchContains<Message>>())
            {
                sb.AppendLine(string.Format("{0} {1}", (item.Contains == true) ? "+" : "-", LairConverter.ToMessageString(item.Value)));
            }

            Clipboard.SetText(sb.ToString());
        }

        private void _messageListViewCutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _messageListViewCopyMenuItem_Click(null, null);
            _messageDeleteButton_Click(null, null);
        }

        private void _messageListViewPasteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Regex regex = new Regex(@"^([\+-]) (.*)$");

            foreach (var line in Clipboard.GetText().Split('\r', '\n'))
            {
                try
                {
                    var match = regex.Match(line);
                    if (!match.Success) continue;

                    var message = LairConverter.FromMessageString(match.Groups[2].Value);
                    if (!message.VerifyCertificate()) continue;

                    var item = new SearchContains<Message>()
                    {
                        Contains = (match.Groups[1].Value == "+") ? true : false,
                        Value = message,
                    };

                    if (_searchMessageCollection.Contains(item)) continue;
                    _searchMessageCollection.Add(item);
                }
                catch (Exception)
                {
                    continue;
                }
            }

            _messageTextBox.Text = "";
            _messageListView.SelectedIndex = _searchMessageCollection.Count - 1;

            _messageListView.Items.Refresh();
            _messageListViewUpdate();
        }

        private void _messageUpButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _messageListView.SelectedItem as SearchContains<Message>;
            if (item == null) return;

            var selectIndex = _messageListView.SelectedIndex;
            if (selectIndex == -1) return;

            _searchMessageCollection.Remove(item);
            _searchMessageCollection.Insert(selectIndex - 1, item);
            _messageListView.Items.Refresh();

            _messageListViewUpdate();
        }

        private void _messageDownButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _messageListView.SelectedItem as SearchContains<Message>;
            if (item == null) return;

            var selectIndex = _messageListView.SelectedIndex;
            if (selectIndex == -1) return;

            _searchMessageCollection.Remove(item);
            _searchMessageCollection.Insert(selectIndex + 1, item);
            _messageListView.Items.Refresh();

            _messageListViewUpdate();
        }

        private void _messageAddButton_Click(object sender, RoutedEventArgs e)
        {
            if (_messageTextBox.Text == "") return;
            if (!Regex.IsMatch(_messageTextBox.Text, @"^[a-zA-Z0-9\-_]*$")) return;

            try
            {
                var item = new SearchContains<Message>()
                {
                    Contains = _messageContainsCheckBox.IsChecked.Value,
                    Value = LairConverter.FromMessageString(_messageTextBox.Text),
                };

                if (_searchMessageCollection.Contains(item)) return;
                _searchMessageCollection.Add(item);
            }
            catch (Exception)
            {
                return;
            }

            _messageTextBox.Text = "";
            _messageListView.SelectedIndex = _searchMessageCollection.Count - 1;

            _messageListView.Items.Refresh();
            _messageListViewUpdate();
        }

        private void _messageEditButton_Click(object sender, RoutedEventArgs e)
        {
            if (_messageTextBox.Text == "") return;
            if (!Regex.IsMatch(_messageTextBox.Text, @"^[a-zA-Z0-9\-_]*$")) return;

            try
            {
                var uitem = new SearchContains<Message>()
                {
                    Contains = _messageContainsCheckBox.IsChecked.Value,
                    Value = LairConverter.FromMessageString(_messageTextBox.Text),
                };

                if (_searchMessageCollection.Contains(uitem)) return;

                var item = _messageListView.SelectedItem as SearchContains<Message>;
                if (item == null) return;

                item.Contains = _messageContainsCheckBox.IsChecked.Value;
                item.Value = LairConverter.FromMessageString(_messageTextBox.Text);
            }
            catch (Exception)
            {
                return;
            }

            _messageListView.Items.Refresh();
            _messageListViewUpdate();
        }

        private void _messageDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            int selectIndex = _messageListView.SelectedIndex;
            if (selectIndex == -1) return;

            _messageTextBox.Text = "";

            foreach (var item in _messageListView.SelectedItems.OfType<SearchContains<Message>>().ToArray())
            {
                _searchMessageCollection.Remove(item);
            }

            _messageListView.Items.Refresh();
            _messageListView.SelectedIndex = selectIndex;
            _messageListViewUpdate();
        }

        #endregion

        private void _okButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;

            lock (_board.ThisLock)
            {
                lock (_board.SearchWordCollection.ThisLock)
                {
                    _board.SearchWordCollection.Clear();
                    _board.SearchWordCollection.AddRange(_searchWordCollection.Select(n => n.DeepClone()).ToList());
                }

                lock (_board.SearchRegexCollection.ThisLock)
                {
                    _board.SearchRegexCollection.Clear();
                    _board.SearchRegexCollection.AddRange(_searchRegexCollection.Select(n => n.DeepClone()).ToList());
                }

                lock (_board.SearchSignatureCollection.ThisLock)
                {
                    _board.SearchSignatureCollection.Clear();
                    _board.SearchSignatureCollection.AddRange(_searchSignatureCollection.Select(n => n.DeepClone()).ToList());
                }

                lock (_board.SearchCreationTimeRangeCollection.ThisLock)
                {
                    _board.SearchCreationTimeRangeCollection.Clear();
                    _board.SearchCreationTimeRangeCollection.AddRange(_searchCreationTimeRangeCollection.Select(n => n.DeepClone()).ToList());
                }

                lock (_board.SearchMessageCollection.ThisLock)
                {
                    _board.SearchMessageCollection.Clear();
                    _board.SearchMessageCollection.AddRange(_searchMessageCollection.Select(n => n.DeepClone()).ToList());
                }
            }
        }

        private void _cancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void Execute_Delete(object sender, ExecutedRoutedEventArgs e)
        {
            if (_wordTabItem.IsSelected)
            {
                _wordListViewDeleteMenuItem_Click(null, null);
            }
            else if (_regexTabItem.IsSelected)
            {
                _regexListViewDeleteMenuItem_Click(null, null);
            }
            else if (_signatureTabItem.IsSelected)
            {
                _signatureListViewDeleteMenuItem_Click(null, null);
            }
            else if (_creationTimeRangeTabItem.IsSelected)
            {
                _creationTimeRangeListViewDeleteMenuItem_Click(null, null);
            }
            else if (_messageTabItem.IsSelected)
            {
                _messageListViewDeleteMenuItem_Click(null, null);
            }
        }

        private void Execute_Copy(object sender, ExecutedRoutedEventArgs e)
        {
            if (_wordTabItem.IsSelected)
            {
                _wordListViewCopyMenuItem_Click(null, null);
            }
            else if (_regexTabItem.IsSelected)
            {
                _regexListViewCopyMenuItem_Click(null, null);
            }
            else if (_signatureTabItem.IsSelected)
            {
                _signatureListViewCopyMenuItem_Click(null, null);
            }
            else if (_creationTimeRangeTabItem.IsSelected)
            {
                _creationTimeRangeListViewCopyMenuItem_Click(null, null);
            }
            else if (_messageTabItem.IsSelected)
            {
                _messageListViewCopyMenuItem_Click(null, null);
            }
        }

        private void Execute_Cut(object sender, ExecutedRoutedEventArgs e)
        {
            if (_wordTabItem.IsSelected)
            {
                _wordListViewCutMenuItem_Click(null, null);
            }
            else if (_regexTabItem.IsSelected)
            {
                _regexListViewCutMenuItem_Click(null, null);
            }
            else if (_signatureTabItem.IsSelected)
            {
                _signatureListViewCutMenuItem_Click(null, null);
            }
            else if (_creationTimeRangeTabItem.IsSelected)
            {
                _creationTimeRangeListViewCutMenuItem_Click(null, null);
            }
            else if (_messageTabItem.IsSelected)
            {
                _messageListViewCutMenuItem_Click(null, null);
            }
        }

        private void Execute_Paste(object sender, ExecutedRoutedEventArgs e)
        {
            if (_wordTabItem.IsSelected)
            {
                _wordListViewPasteMenuItem_Click(null, null);
            }
            else if (_regexTabItem.IsSelected)
            {
                _regexListViewPasteMenuItem_Click(null, null);
            }
            else if (_signatureTabItem.IsSelected)
            {
                _signatureListViewPasteMenuItem_Click(null, null);
            }
            else if (_creationTimeRangeTabItem.IsSelected)
            {
                _creationTimeRangeListViewPasteMenuItem_Click(null, null);
            }
            else if (_messageTabItem.IsSelected)
            {
                _messageListViewPasteMenuItem_Click(null, null);
            }
        }
    }
}
