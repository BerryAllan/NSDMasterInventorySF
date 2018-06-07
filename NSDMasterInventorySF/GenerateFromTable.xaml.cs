using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using NSDMasterInventorySF.Properties;

namespace NSDMasterInventorySF
{
	/// <summary>
	/// Interaction logic for GenerateFromTable.xaml
	/// </summary>
	public partial class GenerateFromTable
	{
		private readonly PrefabManager _mgr;

		public GenerateFromTable(PrefabManager mgr)
		{
			InitializeComponent();
			_mgr = mgr;

			foreach (DataTable table in MainWindow.MasterDataSet.Tables)
				TableList.Items.Add(table.TableName);
		}

		private void DoneButton_OnClick(object sender, RoutedEventArgs e)
		{
			using (var conn = new SqlConnection(App.ConnectionString))
			{
				conn.Open();
				if (App.GetTableNames(conn, $"{Settings.Default.Schema}_PREFABS").Contains(PrefabNameBox.Text))
					MessageBox.Show("Identical Prefab exists. Please rename.", "Warning", MessageBoxButton.OK,
						MessageBoxImage.Warning);

				DataTable prefabTable = new DataTable();
				prefabTable.Columns.Add("COLUMNS");
				prefabTable.Columns.Add("TYPES");
				prefabTable.Columns.Add("SORTBYS");
				prefabTable.Columns.Add("GROUPS");
				foreach (DataColumn column in MainWindow.MasterDataSet.Tables[TableList.SelectedItem.ToString()].Columns)
				{
					DataRow dr = prefabTable.NewRow();
					dr[0] = column.ColumnName;
					dr[1] = "TextField";
					prefabTable.Rows.Add(dr);
				}

				if (conn.State != ConnectionState.Open)
					conn.Open();
				using (var comm =
					new SqlCommand(
						$"CREATE TABLE [{Settings.Default.Schema}_PREFABS].[{PrefabNameBox.Text}] (COLUMNS NVARCHAR(MAX), TYPES NVARCHAR(MAX), SORTBYS NVARCHAR(MAX), GROUPS NVARCHAR(MAX))",
						conn))
				{
					comm.ExecuteNonQuery();
				}

				var bulkCopy =
					new SqlBulkCopy(conn)
					{
						DestinationTableName =
							$"[{Settings.Default.Schema}_PREFABS].[{PrefabNameBox.Text}]"
					};
				bulkCopy.WriteToServer(prefabTable);
				conn.Close();
			}
			_mgr.PopulateListBox();
			Close();
		}

		private void TableList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			DoneButton.IsEnabled = TableList.SelectedItems.Count > 0 && !string.IsNullOrEmpty(PrefabNameBox.Text);
		}

		private void PrefabNameBox_OnTextChanged(object sender, TextChangedEventArgs e)
		{
			DoneButton.IsEnabled = TableList.SelectedItems.Count > 0 && !string.IsNullOrEmpty(PrefabNameBox.Text);
		}
	}
}