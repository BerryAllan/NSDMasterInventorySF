using System.Collections.Generic;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using NSDMasterInventorySF.Properties;

namespace NSDMasterInventorySF
{
	/// <summary>
	///     Interaction logic for RestorationManager.xaml
	/// </summary>
	public partial class RestorationManager : Window
	{
		public RestorationManager()
		{
			InitializeComponent();

			PopulateListBox();
		}

		public void PopulateListBox()
		{
			using (var conn = new SqlConnection(App.ConnectionString))
			{
				conn.Open();
				foreach (string tableName in App.GetTableNames(conn, $"{Settings.Default.Schema}_BACKUPS")) SheetListBox.Items.Add(tableName);

				conn.Close();
			}
		}

		private void SheetListBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			RestoreButton.IsEnabled = SheetListBox.SelectedItems.Count > 0;
		}

		private void OnRestoreButtonClicked(object sender, RoutedEventArgs e)
		{
			using (var conn = new SqlConnection(App.ConnectionString))
			{
				conn.Open();

				if (App.GetTableNames(conn).Contains(SheetListBox.SelectedItem.ToString()))
					using (var comm = new SqlCommand($"DROP TABLE [{Settings.Default.Schema}].[{SheetListBox.SelectedItem}]", conn))
					{
						comm.ExecuteNonQuery();
					}

				using (var comm = new SqlCommand())
				{
					comm.Connection = conn;
					//Debug.WriteLine(Path.GetFileNameWithoutExtension(file));
					comm.CommandText = $"CREATE TABLE [{Settings.Default.Schema}].[{SheetListBox.SelectedItem}] ( ";
					var j = 0;
					List<string> columns = App.GetAllColumnsOfTable(conn, $"{Settings.Default.Schema}_BACKUPS", SheetListBox.SelectedItem.ToString());
					foreach (string column in columns)
					{
						if (j != columns.Count - 1)
							comm.CommandText += $"[{column}] TEXT, ";
						else
							comm.CommandText += $"[{column}] TEXT";
						j++;
					}

					comm.CommandText += " )";

					comm.ExecuteNonQuery();
				}

				using (var comm =
					new SqlCommand(
						$"INSERT INTO [{Settings.Default.Schema}].[{SheetListBox.SelectedItem}] SELECT * FROM [{Settings.Default.Schema}_BACKUPS].[{SheetListBox.SelectedItem}]",
						conn))
				{
					comm.ExecuteNonQuery();
				}

				conn.Close();
			}

			SheetListBox.SelectedItem = null;
		}
	}
}