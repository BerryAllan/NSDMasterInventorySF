using System;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NSDMasterInventorySF.Properties;
using Syncfusion.SfSkinManager;

namespace NSDMasterInventorySF
{
	/// <summary>
	///     Interaction logic for PrefabManager.xaml
	/// </summary>
	public partial class PrefabManager
	{
		public static RoutedCommand CloseWindow = new RoutedCommand();
		private readonly MainWindow _window;

		private string _currentVisualStyle;

		public PrefabManager(MainWindow window)
		{
			CloseWindow.InputGestures.Add(new KeyGesture(Key.Escape));
			CommandBindings.Add(new CommandBinding(CloseWindow, CloseCurrentWindow));

			InitializeComponent();
			_window = window;
			PopulateListBox();

			//GotFocus += (sender, args) => PopulateListBox();
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

		public void PopulateListBox()
		{
			PrefabListBox.Items.Clear();

			using (var conn = new SqlConnection(App.ConnectionString))
			{
				conn.Open();
				foreach (string tableName in App.GetTableNames(conn, "PREFABS"))
					if (!string.IsNullOrEmpty(tableName) &&
					    !tableName.Equals("ComboBoxes"))
						PrefabListBox.Items.Add(tableName);

				conn.Close();
			}

			PrefabListBox.SelectionMode = SelectionMode.Single;
		}

		private void OpenAddPrefabBuilder(object sender, RoutedEventArgs e)
		{
			var prefabBuilder = new PrefabBuilder
			{
				Owner = this,
				ShowInTaskbar = false
			};
			prefabBuilder.ShowDialog();
		}

		private void OpenEditPrefabBuilder(object sender, RoutedEventArgs e)
		{
			var prefabBuilder = new PrefabBuilder(PrefabListBox.SelectedItem.ToString(), _window)
			{
				Owner = this,
				ShowInTaskbar = false
			};
			prefabBuilder.ShowDialog();
		}

		private void RemovePrefab(object sender, RoutedEventArgs e)
		{
			if (MessageBox.Show("Are you sure?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning) !=
			    MessageBoxResult.Yes) return;

			using (var conn = new SqlConnection(App.ConnectionString))
			{
				conn.Open();

				if (App.GetTableNames(conn, "PREFABS").Contains(PrefabListBox.SelectedItem))
					using (var comm = new SqlCommand($"DROP TABLE PREFABS.[{PrefabListBox.SelectedItem}]", conn))
					{
						comm.ExecuteNonQuery();
					}

				if (App.GetTableNames(conn, "COMBOBOXES").Contains(PrefabListBox.SelectedItem))
					using (var comm = new SqlCommand($"DROP TABLE COMBOBOXES.[{PrefabListBox.SelectedItem}]", conn))
					{
						comm.ExecuteNonQuery();
					}

				conn.Close();
			}

			PopulateListBox();
		}

		private void PrefabManager_OnClosed(object sender, EventArgs e)
		{
			_window.InitializeOrRefreshEverything(_window.MasterTabControl.SelectedIndex);
		}

		private void PrefabListBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (PrefabListBox.SelectedItems.Count > 0)
			{
				EditButton.IsEnabled = true;
				RemoveButton.IsEnabled = PrefabListBox.Items.Count > 1;
			}
			else
			{
				EditButton.IsEnabled = false;
				RemoveButton.IsEnabled = false;
			}
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