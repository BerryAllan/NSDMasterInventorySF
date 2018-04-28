using System;
using System.Data.SqlClient;
using System.Diagnostics;
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
	///     Interaction logic for DatabaseManager.xaml
	/// </summary>
	public partial class DatabaseManager : Window
	{
		public static RoutedCommand CloseWindow = new RoutedCommand();
		private readonly MainWindow _window;

		private string _currentVisualStyle;

		public DatabaseManager(MainWindow window)
		{
			CloseWindow.InputGestures.Add(new KeyGesture(Key.Escape));
			CommandBindings.Add(new CommandBinding(CloseWindow, CloseCurrentWindow));

			//SkinStorage.SetVisualStyle(this, Settings.Default.Theme);
			InitializeComponent();

			ServerBox.Text = Settings.Default.Server;
			UserIDBox.Text = Settings.Default.UserID;

			DatabaseComboBox.Text = Settings.Default.Database;
			SchemaComboBox.Text = Settings.Default.Schema;

			ServerBox.Focus();

			_window = window;
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

		private void OnLostFocus(object sender, RoutedEventArgs e)
		{
			TryFillDbAutoComplete();
			TryFillSchemaAutoComplete();
		}

		private void TryFillDbAutoComplete()
		{
			string dbText = DatabaseComboBox.SelectedValue?.ToString();
			DatabaseComboBox.Items.Clear();

			if (string.IsNullOrEmpty(ServerBox.Text) ||
			    string.IsNullOrEmpty(UserIDBox.Text) || string.IsNullOrEmpty(PasswordBox.Password))
				return;

			try
			{
				using (var conn = new SqlConnection($"Server={ServerBox.Text};User ID={UserIDBox.Text};Password={PasswordBox.Password};"))
				{
					conn.Open();
					foreach (string s in App.GetAllNames(conn, "databases"))
						DatabaseComboBox.Items.Add(s);
					conn.Close();
				}
			}
			catch (SqlException)
			{
				Debug.WriteLine("failed DB complete");
				//MessageBox.Show("Error refreshing Database list; Ensure all fields contain correct information.", "",
				//	MessageBoxButton.OK, MessageBoxImage.Error);
			}

			if (DatabaseComboBox.Items.Contains(dbText) && !string.IsNullOrEmpty(dbText))
				DatabaseComboBox.SelectedValue = dbText;
		}

		private void TryFillSchemaAutoComplete()
		{
			string schemaText = SchemaComboBox.SelectedValue?.ToString();
			SchemaComboBox.Items.Clear();

			if (string.IsNullOrEmpty(ServerBox.Text) || string.IsNullOrEmpty(DatabaseComboBox.Text) ||
			    string.IsNullOrEmpty(UserIDBox.Text) || string.IsNullOrEmpty(PasswordBox.Password))
				return;

			try
			{
				using (var conn = new SqlConnection(
					$"Server={ServerBox.Text};Database={DatabaseComboBox.Text};User ID={UserIDBox.Text};Password={PasswordBox.Password};"))
				{
					conn.Open();
					foreach (string s in App.GetAllNames(conn, "schemas"))
						SchemaComboBox.Items.Add(s);
					conn.Close();
				}
			}
			catch (SqlException)
			{
				Debug.WriteLine("failed SC complete");
				//MessageBox.Show("Error refreshing Schema list; Ensure all fields contain correct information.", "",
				//	MessageBoxButton.OK, MessageBoxImage.Error);
			}

			if (SchemaComboBox.Items.Contains(schemaText) && !string.IsNullOrEmpty(schemaText))
				SchemaComboBox.SelectedValue = schemaText;
		}

		private void ConnectClicked(object sender, RoutedEventArgs e)
		{
			try
			{
				using (var conn = new SqlConnection(
					$"Server={ServerBox.Text};Database={DatabaseComboBox.Text};User ID={UserIDBox.Text};Password={PasswordBox.Password};"))
				{
					if (!SchemaComboBox.Items.Contains(SchemaComboBox.Text))
					{
						conn.Open();

						using (var comm = new SqlCommand($"CREATE SCHEMA [{SchemaComboBox.Text}]", conn))
						{
							comm.ExecuteNonQuery();
						}

						conn.Close();
					}
				}

				Settings.Default.Server = ServerBox.Text;
				Settings.Default.Database = DatabaseComboBox.Text;
				Settings.Default.UserID = UserIDBox.Text;
				Settings.Default.Password = PasswordBox.Password;
				Settings.Default.Schema = SchemaComboBox.Text;
				Settings.Default.Save();

				App.ConnectionString =
					$"Server={Settings.Default.Server};Database={Settings.Default.Database};User ID={Settings.Default.UserID};Password={Settings.Default.Password};";

				_window.InitializeOrRefreshEverything(_window.MasterTabControl.SelectedIndex);

				Close();
			}
			catch (SqlException)
			{
				MessageBox.Show("Could not connect to specified server; Ensure all fields contain correct information.", "",
					MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void CloseCurrentWindow(object sender, EventArgs e)
		{
			Close();
		}

		private void BoxGotFocus(object sender, RoutedEventArgs e)
		{
			((SfTextBoxExt) sender).SelectAll();
		}

		private void AutoCompleteGotFocus(object sender, RoutedEventArgs e)
		{
			(((AutoComplete) sender).Template.FindName("PART_EditableTextBox", (AutoComplete) sender) as TextBox)?.SelectAll();
		}

		private void PasswordBox_OnGotKeyboardFocus(object sender, RoutedEventArgs e)
		{
			((PasswordBox) sender).SelectAll();
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