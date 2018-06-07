using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using NSDMasterInventorySF.Properties;
using Syncfusion.SfSkinManager;

namespace NSDMasterInventorySF
{
	/// <summary>
	///     Interaction logic for EditTable.xaml
	/// </summary>
	public partial class EditTable
	{
		public static RoutedCommand CloseWindow = new RoutedCommand();
		private readonly string _originalName;
		private readonly bool _showWarning;

		private string _currentVisualStyle;
		private readonly bool _showPrefabs;
		private readonly bool _isCreatingSchema;

		public EditTable(bool showWarning, string originalName, bool showPrefabs, bool isCreatingSchema)
		{
			InitializeComponent();

			//SkinStorage.SetVisualStyle(this, "Metro");
			CloseWindow.InputGestures.Add(new KeyGesture(Key.Escape));
			CommandBindings.Add(new CommandBinding(CloseWindow, CloseCurrentWindow));
			using (var conn = new SqlConnection(App.ConnectionString))
			{
				conn.Open();
				foreach (string tableName in App.GetTableNames(conn, $"{Settings.Default.Schema}_PREFABS"))
					PrefabComboBox.Items.Add(tableName);
				conn.Close();
			}

			if (PrefabComboBox.Items.Count <= 0)
			{
				MessageBox.Show("Please create a Prefab first.", "Cannot create table", MessageBoxButton.OK,
					MessageBoxImage.Warning);
				Close();
				return;
			}

			_showWarning = showWarning;
			_originalName = originalName;
			_showPrefabs = showPrefabs;
			_isCreatingSchema = isCreatingSchema;

			TableNameBox.TextChanged += (sender, args) =>
			{
				if (showPrefabs)
					ChooseButton.IsEnabled =
						!string.IsNullOrEmpty(TableNameBox.Text) && PrefabComboBox.SelectedIndex >= 0;
				else
					ChooseButton.IsEnabled = !string.IsNullOrEmpty(TableNameBox.Text);
			};
			if (showPrefabs)
			{
				PrefabComboBox.SelectionChanged += (sender, args) =>
				{
					ChooseButton.IsEnabled =
						!string.IsNullOrEmpty(TableNameBox.Text) && PrefabComboBox.SelectedIndex >= 0;
				};
				PrefabComboBox.SelectedValue = PrefabComboBox.Items[0];
			}
			else
			{
				PrefabComboBox.Visibility = Visibility.Hidden;
				PrefabComboBox.IsDropDownOpen = false;
				TableNameBox.Focus();
			}

			if (string.IsNullOrEmpty(originalName)) return;
			TableNameBox.Focus();
			TableNameBox.SelectAll();
			var i = 0;
			GotFocus += (sender, args) =>
			{
				if (i == 0)
				{
					TableNameBox.Focus();
					TableNameBox.SelectAll();
				}

				i++;
			};
			TableNameBox.Text = originalName;

			using (var conn = new SqlConnection(App.ConnectionString))
			{
				conn.Open();
				//Debug.WriteLine(App.GetPrefabOfTable(conn, originalName));
				PrefabComboBox.SelectedValue = App.GetPrefabOfTable(conn, originalName);
				//Debug.WriteLine(App.GetPrefabOfTable(conn, originalName));
				conn.Close();
			}

			if (string.IsNullOrEmpty(originalName) || PrefabComboBox.SelectedIndex < 0) ChooseButton.IsEnabled = false;
		}

		public static string PrefabSelected { get; set; }
		public static string TableName { get; set; }

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

		private void DoneButtonOnClicked(object sender, RoutedEventArgs e)
		{
			if (!_showPrefabs && _isCreatingSchema)
			{
				using (var conn = new SqlConnection(App.ConnectionString))
				{
					conn.Open();
					if (App.GetAllNames(conn, "schemas").Contains(TableNameBox.Text))
					{
						MessageBox.Show("Identical schema exists. Please enter a different name.",
							"Cannot create schema", MessageBoxButton.OK, MessageBoxImage.Exclamation);
						return;
					}
					using (var comm = new SqlCommand($"CREATE SCHEMA [{TableNameBox.Text}]", conn))
						comm.ExecuteNonQuery();
					conn.Close();
				}

				Close();
				return;
			}

			if (_showWarning)
				if (MessageBox.Show(
					    "Are you sure you would like to change this table? It could result in permanent loss of data (columns).",
					    "Are you sure?", MessageBoxButton.YesNo, MessageBoxImage.Exclamation) != MessageBoxResult.Yes)
					return;

			if (string.IsNullOrEmpty(PrefabComboBox.SelectedValue.ToString()))
			{
				MessageBox.Show("Please select a Prefab.", "Error", MessageBoxButton.OK, MessageBoxImage.Stop);
				return;
			}

			if (!string.IsNullOrEmpty(_originalName))
				using (var conn = new SqlConnection(App.ConnectionString))
				{
					conn.Open();
					using (var comm =
						new SqlCommand(
							$"sp_rename \'{Settings.Default.Schema}.{_originalName}\', \'{TableNameBox.Text}\'",
							conn))
					{
						comm.ExecuteNonQuery();
					}

					conn.Close();
				}

			PrefabSelected = PrefabComboBox.Text;
			TableName = TableNameBox.Text;
			Close();
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
	}
}