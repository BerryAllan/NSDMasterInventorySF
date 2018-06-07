using System;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NSDMasterInventorySF.Properties;
using Syncfusion.SfSkinManager;
using Syncfusion.Windows.Controls.Input;
using Syncfusion.Windows.Tools.Controls;

namespace NSDMasterInventorySF
{
	/// <summary>
	///     Interaction logic for ConnectionManager.xaml
	/// </summary>
	public partial class DatabaseManagerError
	{
		public static RoutedCommand CloseWindow = new RoutedCommand();

		private string _currentVisualStyle;

		public DatabaseManagerError()
		{
			CloseWindow.InputGestures.Add(new KeyGesture(Key.Escape));
			CommandBindings.Add(new CommandBinding(CloseWindow, CloseCurrentWindow));

			//SkinStorage.SetVisualStyle(this, Settings.Default.Theme);
			InitializeComponent();

			ServerBox.Focus();
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

		private void ConnectClicked(object sender, RoutedEventArgs e)
		{
			try
			{
				Settings.Default.Server = ServerBox.Text;
				Settings.Default.Database = DatabaseComboBox.Text;
				Settings.Default.UserID = UserIdBox.Text;
				Settings.Default.Password = PasswordBox.Password;
				Settings.Default.Schema = SchemaComboBox.Text;
				Settings.Default.Save();

				App.ConnectionString =
					$"Server={Settings.Default.Server};Database={Settings.Default.Database};User ID={Settings.Default.UserID};Password={Settings.Default.Password};";
			}
			catch (SqlException)
			{
				MessageBox.Show("Could not connect to specified server; Ensure all fields contain correct information.",
					"Error",
					MessageBoxButton.OK, MessageBoxImage.Error);
			}
			App.Restart();
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

		private void CancelClicked(object sender, RoutedEventArgs e)
		{
			Application.Current.Shutdown();
		}
	}
}