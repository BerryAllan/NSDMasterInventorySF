#region Copyright Syncfusion Inc. 2001-2018.

// Copyright Syncfusion Inc. 2001-2018. All rights reserved.
// Use of this code is subject to the terms of our license.
// A copy of the current license can be obtained at any time by e-mailing
// licensing@syncfusion.com. Any infringement will be prosecuted under
// applicable laws. 

#endregion

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Core;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace NSDMasterInventorySFUWP
{
	/// <inheritdoc />
	/// <summary>
	/// Provides application-specific behavior to supplement the default Application class.
	/// </summary>
	sealed partial class App
	{
		public static volatile IPropertySet Settings;
		public static volatile bool SavingCurrently = false;
		public static volatile bool ThisMadeLastChange;
		public static volatile bool ThisIsNowConcurrent;
		public static volatile bool BackingUpCurrently;
		public static readonly object AutoSaveLock = new object();
		public static readonly object UpdateLock = new object();
		public static volatile string ConnectionString;
		private static readonly Random Random = new Random();
		/// <summary>
		/// Initializes the singleton application object.  This is the first line of authored code
		/// executed, and as such is the logical equivalent of main() or WinMain().
		/// </summary>
		public App()
		{
			InitializeComponent();
			Suspending += OnSuspending;
		}

		/// <summary>
		/// Invoked when the application is launched normally by the end user.  Other entry points
		/// will be used such as when the application is launched to open a specific file.
		/// </summary>
		/// <param name="e">Details about the launch request and process.</param>
		protected override void OnLaunched(LaunchActivatedEventArgs e)
		{
			Thread.CurrentThread.Name = "Main UI Thread";
			Settings = ApplicationData.Current.LocalSettings.Values;
			if(!Settings.ContainsKey("Server"))
				Settings.Add("Server", "localhost");
			if(!Settings.ContainsKey("UserID"))
				Settings.Add("UserID", "titom7373");
			if(!Settings.ContainsKey("Password"))
				Settings.Add("Password", "PeanutsWalnuts");
			if(!Settings.ContainsKey("Schema"))
				Settings.Add("Schema", "IT Tables");
			if(!Settings.ContainsKey("Database"))
				Settings.Add("Database", "Inventory");

			//ConfigurationEcnrypterDecrypter.UnEncryptConfig();
			ConnectionString =
				$"Server={Settings["Server"]};Database={Settings["Database"]};User ID={Settings["UserID"]};Password={Settings["Password"]};";
			try
			{
				using (var conn = new SqlConnection(ConnectionString))
				{
					conn.Open();
					conn.Close();
				}
			}
			catch
			{
				//TODO
				/*DatabaseManagerError dme = new DatabaseManagerError
				{
					ShowInTaskbar = true
				};
				dme.ShowDialog();*/
				return;
			}

			using (var conn = new SqlConnection(ConnectionString))
			{
				conn.Open();
				if (!GetAllNames(conn, "schemas").Contains($"{Settings["Schema"]}"))
					using (var comm = new SqlCommand($"CREATE SCHEMA [{Settings["Schema"]}]", conn))
					{
						comm.ExecuteNonQuery();
					}

				if (!GetAllNames(conn, "schemas").Contains($"{Settings["Schema"]}_BACKUPS"))
					using (var comm = new SqlCommand($"CREATE SCHEMA [{Settings["Schema"]}_BACKUPS]", conn))
					{
						comm.ExecuteNonQuery();
					}

				if (!GetAllNames(conn, "schemas").Contains($"{Settings["Schema"]}_PREFABS"))
					using (var comm = new SqlCommand($"CREATE SCHEMA [{Settings["Schema"]}_PREFABS]", conn))
					{
						comm.ExecuteNonQuery();
					}

				if (!GetAllNames(conn, "schemas").Contains($"{Settings["Schema"]}_COMBOBOXES"))
					using (var comm = new SqlCommand($"CREATE SCHEMA [{Settings["Schema"]}_COMBOBOXES]", conn))
					{
						comm.ExecuteNonQuery();
					}

				if (!GetAllNames(conn, "schemas").Contains("RECYCLED"))
					using (var comm = new SqlCommand("CREATE SCHEMA RECYCLED", conn))
					{
						comm.ExecuteNonQuery();
					}

				conn.Close();
			}

			//SqlDependency.Start(ConnectionString);
			//Debug.WriteLine("Starting server...");
			/*Task.Run(() =>
			{
				StartServer();
				ConnectAsync();
			});*/
			//Debug.WriteLine("Connecting to server...");

			// Do not repeat app initialization when the Window already has content,
			// just ensure that the page is active
			if (!(Window.Current.Content is Frame rootFrame))
			{
				// Create a Frame to act as the navigation context and navigate to the first page
				rootFrame = new Frame();

				rootFrame.NavigationFailed += OnNavigationFailed;

				if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
				{
					//TODO: Load state from previously suspended application
				}

				// Place the frame in the current Window
				Window.Current.Content = rootFrame;
			}

			if (e.PrelaunchActivated == false)
			{
				if (rootFrame.Content == null)
				{
					// When the navigation stack isn't restored navigate to the first page,
					// configuring the new page by passing required information as a navigation
					// parameter
					rootFrame.Navigate(typeof(MainPage), e.Arguments);
				}

				// Ensure the current page is active
				Window.Current.Activate();
			}

			ExtendAcrylicIntoTitleBar();
		}
		public static void Restart()
		{
			//TODO: Process.Start(ResourceAssembly.Location);
			Current.Exit();
		}

		public static void Backup()
		{
			if (BackingUpCurrently) return;
			BackingUpCurrently = true;
			Task task = Task.Run(() =>
			{
				try
				{
					using (var conn = new SqlConnection(ConnectionString))
					{
						conn.Open();

						if (!GetAllNames(conn, "schemas").Contains($"{Settings["Schema"]}_BACKUPS"))
							using (var comm = new SqlCommand($"CREATE SCHEMA [{Settings["Schema"]}_BACKUPS]",
								conn))
							{
								comm.ExecuteNonQuery();
							}

						foreach (string table in GetTableNames(conn, Settings["Schema"].ToString()))
						{
							if (GetTableNames(conn, $"{Settings["Schema"]}_BACKUPS").Contains(table))
								using (var comm =
									new SqlCommand($"DROP TABLE [{Settings["Schema"]}_BACKUPS].[{table}]", conn))
									comm.ExecuteNonQuery();
							using (var comm = new SqlCommand())
							{
								comm.Connection = conn;
								//Debug.WriteLine(Path.GetFileNameWithoutExtension(file));
								comm.CommandText = $"CREATE TABLE [{Settings["Schema"]}_BACKUPS].[{table}] ( ";
								var j = 0;
								List<string> columns = GetAllColumnsOfTable(conn, table);
								foreach (string column in columns)
								{
									if (j != columns.Count - 1)
										comm.CommandText += $"[{column}] NVARCHAR(MAX), ";
									else
										comm.CommandText += $"[{column}] NVARCHAR(MAX)";
									j++;
								}

								comm.CommandText += " )";

								comm.ExecuteNonQuery();
							}

							using (var comm =
								new SqlCommand(
									$"INSERT INTO [{Settings["Schema"]}_BACKUPS].[{table}] SELECT * FROM [{Settings["Schema"]}].[{table}]",
									conn))
							{
								comm.ExecuteNonQuery();
							}
						}

						conn.Close();
					}
				}
				catch
				{
					//MessageBox.Show("Failed Backup.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
					throw;
				}

				Thread.CurrentThread.IsBackground = true;
			});
			Task.Run(() =>
			{
				task.Wait();
				BackingUpCurrently = false;
			});
		}
		public static List<int> GetSorts(string prefab)
		{
			using (var conn = new SqlConnection(ConnectionString))
			{
				conn.Open();
				DataTable prefabTable = GetPrefabDataTable(conn, $"{Settings["Schema"]}_PREFABS", prefab);

				var treeMap = new SortedDictionary<int, int>();
				var sorts = new List<int>();

				for (var i = 0; i < prefabTable.Rows.Count; i++)
					if (!string.IsNullOrEmpty(prefabTable.Rows[i]["SORTBYS"].ToString()) &&
					    !prefabTable.Rows[i]["SORTBYS"].ToString().Equals("0"))
						treeMap.Add(int.Parse(prefabTable.Rows[i]["SORTBYS"].ToString()), i);

				SortedDictionary<int, int>.KeyCollection keySets = treeMap.Keys;

				foreach (int i in keySets) sorts.Add(treeMap[i]);

				//sorts.Reverse();
				conn.Close();
				return sorts;
			}
		}

		public static List<int> GetGroups(string prefab)
		{
			using (var conn = new SqlConnection(ConnectionString))
			{
				conn.Open();
				DataTable prefabTable = GetPrefabDataTable(conn, $"{Settings["Schema"]}_PREFABS", prefab);

				var treeMap = new SortedDictionary<int, int>();
				var sorts = new List<int>();

				for (var i = 0; i < prefabTable.Rows.Count; i++)
					if (!string.IsNullOrEmpty(prefabTable.Rows[i]["GROUPS"].ToString()) &&
					    !prefabTable.Rows[i]["GROUPS"].ToString().Equals("0"))
						treeMap.Add(int.Parse(prefabTable.Rows[i]["GROUPS"].ToString()), i);

				SortedDictionary<int, int>.KeyCollection keySets = treeMap.Keys;

				foreach (int i in keySets) sorts.Add(treeMap[i]);

				//sorts.Reverse();
				conn.Close();
				return sorts;
			}
		}

		public static List<string> GetTableNames(SqlConnection conn)
		{
			//SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString);
			var tables = new List<string>();

			using (var cmd = new SqlCommand("SELECT * FROM INFORMATION_SCHEMA.TABLES", conn))
			{
				using (IDataReader dr = cmd.ExecuteReader())
				{
					while (dr.Read())
						if (dr["TABLE_SCHEMA"].ToString().Equals(Settings["Schema"]) &&
						    dr["TABLE_CATALOG"].Equals(conn.Database))
							tables.Add(dr["TABLE_NAME"].ToString());
				}
			}

			tables.Sort();
			return tables;
		}

		public static List<string> GetTableNames(SqlConnection conn, string schema)
		{
			var tables = new List<string>();

			using (var cmd = new SqlCommand("SELECT * FROM INFORMATION_SCHEMA.TABLES", conn))
			{
				if (conn.State != ConnectionState.Open)
					conn.Open();
				using (IDataReader dr = cmd.ExecuteReader())
				{
					while (dr.Read())
						if (dr["TABLE_SCHEMA"].ToString().Equals(schema))
							tables.Add(dr["TABLE_NAME"].ToString());
				}
			}

			tables.Sort();
			return tables;
		}

		public static List<string> GetAllNames(SqlConnection conn, string type)
		{
			var list = new List<string>();

			// MainSet up a command with the given query and associate
			// this with the current connection.
			using (var cmd = new SqlCommand($"SELECT name FROM sys.{type}", conn))
			{
				using (IDataReader dr = cmd.ExecuteReader())
				{
					while (dr.Read()) list.Add(dr[0].ToString());
				}
			}

			return list;
		}

		public static List<string> GetAllColumnsOfTable(SqlConnection conn, string tableName)
		{
			var list = new List<string>();

			using (var cmd = new SqlCommand("SELECT * FROM INFORMATION_SCHEMA.COLUMNS", conn))
			{
				using (IDataReader dr = cmd.ExecuteReader())
				{
					while (dr.Read())
						if (dr["TABLE_NAME"].Equals(tableName) && dr["TABLE_SCHEMA"].Equals(Settings["Schema"]) &&
						    dr["TABLE_CATALOG"].Equals(conn.Database))
							list.Add(dr["COLUMN_NAME"].ToString());
				}
			}

			return list;
		}

		public static List<string> GetAllColumnsOfTable(SqlConnection conn, string schema, string tableName)
		{
			var list = new List<string>();

			using (var cmd = new SqlCommand("SELECT * FROM INFORMATION_SCHEMA.COLUMNS", conn))
			{
				using (IDataReader dr = cmd.ExecuteReader())
				{
					while (dr.Read())
						if (dr["TABLE_NAME"].Equals(tableName) && dr["TABLE_SCHEMA"].Equals(schema) &&
						    dr["TABLE_CATALOG"].Equals(conn.Database))
							list.Add(dr["COLUMN_NAME"].ToString());
				}
			}

			return list;
		}

		public static string GetPrefabOfTable(SqlConnection conn, string table)
		{
			string columnNames = string.Empty;

			foreach (string columnName in GetAllColumnsOfTable(conn, table)) columnNames += columnName;

			foreach (string prefab in GetTableNames(conn, $"{Settings["Schema"]}_PREFABS"))
			{
				DataTable propTable = GetPrefabDataTable(conn, $"{Settings["Schema"]}_PREFABS", prefab);
				string fieldNames = string.Empty;
				for (var i = 0; i < propTable.Rows.Count; i++) fieldNames += propTable.Rows[i]["COLUMNS"];

				if (columnNames.Equals(fieldNames))
					return prefab;
			}

			return string.Empty;
		}

		public static List<DataTable> GetDatatablesOfPrefab(DataSet tables, string prefab)
		{
			using (var conn = new SqlConnection(ConnectionString))
			{
				conn.Open();
				var dataTables = new List<DataTable>();

				DataTable prefabTable = GetPrefabDataTable(conn, $"{Settings["Schema"]}_PREFABS", prefab);
				string fieldNames = string.Empty;
				for (var i = 0; i < prefabTable.Rows.Count; i++) fieldNames += prefabTable.Rows[i]["COLUMNS"];

				foreach (DataTable table in tables.Tables)
				{
					string columnNames = string.Empty;
					foreach (DataColumn column in table.Columns)
						columnNames += column.ColumnName;

					if (columnNames.Equals(fieldNames))
						dataTables.Add(table);
				}

				conn.Close();
				return dataTables;
			}
		}

		public static List<string> GetTablesOfPrefab(string prefab)
		{
			using (var conn = new SqlConnection(ConnectionString))
			{
				var tables = new List<string>();

				DataTable prefabTable = GetPrefabDataTable(conn, $"{Settings["Schema"]}_PREFABS", prefab);
				string fieldNames = string.Empty;
				for (var i = 0; i < prefabTable.Rows.Count; i++) fieldNames += prefabTable.Rows[i]["COLUMNS"];

				foreach (string tableName in GetTableNames(conn))
				{
					string columnNames = string.Empty;
					foreach (string columnName in GetAllColumnsOfTable(conn, tableName))
						columnNames += columnName;

					if (columnNames.Equals(fieldNames))
						tables.Add(tableName);
				}

				return tables;
			}
		}

		public static DataSet MainSet(MasterTablePage page)
		{
			DataSet dataTableList = new DataSet(Settings["Schema"].ToString());

			var conn = new SqlConnection(ConnectionString);
			conn.Open();
			foreach (string tableName in GetTableNames(conn))
			{
				var cmd = new SqlCommand($"SELECT * FROM [{Settings["Schema"]}].[{tableName}]", conn);
				var sda = new SqlDataAdapter(cmd)
				{
					AcceptChangesDuringUpdate = true,
					AcceptChangesDuringFill = true
				};

				DataTable table = new DataTable(tableName);
				lock (AutoSaveLock)
				{
					sda.Fill(table);
				}

				dataTableList.Tables.Add(table);
				//foreach(DataRow row in set.Tables[tableName].Rows)
				//	for(int i = 0; i < row.ItemArray.Length; i++)
				//		if (string.IsNullOrEmpty(row[i].ToString()))
				//			row[i] = null;

				Task.Run(() =>
				{
					lock (AutoSaveLock)
					{
						Thread.CurrentThread.Name = $"AutoSave/Update {table.TableName}";
						Thread.CurrentThread.IsBackground = true;
						SetTableOnChangeds(table, sda, conn, page);
					}
				});
				//Task.Run(() => NotifyNewItem(table, page, dataTableList.Tables.IndexOf(table)));
			}

			conn.Close();

			return dataTableList;
		}

		private static void SetTableOnChangeds(DataTable table, SqlDataAdapter sda, SqlConnection conn,
			MasterTablePage page)
		{
			if (conn.State != ConnectionState.Open)
				conn.Open();
			table.RowChanged += (sender, args) =>
			{
				if (!(args.Action == DataRowAction.Add || args.Action == DataRowAction.Change)) return;

				SetSdaCommands(table, args, sda, conn, page);
				ThisMadeLastChange = true;
			};
			table.RowDeleting += (sender, args) =>
			{
				DataRow newRecycledRow = Recycled.RecycledDataTable.NewRow();
				for (int i = 0; i < args.Row.ItemArray.Length; i++)
					newRecycledRow[i] = args.Row[i];
				Recycled.RecycledDataTable.Rows.Add(newRecycledRow);
			};
			table.RowDeleted += (sender, args) =>
			{
				if (args.Action != DataRowAction.Delete) return;

				SetSdaCommands(table, args, sda, conn, page);
				ThisMadeLastChange = true;
			};
			if (conn.State != ConnectionState.Closed)
				conn.Close();
		}

		public static DataTable GetPrefabDataTable(SqlConnection conn, string schema, string prefabName)
		{
			if (string.IsNullOrEmpty(prefabName))
				return new DataTable();

			if (!GetTableNames(conn, schema).Contains(prefabName))
			{
				if (schema.Equals($"{Settings["Schema"]}_PREFABS"))
					using (var comm = new SqlCommand(
						$"CREATE TABLE [{schema}].[{prefabName}] (COLUMNS TEXT, TYPES TEXT, SORTBYS TEXT, GROUPS TEXT)",
						conn))
					{
						comm.ExecuteNonQuery();
					}
				else
				{
					return new DataTable();
				}
			}

			using (var cmd = new SqlCommand($"SELECT * FROM [{schema}].[{prefabName}]", conn))
			{
				cmd.CommandType = CommandType.Text;
				using (var sda = new SqlDataAdapter(cmd))
				{
					using (var dt = new DataTable())
					{
						dt.TableName = prefabName;
						sda.Fill(dt);

						foreach (DataRow row in dt.Rows)
							for (var i = 0; i < row.ItemArray.Length; i++)
								if (string.IsNullOrEmpty(row[i].ToString()))
									row[i] = null;

						return dt;
					}
				}
			}
		}

		public static string GetPrefabOfDataTable(SqlConnection conn, DataTable dt)
		{
			string columnNames = string.Empty;
			foreach (DataColumn column in dt.Columns) columnNames += column.ColumnName;

			foreach (string tableName in GetTableNames(conn, $"{Settings["Schema"]}_PREFABS"))
			{
				string fieldNames = string.Empty;

				DataTable prefabTable = GetPrefabDataTable(conn, $"{Settings["Schema"]}_PREFABS", tableName);
				for (var i = 0; i < prefabTable.Rows.Count; i++)
					fieldNames += prefabTable.Rows[i]["COLUMNS"];

				if (fieldNames.Equals(columnNames))
					return tableName;
			}

			return string.Empty;
		}

		public static string RandomString(int length)
		{
			const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789abcdefghijklmnopqrstuvwxyz";
			return new string(Enumerable.Repeat(chars, length)
				.Select(s => s[Random.Next(s.Length)]).ToArray());
		}

		public static void SetSdaCommands(DataTable table, DataRowChangeEventArgs args, SqlDataAdapter sda,
			SqlConnection conn, MasterTablePage page)
		{
			lock (AutoSaveLock)
			{
				//Debug.WriteLine(string.Join(" | ", args.Row.ItemArray));
				if (ThisIsNowConcurrent)
				{
					ThisIsNowConcurrent = false;
					return;
				}
				/*List<object> oldRow = new List<object>();
				try
				{
					for (int n = 0; n < args.Row.ItemArray.Length; n++)
					{
						try
						{
							oldRow.Add(args.Row[n, DataRowVersion.Original]);
						}
						catch
						{
							break;
						}
					}
				}
				catch
				{
					//ignored
				}*/

				int rowIndex = table.Rows.IndexOf(args.Row);

				var sdaUpdateCommand = new SqlCommand
				{
					Connection = conn,
					CommandText = $"UPDATE [{Settings["Schema"]}].[{table.TableName}] SET "
				};
				var sdaInsertCommand = new SqlCommand
				{
					Connection = conn,
					CommandText = $"INSERT INTO [{Settings["Schema"]}].[{table.TableName}] ("
				};
				var sdaDeleteCommand = new SqlCommand
				{
					Connection = conn,
					CommandText = $"DELETE FROM [{Settings["Schema"]}].[{table.TableName}] WHERE "
				};

				int i = 0;
				List<string> columnsList = new List<string>();
				foreach (DataColumn col in table.Columns)
					columnsList.Add(col.ColumnName);
				string[] columns = columnsList.ToArray();
				if (args.Action != DataRowAction.Delete)
					foreach (var column in columns)
					{
						var value = $"'{table.Rows[rowIndex][i].ToString().Replace("'", "''").Replace("\"", "\"\"")}'";
						if (table.Rows[rowIndex][i] is DBNull || table.Rows[rowIndex][i] == null)
							value = "NULL";

						/*_dispatcher.Invoke(() =>
						{
							if (string.IsNullOrEmpty(value.ToString()) &&
							    NSDMasterInventorySF.MainWindow.MasterDataGrids[page.MasterTabControl.SelectedIndex].Columns[i] is
								    GridCheckBoxColumn)
								value = "False";
						});*/
						if (i != columns.Length - 1)
						{
							sdaUpdateCommand.CommandText += $"[{column}] = {value}, ";
							sdaInsertCommand.CommandText += $"[{column}], ";
						}
						else
						{
							sdaUpdateCommand.CommandText += $"[{column}] = {value} ";
							sdaInsertCommand.CommandText += $"[{column}]) ";
						}

						i++;
					}

				sdaUpdateCommand.CommandText += "WHERE ";
				sdaInsertCommand.CommandText += "VALUES (";
				int k = 0;

				foreach (var column in columns)
				{
					string value = "NULL";
					if (args.Row.HasVersion(DataRowVersion.Original))
					{
						value =
							$"'{table.Rows[rowIndex][k, DataRowVersion.Original].ToString().Replace("'", "''").Replace("\"", "\"\"")}'";
						if (table.Rows[rowIndex][k, DataRowVersion.Original] is DBNull ||
						    table.Rows[rowIndex][k, DataRowVersion.Original] == null)
							value = "NULL";
					}

					if (k != columns.Length - 1)
					{
						switch (value)
						{
							case "NULL":
								sdaUpdateCommand.CommandText += $"[{column}] IS {value} AND ";
								sdaInsertCommand.CommandText += $"{value}, ";
								sdaDeleteCommand.CommandText += $"[{column}] IS {value} AND ";
								break;
							default:
								sdaUpdateCommand.CommandText += $"[{column}] = {value} AND ";
								sdaInsertCommand.CommandText += $"{value}, ";
								sdaDeleteCommand.CommandText += $"[{column}] = {value} AND ";
								break;
						}
					}
					else
					{
						switch (value)
						{
							case "NULL":
								sdaUpdateCommand.CommandText += $"[{column}] IS {value} ";
								sdaInsertCommand.CommandText += $"{value})";
								sdaDeleteCommand.CommandText += $"[{column}] IS {value}";
								break;
							default:
								sdaUpdateCommand.CommandText += $"[{column}] = {value} ";
								sdaInsertCommand.CommandText += $"{value})";
								sdaDeleteCommand.CommandText += $"[{column}] = {value}";
								break;
						}
					}

					k++;
				}

				sda.UpdateCommand = sdaUpdateCommand;
				sda.InsertCommand = sdaInsertCommand;
				sda.DeleteCommand = sdaDeleteCommand;
				try
				{
					sda.Update(table);
					ThisMadeLastChange = true;
				}
				catch
				{
					//Debug.WriteLine("UPDATE FAILED");
					//table.AcceptChanges();
					//Debug.WriteLine("Failed Row: " + table.Rows.IndexOf(args.Row) + ": " + string.Join(" | ", args.Row.ItemArray));
					/*if (args.Action == DataRowAction.Change)
					{
						string selectTxt = sdaDeleteCommand.CommandText
							.Substring(sdaDeleteCommand.CommandText.IndexOf("WHERE", StringComparison.Ordinal)).Replace("WHERE ", string.Empty);

						foreach (DataRow row in table.Select(selectTxt))
						{
							for (int index = 0; index < row.ItemArray.Length; index++)
							{
								ThisIsNowConcurrent = true;
								Debug.WriteLine("orig: " + row[index] + " change: " + args.Row[index]);
								row[index] = args.Row[index];
							}
						}
						if (conn.State != ConnectionState.Open)
							conn.Open();
						sda.InsertCommand.ExecuteNonQuery();
						if (conn.State != ConnectionState.Closed)
							conn.Close();
						ThisMadeLastChange = true;
					}*/
					page.InitializeOrRefreshEverything(page.MasterPivot.SelectedIndex);
					/*MessageBox.Show(
						$"Failed to update Database {App.Settings["Database"]}; Error occured.\nThis was most likely caused by a concurrency issue and/or duplicate rows. The datagrids have been refreshed.",
						"Error!", MessageBoxButton.OK, MessageBoxImage.Error);*/
					throw;
					//ThisMadeLastChange = true;
					//page.SaveToDb();
				}

				/*string selectText = sdaDeleteCommand.CommandText
					.Substring(sdaDeleteCommand.CommandText.IndexOf("WHERE", StringComparison.Ordinal))
					.Replace("WHERE ", string.Empty);*/

				/*switch (args.Action)
				{
					case DataRowAction.Add:
						break;
					case DataRowAction.Change:
						//foreach (DataRow row in table.Select(selectText))
						//{
						//	for (int index = 0; index < row.ItemArray.Length; index++)
						//	{
						//		ThisIsNowConcurrent = true;
						//		Debug.WriteLine("orig: " + row[index] + " change: " + args.Row[index]);
						//		row[index] = args.Row[index];
						//	}
						//}
						break;
					case DataRowAction.Delete:
						foreach (DataRow row in table.Select(selectText))
						{
							ThisIsNowConcurrent = true;
							row.Delete();
						}
						table.AcceptChanges();
						break;
				}*/

				//ThisIsNowConcurrent = true;

				/*switch (args.Action)
				{
					case DataRowAction.Delete:
						HubProxy.Invoke("Send", "DELETE", table.TableName, string.Join("\t", oldRow.ToArray()),
							string.Join("\t", oldRow));
						break;
					case DataRowAction.Change:
						HubProxy.Invoke("Send", "UPDATE", table.TableName, string.Join("\t", oldRow.ToArray()),
							string.Join("\t", args.Row.ItemArray));
						break;
					case DataRowAction.Add:
						HubProxy.Invoke("Send", "INSERT", table.TableName, string.Join("\t", oldRow.ToArray()),
							string.Join("\t", args.Row.ItemArray));
						break;
				}*/
			}
		}

		/// <summary>
		/// Invoked when Navigation to a certain page fails
		/// </summary>
		/// <param name="sender">The Frame which failed navigation</param>
		/// <param name="e">Details about the navigation failure</param>
		private static void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
		{
			throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
		}

		/// <summary>
		/// Invoked when application execution is being suspended.  Application state is saved
		/// without knowing whether the application will be terminated or resumed with the contents
		/// of memory still intact.
		/// </summary>
		/// <param name="sender">The source of the suspend request.</param>
		/// <param name="e">Details about the suspend request.</param>
		private static void OnSuspending(object sender, SuspendingEventArgs e)
		{
			var deferral = e.SuspendingOperation.GetDeferral();
			//TODO: Save application state and stop any background activity
			deferral.Complete();
		}

		/// Extend acrylic into the title bar. 
		private static void ExtendAcrylicIntoTitleBar()
		{
			CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = true;
			ApplicationViewTitleBar titleBar = ApplicationView.GetForCurrentView().TitleBar;
			titleBar.ButtonBackgroundColor = Colors.Transparent;
			titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
		}
	}
}