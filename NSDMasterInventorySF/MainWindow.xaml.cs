using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using NSDMasterInventorySF.io;
using NSDMasterInventorySF.Properties;
using NSDMasterInventorySF.ui;
using Ookii.Dialogs.Wpf;
using Syncfusion.SfSkinManager;
using Syncfusion.UI.Xaml.Grid;
using Syncfusion.UI.Xaml.Grid.Converter;
using Syncfusion.Windows.Tools.Controls;
using Syncfusion.XlsIO;
using ZXing;
using Cursors = System.Windows.Forms.Cursors;
using DataColumn = System.Data.DataColumn;
using DataRow = System.Data.DataRow;
using TextChangedEventArgs = System.Windows.Controls.TextChangedEventArgs;

namespace NSDMasterInventorySF
{
	/// <summary>
	///     Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow
	{
		public MainWindow()
		{
			InitializeComponent();

			Recycled.FillRecycledDataTable();

			NewRowCommand.InputGestures.Add(new KeyGesture(Key.N, ModifierKeys.Control));
			CommandBindings.Add(new CommandBinding(NewRowCommand, NewRowOnClicked));
			FindReplaceCommand.InputGestures.Add(new KeyGesture(Key.F, ModifierKeys.Control | ModifierKeys.Shift));
			CommandBindings.Add(new CommandBinding(FindReplaceCommand, FindReplace));
			FindCommand.InputGestures.Add(new KeyGesture(Key.F, ModifierKeys.Control));
			CommandBindings.Add(new CommandBinding(FindCommand, Find));
			FullScreenCommand.InputGestures.Add(new KeyGesture(Key.F11));
			CommandBindings.Add(new CommandBinding(FullScreenCommand, Fullscreen));
			NextTabCommand.InputGestures.Add(new KeyGesture(Key.Tab, ModifierKeys.Control));
			CommandBindings.Add(new CommandBinding(NextTabCommand, NextTab));
			PreviousTabCommand.InputGestures.Add(new KeyGesture(Key.Tab, ModifierKeys.Control | ModifierKeys.Shift));
			CommandBindings.Add(new CommandBinding(PreviousTabCommand, PreviousTab));
			CreateBarcodeCommand.InputGestures.Add(new KeyGesture(Key.B, ModifierKeys.Control));
			CommandBindings.Add(new CommandBinding(CreateBarcodeCommand, CreateBarcode));
			SaveCurrentSheetCommand.InputGestures.Add(new KeyGesture(Key.S, ModifierKeys.Control));
			CommandBindings.Add(new CommandBinding(SaveCurrentSheetCommand, ExportCurrentSheetToExcel));
			RefreshCommand.InputGestures.Add(new KeyGesture(Key.R, ModifierKeys.Control));
			CommandBindings.Add(new CommandBinding(RefreshCommand, RefreshAll_OnClick));

			InitializeOrRefreshEverything(0);

			var backupTimer = new Timer(240000)
			{
				Enabled = true
			};
			backupTimer.Elapsed += Backup;
		}

		private void ExportCurrentSheetToExcel(object sender, ExecutedRoutedEventArgs e)
		{
			var options = new ExcelExportingOptions {ExcelVersion = ExcelVersion.Excel2013};
			var excelEngine = new ExcelEngine();
			DataTable itemsSource = ((DataTable) MasterDataGrids[MasterTabControl.SelectedIndex].ItemsSource);
			string tempSheetName = App.RandomString(12);
			var workBook = excelEngine.Excel.Workbooks.Create(new[] {tempSheetName});

			var tempExcelEngine = MasterDataGrids[MasterTabControl.SelectedIndex]
				.ExportToExcel(MasterDataGrids[MasterTabControl.SelectedIndex].View, options);
			var workSheet = tempExcelEngine.Excel.Workbooks[0].Worksheets[0];
			workSheet.Name = itemsSource.TableName;
			workBook.Worksheets.AddCopy(workSheet);
			//workBook.Worksheets.Remove(1);
			SaveFileDialog sfd = new SaveFileDialog
			{
				FilterIndex = 3,
				Filter =
					"Excel 97 to 2003 Files(*.xls)|*.xls|Excel 2007 to 2010 Files(*.xlsx)|*.xlsx|Excel 2013 File(*.xlsx)|*.xlsx"
			};

			if (sfd.ShowDialog() == true)
			{
				using (Stream stream = sfd.OpenFile())
				{
					if (sfd.FilterIndex == 1)
						workBook.Version = ExcelVersion.Excel97to2003;

					else if (sfd.FilterIndex == 2)
						workBook.Version = ExcelVersion.Excel2010;

					else
						workBook.Version = ExcelVersion.Excel2013;
					workBook.Worksheets.Remove(tempSheetName);
					workBook.SaveAs(stream);
				}

				//Message box confirmation to view the created workbook.

				if (MessageBox.Show("Do you want to view the spreadsheet?", "Spreadsheet has been created",
					    MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
				{
					new SpreadsheetEditor(sfd.FileName).Show();
				}
			}
		}

		private void CreateBarcode(object sender, ExecutedRoutedEventArgs e)
		{
			if (MasterDataGrids[MasterTabControl.SelectedIndex].SelectionController.SelectedCells.Count > 0)
			{
				var sfd = new SaveFileDialog
				{
					DefaultExt = ".png",
					Filter = @"PNG File (*.png)|*.png|JPG File(*.jpg)|*.jpg|JPEG File(*.jpeg)|*.jpeg"
				};

				if (sfd.ShowDialog() != true) return;

				string item = BarcodeGenerator.GetItemStringFromDataRow(
					((DataRowView) MasterDataGrids[MasterTabControl.SelectedIndex].SelectionController
						.SelectedCells[
							MasterDataGrids[MasterTabControl.SelectedIndex].SelectionController.SelectedCells.Count - 1]
						.RowData).Row);
				BarcodeGenerator.SaveBarcode(item, Path.GetFileNameWithoutExtension(sfd.FileName), sfd.FileName);
			}
			else if (MasterDataGrids[MasterTabControl.SelectedIndex].SelectionController.SelectedRows.Count > 0)
			{
				var sfd = new SaveFileDialog
				{
					DefaultExt = ".png",
					Filter = @"PNG File (*.png)|*.png|JPG File(*.jpg)|*.jpg|JPEG File(*.jpeg)|*.jpeg"
				};

				if (sfd.ShowDialog() != true) return;

				string item = BarcodeGenerator.GetItemStringFromDataRow(
					((DataRowView) MasterDataGrids[MasterTabControl.SelectedIndex].SelectionController
						.SelectedRows[
							MasterDataGrids[MasterTabControl.SelectedIndex].SelectionController.SelectedRows.Count - 1]
						.RowData).Row);
				BarcodeGenerator.SaveBarcode(item, Path.GetFileNameWithoutExtension(sfd.FileName), sfd.FileName);
			}
		}

		private void PreviousTab(object sender, ExecutedRoutedEventArgs e)
		{
			MasterTabControl.SelectedIndex--;
			if (MasterTabControl.SelectedIndex < 0)
				MasterTabControl.SelectedIndex = MasterTabControl.Items.Count - 1;
		}

		private void NextTab(object sender, ExecutedRoutedEventArgs e)
		{
			if (MasterTabControl.SelectedIndex >= MasterTabControl.Items.Count - 1)
			{
				MasterTabControl.SelectedIndex = 0;
				return;
			}

			MasterTabControl.SelectedIndex++;
		}

		private void Fullscreen(object sender, ExecutedRoutedEventArgs e)
		{
			MasterTabControl.FullScreenMode = MasterTabControl.FullScreenMode == FullScreenMode.None
				? FullScreenMode.WindowMode
				: FullScreenMode.None;
		}

		public void Backup(object o, ElapsedEventArgs e)
		{
			App.Backup();
		}

		public void InitializeOrRefreshEverything(int tabIndex)
		{
			System.Windows.Forms.Cursor.Current = Cursors.WaitCursor;

			CurrentVisualStyle = Settings.Default.Theme;
			App.ThisIsNowConcurrent = false;
			App.ThisMadeLastChange = false;

			ResetGroupsBox.IsChecked = false;
			DeleteModeCheckBox.IsChecked = false;
			SearchField.Clear();
			SearchField.AutoCompleteSource = SearchBoxAutoCompleteItems;

			MasterTabControl.Items.Clear();
			var dataSets = App.MainSet(this);
			MasterDataSet = dataSets;
			MasterDataGrids.Clear();
			EditedCells.Clear();
			ProgressGrid.Visibility = Visibility.Hidden;

			using (var conn =
				new SqlConnection(App.ConnectionString))
			{
				conn.Open();

				foreach (string s in App.GetTableNames(conn))
				{
					var tab = new TabItemExt
					{
						Header = s
					};
					MasterTabControl.Items.Add(tab);
				}

				int i = 0;
				foreach (DataTable dt in MasterDataSet.Tables)
				{
					WriteToDataGrid(dt, App.GetPrefabOfDataTable(conn, dt), (TabItem) MasterTabControl.Items[i]);
					i++;
				}

				conn.Close();
			}

			if (MasterTabControl.Items.Count > 0 && tabIndex < MasterTabControl.Items.Count)
				MasterTabControl.SelectedIndex = tabIndex;
			else if (tabIndex >= MasterTabControl.Items.Count)
				MasterTabControl.SelectedIndex = MasterTabControl.Items.Count - 1;
			else
				MasterTabControl.SelectedIndex = 0;

			RefreshRevertTables();

			if (MasterTabControl.Items.Count < 1)
			{
				SearchField.IsEnabled = false;
				ResetSorts.IsEnabled = false;
				ResetGroupsBox.IsEnabled = false;
				DeleteModeCheckBox.IsEnabled = false;
				//RevertChanges.IsEnabled = false;
				NewRowButton.IsEnabled = false;
				RefreshAll.IsEnabled = false;
				BarcodeTextBox.IsEnabled = false;
				BarcodeInventoryCommit.IsEnabled = false;
			}
			else
			{
				SearchField.IsEnabled = true;
				ResetSorts.IsEnabled = true;
				ResetSorts.IsChecked = true;
				ResetGroupsBox.IsEnabled = true;
				DeleteModeCheckBox.IsEnabled = true;
				RefreshAll.IsEnabled = true;
				BarcodeTextBox.IsEnabled = true;
				BarcodeInventoryCommit.IsEnabled = true;
				NewRowButton.IsEnabled = true;
			}

			Recycled.FillRecycledDataTable();
			System.Windows.Forms.Cursor.Current = Cursors.Default;
		}

		private void RefreshRevertTables()
		{
			RevertDataTables.Clear();

			foreach (DataTable table in MasterDataSet.Tables) RevertDataTables.Add(table.Copy());

			//RevertChanges.IsEnabled = false;

			ResetChanges();
		}

		public void ResetChanges()
		{
			foreach (Dictionary<int, List<int>> v in EditedCells) EditedCells[EditedCells.IndexOf(v)].Clear();

			//foreach (var grid in MasterDataGrids)
			//	try
			//	{
			//		grid.View.Refresh();
			//	}
			//	catch
			//	{
			//	}
		}

		public void WriteToDataGrid(DataTable dataTable, string prefab, ContentControl tab)
		{
			int sheetIndex = MasterTabControl.Items.IndexOf(tab);

			EditedCells.Insert(sheetIndex, new Dictionary<int, List<int>>());

			SfDataGrid dataGrid = UiHelper.DefaultDataGridTemplate(dataTable, sheetIndex, this, prefab);

			MasterDataGrids.Insert(sheetIndex, dataGrid);
			tab.Content = dataGrid;
		}

/*
		private void Revert_Changes(object sender, RoutedEventArgs e)
		{
			//for (var i = 0; i < MasterDataSet.Tables.Count; i++)
			//{
			//	MasterDataSet.Tables[i].Rows.Clear();
			//	foreach (DataRow row in RevertDataTables[i].Rows) MasterDataSet.Tables[i].ImportRow(row);
			//}

			//RefreshRevertTables();
			foreach (DataTable table in MasterDataSet.Tables)
				table.RejectChanges();
		}
*/

		private void ExportToExcel(object sender, RoutedEventArgs e)
		{
			ExcelWriter.Write(MasterDataGrids);
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

		private void SearchFieldTextChanged(object sender, TextChangedEventArgs e)
		{
			NewRowButton.IsEnabled = string.IsNullOrEmpty(SearchField.Text);
			Search(SearchField.Text);
		}

		private void Search(string searchText)
		{
			ClearRowFilters();

			try
			{
				if (!string.IsNullOrEmpty(searchText))
				{
					string selection = string.Empty;

					var counter = 0;
					foreach (DataColumn column in MasterDataSet.Tables[MasterTabControl.SelectedIndex].Columns)
					{
						if (counter != MasterDataSet.Tables[MasterTabControl.SelectedIndex].Columns.Count - 1)
							selection += $"[{column.ColumnName}] LIKE \'{searchText}*\' OR ";
						else
							selection += $"[{column.ColumnName}] LIKE \'{searchText}*\'";

						counter++;
					}

					MasterDataSet.Tables[MasterTabControl.SelectedIndex].DefaultView.RowFilter = selection;
					//MasterDataGrids[MasterTabControl.SelectedIndex].SearchHelper.Search(searchText);
				}
				else
				{
					MasterDataSet.Tables[MasterTabControl.SelectedIndex].DefaultView.RowFilter = string.Empty;
					//MasterDataGrids[MasterTabControl.SelectedIndex].SearchHelper.ClearSearch();
				}
			}
			catch (Exception)
			{
				//Debug.WriteLine(ex);
			}
		}

		private void ResetSorting(object sender, RoutedEventArgs e)
		{
			using (var conn = new SqlConnection(App.ConnectionString))
			{
				foreach (SfDataGrid dataGrid in MasterDataGrids)
				{
					if (ResetSorts.IsChecked != null && (bool) ResetSorts.IsChecked)
					{
						UiHelper.ResetGridSorting(dataGrid,
							App.GetPrefabOfDataTable(conn, (DataTable) dataGrid.ItemsSource), true);
					}
					else
					{
						UiHelper.ResetGridSorting(dataGrid,
							App.GetPrefabOfDataTable(conn, (DataTable) dataGrid.ItemsSource), false);
					}

					dataGrid.ClearFilters();
				}
			}
		}

		private void ResetGrouping(object sender, RoutedEventArgs e)
		{
			using (var conn = new SqlConnection(App.ConnectionString))
			{
				conn.Open();
				foreach (SfDataGrid dataGrid in MasterDataGrids)
				{
					if (ResetGroupsBox.IsChecked != null && (bool) ResetGroupsBox.IsChecked)
					{
						UiHelper.ResetGridGrouping(dataGrid,
							App.GetPrefabOfDataTable(conn, (DataTable) dataGrid.ItemsSource), this, true);
					}
					else
					{
						UiHelper.ResetGridGrouping(dataGrid,
							App.GetPrefabOfDataTable(conn, (DataTable) dataGrid.ItemsSource), this, false);
					}
				}

				conn.Close();
			}
		}

		private void ClearRowFilters()
		{
			MasterDataSet.Tables[MasterTabControl.SelectedIndex].DefaultView.RowFilter = string.Empty;
			MasterDataGrids[MasterTabControl.SelectedIndex].ClearFilters();
			MasterDataGrids[MasterTabControl.SelectedIndex].GroupColumnDescriptions.Clear();
		}

		private void OpenPrefabManagerMenuItemClick(object sender, RoutedEventArgs e)
		{
			//SaveToDb();
			var prefabManager = new PrefabManager(this)
			{
				Owner = this,
				ShowInTaskbar = false
			};
			prefabManager.ShowDialog();
		}

		/// <summary>
		///     Called when [loaded].
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="RoutedEventArgs" /> instance containing the event data.</param>
		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			CurrentVisualStyle = Settings.Default.Theme;
		}

		/// <summary>
		///     On Visual Style Changed.
		/// </summary>
		/// <remarks></remarks>
		private void OnVisualStyleChanged()
		{
			Enum.TryParse(CurrentVisualStyle, out VisualStyles visualStyle);
			if (visualStyle == VisualStyles.Default) return;
			SfSkinManager.ApplyStylesOnApplication = true;
			SfSkinManager.SetVisualStyle(this, visualStyle);
			SfSkinManager.ApplyStylesOnApplication = false;
		}

		private void EnterDeleteMode(object sender, RoutedEventArgs e)
		{
			if (DeleteModeCheckBox.IsChecked != null && (bool) DeleteModeCheckBox.IsChecked)
				foreach (SfDataGrid dataGrid in MasterDataGrids)
					dataGrid.SelectionUnit = GridSelectionUnit.Row;
			else
				foreach (SfDataGrid dataGrid in MasterDataGrids)
					dataGrid.SelectionUnit = GridSelectionUnit.Any;
		}

		private void CloseApplication(object sender, RoutedEventArgs e)
		{
			Close();
		}

		private void OpenConnectionManagerMenuItemClick(object sender, RoutedEventArgs e)
		{
			//SaveToDb();
			var connectionManager = new ConnectionManager(this)
			{
				Owner = this,
				ShowInTaskbar = false,
				ResizeMode = ResizeMode.NoResize
			};
			connectionManager.ShowDialog();
		}

		private void OpenSheetManagerMenuItemClick(object sender, RoutedEventArgs e)
		{
			//SaveToDb();
			var sheetManager = new TableManager(this)
			{
				Owner = this,
				ShowInTaskbar = false,
				ResizeMode = ResizeMode.NoResize
			};
			sheetManager.ShowDialog();
		}

		private void BarcodeTextBox_OnKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				InventoryItemFromBarcode();
				BarcodeTextBox.Clear();
			}
		}

		private void BarcodeInventoryCommit_OnClick(object sender, RoutedEventArgs e)
		{
			InventoryItemFromBarcode();
			BarcodeTextBox.Clear();
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

			DataTable dt = MasterDataSet.Tables[MasterTabControl.SelectedIndex];
			DataRow newRow = dt.NewRow();
			dt.Rows.Add(newRow);
			int index = dt.Rows.IndexOf(newRow);

			if (dt.Columns[0].ColumnName.Equals("Inventoried"))
				dt.Rows[index][0] = true;
			int i = MasterDataSet.Tables[MasterTabControl.SelectedIndex].Columns[0].ColumnName.Equals("Inventoried")
				? 1
				: 0;
			foreach (string s in item)
			{
				dt.Rows[index][i] = s;
				i++;
			}
		}

		private void BackupTables(object sender, EventArgs e)
		{
			App.Backup();
			//SqlDependency.Stop(App.ConnectionString);
		}

		private void RestoreFromBackups(object sender, EventArgs e)
		{
			//SaveToDb();
			new RestorationManager
			{
				Owner = this,
				ShowInTaskbar = false,
				ResizeMode = ResizeMode.NoResize
			}.ShowDialog();
			InitializeOrRefreshEverything(MasterTabControl.SelectedIndex);
		}

		private void FindReplace(object sender, EventArgs e)
		{
			var findAndReplace = new FindAndReplace(this)
			{
				Owner = this,
				ShowInTaskbar = false,
				ResizeMode = ResizeMode.NoResize
			};
			findAndReplace.Show();
		}

		private void Find(object sender, EventArgs e)
		{
			SearchField.Focus();
		}

		private void SearchField_OnKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key != Key.Enter || string.IsNullOrEmpty(SearchField.Text)) return;
			SearchBoxAutoCompleteItems.Add(SearchField.Text);
			Search(SearchField.Text);
		}

		private void OpenStyleChooser(object sender, RoutedEventArgs e)
		{
			//SaveToDb();
			new StyleChooser
			{
				Owner = this,
				ShowInTaskbar = false,
				ResizeMode = ResizeMode.NoResize
			}.ShowDialog();
		}

		private void RefreshAll_OnClick(object sender, RoutedEventArgs e)
		{
			InitializeOrRefreshEverything(MasterTabControl.SelectedIndex);
		}

/*
		public static bool IsAvailable(SqlConnection conn)
		{
			try
			{
				conn.Open();
				conn.Close();
			}
			catch
			{
				return false;
			}

			return true;
		}
*/

		private void ViewRecycled(object sender, RoutedEventArgs e)
		{
			new Recycled().Show();
		}

		#region Fields

		private string _currentVisualStyle;
		public static volatile DataSet MasterDataSet;
		public static volatile List<DataTable> RevertDataTables = new List<DataTable>();
		public static volatile List<SfDataGrid> MasterDataGrids = new List<SfDataGrid>();
		public static volatile List<Dictionary<int, List<int>>> EditedCells = new List<Dictionary<int, List<int>>>();

		public  RoutedCommand FindReplaceCommand = new RoutedCommand();
		public  RoutedCommand FindCommand = new RoutedCommand();
		public  RoutedCommand NewRowCommand = new RoutedCommand();
		public  RoutedCommand FullScreenCommand = new RoutedCommand();
		public  RoutedCommand NextTabCommand = new RoutedCommand();
		public  RoutedCommand PreviousTabCommand = new RoutedCommand();
		public  RoutedCommand CreateBarcodeCommand = new RoutedCommand();
		public  RoutedCommand SaveCurrentSheetCommand = new RoutedCommand();
		public  RoutedCommand RefreshCommand = new RoutedCommand();

		#endregion

		#region Properties

		/// <summary>
		///     Gets or sets the current visual style.
		/// </summary>
		/// <value></value>
		/// <remarks></remarks>
		public string CurrentVisualStyle
		{
			get => _currentVisualStyle;
			set
			{
				_currentVisualStyle = value;
				OnVisualStyleChanged();
			}
		}

		private HashSet<string> SearchBoxAutoCompleteItems { get; } = new HashSet<string>();

		#endregion

		private void NewRowOnClicked(object sender, RoutedEventArgs e)
		{
			DataRow newRow = MasterDataSet.Tables[MasterTabControl.SelectedIndex].NewRow();
			MasterDataSet.Tables[MasterTabControl.SelectedIndex].Rows.Add(newRow);
			for (int i = 0; i < newRow.ItemArray.Length; i++)
			{
				if (MasterDataGrids[MasterTabControl.SelectedIndex].Columns[i] is GridCheckBoxColumn)
					newRow[i] = false;
			}

			foreach (var filter in MasterDataGrids[MasterTabControl.SelectedIndex].View.FilterPredicates)
			{
				int index = 0;
				foreach (var column in MasterDataGrids[MasterTabControl.SelectedIndex].Columns)
				{
					if (column.MappingName == filter.MappingName)
						index = MasterDataGrids[MasterTabControl.SelectedIndex].Columns.IndexOf(column);
				}

				if (filter.FilterPredicates.Count > 0)
					newRow[index] = filter.FilterPredicates[0].FilterValue.ToString();
			}
			//newRow.AcceptChanges();

			App.ThisMadeLastChange = true;
		}

		private void MasterTabControl_OnSelectedItemChangedEvent(object sender, SelectedItemChangedEventArgs e)
		{
			/*if (MasterDataGrids != null && MasterDataGrids.Count > 0)
				SearchField.IsEnabled =
					!(MasterDataGrids[MasterTabControl.SelectedIndex].GroupColumnDescriptions.Count > 0);*/
		}

		private void OpenDatabaseManagerMenuItemClick(object sender, RoutedEventArgs e)
		{
			var databaseManager = new DatabaseManager(this)
			{
				Owner = this,
				ShowInTaskbar = false,
				ResizeMode = ResizeMode.NoResize
			};
			databaseManager.ShowDialog();
		}

		private void AboutMenuItemClick(object sender, RoutedEventArgs e)
		{
			new AboutWindow
			{
				ShowInTaskbar = false,
				Owner = this
			}.ShowDialog();
		}
	}
}