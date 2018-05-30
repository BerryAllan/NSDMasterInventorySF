using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NSDMasterInventorySF.Properties;
using NSDMasterInventorySF.ui;
using Syncfusion.UI.Xaml.Grid;
using Cursors = System.Windows.Forms.Cursors;
using DataColumn = System.Data.DataColumn;
using DataRow = System.Data.DataRow;

namespace NSDMasterInventorySF
{
	/// <inheritdoc cref="Window" />
	/// <summary>
	///     Interaction logic for Recyled.xaml
	/// </summary>
	public partial class Recycled
	{
		public static volatile DataTable RecycledDataTable = new DataTable();
		public static RoutedCommand SaveCommand = new RoutedCommand();

		public Recycled()
		{
			SaveCommand.InputGestures.Add(new KeyGesture(Key.S, ModifierKeys.Control));
			CommandBindings.Add(new CommandBinding(SaveCommand, Save_Changes));

			InitializeComponent();

			SearchField.AutoCompleteSource = SearchBoxAutoCompleteItems;
			Refresh();
		}

		public HashSet<string> SearchBoxAutoCompleteItems { get; } = new HashSet<string>();

		private void Refresh()
		{
			//RecycledDataTable.Clear();
			//FillRecycledDataTable();
			RecycledGrid.ItemsSource = RecycledDataTable;
			try
			{
				RecycledGrid.View.Refresh();
			}
			catch
			{
				// ignored
			}

			RecycledGrid.Loaded += (sender, args) =>
			{
				UiHelper.GenerateColumnsSfDataGrid(RecycledGrid, RecycledDataTable, string.Empty);
			};
		}

		private void SearchFieldTextChanged(object sender, TextChangedEventArgs e)
		{
			Search();
		}

		private void SearchField_OnKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key != Key.Enter || string.IsNullOrEmpty(SearchField.Text)) return;
			SearchBoxAutoCompleteItems.Add(SearchField.Text);
			Search();
		}

		private void EnterDeleteMode(object sender, RoutedEventArgs e)
		{
			RecycledGrid.SelectionUnit = DeleteModeCheckBox.IsChecked != null && (bool) DeleteModeCheckBox.IsChecked
				? GridSelectionUnit.Row
				: GridSelectionUnit.Any;
		}

		private void Save_Changes(object sender, RoutedEventArgs e)
		{
			SaveRecycledTable();
		}

		public static void SaveRecycledTable()
		{
			System.Windows.Forms.Cursor.Current = Cursors.WaitCursor;
			using (var conn = new SqlConnection(App.ConnectionString))
			{
				conn.Open();
				using (var comm = new SqlCommand($"TRUNCATE TABLE [RECYCLED].[{Settings.Default.Schema}]", conn))
				{
					comm.ExecuteNonQuery();
				}

				var bulkCopy =
					new SqlBulkCopy(conn)
					{
						DestinationTableName =
							$"[RECYCLED].[{Settings.Default.Schema}]"
					};
				try
				{
					bulkCopy.WriteToServer(RecycledDataTable);
				}
				catch
				{
					// ignored
				}

				conn.Close();
			}

			System.Windows.Forms.Cursor.Current = Cursors.Default;
		}

		private void RefreshAll_OnClick(object sender, RoutedEventArgs e)
		{
			Refresh();
		}

		private void BarcodeInventoryCommit_OnClick(object sender, RoutedEventArgs e)
		{
			InventoryItemFromBarcode();
			BarcodeTextBox.Clear();
		}

		private void BarcodeTextBox_OnKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key != Key.Enter) return;
			InventoryItemFromBarcode();
			BarcodeTextBox.Clear();
		}

		public static void FillRecycledDataTable()
		{
			using (var conn =
				new SqlConnection(App.ConnectionString))
			{
				conn.Open();
				if (!App.GetTableNames(conn, "RECYCLED").Contains(Settings.Default.Schema))
					using (var comm = new SqlCommand())
					{
						comm.Connection = conn;
						comm.CommandText = $"CREATE TABLE RECYCLED.[{Settings.Default.Schema}] ( ";
						for (var i = 0; i < 250; i++)
							if (i != 249)
								comm.CommandText += $"Column{i} TEXT,";
							else
								comm.CommandText += $"Column{i} TEXT";

						comm.CommandText += " )";
						comm.ExecuteNonQuery();
					}

				using (var cmd = new SqlCommand($"SELECT * FROM [RECYCLED].[{Settings.Default.Schema}]", conn))
				{
					cmd.CommandType = CommandType.Text;
					using (var sda = new SqlDataAdapter(cmd))
					{
						RecycledDataTable.TableName = Settings.Default.Schema;

						sda.Fill(RecycledDataTable);
					}
				}

				conn.Close();
			}

			RecycledDataTable.RowChanged += (sender, args) =>
			{
				if(args.Action == DataRowAction.Change || args.Action == DataRowAction.Add || args.Action == DataRowAction.Delete)
					RecycledDataTable.AcceptChanges();
			};
			RecycledDataTable.RowDeleted += (sender, args) =>
			{
				if(args.Action == DataRowAction.Delete)
					RecycledDataTable.AcceptChanges();
			};

			var autoSaveTimer = new Timer(12000)
			{
				Enabled = true
			};
			autoSaveTimer.Elapsed += (sender, args) => Task.Run(() => SaveRecycledTable());
		}

		private static void ClearRowFilters()
		{
			RecycledDataTable.DefaultView.RowFilter = string.Empty;
		}

		private void Search()
		{
			ClearRowFilters();

			try
			{
				if (!string.IsNullOrEmpty(SearchField.Text))
				{
					string selection = string.Empty;

					var counter = 0;
					foreach (DataColumn column in RecycledDataTable.Columns)
					{
						if (counter != RecycledDataTable.Columns.Count - 1)
							selection += $"[{column.ColumnName}] LIKE \'{SearchField.Text}*\' OR ";
						else
							selection += $"[{column.ColumnName}] LIKE \'{SearchField.Text}*\'";

						counter++;
					}

					RecycledDataTable.DefaultView.RowFilter = selection;
				}
				else
				{
					RecycledDataTable.DefaultView.RowFilter = string.Empty;
				}
			}
			catch (Exception)
			{
				//Debug.WriteLine(ex);
			}
		}

		private void InventoryItemFromBarcode()
		{
			if (string.IsNullOrEmpty(BarcodeTextBox.Text.Trim())) return;

			List<string> item = BarcodeTextBox.Text.Split('\t').ToList();
			//if (bool.TryParse(item[0], out bool _)) item.RemoveAt(0);
			var inventoried = false;

			foreach (DataRow row in RecycledDataTable.Rows)
			{
				List<string> fieldsInRow = row.ItemArray.Select(field => field.ToString()).ToList();

				foreach (string i in item)
				{
					if (!i.Equals(fieldsInRow[item.IndexOf(i)]))
						break;
					inventoried = true;
				}
			}

			if (inventoried) return;

			DataRow newRow = RecycledDataTable.NewRow();
			for (int i = 0; i < item.Count; i++)
				newRow[i] = item[i];
			Debug.WriteLine(string.Join(" | ", newRow.ItemArray));

			bool wasDeleted = false;
			foreach (DataTable table in MainWindow.MasterDataSet.Tables)
			{
				for (int j = 0; j < table.Rows.Count; j++)
				{
					var tableRowArr = table.Rows[j].ItemArray.ToList();
					var newRowArr = newRow.ItemArray.ToList();

					if (table.Columns[0].ColumnName.ToLower().Equals("inventoried") &&
					    bool.TryParse(tableRowArr[0].ToString(), out _))
					{
						tableRowArr.RemoveAt(0);
					}
					Debug.WriteLine(string.Join(string.Empty, tableRowArr));
					Debug.WriteLine(string.Join(string.Empty, newRowArr));

					if (!string.Join(string.Empty, tableRowArr)
						.Equals(string.Join(string.Empty, newRowArr))) continue;
					table.Rows[j].Delete();
					wasDeleted = true;
				}
			}

			if (!wasDeleted)
				RecycledDataTable.Rows.Add(newRow);
		}

		private void RecycledGrid_OnRecordDeleting(object sender, RecordDeletingEventArgs e)
		{
			if (MessageBox.Show("Are you sure? This action is irreversible. These records will be permanently deleted.",
				    "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
				e.Cancel = true;
		}
	}
}