using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using NSDMasterInventorySF.Properties;
using Syncfusion.SfSkinManager;
using Syncfusion.Windows.Tools.Controls;
using SelectionChangedEventArgs = System.Windows.Controls.SelectionChangedEventArgs;

namespace NSDMasterInventorySF
{
	/// <inheritdoc />
	/// <summary>
	///     Interaction logic for TableManager.xaml
	/// </summary>
	public partial class TableManager
	{
		public static RoutedCommand CloseWindow = new RoutedCommand();
		private readonly MainWindow _window;

		private string _currentVisualStyle;

		public TableManager(MainWindow window)
		{
			CloseWindow.InputGestures.Add(new KeyGesture(Key.Escape));
			CommandBindings.Add(new CommandBinding(CloseWindow, CloseCurrentWindow));

			InitializeComponent();

			PopulateListBox();

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

		public void PopulateListBox()
		{
			SheetListBox.Items.Clear();

			using (var conn = new SqlConnection(App.ConnectionString))
			{
				conn.Open();
				foreach (string s in App.GetTableNames(conn)) SheetListBox.Items.Add(s);

				conn.Close();
			}
		}

		private void SheetListBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (SheetListBox.SelectedItems.Count > 0)
			{
				Edit.IsEnabled = true;
				RemoveSheetButton.IsEnabled = true;
			}
			else
			{
				Edit.IsEnabled = false;
				RemoveSheetButton.IsEnabled = false;
			}
		}

		private void DeleteSelectedItem()
		{
			MessageBoxResult box = MessageBox.Show("Are you sure you would like to delete this table? This action cannot be undone.",
				"Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning);

			if (box == MessageBoxResult.Yes)
			{
				using (var conn = new SqlConnection(App.ConnectionString))
				{
					conn.Open();
					if (App.GetTableNames(conn).Contains(SheetListBox.SelectedItem))
						using (var comm = new SqlCommand(
							$"DROP TABLE [{Settings.Default.Schema}].[{SheetListBox.SelectedItem}]",
							conn))
						{
							comm.ExecuteNonQuery();
						}

					conn.Close();
				}

				SheetListBox.Items.Remove(SheetListBox.SelectedItem);
			}

			//_window.InitializeOrRefreshEverything(_window.MasterTabControl.SelectedIndex);
		}

		private void RemoveSheetButton_OnClick(object sender, RoutedEventArgs e)
		{
			DeleteSelectedItem();
		}

		private void SheetListBox_OnKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Delete) DeleteSelectedItem();
		}

		private void SheetManager_OnClosed(object sender, EventArgs e)
		{
			_window.InitializeOrRefreshEverything(_window.MasterTabControl.SelectedIndex);
		}

		private void CloseCurrentWindow(object sender, EventArgs e)
		{
			Close();
		}

		private void EditButton_OnClick(object sender, RoutedEventArgs e)
		{
			string tableName = SheetListBox.SelectedItem.ToString();
			var prefabChooser = new EditTable(_window, true, tableName, this)
			{
				Owner = this,
				ShowInTaskbar = false
			};
			prefabChooser.ShowDialog();

			if (string.IsNullOrEmpty(EditTable.PrefabSelected)) return;

			using (var conn = new SqlConnection(App.ConnectionString))
			{
				conn.Open();
				DataTable prefabTable = App.GetPrefabDataTable(conn, "PREFABS", EditTable.PrefabSelected);

				if (!App.GetTableNames(conn).Contains(EditTable.TableName))
					using (var comm = new SqlCommand())
					{
						comm.Connection = conn;
						//Debug.WriteLine(Path.GetFileNameWithoutExtension(file));
						comm.CommandText = $"CREATE TABLE IF NOT EXISTS [{Settings.Default.Schema}].[{EditTable.TableName}] ( ";
						for (var j = 0; j < prefabTable.Rows.Count; j++)
							if (j != prefabTable.Rows.Count - 1)
								comm.CommandText += $"[{prefabTable.Rows[j]["COLUMNS"]}] TEXT, ";
							else
								comm.CommandText += $"[{prefabTable.Rows[j]["COLUMNS"]}] TEXT";

						comm.CommandText += " )";

						comm.ExecuteNonQuery();
					}

				var k = 0;
				foreach (string column in App.GetAllColumnsOfTable(conn, tableName))
				{
					using (var comm =
						new SqlCommand(
							$"sp_rename \'{Settings.Default.Schema}.{tableName}.{column}\', \'C_-_O_-_L_-_U_-_M_-_N_-_{k}\', \'COLUMN\'", conn))
					{
						comm.ExecuteNonQuery();
					}

					k++;
				}

				//Debug.WriteLine("table: " + table);
				List<string> columns = App.GetAllColumnsOfTable(conn, EditTable.TableName);

				for (var i = 0; i < prefabTable.Rows.Count; i++)
					//Debug.WriteLine("   column: " + column);
					//Debug.WriteLine("      rename column: " + fieldsProp.Get("field" + i));

					if (i < columns.Count)
						using (var comm =
							new SqlCommand(
								$"sp_rename \'{Settings.Default.Schema}.{EditTable.TableName}.{columns[i]}\', \'{prefabTable.Rows[i]["COLUMNS"]}\', \'COLUMN\'", conn))
						{
							comm.ExecuteNonQuery();
						}
					else
						using (var comm =
							new SqlCommand(
								$"ALTER TABLE [{Settings.Default.Schema}].[{EditTable.TableName}]  ADD [{prefabTable.Rows[i]["COLUMNS"]}] TEXT",
								conn))
						{
							comm.ExecuteNonQuery();
						}

				if (columns.Count > prefabTable.Rows.Count)
					for (int i = prefabTable.Rows.Count; i < columns.Count; i++)
						using (var comm =
							new SqlCommand(
								$"ALTER TABLE [{Settings.Default.Schema}].[{EditTable.TableName}]  DROP COLUMN [{columns[i]}]",
								conn))
						{
							comm.ExecuteNonQuery();
						}

				conn.Close();
			}

			EditTable.PrefabSelected = string.Empty;
			EditTable.TableName = string.Empty;
		}

		private void OnNewButtonClick(object sender, EventArgs e)
		{
			var choosePrefab = new EditTable(_window, false, string.Empty, this)
			{
				Owner = this,
				ShowInTaskbar = false
			};
			choosePrefab.ShowDialog();
			string tableName = EditTable.TableName;

			if (string.IsNullOrEmpty(EditTable.PrefabSelected)) return;
			if (string.IsNullOrEmpty(EditTable.TableName)) return;

			List<string> headers = (from TabItemExt tab in _window.MasterTabControl.Items select tab.Header.ToString()).ToList();
			headers.Add(tableName);
			headers.Sort();

			var table = new DataTable();
			using (var conn = new SqlConnection(App.ConnectionString))
			{
				conn.Open();
				DataTable prefabTable = App.GetPrefabDataTable(conn, "PREFABS", EditTable.PrefabSelected);
				for (var i = 0; i < prefabTable.Rows.Count; i++)
					table.Columns.Add(new DataColumn(prefabTable.Rows[i]["COLUMNS"].ToString()));
				conn.Close();
			}

			table.TableName = EditTable.TableName;

			var tabItemExt = new TabItemExt {Header = tableName};
			_window.MasterTabControl.Items.Insert(_window.MasterTabControl.Items.Count, tabItemExt);
			_window.WriteToDataGrid(table, EditTable.PrefabSelected, tabItemExt);

			_window.SaveToDb(true);
			//_window.InitializeOrRefreshEverything(_window.MasterTabControl.SelectedIndex);

			PopulateListBox();

			EditTable.PrefabSelected = string.Empty;
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