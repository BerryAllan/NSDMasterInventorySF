using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
using Syncfusion.Windows.Tools.Controls;
using ZXing;
using Cursors = System.Windows.Forms.Cursors;
using DataColumn = System.Data.DataColumn;
using DataRow = System.Data.DataRow;
using TextChangedEventArgs = System.Windows.Controls.TextChangedEventArgs;
using Timer = System.Timers.Timer;

namespace NSDMasterInventorySF
{
	/// <summary>
	///     Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow
	{
		#region Fields

		private string _currentVisualStyle;
		public static volatile List<DataTable> MasterDataTables = new List<DataTable>();
		public static volatile List<DataTable> RevertDataTables = new List<DataTable>();
		public static volatile List<SfDataGrid> MasterDataGrids = new List<SfDataGrid>();
		public static volatile List<string> Prefabs = new List<string>();
		public static volatile List<Dictionary<int, List<int>>> EditedCells = new List<Dictionary<int, List<int>>>();
		public static RoutedCommand SaveCommand = new RoutedCommand();
		public static RoutedCommand NewCommand = new RoutedCommand();
		public static RoutedCommand FindReplaceCommand = new RoutedCommand();

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

		public MainWindow()
		{
			InitializeComponent();

			

			try
			{
				using (var conn = new SqlConnection(App.ConnectionString))
				{
					conn.Open();
					conn.Close();
				}
			}
			catch
			{
				MessageBox.Show(
					"Failed to connect to specified server. \nEnsure your server is running, and that you have the correct fields entered in C:\\ProgramFiles (x86)\\SpeedyFeet Inc\\Rapid Recorder\\Rapid Recorder.exe.config.",
					"Severe Failure!", MessageBoxButton.OK, MessageBoxImage.Error);
				Application.Current.Shutdown();
				return;
			}

			Recycled.FillRecycledDataTable();
			
			SaveCommand.InputGestures.Add(new KeyGesture(Key.S, ModifierKeys.Control));
			CommandBindings.Add(new CommandBinding(SaveCommand, Save_Changes));
			FindReplaceCommand.InputGestures.Add(new KeyGesture(Key.F, ModifierKeys.Control));
			CommandBindings.Add(new CommandBinding(FindReplaceCommand, FindReplace));

			InitializeOrRefreshEverything(0);

			var backupTimer = new Timer(240000)
			{
				Enabled = true
			};
			backupTimer.Elapsed += Backup;

			var autoSaveTimer = new Timer(24000)
			{
				Enabled = true
			};
			autoSaveTimer.Elapsed += AutoSave;
		}

		public void Backup(object o, ElapsedEventArgs e)
		{
			App.Backup();
		}

		public void AutoSave(object o, ElapsedEventArgs e)
		{
			SaveToDb(false);
		}

		public void InitializeOrRefreshEverything(int tabIndex)
		{
			System.Windows.Forms.Cursor.Current = Cursors.WaitCursor;

			CurrentVisualStyle = Settings.Default.Theme;

			ResetGroupsBox.IsChecked = false;
			DeleteModeCheckBox.IsChecked = false;
			SearchField.Clear();
			SearchField.AutoCompleteSource = SearchBoxAutoCompleteItems;

			MasterTabControl.Items.Clear();
			MasterDataTables.Clear();
			MasterDataGrids.Clear();
			Prefabs.Clear();
			EditedCells.Clear();

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

				var i = 0;
				foreach (DataTable dt in App.GetDataTablesFromDb())
				{
					WriteToDataGrid(dt, App.GetPrefabOfDataTable(conn, dt), (TabItem) MasterTabControl.Items[i]);
					i++;
				}

				conn.Close();
			}

			//Debug.WriteLine(tabIndex);

			//for (int i = 0; i < MasterTabControl.Items.Count; i++)
			//{
			//	MasterTabControl.SelectedIndex = i;
			//}

			if (MasterTabControl.Items.Count > 0 && tabIndex < MasterTabControl.Items.Count)
				MasterTabControl.SelectedIndex = tabIndex;
			else if (tabIndex >= MasterTabControl.Items.Count)
				MasterTabControl.SelectedIndex = MasterTabControl.Items.Count - 1;

			RefreshRevertTables();

			if (MasterTabControl.Items.Count < 1)
			{
				SearchField.IsEnabled = false;
				ResetSorts.IsEnabled = false;
				ResetGroupsBox.IsEnabled = false;
				DeleteModeCheckBox.IsEnabled = false;
				SaveButton.IsEnabled = false;
				RevertChanges.IsEnabled = false;
				RefreshAll.IsEnabled = false;
				BarcodeTextBox.IsEnabled = false;
				BarcodeInventoryCommit.IsEnabled = false;
			}
			else
			{
				SearchField.IsEnabled = true;
				ResetSorts.IsEnabled = true;
				ResetGroupsBox.IsEnabled = true;
				DeleteModeCheckBox.IsEnabled = true;
				SaveButton.IsEnabled = true;
				RefreshAll.IsEnabled = true;
				BarcodeTextBox.IsEnabled = true;
				BarcodeInventoryCommit.IsEnabled = true;
			}

			System.Windows.Forms.Cursor.Current = Cursors.Default;
		}

		public void RefreshRevertTables()
		{
			RevertDataTables.Clear();

			foreach (DataTable table in MasterDataTables) RevertDataTables.Add(table.Copy());

			RevertChanges.IsEnabled = false;

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

			MasterDataTables.Insert(sheetIndex, dataTable);
			Prefabs.Insert(sheetIndex, prefab);
			EditedCells.Insert(sheetIndex, new Dictionary<int, List<int>>());

			SfDataGrid dataGrid = UiHelper.DefaultDataGridTemplate(dataTable, sheetIndex, this, prefab);

			MasterDataGrids.Insert(sheetIndex, dataGrid);
			tab.Content = dataGrid;
		}

		private void Save_Changes(object sender, RoutedEventArgs e)
		{
			SaveToDb(true);
			//SaveButton.IsEnabled = false;
		}

		public void SaveToDb(bool userInitiated)
		{
			if (App.SavingCurrently) return;

			App.SavingCurrently = true;
			System.Windows.Forms.Cursor.Current = Cursors.WaitCursor;
			Task task = Task.Run(() =>
			{
				try
				{
					using (var conn =
						new SqlConnection(App.ConnectionString))
					{
						conn.Open();
						foreach (DataTable table in MasterDataTables)
						{
							if (App.GetTableNames(conn).Contains(table.TableName))
								using (var comm = new SqlCommand($"TRUNCATE TABLE [{Settings.Default.Schema}].[{table.TableName}]",
									conn))
								{
									comm.ExecuteNonQuery();
								}

							var bulkCopy =
								new SqlBulkCopy(conn)
								{
									DestinationTableName =
										$"[{Settings.Default.Schema}].[{table.TableName}]"
								};
							try
							{
								bulkCopy.WriteToServer(table);
							}
							catch
							{
								try
								{
									if (App.GetTableNames(conn).Contains(table.TableName))
										using (var comm =
											new SqlCommand($"DROP TABLE [{Settings.Default.Schema}].[{table.TableName}]", conn))
										{
											comm.ExecuteNonQuery();
										}

									//Debug.WriteLine(table.TableName);
									//if (!App.GetTableNames(conn).Contains(table.TableName))
									using (var comm = new SqlCommand())
									{
										comm.Connection = conn;
										comm.CommandText = $"CREATE TABLE [{Settings.Default.Schema}].[{table.TableName}] ( ";
										var j = 0;
										foreach (DataColumn s in table.Columns)
										{
											if (j != table.Columns.Count - 1)
												comm.CommandText += $"[{s.ColumnName}] TEXT, ";
											else
												comm.CommandText += $"[{s.ColumnName}] TEXT";
											j++;
										}

										comm.CommandText += " )";

										comm.ExecuteNonQuery();
									}

									bulkCopy.WriteToServer(table);
								}
								catch
								{
									MessageBox.Show("Fatal Error. Please report this to 18grmathias@students.nekoosasd.net .", "Error!",
										MessageBoxButton.OK, MessageBoxImage.Error);
								}
							}
						}

						conn.Close();
					}
				}
				catch (Exception e) when (e is SqlException || e is NullReferenceException)
				{
					MessageBox.Show(this, "Error in saving to database.", "Error", MessageBoxButton.OK, MessageBoxImage.Error,
						MessageBoxResult.OK);
					Debug.WriteLine(e);
				}

				Thread.CurrentThread.IsBackground = true;
			});
			if (userInitiated)
				RefreshRevertTables();
			Task.Run(() =>
			{
				task.Wait();
				App.SavingCurrently = false;
			});

			System.Windows.Forms.Cursor.Current = Cursors.Default;
		}

		private void Revert_Changes(object sender, RoutedEventArgs e)
		{
			for (var i = 0; i < MasterDataTables.Count; i++)
			{
				MasterDataTables[i].Rows.Clear();
				foreach (DataRow row in RevertDataTables[i].Rows) MasterDataTables[i].ImportRow(row);
			}

			RefreshRevertTables();
		}

		private void ExportToExcel(object sender, RoutedEventArgs e)
		{
			var saveFileDialog = new SaveFileDialog
			{
				DefaultExt = ".xlsx",
				Filter = "Excel 2007+ (*.xlsx)|*.xlsx|Excel 2007- (*.xls)|*.xls"
			};
			if (saveFileDialog.ShowDialog() == true)
				ExcelWriter.Write(MasterDataTables, saveFileDialog.FileName,
					Path.GetExtension(saveFileDialog.FileName).Equals(".xlsx"));
		}

		private void ExportToCsv(object sender, RoutedEventArgs e)
		{
			var saveFileDialog = new VistaFolderBrowserDialog();
			if (saveFileDialog.ShowDialog() != true) return;

			List<string> fileNamesWithoutPath =
				(from TabItemExt item in MasterTabControl.Items select item.Header.ToString()).ToList();

			SvWriter.Write(MasterDataTables, fileNamesWithoutPath, saveFileDialog.SelectedPath, ".csv");
		}

		private void ExportToTsv(object sender, RoutedEventArgs e)
		{
			var saveFileDialog = new VistaFolderBrowserDialog();
			if (saveFileDialog.ShowDialog() != true) return;

			List<string> fileNamesWithoutPath =
				(from TabItemExt item in MasterTabControl.Items select item.Header.ToString()).ToList();

			SvWriter.Write(MasterDataTables, fileNamesWithoutPath, saveFileDialog.SelectedPath, ".tsv");
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
						BarcodeGenerator.CreateDmCodes(saveFileDialog.SelectedPath, MasterDataTables);
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
					foreach (DataColumn column in MasterDataTables[MasterTabControl.SelectedIndex].Columns)
					{
						if (counter != MasterDataTables[MasterTabControl.SelectedIndex].Columns.Count - 1)
							selection += $"[{column.ColumnName}] LIKE \'{searchText}*\' OR ";
						else
							selection += $"[{column.ColumnName}] LIKE \'{searchText}*\'";

						counter++;
					}

					MasterDataTables[MasterTabControl.SelectedIndex].DefaultView.RowFilter = selection;
					//MasterDataGrids[MasterTabControl.SelectedIndex].SearchHelper.Search(searchText);
				}
				else
				{
					MasterDataTables[MasterTabControl.SelectedIndex].DefaultView.RowFilter = string.Empty;
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

		private static void ClearRowFilters()
		{
			foreach (DataTable table in MasterDataTables) table.DefaultView.RowFilter = string.Empty;
		}

		private void OpenPrefabManagerMenuItemClick(object sender, RoutedEventArgs e)
		{
			SaveToDb(false);
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

		private void OpenDatabaseManagerMenuItemClick(object sender, RoutedEventArgs e)
		{
			SaveToDb(false);
			var databaseManager = new DatabaseManager(this)
			{
				Owner = this,
				ShowInTaskbar = false,
				ResizeMode = ResizeMode.NoResize
			};
			databaseManager.ShowDialog();
		}

		private void OpenSheetManagerMenuItemClick(object sender, RoutedEventArgs e)
		{
			SaveToDb(false);
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

			foreach (DataTable table in MasterDataTables)
			foreach (DataRow row in table.Rows)
			{
				List<string> fieldsInRow = row.ItemArray.Select(field => field.ToString()).ToList();

				if (bool.TryParse(fieldsInRow[0], out bool _) && table.Columns[0].ColumnName.Equals("Inventoried"))
					fieldsInRow.RemoveAt(0);

				if (!item.SequenceEqual(fieldsInRow)) continue;

				if (bool.TryParse(row[0].ToString(), out bool _) && table.Columns[0].ColumnName.Equals("Inventoried"))
				{
					row[0] = true;
					inventoried = true;
				}
			}

			if (inventoried) return;

			DataRow newRow = MasterDataTables[MasterTabControl.SelectedIndex].NewRow();

			if (MasterDataTables[MasterTabControl.SelectedIndex].Columns[0].ColumnName.Equals("Inventoried"))
				newRow[0] = true;
			int i = MasterDataTables[MasterTabControl.SelectedIndex].Columns[0].ColumnName.Equals("Inventoried") ? 1 : 0;
			foreach (string s in item)
			{
				newRow[i] = s;
				i++;
			}

			MasterDataTables[MasterTabControl.SelectedIndex].Rows.Add(newRow);
		}

		private void BackupTables(object sender, EventArgs e)
		{
			App.Backup();
			ConfigurationEcnrypterDecrypter.EncryptConfig();
			SqlDependency.Stop(App.ConnectionString);
		}

		private void RestoreFromBackups(object sender, EventArgs e)
		{
			SaveToDb(false);
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
			findAndReplace.ShowDialog();
		}

		private void SearchField_OnKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key != Key.Enter || string.IsNullOrEmpty(SearchField.Text)) return;
			SearchBoxAutoCompleteItems.Add(SearchField.Text);
			Search(SearchField.Text);
		}

		private void OpenStyleChooser(object sender, RoutedEventArgs e)
		{
			SaveToDb(false);
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

		private void ViewChangesTextBox_OnChecked(object sender, RoutedEventArgs e)
		{
		}

		private void ViewChangesTextBox_OnUnChecked(object sender, RoutedEventArgs e)
		{
			ResetChanges();
		}

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

		private void ViewRecycled(object sender, RoutedEventArgs e)
		{
			new Recycled().Show();
		}

		private void UnResetSorting(object sender, RoutedEventArgs e)
		{
			int j = 0;
			foreach (var grid in MasterDataGrids)
			{
				UiHelper.ResetGridSorting(grid, Prefabs[j], false);
			}
		}
	}
}