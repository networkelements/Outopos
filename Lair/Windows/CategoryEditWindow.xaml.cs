using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Lair.Properties;
using Library.Net;
using Library.Net.Lair;
using Library.Security;
using System.Text.RegularExpressions;

namespace Lair.Windows
{
    /// <summary>
    /// CategoryEditWindow.xaml の相互作用ロジック
    /// </summary>
    partial class CategoryEditWindow : Window
    {
        private Category _category;
        private List<SearchContains<string>> _searchKeywordCollection;
        private List<SearchContains<SearchRegex>> _searchNameRegexCollection;
        private List<SearchContains<string>> _searchSignatureCollection;
        private List<SearchContains<Message>> _searchMessageCollection;

        public CategoryEditWindow(ref Category category)
        {
            _category = category;

            var digitalSignatureCollection = new List<object>();
            digitalSignatureCollection.Add(new ComboBoxItem() { Content = "" });
            digitalSignatureCollection.AddRange(Settings.Instance.Global_DigitalSignatureCollection.Select(n => new DigitalSignatureComboBoxItem(n)).ToArray());

            _searchKeywordCollection = _category.SearchWordCollection.Select(n => n.DeepClone()).ToList();
            _searchNameRegexCollection = _category.SearchRegexCollection.Select(n => n.DeepClone()).ToList();
            _searchSignatureCollection = _category.SearchSignatureCollection.Select(n => n.DeepClone()).ToList();
            _searchMessageCollection = _category.SearchMessageCollection.Select(n => n.DeepClone()).ToList();

            InitializeComponent();

            using (FileStream stream = new FileStream(System.IO.Path.Combine(App.DirectoryPaths["Icons"], "Lair.ico"), FileMode.Open))
            {
                this.Icon = BitmapFrame.Create(stream);
            }

            _filterUploadSignatureComboBox.ItemsSource = digitalSignatureCollection;

            var index = Settings.Instance.Global_DigitalSignatureCollection.IndexOf(_category.FilterUploadDigitalSignature);
            _filterUploadSignatureComboBox.SelectedIndex = index + 1;

            _filterUploadCheckBox.IsChecked = category.FilterUpload_IsEnabled;

            _nameTextBox.Text = category.Name;

            _wordContainsCheckBox.IsChecked = true;
            _regexContainsCheckBox.IsChecked = true;
            _signatureContainsCheckBox.IsChecked = true;
            _messageContainsCheckBox.IsChecked = true;

            _wordListView.ItemsSource = _searchKeywordCollection;
            _regexListView.ItemsSource = _searchNameRegexCollection;
            _signatureListView.ItemsSource = _searchSignatureCollection;
            _messageListView.ItemsSource = _searchMessageCollection;
        }

        #region Upload

        private void _filterUploadSignatureComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _filterUploadCheckBox.IsEnabled = _filterUploadSignatureComboBox.SelectedIndex != 0;
        }

        #endregion

        #region _wordListView

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

                    if (selectIndex == _searchKeywordCollection.Count - 1)
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

            _wordListViewCopyMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);

            {
                var text = Clipboard.GetText();

                _wordListViewPasteMenuItem.IsEnabled = (text != null && Regex.IsMatch(text, @"([\+-]) (.*)"));
            }
        }

        private void _wordListViewCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var sb = new StringBuilder();

            foreach (var item in _wordListView.SelectedItems.OfType<SearchContains<string>>())
            {
                sb.AppendLine(string.Format("{0} {1}", (item.Contains == true) ? "+" : "-", item.Value));
            }

            Clipboard.SetText(sb.ToString());
        }

        private void _wordListViewPasteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Regex regex = new Regex(@"([\+-]) (.*)");

            foreach (var line in Clipboard.GetText().Split('\r', '\n'))
            {
                try
                {
                    var match = regex.Match(line);
                    if (!match.Success) continue;

                    if (!Regex.IsMatch(match.Groups[2].Value, "^[a-z0-9_]*$")) continue;

                    var item = new SearchContains<string>()
                    {
                        Contains = (match.Groups[1].Value == "+") ? true : false,
                        Value = match.Groups[2].Value,
                    };

                    if (_searchKeywordCollection.Contains(item)) continue;
                    _searchKeywordCollection.Add(item);
                }
                catch (Exception)
                {
                    continue;
                }
            }

            _wordTextBox.Text = "";
            _wordListView.SelectedIndex = _searchKeywordCollection.Count - 1;

            _wordListView.Items.Refresh();
            _wordListViewUpdate();
        }

        private void _wordUpButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _wordListView.SelectedItem as SearchContains<string>;
            if (item == null) return;

            var selectIndex = _wordListView.SelectedIndex;
            if (selectIndex == -1) return;

            _searchKeywordCollection.Remove(item);
            _searchKeywordCollection.Insert(selectIndex - 1, item);
            _wordListView.Items.Refresh();

            _wordListViewUpdate();
        }

        private void _wordDownButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _wordListView.SelectedItem as SearchContains<string>;
            if (item == null) return;

            var selectIndex = _wordListView.SelectedIndex;
            if (selectIndex == -1) return;

            _searchKeywordCollection.Remove(item);
            _searchKeywordCollection.Insert(selectIndex + 1, item);
            _wordListView.Items.Refresh();

            _wordListViewUpdate();
        }

        private void _wordAddButton_Click(object sender, RoutedEventArgs e)
        {
            if (_wordTextBox.Text == "") return;
            if (!Regex.IsMatch(_wordTextBox.Text, "^[a-z0-9_]*$")) return;

            var item = new SearchContains<string>()
            {
                Contains = _wordContainsCheckBox.IsChecked.Value,
                Value = _wordTextBox.Text,
            };

            if (_searchKeywordCollection.Contains(item)) return;
            _searchKeywordCollection.Add(item);

            _wordTextBox.Text = "";
            _wordListView.SelectedIndex = _searchKeywordCollection.Count - 1;

            _wordListView.Items.Refresh();
            _wordListViewUpdate();
        }

        private void _wordEditButton_Click(object sender, RoutedEventArgs e)
        {
            if (_wordTextBox.Text == "") return;
            if (!Regex.IsMatch(_wordTextBox.Text, "^[a-z0-9_]*$")) return;

            var uitem = new SearchContains<string>()
            {
                Contains = _wordContainsCheckBox.IsChecked.Value,
                Value = _wordTextBox.Text,
            };

            if (_searchKeywordCollection.Contains(uitem)) return;

            var item = _wordListView.SelectedItem as SearchContains<string>;
            if (item == null) return;

            item.Contains = _wordContainsCheckBox.IsChecked.Value;
            item.Value = _wordTextBox.Text;

            _wordListView.Items.Refresh();
            _wordListViewUpdate();
        }

        private void _wordDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _wordListView.SelectedItem as SearchContains<string>;
            if (item == null) return;

            _wordTextBox.Text = "";

            int selectIndex = _wordListView.SelectedIndex;
            _searchKeywordCollection.Remove(item);
            _wordListView.Items.Refresh();
            _wordListView.SelectedIndex = selectIndex;
            _wordListViewUpdate();
        }

        #endregion

        #region _regexListView

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

                    if (selectIndex == _searchNameRegexCollection.Count - 1)
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

            _regexListViewCopyMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);

            {
                var text = Clipboard.GetText();

                _regexListViewPasteMenuItem.IsEnabled = (text != null && Regex.IsMatch(text, @"([\+-]) ([\+-]) (.*)"));
            }
        }

        private void _regexListViewCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var sb = new StringBuilder();

            foreach (var item in _regexListView.SelectedItems.OfType<SearchContains<SearchRegex>>())
            {
                sb.AppendLine(string.Format("{0} {1} {2}", (item.Contains == true) ? "+" : "-", (item.Value.IsIgnoreCase == true) ? "+" : "-", item.Value.Value));
            }

            Clipboard.SetText(sb.ToString());
        }

        private void _regexListViewPasteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Regex regex = new Regex(@"([\+-]) ([\+-]) (.*)");

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

                    if (_searchNameRegexCollection.Contains(item)) continue;
                    _searchNameRegexCollection.Add(item);
                }
                catch (Exception)
                {
                    continue;
                }
            }

            _regexTextBox.Text = "";
            _regexListView.SelectedIndex = _searchNameRegexCollection.Count - 1;

            _regexListView.Items.Refresh();
            _regexListViewUpdate();
        }

        private void _regexUpButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _regexListView.SelectedItem as SearchContains<SearchRegex>;
            if (item == null) return;

            var selectIndex = _regexListView.SelectedIndex;
            if (selectIndex == -1) return;

            _searchNameRegexCollection.Remove(item);
            _searchNameRegexCollection.Insert(selectIndex - 1, item);
            _regexListView.Items.Refresh();

            _regexListViewUpdate();
        }

        private void _regexDownButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _regexListView.SelectedItem as SearchContains<SearchRegex>;
            if (item == null) return;

            var selectIndex = _regexListView.SelectedIndex;
            if (selectIndex == -1) return;

            _searchNameRegexCollection.Remove(item);
            _searchNameRegexCollection.Insert(selectIndex + 1, item);
            _regexListView.Items.Refresh();

            _regexListViewUpdate();
        }

        private void _regexAddButton_Click(object sender, RoutedEventArgs e)
        {
            if (_regexTextBox.Text == "") return;

            try
            {
                new Regex(_regexTextBox.Text);
            }
            catch (Exception)
            {
                return;
            }

            var item = new SearchContains<SearchRegex>()
            {
                Contains = _regexContainsCheckBox.IsChecked.Value,
                Value = new SearchRegex()
                {
                    IsIgnoreCase = _regexIsIgnoreCaseCheckBox.IsChecked.Value,
                    Value = _regexTextBox.Text
                },
            };

            if (_searchNameRegexCollection.Contains(item)) return;
            _searchNameRegexCollection.Add(item);

            _regexTextBox.Text = "";
            _regexListView.SelectedIndex = _searchNameRegexCollection.Count - 1;

            _regexListView.Items.Refresh();
            _regexListViewUpdate();
        }

        private void _regexEditButton_Click(object sender, RoutedEventArgs e)
        {
            if (_regexTextBox.Text == "") return;

            try
            {
                new Regex(_regexTextBox.Text);
            }
            catch (Exception)
            {
                return;
            }

            var uitem = new SearchContains<SearchRegex>()
            {
                Contains = _regexContainsCheckBox.IsChecked.Value,
                Value = new SearchRegex()
                {
                    IsIgnoreCase = _regexIsIgnoreCaseCheckBox.IsChecked.Value,
                    Value = _regexTextBox.Text
                },
            };

            if (_searchNameRegexCollection.Contains(uitem)) return;

            var item = _regexListView.SelectedItem as SearchContains<SearchRegex>;
            if (item == null) return;

            item.Contains = _regexContainsCheckBox.IsChecked.Value;
            item.Value = new SearchRegex() { IsIgnoreCase = _regexIsIgnoreCaseCheckBox.IsChecked.Value, Value = _regexTextBox.Text };

            _regexListView.Items.Refresh();
            _regexListViewUpdate();
        }

        private void _regexDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _regexListView.SelectedItem as SearchContains<SearchRegex>;
            if (item == null) return;

            _regexTextBox.Text = "";

            int selectIndex = _regexListView.SelectedIndex;
            _searchNameRegexCollection.Remove(item);
            _regexListView.Items.Refresh();
            _regexListView.SelectedIndex = selectIndex;
            _regexListViewUpdate();
        }

        #endregion

        #region _signatureListView

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
                _signatureTextBox.Text = "";
                return;
            }

            var item = _signatureListView.SelectedItem as SearchContains<string>;
            if (item == null) return;

            _signatureContainsCheckBox.IsChecked = item.Contains;
            _signatureTextBox.Text = item.Value;
        }

        private void _signatureListView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var selectItems = _signatureListView.SelectedItems;

            _signatureListViewCopyMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);

            {
                var text = Clipboard.GetText();

                _signatureListViewPasteMenuItem.IsEnabled = (text != null && Regex.IsMatch(text, @"([\+-]) (.*)"));
            }
        }

        private void _signatureListViewCopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var sb = new StringBuilder();

            foreach (var item in _signatureListView.SelectedItems.OfType<SearchContains<string>>())
            {
                sb.AppendLine(string.Format("{0} {1}", (item.Contains == true) ? "+" : "-", item.Value));
            }

            Clipboard.SetText(sb.ToString());
        }

        private void _signatureListViewPasteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Regex regex = new Regex(@"([\+-]) (.*)");

            foreach (var line in Clipboard.GetText().Split('\r', '\n'))
            {
                try
                {
                    var match = regex.Match(line);
                    if (!match.Success) continue;

                    if (!Regex.IsMatch(match.Groups[2].Value, @"^[a-zA-Z0-9\-_]*$")) continue;

                    var item = new SearchContains<string>()
                    {
                        Contains = (match.Groups[1].Value == "+") ? true : false,
                        Value = match.Groups[2].Value,
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
            var item = _signatureListView.SelectedItem as SearchContains<string>;
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
            var item = _signatureListView.SelectedItem as SearchContains<string>;
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
            if (!Regex.IsMatch(_signatureTextBox.Text, @"^[a-zA-Z0-9\-_]*$")) return;

            var item = new SearchContains<string>()
            {
                Contains = _signatureContainsCheckBox.IsChecked.Value,
                Value = _signatureTextBox.Text,
            };

            if (_searchSignatureCollection.Contains(item)) return;
            _searchSignatureCollection.Add(item);

            _signatureTextBox.Text = "";
            _signatureListView.SelectedIndex = _searchSignatureCollection.Count - 1;

            _signatureListView.Items.Refresh();
            _signatureListViewUpdate();
        }

        private void _signatureEditButton_Click(object sender, RoutedEventArgs e)
        {
            if (_signatureTextBox.Text == "") return;
            if (!Regex.IsMatch(_signatureTextBox.Text, @"^[a-zA-Z0-9\-_]*$")) return;

            var uitem = new SearchContains<string>()
            {
                Contains = _signatureContainsCheckBox.IsChecked.Value,
                Value = _signatureTextBox.Text,
            };

            if (_searchSignatureCollection.Contains(uitem)) return;

            var item = _signatureListView.SelectedItem as SearchContains<string>;
            if (item == null) return;

            item.Contains = _signatureContainsCheckBox.IsChecked.Value;
            item.Value = _signatureTextBox.Text;

            _signatureListView.Items.Refresh();
            _signatureListViewUpdate();
        }

        private void _signatureDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _signatureListView.SelectedItem as SearchContains<string>;
            if (item == null) return;

            _signatureTextBox.Text = "";

            int selectIndex = _signatureListView.SelectedIndex;
            _searchSignatureCollection.Remove(item);
            _signatureListView.Items.Refresh();
            _signatureListView.SelectedIndex = selectIndex;
            _signatureListViewUpdate();
        }

        #endregion

        #region _messageListView

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

            _messageListViewCopyMenuItem.IsEnabled = (selectItems == null) ? false : (selectItems.Count > 0);

            {
                var text = Clipboard.GetText();

                _messageListViewPasteMenuItem.IsEnabled = (text != null && Regex.IsMatch(text, @"([\+-]) (.*)"));
            }
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

        private void _messageListViewPasteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Regex regex = new Regex(@"([\+-]) (.*)");

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

            var item = new SearchContains<Message>()
            {
                Contains = _messageContainsCheckBox.IsChecked.Value,
                Value = LairConverter.FromMessageString(_messageTextBox.Text),
            };

            if (_searchMessageCollection.Contains(item)) return;
            _searchMessageCollection.Add(item);

            _messageTextBox.Text = "";
            _messageListView.SelectedIndex = _searchMessageCollection.Count - 1;

            _messageListView.Items.Refresh();
            _messageListViewUpdate();
        }

        private void _messageEditButton_Click(object sender, RoutedEventArgs e)
        {
            if (_messageTextBox.Text == "") return;
            if (!Regex.IsMatch(_messageTextBox.Text, @"^[a-zA-Z0-9\-_]*$")) return;

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

            _messageListView.Items.Refresh();
            _messageListViewUpdate();
        }

        private void _messageDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var item = _messageListView.SelectedItem as SearchContains<Message>;
            if (item == null) return;

            _messageTextBox.Text = "";

            int selectIndex = _messageListView.SelectedIndex;
            _searchMessageCollection.Remove(item);
            _messageListView.Items.Refresh();
            _messageListView.SelectedIndex = selectIndex;
            _messageListViewUpdate();
        }

        #endregion

        private void _okButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;

            _category.Name = _nameTextBox.Text;

            var digitalSignatureComboBoxItem = _filterUploadSignatureComboBox.SelectedItem as DigitalSignatureComboBoxItem;
            DigitalSignature digitalSignature = digitalSignatureComboBoxItem == null ? null : digitalSignatureComboBoxItem.Value;

            _category.FilterUploadDigitalSignature = digitalSignature;
            _category.FilterUpload_IsEnabled = _filterUploadCheckBox.IsChecked.Value;

            _category.SearchWordCollection.Clear();
            _category.SearchWordCollection.AddRange(_searchKeywordCollection.Select(n => n.DeepClone()).ToList());
            _category.SearchRegexCollection.Clear();
            _category.SearchRegexCollection.AddRange(_searchNameRegexCollection.Select(n => n.DeepClone()).ToList());
            _category.SearchSignatureCollection.Clear();
            _category.SearchSignatureCollection.AddRange(_searchSignatureCollection.Select(n => n.DeepClone()).ToList());
            _category.SearchMessageCollection.Clear();
            _category.SearchMessageCollection.AddRange(_searchMessageCollection.Select(n => n.DeepClone()).ToList());
        }

        private void _cancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}
