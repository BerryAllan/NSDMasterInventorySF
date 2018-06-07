using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;
using Syncfusion.Data;
using Syncfusion.UI.Xaml.Grid;
using DataColumn = System.Data.DataColumn;
using ListSortDirection = Syncfusion.Data.ListSortDirection;

namespace NSDMasterInventorySFUWP.ui
{
	public static class UiHelper
	{
		public static bool ShouldFireGridSorting = true;

		public static SfDataGrid DefaultDataGridTemplate(DataTable dataTable, int sheetIndex, MasterTablePage page,
			string prefab)
		{
			SfDataGrid dataGrid = new SfDataGrid
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
				IsDynamicItemsSource = true,
				LiveDataUpdateMode = LiveDataUpdateMode.AllowDataShaping,
				UsePLINQ = true,
				AddNewRowPosition = AddNewRowPosition.FixedTop
			};

			//dataGrid.SearchHelper = new SearchHelperExt(dataGrid);
			//dataGrid.SortColumnDescriptions.CollectionChanged += (sender, args) =>
			//{
			//	page.ResetSorts.IsChecked = false;
			//};
			//dataGrid.RecordDeleted += (sender, args) => { page.RevertChanges.IsEnabled = true; };

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
					DataTable prefabTable = App.GetPrefabDataTable(conn, $"{App.Settings["Schema"]}_PREFABS", prefab);
					DataTable comboTable =
						App.GetPrefabDataTable(conn, $"{App.Settings["Schema"]}_COMBOBOXES", prefab);

					//could loop by table.ColumnNames.Count; but if prebab is changed... problems; maybe not all columns showing
					for (var i = 0; i < table.Columns.Count; i++)
					{
						GridColumn column;

						try
						{
							if (prefabTable.Rows[i]["TYPES"].ToString().ToLower().Equals("autocomplete"))
							{
								var comboStrings = new HashSet<string>();
								for (var j = 0; j < comboTable.Rows.Count; j++)
									if (!string.IsNullOrEmpty(
										comboTable.Rows[j][prefabTable.Rows[i]["COLUMNS"].ToString()].ToString()))
										comboStrings.Add(comboTable.Rows[j][prefabTable.Rows[i]["COLUMNS"].ToString()]
											.ToString());

								StringBuilder sb1 = new StringBuilder();
								sb1.AppendLine(
									"<DataTemplate xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">");
								sb1.AppendLine("<ComboBox PlaceholderText=\"{Binding " + prefabTable.Rows[i]["COLUMNS"] +
								           ", Mode=TwoWay}\" IsEditable=\"True\" IsTextSearchEnabled=\"True\" IsDropDownOpen=\"True\" >");
								foreach (var comboString in comboStrings)
								{
									sb1.AppendLine($"<x:String>{comboString}</x:String>");
								}
								sb1.AppendLine("</ComboBox>");
								sb1.AppendLine("</DataTemplate>");
								DataTemplate cellTemplate = (DataTemplate) XamlReader.Load(sb1.ToString());
								column = new GridTemplateColumn
								{
									MappingName = table.Columns[i].ColumnName,
									HeaderText = prefabTable.Rows[i]["COLUMNS"].ToString(),
									EditTemplate = cellTemplate
								};
								dataGrid.Columns.Add(column);
							}
							else if (prefabTable.Rows[i]["TYPES"].ToString().ToLower().Equals("datepicker"))
							{
								column = new GridDateTimeColumn
								{
									MappingName = table.Columns[i].ColumnName,
									FormatString = "MM/dd/yyyy",
									AllowInlineEditing = true,
									AllowNullValue = true,
									HeaderText = prefabTable.Rows[i]["COLUMNS"].ToString(),
									WaterMark = string.Empty
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
									MaximumNumberDecimalDigits = 3
								};

								dataGrid.Columns.Add(column);
							}/*
							else if (prefabTable.Rows[i]["TYPES"].ToString().ToLower().Equals("currency"))
							{
								column = new GridCurrencyColumn
								{
									MappingName = table.Columns[i].ColumnName,
									HeaderText = prefabTable.Rows[i]["COLUMNS"].ToString()
								};

								dataGrid.Columns.Add(column);
							}*/
							else if (prefabTable.Rows[i]["TYPES"].ToString().ToLower().Equals("hyperlink"))
							{
								column = new GridHyperlinkColumn
								{
									MappingName = table.Columns[i].ColumnName,
									HeaderText = prefabTable.Rows[i]["COLUMNS"].ToString()
								};

								dataGrid.Columns.Add(column);
							}/*
							else if (prefabTable.Rows[i]["TYPES"].ToString().ToLower().Equals("percentage"))
							{
								column = new GridPercentColumn
								{
									MappingName = table.Columns[i].ColumnName,
									HeaderText = prefabTable.Rows[i]["COLUMNS"].ToString()
								};

								dataGrid.Columns.Add(column);
							}*/
							else if (prefabTable.Rows[i]["TYPES"].ToString().ToLower().Equals("combobox"))
							{
								var comboStrings = new HashSet<string>();
								for (var j = 0; j < comboTable.Rows.Count; j++)
									if (!string.IsNullOrEmpty(
										comboTable.Rows[j][prefabTable.Rows[i]["COLUMNS"].ToString()].ToString()))
										comboStrings.Add(comboTable.Rows[j][prefabTable.Rows[i]["COLUMNS"].ToString()]
											.ToString());

								column = new GridComboBoxColumn
								{
									MappingName = table.Columns[i].ColumnName,
									ItemsSource = comboStrings,
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
							} //TODO: More? CalendarDatePicker, TimePicker, DatePicker, Slider, ToggleSwitch, RichEditBox, PasswordBox, ColorPicker, ComboBox, RatingControl
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
				DataTable prefabTable = App.GetPrefabDataTable(conn, $"{App.Settings["Schema"]}_PREFABS", prefab);

				for (var i = 0; i < prefabTable.Rows.Count; i++)
					dataTable.Columns[i].ColumnName = prefabTable.Rows[i]["COLUMNS"].ToString();
				conn.Close();
			}
		}

		public static void ResetGridSorting(SfDataGrid dataGrid, string prefab, bool shouldSort)
		{
			if (!ShouldFireGridSorting)
			{
				ShouldFireGridSorting = true;
				return;
			}

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
				DataTable prefabTable = App.GetPrefabDataTable(conn, $"{App.Settings["Schema"]}_PREFABS", prefab);

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

		public static void ResetGridGrouping(SfDataGrid dataGrid, string prefab, MasterTablePage page, bool shouldReset)
		{
			if (!shouldReset)
			{
				dataGrid.GroupColumnDescriptions.Clear();
				//ResetGridSorting(dataGrid, prefab, true);

				page.DefaultGroupingButton.IsChecked = false;

				page.SearchBox.IsEnabled = true;

				return;
			}

			dataGrid.GroupColumnDescriptions.Clear();
			if (string.IsNullOrWhiteSpace(prefab)) return;
			List<int> groups = App.GetGroups(prefab);
			//groups.Reverse();
			using (var conn = new SqlConnection(App.ConnectionString))
			{
				conn.Open();
				DataTable prefabTable = App.GetPrefabDataTable(conn, $"{App.Settings["Schema"]}_PREFABS", prefab);

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

			page.SearchBox.IsEnabled = false;

			//ResetGridSorting(dataGrid, prefab);
		}
	}
}