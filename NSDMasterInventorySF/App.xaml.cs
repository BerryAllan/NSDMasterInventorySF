using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.Owin.Cors;
using Microsoft.Owin.Hosting;
using NSDMasterInventorySF.io;
using NSDMasterInventorySF.Properties;
using Owin;
using Syncfusion.Data.Extensions;
using ConnectionState = System.Data.ConnectionState;
using DataColumn = System.Data.DataColumn;
using DataRow = System.Data.DataRow;

namespace NSDMasterInventorySF
{
	/// <inheritdoc />
	/// <summary>
	///     Interaction logic for App.xaml
	/// </summary>
	public partial class App
	{
		public static volatile bool SavingCurrently = false;
		public static volatile bool ThisMadeLastChange;
		public static volatile bool ThisIsNowConcurrent;
		public static volatile bool BackingUpCurrently;
		public static readonly object AutoSaveLock = new object();
		public static readonly object UpdateLock = new object();
		private static Dispatcher _dispatcher;

		private static readonly string GetLastExecutedQueriesCmd =
			$@"SELECT deqs.last_execution_time AS [Time], dest.text AS [Query]
										FROM sys.dm_exec_query_stats AS deqs
										CROSS APPLY sys.dm_exec_sql_text(deqs.sql_handle) AS dest
										CROSS APPLY sys.dm_exec_plan_attributes(deqs.plan_handle) depa
										WHERE (depa.attribute = 'dbid' AND depa.value = db_id('{Settings.Default.Database}')) OR
													 dest.dbid = db_id('{Settings.Default.Database}')
										ORDER BY deqs.last_execution_time DESC";

		public static volatile string ConnectionString;

		//Client stuff
		private static readonly Random Random = new Random();
		public static IHubProxy HubProxy { get; set; }
		const string ServerUriClient = "http://localhost:8080/signalr";
		public static HubConnection Connection { get; set; }

		//Server stuff
		public static IDisposable SignalR { get; set; }
		const string ServerUriServer = "http://localhost:8080";

		protected override void OnStartup(StartupEventArgs e)
		{
			Thread.CurrentThread.Name = "Main Application Thread";
			base.OnStartup(e);
			_dispatcher = Dispatcher;

			Settings.Default.Password =
				ConfigurationEcnrypterDecrypter.ToInsecureString(
					ConfigurationEcnrypterDecrypter.DecryptString(Settings.Default.Password));

			//ConfigurationEcnrypterDecrypter.UnEncryptConfig();
			ConnectionString =
				$"Server={Settings.Default.Server};Database={Settings.Default.Database};User ID={Settings.Default.UserID};Password={Settings.Default.Password};";
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
				DatabaseManagerError dme = new DatabaseManagerError
				{
					ShowInTaskbar = true
				};
				dme.ShowDialog();
				return;
			}

			using (var conn = new SqlConnection(ConnectionString))
			{
				conn.Open();
				if (!GetAllNames(conn, "schemas").Contains($"{Settings.Default.Schema}"))
				{
					DatabaseManagerError dme = new DatabaseManagerError
					{
						ShowInTaskbar = true
					};
					dme.ShowDialog();
					return;
				}

				if (!GetAllNames(conn, "schemas").Contains($"{Settings.Default.Schema}_BACKUPS"))
					using (var comm = new SqlCommand($"CREATE SCHEMA [{Settings.Default.Schema}_BACKUPS]", conn))
					{
						comm.ExecuteNonQuery();
					}

				if (!GetAllNames(conn, "schemas").Contains($"{Settings.Default.Schema}_PREFABS"))
					using (var comm = new SqlCommand($"CREATE SCHEMA [{Settings.Default.Schema}_PREFABS]", conn))
					{
						comm.ExecuteNonQuery();
					}

				if (!GetAllNames(conn, "schemas").Contains($"{Settings.Default.Schema}_COMBOBOXES"))
					using (var comm = new SqlCommand($"CREATE SCHEMA [{Settings.Default.Schema}_COMBOBOXES]", conn))
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
		}

		public static void StartServer()
		{
			try
			{
				SignalR = WebApp.Start<Startup>(ServerUriServer);
			}
			catch (TargetInvocationException)
			{
				Debug.WriteLine("A server is already running at " + ServerUriServer);
				return;
			}

			Debug.WriteLine("Server started at " + ServerUriServer);
		}

		public static async void ConnectAsync()
		{
			Connection = new HubConnection(ServerUriClient);
			Connection.Closed += Connection_Closed;
			HubProxy = Connection.CreateHubProxy("MyHub");
			//Handle incoming event from server: use Invoke to write to console from SignalR's thread
			HubProxy.On<string, string, string, string>("AddMessage", (command, tableName, oldRow, newRow) =>
			{
				Debug.WriteLine($"{command}: {tableName}: {oldRow}: {newRow}");
				if (ThisMadeLastChange)
				{
					ThisMadeLastChange = false;
					return;
				}

				Debug.WriteLine($"{newRow}");
				string selectCommand = string.Empty;
				int i = 0;
				string[] oldValues = oldRow.Split('\t');
				string[] newValues = newRow.Split('\t');
				foreach (string value in oldValues)
				{
					if (i != oldValues.Length - 1)
						selectCommand +=
							$"[{NSDMasterInventorySF.MainWindow.MasterDataSet.Tables[tableName].Columns[i].ColumnName}] = '{value}' AND ";
					else
						selectCommand +=
							$"[{NSDMasterInventorySF.MainWindow.MasterDataSet.Tables[tableName].Columns[i].ColumnName}] = '{value}'";
					i++;
				}

				Debug.WriteLine(selectCommand);
				switch (command)
				{
					case "UPDATE":
						foreach (DataRow rowUpd in NSDMasterInventorySF.MainWindow.MasterDataSet.Tables[tableName]
							.Select(selectCommand))
						{
							for (int k = 0; k < rowUpd.ItemArray.Length; k++)
							{
								ThisIsNowConcurrent = true;
								rowUpd[k] = newValues[k];
							}
						}

						break;
					case "INSERT":
						DataRow rowIns = NSDMasterInventorySF.MainWindow.MasterDataSet.Tables[tableName].NewRow();
						foreach (string val in newValues)
							rowIns[newValues.IndexOf(val)] = val;
						ThisIsNowConcurrent = true;
						NSDMasterInventorySF.MainWindow.MasterDataSet.Tables[tableName].Rows.Add(rowIns);
						break;
					case "DELETE":
						foreach (DataRow rowDel in NSDMasterInventorySF.MainWindow.MasterDataSet.Tables[tableName]
							.Select(selectCommand))
						{
							ThisIsNowConcurrent = true;
							rowDel.Delete();
						}

						break;
					default:
						Debug.WriteLine("didn't update table");
						break;
				}
			});
			try
			{
				await Connection.Start();
			}
			catch (HttpRequestException)
			{
				Debug.WriteLine("Unable to connect to server: Start server before connecting clients.");
				//No connection: Don't enable Send button or show chat UI
				return;
			}

			//Show chat UI; hide login UI
			Debug.WriteLine("Connected to server at " + ServerUriClient);
		}

		private static void Connection_Closed()
		{
			//Hide chat UI; show login UI
			Debug.WriteLine("DISCONNECTED!!!");
		}

		public static void Restart()
		{
			Process.Start(ResourceAssembly.Location);
			Current.Shutdown();
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

						if (!GetAllNames(conn, "schemas").Contains($"{Settings.Default.Schema}_BACKUPS"))
							using (var comm = new SqlCommand($"CREATE SCHEMA [{Settings.Default.Schema}_BACKUPS]",
								conn))
							{
								comm.ExecuteNonQuery();
							}

						foreach (string table in GetTableNames(conn, Settings.Default.Schema))
						{
							if (GetTableNames(conn, $"{Settings.Default.Schema}_BACKUPS").Contains(table))
								using (var comm =
									new SqlCommand($"DROP TABLE [{Settings.Default.Schema}_BACKUPS].[{table}]", conn))
									comm.ExecuteNonQuery();
							using (var comm = new SqlCommand())
							{
								comm.Connection = conn;
								//Debug.WriteLine(Path.GetFileNameWithoutExtension(file));
								comm.CommandText = $"CREATE TABLE [{Settings.Default.Schema}_BACKUPS].[{table}] ( ";
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
									$"INSERT INTO [{Settings.Default.Schema}_BACKUPS].[{table}] SELECT * FROM [{Settings.Default.Schema}].[{table}]",
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
					MessageBox.Show("Failed Backup.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

		public static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
		{
			// Get the subdirectories for the specified directory.
			var dir = new DirectoryInfo(sourceDirName);

			if (!dir.Exists)
				throw new DirectoryNotFoundException(
					"Source directory does not exist or could not be found: "
					+ sourceDirName);

			DirectoryInfo[] dirs = dir.GetDirectories();
			// If the destination directory doesn't exist, create it.
			if (!Directory.Exists(destDirName)) Directory.CreateDirectory(destDirName);

			// Get the files in the directory and copy them to the new location.
			FileInfo[] files = dir.GetFiles();
			foreach (FileInfo file in files)
			{
				string temppath = Path.Combine(destDirName, file.Name);
				file.CopyTo(temppath, false);
			}

			// If copying subdirectories, copy them and their contents to new location.
			if (copySubDirs)
				foreach (DirectoryInfo subdir in dirs)
				{
					string temppath = Path.Combine(destDirName, subdir.Name);
					DirectoryCopy(subdir.FullName, temppath, true);
				}
		}

		public static List<int> GetSorts(string prefab)
		{
			using (var conn = new SqlConnection(ConnectionString))
			{
				conn.Open();
				DataTable prefabTable = GetPrefabDataTable(conn, $"{Settings.Default.Schema}_PREFABS", prefab);

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
				DataTable prefabTable = GetPrefabDataTable(conn, $"{Settings.Default.Schema}_PREFABS", prefab);

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
						if (dr["TABLE_SCHEMA"].ToString().Equals(Settings.Default.Schema) &&
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
						if (dr["TABLE_NAME"].Equals(tableName) && dr["TABLE_SCHEMA"].Equals(Settings.Default.Schema) &&
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

			foreach (string prefab in GetTableNames(conn, $"{Settings.Default.Schema}_PREFABS"))
			{
				DataTable propTable = GetPrefabDataTable(conn, $"{Settings.Default.Schema}_PREFABS", prefab);
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

				DataTable prefabTable = GetPrefabDataTable(conn, $"{Settings.Default.Schema}_PREFABS", prefab);
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

				DataTable prefabTable = GetPrefabDataTable(conn, $"{Settings.Default.Schema}_PREFABS", prefab);
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

		public static DataSet MainSet(MainWindow window)
		{
			DataSet dataTableList = new DataSet(Settings.Default.Schema);

			var conn = new SqlConnection(ConnectionString);
			conn.Open();
			foreach (string tableName in GetTableNames(conn))
			{
				var cmd = new SqlCommand($"SELECT * FROM [{Settings.Default.Schema}].[{tableName}]", conn);
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
						SetTableOnChangeds(table, sda, conn, window);
					}
				});
				//Task.Run(() => NotifyNewItem(table, window, dataTableList.Tables.IndexOf(table)));
			}

			conn.Close();

			return dataTableList;
		}

		private static void SetTableOnChangeds(DataTable table, SqlDataAdapter sda, SqlConnection conn,
			MainWindow window)
		{
			if (conn.State != ConnectionState.Open)
				conn.Open();
			table.RowChanged += (sender, args) =>
			{
				if (!(args.Action == DataRowAction.Add || args.Action == DataRowAction.Change)) return;

				SetSdaCommands(table, args, sda, conn, window);
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

				SetSdaCommands(table, args, sda, conn, window);
				ThisMadeLastChange = true;
			};
			if (conn.State != ConnectionState.Closed)
				conn.Close();
		}

		private static void NotifyNewItem(DataTable table, MainWindow window, int index)
		{
			lock (UpdateLock)
			{
				if (SavingCurrently) Task.Run(() => NotifyNewItem(table, window, index));
				var sqlConnection = new SqlConnection(ConnectionString);
				sqlConnection.Open();
				SqlCommand selectComm = new SqlCommand
				{
					Connection = sqlConnection,
					CommandText = "SELECT "
				};
				int counter = 0;

				string[] columns = GetAllColumnsOfTable(sqlConnection, table.TableName).ToArray();

				foreach (string column in columns)
				{
					if (counter < columns.Length - 1)
						selectComm.CommandText += $"[{column}], ";
					else
						selectComm.CommandText += $"[{column}] ";
					counter++;
				}

				selectComm.CommandText += $"FROM [{Settings.Default.Schema}].[{table.TableName}]";
				SqlDependency dependency = new SqlDependency(selectComm);
				dependency.OnChange += (sender, args) =>
				{
					lock (UpdateLock)
					{
						if (SavingCurrently || ThisMadeLastChange)
						{
							ThisMadeLastChange = false;
							Task.Run(() => NotifyNewItem(table, window, index));
							return;
						}

						Debug.WriteLine("CHANGED AND PASSED");
						/*try
						{*/
							using (var getLastQueriesCmd = new SqlCommand(GetLastExecutedQueriesCmd, sqlConnection))
							{
								if (sqlConnection.State != ConnectionState.Open)
									sqlConnection.Open();
								using (IDataReader reader = getLastQueriesCmd.ExecuteReader())
								{
									Retry:
									reader.Read();
									string lastExecutedCmdText = reader[1].ToString();
									Debug.WriteLine(lastExecutedCmdText);
									if (lastExecutedCmdText.ToLower().StartsWith("update"))
									{
										string selectText = lastExecutedCmdText
											.Substring(lastExecutedCmdText.IndexOf("WHERE", StringComparison.Ordinal))
											.Replace("WHERE ", string.Empty);
										string setText = lastExecutedCmdText
											.Substring(0,
												lastExecutedCmdText.IndexOf("WHERE", StringComparison.Ordinal))
											.Replace($"UPDATE [{Settings.Default.Schema}].[{table.TableName}] SET ",
												string.Empty);
										Debug.WriteLine("UPDATE SELECT TEXT: " + selectText);
										foreach (DataRow row in table.Select(selectText))
										{
											//Debug.WriteLine(string.Join(" \\ ", row.ItemArray));
											foreach (string line in Regex.Split(setText, ", "))
											{
												string[] splitLine = Regex.Split(line,
													line.Contains(" IS ") ? " = " : " IS ");
												string columnName = splitLine[0].Substring(0, splitLine[0].IndexOf(']'))
													.Replace("[", string.Empty);
												string itemName = splitLine[1].Replace("'", string.Empty).Trim();
												itemName = itemName.Equals("NULL") ? null : itemName;
												ThisIsNowConcurrent = true;
												row[columnName] = itemName;
											}

											//Debug.WriteLine(string.Join(" \\ ", row.ItemArray));
										}
									}
									else if (lastExecutedCmdText.ToLower().StartsWith("insert"))
									{
										string itemValues = lastExecutedCmdText
											.Substring(lastExecutedCmdText.IndexOf("VALUES ", StringComparison.Ordinal))
											.Replace("(", string.Empty)
											.Replace(")", string.Empty).Replace("VALUES ", string.Empty);
										DataRow newRow = table.NewRow();
										int i = 0;
										Debug.WriteLine("INSERTING: " + itemValues);
										foreach (string item in Regex.Split(itemValues, ", "))
										{
											string itemName = item.Replace("'", string.Empty).Trim();
											itemName = itemName.Equals("NULL") ? null : itemName;
											newRow[i] = itemName;
											i++;
										}

										ThisIsNowConcurrent = true;
										table.Rows.Add(newRow);
									}
									else if (lastExecutedCmdText.ToLower().StartsWith("delete"))
									{
										string selectText = lastExecutedCmdText
											.Substring(lastExecutedCmdText.IndexOf("WHERE", StringComparison.Ordinal))
											.Replace("WHERE ", string.Empty);
										Debug.WriteLine(selectText);
										foreach (DataRow row in table.Select(selectText))
										{
											ThisIsNowConcurrent = true;
											row.Delete();
										}
									}
									else
									{
										goto Retry;
									}
								}

								if (sqlConnection.State != ConnectionState.Closed)
									sqlConnection.Close();
							}
						/*}
						catch
						{
							//Debug.WriteLine(e);
							//Debug.WriteLine("SUM TING WONG");
							//create new table; dispatcher invoke set datagrid's item source to new table if failed?
							_dispatcher.Invoke(() =>
							{
								SfDataGrid dataGrid =
									(SfDataGrid) ((TabItemExt) window.MasterTabControl.Items[index]).Content;

								if (NSDMasterInventorySF.MainWindow.MasterDataSet.Tables.ToList<DataTable>()
									.Select(tab => tab.TableName)
									.Contains(table.TableName))
									using (SqlDataAdapter sda =
										new SqlDataAdapter(
											$"SELECT * FROM [{Settings.Default.Schema}].[{table.TableName}]",
											ConnectionString))
									{
										DataTable newTable = new DataTable(table.TableName);
										sda.Fill(newTable);
										dataGrid.ItemsSource = newTable;
										ThisIsNowConcurrent = true;
										Task.Run(() => NotifyNewItem(newTable, window, index));
									}

								//return;
							});
							throw;
							/*MessageBox.Show(
								"Unfortunately, there was an error in retrieving changed data from the database.\nThe grid was refreshed; it should now be concurrent.",
								"Error", MessageBoxButton.OK, MessageBoxImage.Warning);#1#
						}*/

						Task.Run(() => NotifyNewItem(table, window, index));
					}
				};

				if (!SavingCurrently)
					try
					{
						selectComm.ExecuteReader().Dispose();
					}
					catch
					{
						Task.Run(() => NotifyNewItem(table, window, index));
						throw;
					}
				else
					Task.Run(() => NotifyNewItem(table, window, index));

				sqlConnection.Close();
			}
		}

		public static DataTable GetPrefabDataTable(SqlConnection conn, string schema, string prefabName)
		{
			if (string.IsNullOrEmpty(prefabName))
				return new DataTable();

			if (!GetTableNames(conn, schema).Contains(prefabName))
			{
				if (schema.Equals($"{Settings.Default.Schema}_PREFABS"))
					using (var comm = new SqlCommand(
						$"CREATE TABLE [{schema}].[{prefabName}] (COLUMNS NVARCHAR(MAX), TYPES NVARCHAR(MAX), SORTBYS NVARCHAR(MAX), GROUPS NVARCHAR(MAX))",
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

			foreach (string tableName in GetTableNames(conn, $"{Settings.Default.Schema}_PREFABS"))
			{
				string fieldNames = string.Empty;

				DataTable prefabTable = GetPrefabDataTable(conn, $"{Settings.Default.Schema}_PREFABS", tableName);
				for (var i = 0; i < prefabTable.Rows.Count; i++)
					fieldNames += prefabTable.Rows[i]["COLUMNS"];

				if (fieldNames.Equals(columnNames))
					return tableName;
			}

			return string.Empty;
		}

		public static void ClearDir(string path)
		{
			var di = new DirectoryInfo(path);
			foreach (FileInfo file in di.GetFiles()) file.Delete();

			foreach (DirectoryInfo dir in di.GetDirectories()) dir.Delete(true);
		}

		public static string RandomString(int length)
		{
			const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789abcdefghijklmnopqrstuvwxyz";
			return new string(Enumerable.Repeat(chars, length)
				.Select(s => s[Random.Next(s.Length)]).ToArray());
		}

		public static void SetSdaCommands(DataTable table, DataRowChangeEventArgs args, SqlDataAdapter sda,
			SqlConnection conn, MainWindow window)
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
					CommandText = $"UPDATE [{Settings.Default.Schema}].[{table.TableName}] SET "
				};
				var sdaInsertCommand = new SqlCommand
				{
					Connection = conn,
					CommandText = $"INSERT INTO [{Settings.Default.Schema}].[{table.TableName}] ("
				};
				var sdaDeleteCommand = new SqlCommand
				{
					Connection = conn,
					CommandText = $"DELETE FROM [{Settings.Default.Schema}].[{table.TableName}] WHERE "
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
							    NSDMasterInventorySF.MainWindow.MasterDataGrids[window.MasterTabControl.SelectedIndex].Columns[i] is
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
					_dispatcher.Invoke(
						() => window.InitializeOrRefreshEverything(window.MasterTabControl.SelectedIndex));
					MessageBox.Show(
						$"Failed to update Database {Settings.Default.Database}; Error occured.\nThis was most likely caused by a concurrency issue and/or duplicate rows. The datagrids have been refreshed.",
						"Error!", MessageBoxButton.OK, MessageBoxImage.Error);
					//throw;
					//ThisMadeLastChange = true;
					//window.SaveToDb();
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

		private void App_OnExit(object sender, ExitEventArgs e)
		{
			//Connection.Stop();
			//Connection.Dispose();
			//SignalR.Dispose();
			//ConfigurationEcnrypterDecrypter.EncryptConfig();
			//SqlDependency.Stop(ConnectionString);
			Settings.Default.Password =
				ConfigurationEcnrypterDecrypter.EncryptString(
					ConfigurationEcnrypterDecrypter.ToSecureString(Settings.Default.Password));
			Settings.Default.Save();
		}
	}
	/// <summary>
	/// Echoes messages sent using the Send newRow by calling the
	/// addMessage method on the client. Also reports to the console
	/// when clients connect and disconnect.
	/// </summary>
	public class MyHub : Hub
	{
		public void Send(string command, string tableName, string oldRow, string newRow)
		{
			Clients.All.addMessage(command, tableName, oldRow, newRow);
		}

		public override Task OnConnected()
		{
			Debug.WriteLine("Client connected: " + Context.ConnectionId);

			return base.OnConnected();
		}

		public override Task OnDisconnected(bool stopCalled)
		{
			Debug.WriteLine("Client disconnected: " + Context.ConnectionId);

			return base.OnDisconnected(stopCalled);
		}
	}
	/// <summary>
	/// Used by OWIN's startup process. 
	/// </summary>
	public class Startup
	{
		public void Configuration(IAppBuilder app)
		{
			app.UseCors(CorsOptions.AllowAll);
			app.MapSignalR();
		}
	}
}