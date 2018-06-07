using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Timers;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using NSDMasterInventorySFUWP.ui;
using Syncfusion.UI.Xaml.Grid;
using DataColumn = System.Data.DataColumn;
using DataRow = System.Data.DataRow;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace NSDMasterInventorySFUWP
{
	/// <inheritdoc cref="Page" />
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class MasterTablePage
	{
		public static volatile DataSet MasterDataSet;
		public static volatile List<SfDataGrid> MasterDataGrids = new List<SfDataGrid>();
		public static volatile List<string> Prefabs = new List<string>();
		private HashSet<string> SearchBoxAutoCompleteItems { get; } = new HashSet<string>();

		public MasterTablePage()
		{
			InitializeComponent();

			InitializeOrRefreshEverything(0);

			var backupTimer = new Timer(240000)
			{
				Enabled = true
			};
			backupTimer.Elapsed += Backup;
		}

		private static void Backup(object sender, ElapsedEventArgs e)
		{
			App.Backup();
		}

		public void InitializeOrRefreshEverything(int tabIndex)
		{
			App.ThisIsNowConcurrent = false;
			App.ThisMadeLastChange = false;

			DefaultGroupingButton.IsChecked = false;
			DeleteModeButton.IsChecked = false;
			SearchBox.Text = string.Empty;
			SearchBox.ItemsSource = SearchBoxAutoCompleteItems;

			MasterPivot.Items?.Clear();
			var dataSets = App.MainSet(this);
			MasterDataSet = dataSets;
			MasterDataGrids.Clear();
			Prefabs.Clear();

			using (var conn = new SqlConnection(App.ConnectionString))
			{
				conn.Open();

				foreach (string tableName in App.GetTableNames(conn))
				{
					var tab = new PivotItem
					{
						Header = tableName
					};
					MasterPivot.Items?.Add(tab);
				}

				int i = 0;
				foreach (DataTable dt in MasterDataSet.Tables)
				{
					WriteToDataGrid(dt, App.GetPrefabOfDataTable(conn, dt), (PivotItem) MasterPivot.Items?[i]);
					i++;
				}

				conn.Close();
			}

			if (MasterPivot.Items != null && MasterPivot.Items.Count > 0 && tabIndex < MasterPivot.Items.Count)
				MasterPivot.SelectedIndex = tabIndex;
			else if (MasterPivot.Items != null && tabIndex >= MasterPivot.Items.Count)
				MasterPivot.SelectedIndex = MasterPivot.Items.Count - 1;
			else
				MasterPivot.SelectedIndex = 0;

			//RefreshRevertTables();

			if (MasterPivot.Items.Count < 1)
			{
				SearchBox.IsEnabled = false;
				DefaultSortingButton.IsEnabled = false;
				DefaultGroupingButton.IsEnabled = false;
				DeleteModeButton.IsEnabled = false;
				RefreshAll.IsEnabled = false;
				BarcodeTextBox.IsEnabled = false;
				BarcodeButton.IsEnabled = false;
			}
			else
			{
				SearchBox.IsEnabled = true;
				DefaultSortingButton.IsEnabled = true;
				DefaultSortingButton.IsChecked = true;
				DefaultGroupingButton.IsEnabled = true;
				DeleteModeButton.IsEnabled = true;
				RefreshAll.IsEnabled = true;
				BarcodeTextBox.IsEnabled = true;
				BarcodeButton.IsEnabled = true;
			}

			//Recycled.FillRecycledDataTable();
		}

		public void WriteToDataGrid(DataTable dataTable, string prefab, PivotItem pivotTab)
		{
			if (MasterPivot.Items == null) return;
			int sheetIndex = MasterPivot.Items.IndexOf(pivotTab);

			Prefabs.Insert(sheetIndex, prefab);

			SfDataGrid dataGrid = UiHelper.DefaultDataGridTemplate(dataTable, sheetIndex, this, prefab);

			MasterDataGrids.Insert(sheetIndex, dataGrid);
			pivotTab.Content = dataGrid;
		}

		/*private void ExportToExcel(object sender, RoutedEventArgs e)
		{
			var saveFileDialog = new SaveFileDialog
			{
				DefaultExt = ".xlsx",
				Filter = "Excel 2007+ (*.xlsx)|*.xlsx|Excel 2007- (*.xls)|*.xls"
			};
			if (saveFileDialog.ShowDialog() == true)
			{
				ExcelWriter.Write(MasterDataSet, saveFileDialog.FileName,
					!Path.GetExtension(saveFileDialog.FileName).Equals(".xls"));
			}
		}

		private void ExportToCsv(object sender, RoutedEventArgs e)
		{
			var saveFileDialog = new VistaFolderBrowserDialog();
			if (saveFileDialog.ShowDialog() != true) return;

			List<string> fileNamesWithoutPath =
				(from TabItemExt item in MasterTabControl.Items select item.Header.ToString()).ToList();

			SvWriter.Write(MasterDataSet, fileNamesWithoutPath, saveFileDialog.SelectedPath, ".csv");
		}

		private void ExportToTsv(object sender, RoutedEventArgs e)
		{
			var saveFileDialog = new VistaFolderBrowserDialog();
			if (saveFileDialog.ShowDialog() != true) return;

			List<string> fileNamesWithoutPath =
				(from TabItemExt item in MasterTabControl.Items select item.Header.ToString()).ToList();

			SvWriter.Write(MasterDataSet, fileNamesWithoutPath, saveFileDialog.SelectedPath, ".tsv");
		}
		private void GenerateBarCodes(object sender, RoutedEventArgs rea)
		{
			var worker = new BackgroundWorker();
			worker.DoWork += (send, ev) =>
			{
				try
				{
					var saveFileDialog = new VistaFolderBrowserDialog();
					if (saveFileDialog.ShowDialog() == true)
						BarcodeGenerator.CreateDmCodes(saveFileDialog.SelectedPath, MasterDataSet, this);
				}
				catch (WriterException e)
				{
					Console.WriteLine($@"Could not generate Barcode, WriterException :: {e.Message}");
				}
				catch (IOException e)
				{
					Console.WriteLine($@"Could not generate Barcode, IOException :: {e.Message}");
				}
			};
			worker.RunWorkerAsync();
			worker.Dispose();
		}
		*/

		private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
		{
			NewRowButton.IsEnabled = string.IsNullOrEmpty(sender.Text);
			Search(sender.Text);
		}

		private void SearchBox_OnQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
		{
			NewRowButton.IsEnabled = string.IsNullOrEmpty(sender.Text);
			SearchBoxAutoCompleteItems.Add(sender.Text);
			Search(sender.Text);
		}

		private void SearchBox_OnSuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
		{
			NewRowButton.IsEnabled = string.IsNullOrEmpty(sender.Text);
			SearchBoxAutoCompleteItems.Add(sender.Text);
			Search(sender.Text);
		}

		private void Search(string searchText)
		{
			foreach (DataTable table in MasterDataSet.Tables) table.DefaultView.RowFilter = string.Empty;

			try
			{
				if (!string.IsNullOrEmpty(searchText))
				{
					string selection = string.Empty;

					var counter = 0;
					foreach (DataColumn column in MasterDataSet.Tables[MasterPivot.SelectedIndex].Columns)
					{
						if (counter != MasterDataSet.Tables[MasterPivot.SelectedIndex].Columns.Count - 1)
							selection += $"[{column.ColumnName}] LIKE \'{searchText}*\' OR ";
						else
							selection += $"[{column.ColumnName}] LIKE \'{searchText}*\'";

						counter++;
					}

					MasterDataSet.Tables[MasterPivot.SelectedIndex].DefaultView.RowFilter = selection;
					//MasterDataGrids[MasterPivot.SelectedIndex].SearchHelper.Search(searchText);
				}
				else
				{
					MasterDataSet.Tables[MasterPivot.SelectedIndex].DefaultView.RowFilter = string.Empty;
					//MasterDataGrids[MasterPivot.SelectedIndex].SearchHelper.ClearSearch();
				}
			}
			catch (Exception)
			{
				//Debug.WriteLine(ex);
			}
		}

		private void EnterDeleteMode(object sender, RoutedEventArgs e)
		{
			if (DeleteModeButton.IsChecked != null && (bool) DeleteModeButton.IsChecked)
				foreach (SfDataGrid dataGrid in MasterDataGrids)
					dataGrid.SelectionUnit = GridSelectionUnit.Row;
			else
				foreach (SfDataGrid dataGrid in MasterDataGrids)
					dataGrid.SelectionUnit = GridSelectionUnit.Any;
		}

		private void ChangeTabKeyboardAcceleratorOnInvoked(KeyboardAccelerator sender,
			KeyboardAcceleratorInvokedEventArgs args)
		{
			if (MasterPivot.Items != null && MasterPivot.SelectedIndex >= MasterPivot.Items.Count - 1)
			{
				MasterPivot.SelectedIndex = 0;
				return;
			}

			MasterPivot.SelectedIndex++;
		}

		private void RefreshAll_OnClick(object sender, RoutedEventArgs e)
		{
			InitializeOrRefreshEverything(MasterPivot.SelectedIndex);
		}

		private void NewRowOnInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
		{
			NewRow();
		}

		private void NewRow()
		{
			DataRow newRow = MasterDataSet.Tables[MasterPivot.SelectedIndex].NewRow();
			MasterDataSet.Tables[MasterPivot.SelectedIndex].Rows.Add(newRow);
			for (int i = 0; i < newRow.ItemArray.Length; i++)
			{
				if (MasterDataGrids[MasterPivot.SelectedIndex].Columns[i] is GridCheckBoxColumn)
					newRow[i] = false;
			}

			foreach (var filter in MasterDataGrids[MasterPivot.SelectedIndex].View.FilterPredicates)
			{
				int index = 0;
				foreach (var column in MasterDataGrids[MasterPivot.SelectedIndex].Columns)
				{
					if (column.MappingName == filter.MappingName)
						index = MasterDataGrids[MasterPivot.SelectedIndex].Columns.IndexOf(column);
				}

				if (filter.FilterPredicates.Count > 0)
					newRow[index] = filter.FilterPredicates[0].FilterValue.ToString();
			}
			//newRow.AcceptChanges();

			App.ThisMadeLastChange = true;
		}

		private void RefreshOnInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
		{
			InitializeOrRefreshEverything(MasterPivot.SelectedIndex);
		}

		private void SetSortingOnInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
		{
			DefaultSortingButton.IsChecked = !DefaultSortingButton.IsChecked;
		}

		private void SetGroupingOnInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
		{
			DefaultGroupingButton.IsChecked = !DefaultGroupingButton.IsChecked;
		}

		private void DeleteModeOnInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
		{
			DeleteModeButton.IsChecked = !DeleteModeButton.IsChecked;
		}

		private void FindOnInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
		{
			SearchBox.Focus(FocusState.Keyboard);
		}

		private void FindReplaceOnInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
		{
			//TODO: FindAndReplace
		}

		private void ResetSorting(object sender, RoutedEventArgs e)
		{
			var j = 0;
			foreach (SfDataGrid dataGrid in MasterDataGrids)
			{
				UiHelper.ResetGridSorting(dataGrid, Prefabs[j], true);
				dataGrid.ClearFilters();
				j++;
			}
		}

		private void ResetGrouping(object sender, RoutedEventArgs e)
		{
			var j = 0;
			foreach (SfDataGrid dataGrid in MasterDataGrids)
			{
				UiHelper.ResetGridGrouping(dataGrid, Prefabs[j], this, true);
				j++;
			}
		}

		private void UnResetGrouping(object sender, RoutedEventArgs e)
		{
			var j = 0;
			foreach (SfDataGrid dataGrid in MasterDataGrids)
			{
				UiHelper.ResetGridGrouping(dataGrid, Prefabs[j], this, false);
				j++;
			}
		}

		private void InventoryItem(object sender, KeyRoutedEventArgs e)
		{
			if (e.Key != VirtualKey.Enter) return;
			InventoryItemFromBarcode();
			BarcodeTextBox.Text = string.Empty;
		}

		private void InventoryItemButton(object sender, RoutedEventArgs e)
		{
			InventoryItemFromBarcode();
			BarcodeTextBox.Text = string.Empty;
		}

		private void InventoryItemFromBarcode()
		{
			if (string.IsNullOrEmpty(BarcodeTextBox.Text.Trim())) return;

			List<string> item = BarcodeTextBox.Text.Split('\t').ToList();
			if (bool.TryParse(item[0], out bool _)) item.RemoveAt(0);
			var inventoried = false;

			foreach (DataTable table in MasterDataSet.Tables)
			{
				foreach (DataRow row in table.Rows)
				{
					List<string> fieldsInRow = row.ItemArray.Select(field => field.ToString()).ToList();

					if (bool.TryParse(fieldsInRow[0], out bool _) && table.Columns[0].ColumnName.Equals("Inventoried"))
						fieldsInRow.RemoveAt(0);

					if (!item.SequenceEqual(fieldsInRow)) continue;

					if (bool.TryParse(row[0].ToString(), out bool _) &&
					    table.Columns[0].ColumnName.Equals("Inventoried"))
					{
						row[0] = true;
						inventoried = true;
					}
				}
			}

			if (inventoried) return;

			DataTable dt = MasterDataSet.Tables[MasterPivot.SelectedIndex];
			DataRow newRow = dt.NewRow();
			dt.Rows.Add(newRow);
			int index = dt.Rows.IndexOf(newRow);

			if (dt.Columns[0].ColumnName.Equals("Inventoried"))
				dt.Rows[index][0] = true;
			int i = MasterDataSet.Tables[MasterPivot.SelectedIndex].Columns[0].ColumnName.Equals("Inventoried")
				? 1
				: 0;
			foreach (string s in item)
			{
				dt.Rows[index][i] = s;
				i++;
			}
		}

		private void NewRowButton_OnClick(object sender, RoutedEventArgs e)
		{
			NewRow();
		}
	}
}