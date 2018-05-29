using System;
using System.Data;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using NSDMasterInventorySF.Properties;
using Syncfusion.SfSkinManager;

namespace NSDMasterInventorySF
{
	/// <summary>
	///     Interaction logic for FindAndReplace.xaml
	/// </summary>
	public partial class FindAndReplace : Window
	{
		public static RoutedCommand CloseWindow = new RoutedCommand();
		private readonly MainWindow _window;

		private string _currentVisualStyle;

		public FindAndReplace(MainWindow window)
		{
			CloseWindow.InputGestures.Add(new KeyGesture(Key.Escape));
			CommandBindings.Add(new CommandBinding(CloseWindow, CloseCurrentWindow));

			InitializeComponent();
			_window = window;
			FindBox.Focus();
		}

		public string CurrentVisualStyle
		{
			get => _currentVisualStyle;
			set
			{
				_currentVisualStyle = value;
				OnVisualStyleChanged();
			}
		}

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			CurrentVisualStyle = Settings.Default.Theme;
		}

		private void RealReplace()
		{
			if (string.IsNullOrEmpty(FindBox.Text))
			{
				MessageBox.Show("Please fill out the \"Find\" text box.", "Error", MessageBoxButton.OK,
					MessageBoxImage.Information);
				return;
			}

			if ((bool) AllTablesCheckBox.IsChecked)
				foreach (DataTable table in MainWindow.MasterDataSet.Tables)
					FindReplace(table, FindBox.Text, ReplaceBox.Text, (bool) MatchEntireContentsCheckBox.IsChecked,
						(bool) MatchCaseCheckBox.IsChecked);
			else
				FindReplace(MainWindow.MasterDataSet.Tables[_window.MasterTabControl.SelectedIndex], FindBox.Text, ReplaceBox.Text,
					(bool) MatchEntireContentsCheckBox.IsChecked, (bool) MatchCaseCheckBox.IsChecked);

			//_window.RevertChanges.IsEnabled = true;
		}

		public void FindReplace(DataTable dt, string find, string replace, bool isWholeCell, bool matchCase)
		{
			if (isWholeCell)
				foreach (DataRow row in dt.Rows)
					for (var i = 0; i < row.ItemArray.Length; i++)
						if (matchCase)
						{
							if (row[i].ToString().Trim().Equals(find)) row[i] = replace;
						}
						else
						{
							if (row[i].ToString().Trim().ToLower().Equals(find.Trim().ToLower())) row[i] = replace;
						}
			else
				foreach (DataRow row in dt.Rows)
					for (var i = 0; i < row.ItemArray.Length; i++)
						if (matchCase)
						{
							if (row[i].ToString().Trim().Contains(find.Trim())) row[i] = row[i].ToString().Trim().Replace(find.Trim(), replace.Trim());
						}
						else
						{
							if (row[i].ToString().Trim().ToLower().Contains(find.Trim().ToLower()))
								row[i] = Regex.Replace(row[i].ToString().Trim(), find.Trim(), replace.Trim(), RegexOptions.IgnoreCase);
						}
		}

		private void CloseCurrentWindow(object sender, EventArgs e)
		{
			Close();
		}

		private void Replace(object sender, RoutedEventArgs e)
		{
			RealReplace();
		}


		private void ReplaceBox_OnKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter) RealReplace();
		}

		private void OnVisualStyleChanged()
		{
			Enum.TryParse(CurrentVisualStyle, out VisualStyles visualStyle);
			if (visualStyle == VisualStyles.Default) return;
			SfSkinManager.ApplyStylesOnApplication = true;
			SfSkinManager.SetVisualStyle(this, visualStyle);
			SfSkinManager.ApplyStylesOnApplication = false;
		}
	}
}