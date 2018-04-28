using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NSDMasterInventorySF.Properties;
using Syncfusion.SfSkinManager;

namespace NSDMasterInventorySF
{
	/// <summary>
	///     Interaction logic for StyleChooser.xaml
	/// </summary>
	public partial class StyleChooser : Window
	{
		public static RoutedCommand CloseWindow = new RoutedCommand();

		private string _currentVisualStyle;

		public StyleChooser()
		{
			CloseWindow.InputGestures.Add(new KeyGesture(Key.Escape));
			CommandBindings.Add(new CommandBinding(CloseWindow, CloseCurrentWindow));

			InitializeComponent();

			foreach (object style in Enum.GetValues(typeof(VisualStyles))) StylesListBox.Items.Add(style.ToString());
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

		private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
		{
			Settings.Default.Theme = StylesListBox.SelectedItem.ToString();
			Settings.Default.Save();

			App.Restart();
		}

		private void StylesListBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			SelectButton.IsEnabled = StylesListBox.SelectedItems.Count > 0;
		}

		private void CloseCurrentWindow(object sender, EventArgs e)
		{
			Close();
		}

		private void OnVisualStyleChanged()
		{
			Enum.TryParse(CurrentVisualStyle, out VisualStyles visualStyle);
			if (visualStyle == VisualStyles.Default) return;
			SfSkinManager.ApplyStylesOnApplication = true;
			SfSkinManager.SetVisualStyle(this, visualStyle);
			SfSkinManager.ApplyStylesOnApplication = false;
		}

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			CurrentVisualStyle = Settings.Default.Theme;
		}
	}
}