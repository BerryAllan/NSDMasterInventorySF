using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NSDMasterInventorySF.Properties;
using Syncfusion.SfSkinManager;
using Syncfusion.UI.Xaml.Grid;
using Cursors = System.Windows.Forms.Cursors;
using DataRow = System.Data.DataRow;

namespace NSDMasterInventorySF
{
	/// <inheritdoc cref="Window" />
	/// <summary>
	///     Interaction logic for PrefabBuilder.xaml
	/// </summary>
	public partial class PrefabBuilder
	{
		public static RoutedCommand CloseWindow = new RoutedCommand();
		private DataTable _prefabTable;
		private string _prefabName;
		private PrefabManager _prefabManager;
		private Dictionary<string, string> _changedColumnNames;
		private List<string> _deletedColumns;

		private string _currentVisualStyle;

		public PrefabBuilder(PrefabManager prefabManager)
		{
			InitializePrefabBuilder();

			_prefabTable.Rows.Add(_prefabTable.NewRow());
			Startup(prefabManager);
		}

		public PrefabBuilder(string prefabName, PrefabManager prefabManager)
		{
			InitializePrefabBuilder();

			_prefabName = prefabName;
			Startup(prefabManager);
		}

		private void InitializePrefabBuilder()
		{
			CloseWindow.InputGestures.Add(new KeyGesture(Key.Escape));
			CommandBindings.Add(new CommandBinding(CloseWindow, CloseCurrentWindow));

			InitializeComponent();
			_prefabTable = new DataTable();
		}

		private void Startup(PrefabManager prefabManager)
		{
			ItemNameField.Text = _prefabName;

			FillPrefabDataTable();
			List<DataRow> origRows = new List<DataRow>();
			foreach (DataRow row in _prefabTable.Rows)
			{
				origRows.Add(row);
			}

			_prefabTable.RowChanged += (sender, args) =>
			{
				SaveButton.IsEnabled = !string.IsNullOrEmpty(ItemNameField.Text);
				if (args.Action == DataRowAction.Add)
				{
					args.Row[1] = "TextField";
					_prefabTable.AcceptChanges();
				}
				else
					switch (args.Action)
					{
						case DataRowAction.Change when !origRows.Contains(args.Row):
							return;
						case DataRowAction.Change:
							int index = origRows.IndexOf(args.Row);
							string[] keys = _changedColumnNames.Keys.ToArray();

							_changedColumnNames[keys[index]] = args.Row[0].ToString();
							_prefabTable.AcceptChanges();
							break;
					}

				RowPositionTextBox.MaxValue = _prefabTable.Rows.Count;
			};
			_prefabTable.RowDeleting += (sender, args) =>
			{
				if (origRows.Contains(args.Row))
				{
					_deletedColumns.Add(args.Row[0].ToString());
				}
			};
			_prefabTable.RowDeleted += (sender, args) =>
			{
				SaveButton.IsEnabled = !string.IsNullOrEmpty(ItemNameField.Text);
				_prefabTable.AcceptChanges();
				RowPositionTextBox.MaxValue = _prefabTable.Rows.Count;
			};
			RowPositionTextBox.MaxValue = _prefabTable.Rows.Count;

			PrefabGrid.ItemsSource = _prefabTable;
			var j = 0;
			PrefabGrid.Loaded += (sender, args) =>
			{
				var comboStrings = new HashSet<string>
				{
					"TextField",
					"AutoComplete",
					"CheckBox",
					"ComboBox",
					"DatePicker",
					"Numeric",
					"Currency",
					"Hyperlink",
					"Percentage"
				};
				if (j == 0)
				{
					for (var i = 0; i < _prefabTable.Columns.Count; i++)
					{
						GridColumn column;
						if (i == 1)
						{
							column = new GridComboBoxColumn
							{
								MappingName = _prefabTable.Columns[i].ColumnName,
								ItemsSource = comboStrings,
								StaysOpenOnEdit = true,
								IsEditable = false,
								HeaderText = _prefabTable.Columns[i].ColumnName
							};

							PrefabGrid.Columns.Add(column);
						}
						else if (i > 1)
						{
							column = new GridNumericColumn
							{
								MappingName = _prefabTable.Columns[i].ColumnName,
								HeaderText = _prefabTable.Columns[i].ColumnName,
								NumberDecimalDigits = 0,
								NumberGroupSeparator = ","
							};

							PrefabGrid.Columns.Add(column);
						}
						else
						{
							column = new GridTextColumn
							{
								MappingName = _prefabTable.Columns[i].ColumnName,
								HeaderText = _prefabTable.Columns[i].ColumnName
							};

							PrefabGrid.Columns.Add(column);
						}
					}
				}

				j++;
			};
			_prefabManager = prefabManager;

			_changedColumnNames = new Dictionary<string, string>();
			foreach (DataRow dr in _prefabTable.Rows)
				_changedColumnNames.Add(dr[0].ToString(), dr[0].ToString());
			_deletedColumns = new List<string>();
		}

		public void FillPrefabDataTable()
		{
			using (var conn =
				new SqlConnection(App.ConnectionString))
			{
				conn.Open();
				if (string.IsNullOrEmpty(_prefabName))
				{
					_prefabName = App.RandomString(12);
					_prefabTable = new DataTable(_prefabName);
					_prefabTable.Columns.Add("COLUMNS");
					_prefabTable.Columns.Add("TYPES");
					_prefabTable.Columns.Add("SORTBYS");
					_prefabTable.Columns.Add("GROUPS");
					return;
				}

				using (var cmd = new SqlCommand($"SELECT * FROM [{Settings.Default.Schema}_PREFABS].[{_prefabName}]",
					conn))
				{
					using (var sda = new SqlDataAdapter(cmd))
					{
						_prefabTable.TableName = _prefabName;

						sda.Fill(_prefabTable);
					}
				}

				conn.Close();
			}
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

		private void SavePrefab(object sender, RoutedEventArgs e)
		{
			if (MessageBox.Show(
				    "Are you sure you would like to save these changes?\nColumns of data may be lost (depending on the changes you made), and these actions are irreversible.",
				    "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No) return;
			SaveChanges();
			Close();
		}

		private void SaveChanges()
		{
			System.Windows.Forms.Cursor.Current = Cursors.WaitCursor;
			_prefabTable.AcceptChanges();

			List<string> rows = new List<string>();
			foreach (DataRow row in _prefabTable.Rows)
			{
				rows.Add(row[0].ToString());
			}

			if (rows.Count != rows.Distinct().Count())
			{
				MessageBox.Show(
					"There are duplicate column names. This is disallowed.\nPlease alter the table so there are no duplicates.",
					"Disallowed", MessageBoxButton.OK, MessageBoxImage.Exclamation);
				return;
			}

			using (var conn = new SqlConnection(App.ConnectionString))
			{
				conn.Open();
				if (conn.State != ConnectionState.Open)
					conn.Open();

				if (!App.GetTableNames(conn, $"{Settings.Default.Schema}_PREFABS").Contains(_prefabName))
				{
					using (var cmd =
						new SqlCommand(
							$"CREATE TABLE [{Settings.Default.Schema}_PREFABS].[{_prefabName}] ([COLUMNS] NVARCHAR(MAX), [TYPES] NVARCHAR(MAX), [SORTBYS] NVARCHAR(MAX), [GROUPS] NVARCHAR(MAX))",
							conn))
						cmd.ExecuteNonQuery();
				}

				List<string> tablesOfThisPrefab = App.GetTablesOfPrefab($"{_prefabName}");
				using (var comm = new SqlCommand($"TRUNCATE TABLE [{Settings.Default.Schema}_PREFABS].[{_prefabName}]",
					conn))
				{
					comm.ExecuteNonQuery();
				}

				var bulkCopy = new SqlBulkCopy(conn)
				{
					DestinationTableName = $"[{Settings.Default.Schema}_PREFABS].[{_prefabName}]"
				};
				bulkCopy.WriteToServer(_prefabTable);

				foreach (string delCol in _deletedColumns.Distinct())
				{
					Debug.WriteLine(delCol);
					var comboColumnsList =
						App.GetAllColumnsOfTable(conn, $"{Settings.Default.Schema}_COMBOBOXES", _prefabName);
					if (!comboColumnsList.Contains(delCol)) continue;

					if (comboColumnsList.Count <= 1)
						using (var comm =
							new SqlCommand(
								$"DROP TABLE [{Settings.Default.Schema}_COMBOBOXES].[{_prefabName}]", conn))
							comm.ExecuteNonQuery();
					else
						using (var comm =
							new SqlCommand(
								$"ALTER TABLE [{Settings.Default.Schema}_COMBOBOXES].[{_prefabName}] DROP COLUMN [{delCol}]",
								conn))
							comm.ExecuteNonQuery();
				}

				if (!_prefabName.Equals(ItemNameField.Text))
				{
					using (var comm =
						new SqlCommand(
							$"sp_rename '{Settings.Default.Schema}_PREFABS.{_prefabName}', '{ItemNameField.Text}'",
							conn))
					{
						comm.ExecuteNonQuery();
					}

					if (App.GetTableNames(conn, $"{Settings.Default.Schema}_COMBOBOXES").Contains(_prefabName))
						using (var comm =
							new SqlCommand(
								$"sp_rename '{Settings.Default.Schema}_COMBOBOXES.{_prefabName}', '{ItemNameField.Text}'",
								conn))
						{
							comm.ExecuteNonQuery();
						}
				}

				_prefabName = ItemNameField.Text;
				foreach (string tableName in tablesOfThisPrefab)
				{
					string tmpTableName = App.RandomString(12);

					foreach (string key in _changedColumnNames.Keys)
					{
						//Debug.WriteLine(key + " : " + _changedColumnNames[key]);
						/*if(App.GetAllColumnsOfTable(conn, tableName).Contains(_changedColumnNames[key]))
							using (var comm =
								new SqlCommand($"ALTER TABLE [{Settings.Default.Schema}].[{tableName}] DROP [{_changedColumnNames[key]}]",
									conn))
							{
								comm.ExecuteNonQuery();
							}*/

						//string rand = App.RandomString(12);
						if (key.Equals(_changedColumnNames[key])) continue;
						using (var comm = new SqlCommand(
							$"sp_rename '{Settings.Default.Schema}.{tableName}.{key}', '{_changedColumnNames[key]}', 'COLUMN'",
							conn))
						{
							comm.ExecuteNonQuery();
						}

						if (App.GetAllColumnsOfTable(conn, $"{Settings.Default.Schema}_COMBOBOXES", _prefabName)
							.Contains(key))
							using (var comm = new SqlCommand(
								$"sp_rename '{Settings.Default.Schema}_COMBOBOXES.{_prefabName}.{key}', '{_changedColumnNames[key]}', 'COLUMN'",
								conn))
								comm.ExecuteNonQuery();

						/*using (var comm3 = new SqlCommand(
							$"sp_rename '{Settings.Default.Schema}.{tableName}.{rand}', '{_changedColumnNames[key]}', 'COLUMN'", conn))
						{
							comm3.ExecuteNonQuery();
						}*/

						//goto RETRY;
					}

					if (conn.State != ConnectionState.Open)
						conn.Open();
					List<string> newColumns = new List<string>();
					foreach (DataRow dr in _prefabTable.Rows)
					{
						for (int i = 0; i < dr.ItemArray.Length; i++)
							if (dr[i] == null || dr[i] is DBNull)
								dr[i] = string.Empty;
						newColumns.Add(dr[0].ToString());
					}

					using (var comm = new SqlCommand())
					{
						comm.Connection = conn;
						comm.CommandText = $"CREATE TABLE [{Settings.Default.Schema}].[{tmpTableName}] ( ";

						int index = 0;
						foreach (string column in newColumns)
						{
							if (index != newColumns.Count - 1)
								comm.CommandText += $"[{column}] NVARCHAR(MAX), ";
							else
								comm.CommandText += $"[{column}] NVARCHAR(MAX) ";
							index++;
						}

						comm.CommandText += ")";
						comm.ExecuteNonQuery();
					}

					List<string> columns = App.GetAllColumnsOfTable(conn, tableName);
					using (var comm = new SqlCommand())
					{
						comm.Connection = conn;
						comm.CommandText = $"INSERT INTO [{Settings.Default.Schema}].[{tmpTableName}] (";
						for (int i = 0; i < columns.Count; i++)
						{
							if (!newColumns.Contains(columns[i]))
								continue;

							if (i != columns.Count - 1)
								comm.CommandText += $"[{columns[i]}], ";
							else
								comm.CommandText += $"[{columns[i]}]";
						}

						comm.CommandText += ") SELECT ";

						for (int i = 0; i < columns.Count; i++)
						{
							if (!newColumns.Contains(columns[i]))
								continue;

							if (i != columns.Count - 1)
								comm.CommandText += $"[{columns[i]}], ";
							else
								comm.CommandText += $"[{columns[i]}]";
						}

						comm.CommandText += $" FROM [{Settings.Default.Schema}].[{tableName}]";
						comm.ExecuteNonQuery();
					}

					using (var comm = new SqlCommand($"DROP TABLE [{Settings.Default.Schema}].[{tableName}]", conn))
						comm.ExecuteNonQuery();

					using (var comm =
						new SqlCommand($"sp_rename '{Settings.Default.Schema}.{tmpTableName}', '{tableName}'", conn))
						comm.ExecuteNonQuery();
				}

				conn.Close();
			}

			System.Windows.Forms.Cursor.Current = Cursors.Default;
		}

		private void ItemNameField_OnTextChanged(object sender, TextChangedEventArgs e)
		{
			SaveButton.IsEnabled = !string.IsNullOrEmpty(ItemNameField.Text);
			EditComboBoxes.IsEnabled = !string.IsNullOrEmpty(ItemNameField.Text);
		}

		private void EditComboBoxes_OnClick(object sender, RoutedEventArgs e)
		{
			ComboBoxBuilder cb = new ComboBoxBuilder(_prefabTable, _prefabName)
			{
				Owner = this,
				ShowInTaskbar = false
			};
			cb.ShowDialog();
		}

		private void PrefabBuilder_OnClosed(object sender, EventArgs e)
		{
			_prefabManager.PopulateListBox();
		}

		private void AddNewRowButton_OnClick(object sender, RoutedEventArgs e)
		{
			if (RowPositionTextBox.Value == null) return;

			int rowIndex = (int) RowPositionTextBox.Value - 1;
			DataRow dr = _prefabTable.NewRow();
			dr[1] = "TextField";
			_prefabTable.Rows.InsertAt(dr, rowIndex);
		}

		private void RowPositionTextBox_OnValueChanged(object sender, TextChangedEventArgs textChangedEventArgs)
		{
			try
			{
				AddNewRowButton.IsEnabled = !string.IsNullOrEmpty(RowPositionTextBox.Text);
			}
			catch
			{
				// ignored
			}
		}
	}
}