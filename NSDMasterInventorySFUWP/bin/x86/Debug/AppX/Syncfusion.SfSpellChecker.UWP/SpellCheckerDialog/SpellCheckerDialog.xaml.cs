#region Copyright Syncfusion Inc. 2001-2018.
// Copyright Syncfusion Inc. 2001-2018. All rights reserved.
// Use of this code is subject to the terms of our license.
// A copy of the current license can be obtained at any time by e-mailing
// licensing@syncfusion.com. Any infringement will be prosecuted under
// applicable laws. 
#endregion
using Syncfusion.SpellChecker.Base;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Syncfusion.UI.Xaml.Controls
{
    /// <summary>
    /// Interaction logic for SpellCheckerDialog.xaml
    /// </summary>
    [ClassReference(IsReviewed =false)]
    public sealed partial class SpellCheckerDialog : Page,IDisposable
    {
        #region
        /// <summary>
        /// Event which is handled after spell check operations are completed
        /// </summary>
        public event EventHandler SpellCheckCompleted;
        
        #endregion 
        #region Variables

        SfSpellChecker spellChecker;

        IEditorProperties Editor;

        internal Dictionary<int, object> m_errorlist = new Dictionary<int, object>();

        string errorword;

        DispatcherTimer dispatcherTimer = new DispatcherTimer();

        List<string> ignorewords = new List<string>();

        int _SubStrlen = 0, currenterrorword = 0, currentignoreword=0;

        #endregion

        #region Constructor
        /// <summary>
        /// Initializes the instance of SpellCheckerDialog
        /// </summary>
        /// <param name="checker"></param>
        /// <param name="editor"></param>
        [ClassReference(IsReviewed =false)]
        public SpellCheckerDialog(SfSpellChecker checker,IEditorProperties editor)
        {
            this.InitializeComponent();
            spellChecker = checker;
            Editor = editor;
            lstBox.LayoutUpdated += lstBox_LayoutUpdated;
            _btnclose.PointerEntered += Button_PointerEntered;
            _btnclose.PointerExited += _btnclose_PointerExited;
            _btnclose.Click += _btnclose_Click;
           
        }

       

        #endregion

        #region Dependency Property

        /// <summary>
        /// 
        /// </summary>
        [ClassReference(IsReviewed = false)]
        internal ObservableCollection<string> Suggestions
        {
            get { return (ObservableCollection<string>)GetValue(SuggestionsProperty); }
            set { SetValue(SuggestionsProperty, value); }
        }

        /// <summary>
        /// Using a DependencyProperty as the backing store for Suggestions.  This enables animation, styling, binding, etc...
        /// </summary>

        public static readonly DependencyProperty SuggestionsProperty =
            DependencyProperty.Register("Suggestions", typeof(ObservableCollection<string>), typeof(SpellCheckerDialog), null);

        private string localizedSuggestionText = new ResourceWrapper().NoSuggestionText;

        #endregion

        #region Helper Methods

        internal void findingerrorword()
        {
            int i = 0;
            m_errorlist.Clear();
            string[] words = Editor.Text.Split(' ');
            int startindex = 0;

            foreach (string str1 in words)
            {

                if (spellChecker != null && str1.EndsWith("."))
                {
                    string substr = str1.Remove(str1.Length - 1);
                    if (spellChecker.HasError(substr) && !ignorewords.Contains(substr))
                    {
                        m_errorlist.Add(startindex, substr);
                        i++;
                    }
                }
                else if (spellChecker != null && spellChecker.HasError(str1) && !ignorewords.Contains(str1))
                {
                    m_errorlist.Add(startindex, str1);
                    i++;
                }

                startindex += str1.Length + 1;
                if (i == 5)
                {
                    break;
                }
            }

            UpdateRichEditBlock();
        }

        internal void updatederrorword()
        {
            string[] words = Editor.Text.Split(' ');
            int startindex = 0;
            m_errorlist.Clear();
            foreach (string str1 in words)
            {

                if (spellChecker != null && str1.EndsWith("."))
                {
                    string substr = str1.Remove(str1.Length - 1);
                    if (spellChecker.HasError(substr) && !ignorewords.Contains(substr))
                    {
                        m_errorlist.Add(startindex, substr);
                    }
                }
                else if (spellChecker != null && spellChecker.HasError(str1) && !ignorewords.Contains(str1))
                {
                    m_errorlist.Add(startindex, str1);

                }

                startindex += str1.Length + 1;

            }
            UpdateRichEditBlock();
        }
        private void UpdateRichEditBlock()
        {
            int m = 0;
            string str = string.Empty;
            richText.Document.Selection.CharacterFormat.BackgroundColor = Colors.White;
            Paragraph paragraph = new Paragraph();
            //richText.Blocks.Add(paragraph);
            str = Editor.Text;
           
            foreach (KeyValuePair<int, object> pair in m_errorlist)
            {
                if (currenterrorword == m)
                {
                    errorword = pair.Value.ToString();
                    _SubStrlen = pair.Key;
                }
                m++;
            }
            if (!string.IsNullOrEmpty(errorword))
            {
                string str3 = Editor.Text.Substring(0, _SubStrlen);
                paragraph.Inlines.Add(CreateSpan(str3));
                paragraph.Inlines.Add(CreateErrorSpan(errorword));
                str = Editor.Text.Substring(_SubStrlen + errorword.Length);
                paragraph.Inlines.Add(CreateSpan(str));
                richText.Document.SetText(Windows.UI.Text.TextSetOptions.None, str3 + errorword + str);
                richText.Document.Selection.StartPosition = _SubStrlen;
                richText.Document.Selection.EndPosition = _SubStrlen + errorword.Length;
                richText.Document.Selection.CharacterFormat.BackgroundColor = Color.FromArgb(255, 255, 204, 204);
               
            }
            else
            {
                richText.Document.SetText(Windows.UI.Text.TextSetOptions.None, str);
            }
            if(spellChecker != null)
            GetSuggestion();
        }
        private void UpdateSelection()
        {
            if (currenterrorword == 0)
            {

                dispatcherTimer.Tick += DispatcherTimer_Tick;
                dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
                dispatcherTimer.Start();

            }
            currenterrorword++;
            UpdateRichEditBlock();
        }

        private void DispatcherTimer_Tick(object sender, object e)
        {
            updatederrorword();
            dispatcherTimer.Stop();
            dispatcherTimer.Tick -= DispatcherTimer_Tick;
            dispatcherTimer = null;
        }

       
        private Span CreateErrorSpan(string errorword)
        {
            Span span = new Span();
            Run run = new Run();
            run.Foreground = new SolidColorBrush(Colors.Red);
            run.Text = errorword;
            span.Inlines.Add(run);
            return span;
        }
        private void updateselection()
        {
            currenterrorword++;
            UpdateRichEditBlock();

        }
        private Span CreateSpan(string Text)
        {
            Span span = new Span();
            Run run = new Run();
            run.Foreground = new SolidColorBrush(Colors.Black);
            run.Text = Text;
            span.Inlines.Add(run);
            return span;
        }

        internal void GetSuggestion()
        {

            Suggestions = new ObservableCollection<string>();
            Dictionary<string, List<string>> dicionary = spellChecker.GetSuggestions(errorword);
            foreach (KeyValuePair<string, List<string>> pair in dicionary)
            {

                for (int i = 0; i < pair.Value.Count; i++)
                {
                    Suggestions.Add(pair.Value[i]);
                }
                if(Suggestions.Count > 0)
                {
                    replaceBtn.IsEnabled = true;
                    replaceAllBtn.IsEnabled = true;
                }
                if (Suggestions.Count == 0)
                {
                    GetAnagrams();
                }
            }
        }

        private void GetAnagrams()
        {
            replaceBtn.IsEnabled = true;
            replaceAllBtn.IsEnabled = true;
            Suggestions = new ObservableCollection<string>();
            List<string> m_anagram = spellChecker.GetAnagrams(errorword);
            for (int i = 0; i < m_anagram.Count; i++)
            {
                Suggestions.Add(m_anagram[i]);
            }
            if (Suggestions.Count == 0)
            {
                GetPhoneticWords();
            }
        }

        private void GetPhoneticWords()
        {
            Suggestions = new ObservableCollection<string>();
            List<string> m_phoneticlist = spellChecker.GetPhoneticWords(errorword);
            for (int i = 0; i < m_phoneticlist.Count; i++)
            {
                Suggestions.Add(m_phoneticlist[i]);
            }
            if (Suggestions.Count == 0)
            {
                m_phoneticlist.Clear();
                Suggestions = new ObservableCollection<string>();
                m_phoneticlist = spellChecker.GetPhoneticWords(errorword, AccuracyLevels.High);
                for (int i = 0; i < m_phoneticlist.Count; i++)
                {
                    Suggestions.Add(m_phoneticlist[i]);
                }

            }
            if (Suggestions.Count == 0)
            {
                Suggestions.Add("(" +localizedSuggestionText+ ")");
                replaceBtn.IsEnabled = false;
                replaceAllBtn.IsEnabled = false;
            }

        }

        /// <summary>
        /// Disposes the instance of SpellCheckerDialog
        /// </summary>
        [ClassReference(IsReviewed = false)]
        public void Dispose()
        {
            if (_btnclose != null)
            {
                _btnclose.PointerEntered -= Button_PointerEntered;
                _btnclose.PointerExited -= _btnclose_PointerExited;
                _btnclose.Click -= _btnclose_Click;
                _btnclose = null;
            }
            lstBox.LayoutUpdated -= lstBox_LayoutUpdated;

            if(dispatcherTimer != null)
            {
                dispatcherTimer.Stop();
                dispatcherTimer.Tick -= DispatcherTimer_Tick;
                dispatcherTimer = null;
            }

            if(m_errorlist != null)
            {
                m_errorlist.Clear();
                m_errorlist = null;
            }

            if(Suggestions != null)
            {
                Suggestions.Clear();
                Suggestions = null;
            }
            SpellCheckCompleted = null;
            spellChecker = null;
        }
        #endregion

        #region Override Methods

        private void CloseDialog(object sender, RoutedEventArgs e)
        {
            spellChecker.dialog.IsOpen = false;
            if (Window.Current.Content != null)
                Window.Current.Content.IsHitTestVisible = true;
            this.Dispose();
        }

        private void lstBox_LayoutUpdated(object sender, object e)
        {
            if (lstBox.Items.Count > 0 && lstBox.SelectedIndex == -1)
                lstBox.SelectedIndex = 0;
        }

        private void Spell_Ignore(object sender, RoutedEventArgs e)
        {
            UpdateSelection();
            if (currenterrorword >= m_errorlist.Count)
            {
                spellChecker.dialog.IsOpen = false;
                if(Window.Current.Content != null)
                Window.Current.Content.IsHitTestVisible = true;
                OnSpellCheckCompleted();
            }

        }

        private void OnSpellCheckCompleted()
        {
            if (SpellCheckCompleted != null)
                SpellCheckCompleted(this, EventArgs.Empty);
            Dispose();
        }

        private void Spell_IgnoreAll(object sender, RoutedEventArgs e)
        {
            if (currentignoreword == 0 && currenterrorword == 0)
            {
                ignorewords.Add(errorword);
                UpdateRichEditBlock();
                RemoveByValue(m_errorlist, errorword);
                if (currentignoreword == 0)
                    updatederrorword();
                currentignoreword++;
            }
            else
            {
                ignorewords.Add(errorword);
                RemoveByValue(m_errorlist, errorword);
                UpdateRichEditBlock();
            }
            if (currenterrorword >= m_errorlist.Count)
            {
                spellChecker.dialog.IsOpen = false;
                if (Window.Current.Content != null)
                    Window.Current.Content.IsHitTestVisible = true;
                OnSpellCheckCompleted();
            }
        }
        private static void RemoveByValue<TKey, TValue>(Dictionary<TKey, TValue> dictionary, TValue someValue)
        {
            List<TKey> itemsToRemove = new List<TKey>();

            foreach (var pair in dictionary)
            {
                if (pair.Value.Equals(someValue))
                    itemsToRemove.Add(pair.Key);
            }

            foreach (TKey item in itemsToRemove)
            {
                dictionary.Remove(item);
            }
        }
        private void Spell_ReplaceAll(object sender, RoutedEventArgs e)
        {
            if (lstBox.SelectedItem != null && lstBox.SelectedItem.ToString() != localizedSuggestionText)
            {
                Editor.Text = Editor.Text.Replace(errorword, lstBox.SelectedItem.ToString());
            }
            updatederrorword();
            if (currenterrorword >= m_errorlist.Count)
            {
                spellChecker.dialog.IsOpen = false;
                if (Window.Current.Content != null)
                    Window.Current.Content.IsHitTestVisible = true;
                OnSpellCheckCompleted();
            }
        }

        private void Spell_AddToDictionary(object sender, RoutedEventArgs e)
        {
            spellChecker.checkerbase.AddWordToDictionary(errorword);
            updatederrorword();
            if (currenterrorword >= m_errorlist.Count)
            {
                spellChecker.dialog.IsOpen = false;
                if (Window.Current.Content != null)
                    Window.Current.Content.IsHitTestVisible = true;
                OnSpellCheckCompleted();              

            }
        }

        private async void Spell_Replace(object sender, RoutedEventArgs e)
        {
            if (lstBox.SelectedItem != null && lstBox.SelectedItem.ToString() != localizedSuggestionText)
            {
                string sub = Editor.Text.Substring(0, _SubStrlen + errorword.Length);
                string sub1 = Editor.Text.Substring(_SubStrlen + errorword.Length);
                sub = sub.Replace(errorword, lstBox.SelectedItem.ToString());
                Editor.Text = sub + sub1;
            }
           await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High, ()=>
            {
                updatederrorword();
            });
            if (currenterrorword >= m_errorlist.Count)
            {
                spellChecker.dialog.IsOpen = false;
                if (Window.Current.Content != null)
                    Window.Current.Content.IsHitTestVisible = true;
                OnSpellCheckCompleted();
            }
        }

        private void Button_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            _closeforground.Foreground = new SolidColorBrush(Colors.White);
            _btnclose.Background = new SolidColorBrush(Colors.Red);
            
        }

        private void _btnclose_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            _btnclose.Background = new SolidColorBrush(Colors.Transparent);
            _closeforground.Foreground = new SolidColorBrush(Colors.Black);
        }
        private void _btnclose_Click(object sender, RoutedEventArgs e)
        {
            spellChecker.dialog.IsOpen = false;
            if (Window.Current.Content != null)
                if (Window.Current.Content != null)
                    Window.Current.Content.IsHitTestVisible = true;
        }

        #endregion
    }
}
