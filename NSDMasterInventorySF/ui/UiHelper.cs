using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using NSDMasterInventorySF.Properties;
using Syncfusion.Data.Extensions;
using Syncfusion.UI.Xaml.Grid;
using Syncfusion.Windows.Shared;
using Syncfusion.Windows.Tools.Controls;
using DataColumn = System.Data.DataColumn;
using DataRow = System.Data.DataRow;

namespace NSDMasterInventorySF.ui
{
	public static class UiHelper
	{
		public static T GetChildOfType<T>(this DependencyObject depObj) where T : DependencyObject
		{
			if (depObj == null) return null;
			for (var i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
			{
				DependencyObject child = VisualTreeHelper.GetChild(depObj, i);

				T result = child as T ?? GetChildOfType<T>(child);
				if (result != null) return result;
			}

			return null;
		}

		public static T FindParent<T>(this DependencyObject child) where T : DependencyObject
		{
			//get parent item
			DependencyObject parentObject = VisualTreeHelper.GetParent(child);

			//we've reached the end of the tree
			switch (parentObject)
			{
				case null:
					return null;
				case T parent:
					return parent;
			}

			//check if the parent matches the type we're looking for
			return FindParent<T>(parentObject);
		}

		public static SfDataGrid DefaultDataGridTemplate(DataTable dataTable, int sheetIndex, MainWindow window,
			string prefab)
		{
			SfDataGrid dataGrid = new SfDataGridExt
			{
				ItemsSource = dataTable,
				AllowDraggingColumns = true,
				AllowDraggingRows = true,
				AllowDeleting = true,
				AllowGrouping = true,
				ShowRowHeader = true,
				AllowFrozenGroupHeaders = true,
				AllowFiltering = true,
				AllowTriStateSorting = true,
				ShowSortNumbers = true,
				AllowResizingColumns = true,
				AllowEditing = true,
				NavigationMode = NavigationMode.Cell,
				AutoGenerateColumns = false,
				SelectionUnit = GridSelectionUnit.Any,
				SelectionMode = GridSelectionMode.Extended,
				GridValidationMode = GridValidationMode.InView,
				IsEnabled = true,
				HeaderRowHeight = 30,
				ShowGroupDropArea = true,
				ColumnSizer = GridLengthUnitType.Auto,
				AddNewRowPosition = AddNewRowPosition.FixedTop,
				IsDynamicItemsSource = true
				//CellStyleSelector = new CellStyleSelector(sheetIndex)
			};

			//dataGrid.SearchHelper = new SearchHelperExt(dataGrid);
			dataGrid.RecordDeleting += (sender, args) =>
			{
				DataRow row = Recycled.RecycledDataTable.NewRow();
				foreach (object item in args.Items)
					foreach (object field in ((DataRowView) item).Row.ItemArray)
						row[((DataRowView) item).Row.ItemArray.IndexOf(field)] = field;

				Recycled.RecycledDataTable.Rows.Add(row);
			};

			dataGrid.RecordDeleted += (sender, args) => { window.RevertChanges.IsEnabled = true; }
				;

			dataGrid.CurrentCellValueChanged += (sender, args) =>
			{
				window.RevertChanges.IsEnabled = true;

				//if ((bool) window.ViewChangesTextBox.IsChecked)
				//{
				//	int row = args.RowColumnIndex.RowIndex;
				//	int column = args.RowColumnIndex.ColumnIndex;

				//	//Debug.WriteLine(column);
				//	//Debug.WriteLine(row);

				//	List<int> columnIndices = new List<int>
				//	{
				//		column
				//	};

				//	if (!MainWindow.EditedCells[sheetIndex].ContainsKey(row))
				//		MainWindow.EditedCells[sheetIndex].Add(row, columnIndices);
				//	else if (!MainWindow.EditedCells[sheetIndex][row].Contains(column))
				//		MainWindow.EditedCells[sheetIndex][row].Add(column);

				//	dataGrid.UpdateDataRow(row);
				//}
			};
			dataGrid.CurrentCellValidated += (sender, args) =>
			{
				DataTable table = (DataTable) dataGrid.ItemsSource;
				int rowIndex = 0;
				foreach (DataRow row in table.Rows)
				{
					if (row[dataGrid.Columns.IndexOf(args.Column)] == args.NewValue)
						rowIndex = table.Rows.IndexOf(row);
				}

				using (var conn = new SqlConnection(App.ConnectionString))
				{
					conn.Open();

					using (var cmd = new SqlCommand($"SELECT * FROM [{Settings.Default.Schema}].[{table.TableName}]", conn))
					{
						using (var sda = new SqlDataAdapter(cmd))
						{
							string updateComm = $"UPDATE [{Settings.Default.Schema}].[{table.TableName}] SET ";

							int i = 0;
							string[] columns = App.GetAllColumnsOfTable(conn, table.TableName).ToArray();
							foreach (var column in columns)
							{
								if (i != columns.Length - 1)
									updateComm += $"[{column}] = '{table.Rows[rowIndex][i]}', ";
								else
									updateComm += $"[{column}] = '{table.Rows[rowIndex][i]}' ";
								i++;
							}

							updateComm += "WHERE ";
							int k = 0;
							foreach (var column in columns)
							{
								var value = k == dataGrid.Columns.IndexOf(args.Column) ? args.OldValue : table.Rows[rowIndex][k];
								if (k != columns.Length - 1)
									updateComm += $"[{column}] = '{value}' AND ";
								else
									updateComm += $"[{column}] = '{value}' ";
								k++;
							}
							Debug.WriteLine(updateComm);
							sda.UpdateCommand = new SqlCommand(updateComm, conn);
						}
					}

					conn.Close();
				}
			};

			var j = 0;
			dataGrid.Loaded += (sender, args) =>
			{
				if (j == 0)
				{
					GenerateColumnsSfDataGrid(dataGrid, dataTable, prefab);

					ResetGridSorting(dataGrid, prefab, true);
				}

				j++;
			};

			return dataGrid;
		}

		public static void GenerateColumnsSfDataGrid(SfDataGrid dataGrid, DataTable table, string prefab)
		{
			using (var conn = new SqlConnection(App.ConnectionString))
			{
				conn.Open();
				if (!string.IsNullOrEmpty(prefab))
				{
					DataTable prefabTable = App.GetPrefabDataTable(conn, "PREFABS", prefab);
					DataTable comboTable = App.GetPrefabDataTable(conn, "COMBOBOXES", prefab);

					//could loop by table.ColumnNames.Count; but if prebab is changed... problems; maybe not all columns showing
					for (var i = 0; i < table.Columns.Count; i++)
					{
						GridColumn column;

						try
						{
							if (prefabTable.Rows[i]["TYPES"].ToString().ToLower().Equals("autocomplete"))
							{
								column = new GridTemplateColumn
								{
									MappingName = table.Columns[i].ColumnName,
									HeaderText = prefabTable.Rows[i]["COLUMNS"].ToString()
								};

								var comboStrings = new HashSet<string>();
								for (var j = 0; j < comboTable.Rows.Count; j++)
									if (!string.IsNullOrEmpty(comboTable.Rows[j][prefabTable.Rows[i]["COLUMNS"].ToString()].ToString()))
										comboStrings.Add(comboTable.Rows[j][prefabTable.Rows[i]["COLUMNS"].ToString()].ToString());

								var autoCompleteElem = new FrameworkElementFactory(typeof(AutoComplete));

								var autoCompleteItemsBind = new Binding
								{
									Source = comboStrings,
									UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
									BindsDirectlyToSource = true
								};

								var autoCompleteBind =
									new Binding(table.Columns[i].ColumnName)
									{
										UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
										Mode = BindingMode.TwoWay,
										BindsDirectlyToSource = true
									};

								autoCompleteElem.SetValue(UIElement.FocusableProperty, true);
								autoCompleteElem.SetValue(AutoComplete.IsDropDownOpenProperty, true);
								autoCompleteElem.SetValue(ItemsControl.IsTextSearchEnabledProperty, true);
								autoCompleteElem.SetValue(AutoComplete.IsAutoAppendProperty, true);
								autoCompleteElem.SetValue(ItemsControl.IsTextSearchCaseSensitiveProperty, false);
								autoCompleteElem.SetValue(AutoComplete.CustomSourceProperty, autoCompleteItemsBind);
								autoCompleteElem.SetValue(AutoComplete.TextProperty, autoCompleteBind);
								autoCompleteElem.SetValue(AutoComplete.CanResizePopupProperty, false);
								autoCompleteElem.SetValue(AutoComplete.IsFilterProperty, true);
								autoCompleteElem.SetValue(AutoComplete.EnableSortingProperty, false);

								var cellEditingTemplate = new DataTemplate(typeof(AutoComplete)) {VisualTree = autoCompleteElem};
								((GridTemplateColumn) column).EditTemplate = cellEditingTemplate;

								var defaultElement = new FrameworkElementFactory(typeof(TextBlock));

								var bind = new Binding(table.Columns[i].ColumnName)
								{
									UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
									Mode = BindingMode.TwoWay,
									BindsDirectlyToSource = true
								};

								defaultElement.SetValue(TextBlock.TextProperty, bind);
								defaultElement.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);

								var defaultTemplate = new DataTemplate(typeof(TextBlock)) {VisualTree = defaultElement};
								((GridTemplateColumn) column).CellTemplate = defaultTemplate;

								dataGrid.Columns.Add(column);
							}
							else if (prefabTable.Rows[i]["TYPES"].ToString().ToLower().Equals("datepicker"))
							{
								column = new GridDateTimeColumn
								{
									MappingName = table.Columns[i].ColumnName,
									Pattern = DateTimePattern.ShortDate,
									CanEdit = true,
									AllowNullValue = true,
									NullText = string.Empty,
									AllowScrollingOnCircle = true,
									EnableBackspaceKey = true,
									EnableDeleteKey = true,
									ShowRepeatButton = true,
									HeaderText = prefabTable.Rows[i]["COLUMNS"].ToString()
								};

								dataGrid.Columns.Add(column);
							}
							else if (prefabTable.Rows[i]["TYPES"].ToString().ToLower().Equals("checkbox"))
							{
								column = new GridCheckBoxColumn
								{
									MappingName = table.Columns[i].ColumnName,
									IsThreeState = false,
									HeaderText = prefabTable.Rows[i]["COLUMNS"].ToString()
								};

								dataGrid.Columns.Add(column);
							}
							else if (prefabTable.Rows[i]["TYPES"].ToString().ToLower().Equals("numeric"))
							{
								column = new GridNumericColumn
								{
									MappingName = table.Columns[i].ColumnName,
									HeaderText = prefabTable.Rows[i]["COLUMNS"].ToString(),
									NumberDecimalDigits = 3,
									NumberGroupSeparator = ","
								};

								dataGrid.Columns.Add(column);
							}
							else if (prefabTable.Rows[i]["TYPES"].ToString().ToLower().Equals("currency"))
							{
								column = new GridCurrencyColumn
								{
									MappingName = table.Columns[i].ColumnName,
									HeaderText = prefabTable.Rows[i]["COLUMNS"].ToString()
								};

								dataGrid.Columns.Add(column);
							}
							else if (prefabTable.Rows[i]["TYPES"].ToString().ToLower().Equals("hyperlink"))
							{
								column = new GridHyperlinkColumn
								{
									MappingName = table.Columns[i].ColumnName,
									HeaderText = prefabTable.Rows[i]["COLUMNS"].ToString()
								};

								dataGrid.Columns.Add(column);
							}
							else if (prefabTable.Rows[i]["TYPES"].ToString().ToLower().Equals("percentage"))
							{
								column = new GridPercentColumn
								{
									MappingName = table.Columns[i].ColumnName,
									HeaderText = prefabTable.Rows[i]["COLUMNS"].ToString()
								};

								dataGrid.Columns.Add(column);
							}
							else if (prefabTable.Rows[i]["TYPES"].ToString().ToLower().Equals("combobox"))
							{
								var comboStrings = new HashSet<string>();
								for (var j = 0; j < comboTable.Rows.Count; j++)
									if (!string.IsNullOrEmpty(comboTable.Rows[j][prefabTable.Rows[i]["COLUMNS"].ToString()].ToString()))
										comboStrings.Add(comboTable.Rows[j][prefabTable.Rows[i]["COLUMNS"].ToString()].ToString());

								column = new GridComboBoxColumn
								{
									MappingName = table.Columns[i].ColumnName,
									ItemsSource = comboStrings,
									StaysOpenOnEdit = true,
									IsEditable = true,
									HeaderText = prefabTable.Rows[i]["COLUMNS"].ToString()
								};

								dataGrid.Columns.Add(column);
							}
							else
							{
								column = new GridTextColumn
								{
									MappingName = table.Columns[i].ColumnName,
									HeaderText = prefabTable.Rows[i]["COLUMNS"].ToString()
								};

								dataGrid.Columns.Add(column);
							}
						}
						catch
						{
							column = new GridTextColumn
							{
								MappingName = table.Columns[i].ColumnName,
								HeaderText = table.Columns[i].ColumnName
							};

							dataGrid.Columns.Add(column);
						}
					}
				}
				else
				{
					foreach (DataColumn column in table.Columns)
					{
						GridColumn gridColumn = new GridTextColumn
						{
							MappingName = column.ColumnName,
							HeaderText = column.ColumnName
						};

						dataGrid.Columns.Add(gridColumn);
					}
				}

				conn.Close();
			}
		}

		public static void ResetColumNamesDataTable(DataTable dataTable, string prefab)
		{
			using (var conn = new SqlConnection(App.ConnectionString))
			{
				conn.Open();
				DataTable prefabTable = App.GetPrefabDataTable(conn, "PREFABS", prefab);

				for (var i = 0; i < prefabTable.Rows.Count; i++)
					dataTable.Columns[i].ColumnName = prefabTable.Rows[i]["COLUMNS"].ToString();
				conn.Close();
			}
		}

		public static void ResetGridSorting(SfDataGrid dataGrid, string prefab, bool shouldSort)
		{
			if (!shouldSort)
			{
				dataGrid.SortColumnDescriptions.Clear();

				return;
			}

			dataGrid.SortColumnDescriptions.Clear();
			if (string.IsNullOrWhiteSpace(prefab)) return;
			List<int> sorts = App.GetSorts(prefab);
			//sorts.Reverse();
			using (var conn = new SqlConnection(App.ConnectionString))
			{
				conn.Open();
				DataTable prefabTable = App.GetPrefabDataTable(conn, "PREFABS", prefab);

				foreach (int i in sorts)
				{
					//Debug.WriteLine(prop[i]);
					var sorter = new SortColumnDescription
					{
						ColumnName = prefabTable.Rows[i]["COLUMNS"].ToString(),
						SortDirection = ListSortDirection.Ascending
					};
					if (dataGrid.SortColumnDescriptions.Contains(sorter)) continue;
					try
					{
						dataGrid.SortColumnDescriptions.Add(sorter);
					}
					catch
					{
						Debug.WriteLine("sorter ERROR!");
					}
				}

				conn.Close();
			}
		}

		public static void ResetGridGrouping(SfDataGrid dataGrid, string prefab, MainWindow window, bool shouldReset)
		{
			if (!shouldReset)
			{
				dataGrid.GroupColumnDescriptions.Clear();
				//ResetGridSorting(dataGrid, prefab, true);

				window.ResetGroupsBox.IsChecked = false;

				window.SearchField.IsEnabled = true;

				return;
			}

			dataGrid.GroupColumnDescriptions.Clear();
			if (string.IsNullOrWhiteSpace(prefab)) return;
			List<int> groups = App.GetGroups(prefab);
			//groups.Reverse();
			using (var conn = new SqlConnection(App.ConnectionString))
			{
				conn.Open();
				DataTable prefabTable = App.GetPrefabDataTable(conn, "PREFABS", prefab);

				foreach (int i in groups)
				{
					var grouper = new GroupColumnDescription
					{
						ColumnName = prefabTable.Rows[i]["COLUMNS"].ToString()
					};
					if (!dataGrid.GroupColumnDescriptions.Contains(grouper))
						dataGrid.GroupColumnDescriptions.Add(grouper);
				}

				conn.Close();
			}

			window.SearchField.IsEnabled = false;

			//ResetGridSorting(dataGrid, prefab);
		}
	}
}