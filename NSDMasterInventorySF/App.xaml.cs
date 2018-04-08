using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using NSDMasterInventorySF.io;
using NSDMasterInventorySF.Properties;

//TODO: Recycleds window
//TODO: DO NOT TOUCH UNTIL AP EXAMS OVER
namespace NSDMasterInventorySF
{
	/// <inheritdoc />
	/// <summary>
	///     Interaction logic for App.xaml
	/// </summary>
	public partial class App
	{
		public static volatile bool SavingCurrently = false;
		public static volatile bool BackingUpCurrently;

		public static volatile string ConnectionString =
			$"Server={Settings.Default.Server};Database={Settings.Default.Database};User ID={Settings.Default.UserID};Password={Settings.Default.Password};";

		private static readonly Random Random = new Random();

		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);
			ConfigurationEcnrypterDecrypter.UnEncryptConfig();
			using (var conn = new SqlConnection(ConnectionString))
			{
				conn.Open();
				if (!GetAllNames(conn, "schemas").Contains("PREFABS"))
					using (var comm = new SqlCommand("CREATE SCHEMA PREFABS", conn))
					{
						comm.ExecuteNonQuery();
					}

				if (!GetAllNames(conn, "schemas").Contains("COMBOBOXES"))
					using (var comm = new SqlCommand("CREATE SCHEMA COMBOBOXES", conn))
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

			SqlDependency.Start(ConnectionString);
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
							using (var comm = new SqlCommand($"CREATE SCHEMA [{Settings.Default.Schema}_BACKUPS]", conn))
							{
								comm.ExecuteNonQuery();
							}

						foreach (string table in GetTableNames(conn, Settings.Default.Schema))
						{
							if (!GetTableNames(conn, $"{Settings.Default.Schema}_BACKUPS").Contains(table))
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
											comm.CommandText += $"[{column}] TEXT, ";
										else
											comm.CommandText += $"[{column}] TEXT";
										j++;
									}

									comm.CommandText += " )";

									comm.ExecuteNonQuery();
								}

							using (var comm = new SqlCommand($"TRUNCATE TABLE [{Settings.Default.Schema}_BACKUPS].[{table}]",
								conn))
							{
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
					MessageBox.Show("Failed Backup.", "Failure", MessageBoxButton.OK, MessageBoxImage.Error);
					throw;
				}

				Thread.CurrentThread.IsBackground = true;
			});
			task.Wait();
			BackingUpCurrently = false;
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
				DataTable prefabTable = GetPrefabDataTable(conn, "PREFABS", prefab);

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
				DataTable prefabTable = GetPrefabDataTable(conn, "PREFABS", prefab);

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

			// Set up a command with the given query and associate
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
			//SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString);
			var list = new List<string>();

			// Set up a command with the given query and associate
			// this with the current connection.
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
			//SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString);
			var list = new List<string>();

			// Set up a command with the given query and associate
			// this with the current connection.
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

			foreach (string tablePrefabName in GetTableNames(conn, "PREFABS"))
			{
				DataTable propTable = GetPrefabDataTable(conn, "PREFABS", tablePrefabName);
				string fieldNames = string.Empty;
				for (var i = 0; i < propTable.Rows.Count; i++) fieldNames += propTable.Rows[i]["COLUMNS"];

				if (columnNames.Equals(fieldNames))
					return Path.GetFileNameWithoutExtension(tablePrefabName);
			}

			return string.Empty;
		}

		public static List<DataTable> GetDatatablesOfPrefab(List<DataTable> tables, string prefab)
		{
			using (var conn = new SqlConnection(ConnectionString))
			{
				conn.Open();
				var dataTables = new List<DataTable>();

				DataTable prefabTable = GetPrefabDataTable(conn, "PREFABS", prefab);
				string fieldNames = string.Empty;
				for (var i = 0; i < prefabTable.Rows.Count; i++) fieldNames += prefabTable.Rows[i]["COLUMNS"];

				foreach (DataTable table in tables)
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

				DataTable prefabTable = GetPrefabDataTable(conn, "PREFABS", prefab);
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

		public static List<DataTable> GetDataTablesFromDb()
		{
			var dataTables = new List<DataTable>();

			using (var conn =
				new SqlConnection(ConnectionString))
			{
				conn.Open();

				int b = 0;
				foreach (string tableName in GetTableNames(conn))
					using (var cmd = new SqlCommand($"SELECT * FROM [{Settings.Default.Schema}].[{tableName}]", conn))
					{
						cmd.CommandType = CommandType.Text;

						using (var sda = new SqlDataAdapter(cmd))
						{
							using (var dt = new DataTable {TableName = tableName})
							{
								sda.Fill(dt);

								foreach (DataRow row in dt.Rows)
									for (var i = 0; i < row.ItemArray.Length; i++)
										if (string.IsNullOrEmpty(row[i].ToString()))
											row[i] = null;

								dataTables.Add(dt);

								SqlDependencyEx listener =
									new SqlDependencyEx(ConnectionString, Settings.Default.Database, $"[{tableName}]", $"[{Settings.Default.Schema}]",
										identity: b);
								listener.TableChanged += (sender, args) => { Debug.WriteLine("asdf"); };
								listener.Start();

								/*int changesReceived = 0;
								using (SqlCommand comm = new SqlCommand("SELECT ", conn))
								{
									int k = 0;
									List<string> columns = GetAllColumnsOfTable(conn, tableName);
									foreach (string column in columns)
									{
										if (k != columns.Count - 1)
											comm.CommandText += $"[{column}], ";
										else
											comm.CommandText += $"[{column}] ";
										k++;
									}

									comm.CommandText += $"Inventoried FROM [{Settings.Default.Schema}].[{dt.TableName}]";
									Debug.WriteLine(comm.CommandText);
									// Create a dependency and associate it with the SqlCommand.  
									SqlDependency dependency = new SqlDependency(comm);
									// Maintain the refence in a class member.
									comm.ExecuteNonQuery();

									// Subscribe to the SqlDependency event.  
									dependency.OnChange += (sender, args) => { Debug.WriteLine(changesReceived++); };

									/#1#/ Execute the command.  
									using (SqlDataReader reader = comm.ExecuteReader())
									{
										// Process the DataReader.  
									}#1#
								}*/
							}
						}

						b++;
					}

				conn.Close();
			}

			return dataTables;
		}

		public static DataTable GetPrefabDataTable(SqlConnection conn, string schema, string prefabName)
		{
			if (string.IsNullOrEmpty(prefabName))
				return new DataTable();

			if (!GetTableNames(conn, schema).Contains(prefabName))
			{
				if (schema.Equals("PREFABS"))
					using (var comm = new SqlCommand(
						$"CREATE TABLE [{schema}].[{prefabName}] (COLUMNS TEXT, TYPES TEXT, SORTBYS TEXT, GROUPS TEXT)",
						conn))
					{
						comm.ExecuteNonQuery();
					}
				else
					using (var comm = new SqlCommand($"CREATE TABLE [{schema}].[{prefabName}] (_INITIAL_COLUMN_ TEXT)",
						conn))
					{
						comm.ExecuteNonQuery();
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

			foreach (string tableName in GetTableNames(conn, "PREFABS"))
			{
				string fieldNames = string.Empty;

				DataTable prefabTable = GetPrefabDataTable(conn, "PREFABS", tableName);
				for (var i = 0; i < prefabTable.Rows.Count; i++)
					fieldNames += prefabTable.Rows[i]["COLUMNS"];

				if (fieldNames.Equals(columnNames))
					return tableName;
			}

			return string.Empty;
		}

		public static bool IdenticalPrefabExists(string prefab)
		{
			/*
			var inTypesProp = GetProp(prefab, "Types");
			var inFieldsProp = GetProp(prefab, "ColumnNames");

			var inTypesTotal = string.Empty;
			var inFieldsTotal = string.Empty;

			foreach (var i in inTypesProp.Keys) inTypesTotal += inTypesProp[i];

			foreach (var i in inFieldsProp.Keys) inFieldsTotal += inFieldsProp[i];

			foreach (var dirName in Directory.GetDirectories(PrefabsDirectory))
			{
				var dirPrefab = Path.GetFileNameWithoutExtension(dirName);
				if (dirPrefab.Equals(prefab)) continue;

				var typesProp = GetProp(dirPrefab, "Types");
				var fieldsProp = GetProp(dirPrefab, "ColumnNames");

				var typesTotal = string.Empty;
				var fieldsTotal = string.Empty;
				foreach (var i in typesProp.Keys) typesTotal += typesProp[i];

				foreach (var i in fieldsProp.Keys) fieldsTotal += fieldsProp[i];

				if (typesTotal.Equals(inTypesTotal) && fieldsTotal.Equals(inFieldsTotal))
					return true;
			}
			*/
			return false;
		}

		public static void AddProperty(ExpandoObject expando, string propertyName, object propertyValue)
		{
			// ExpandoObject supports IDictionary so we can extend it like this
			var expandoDict = expando as IDictionary<string, object>;
			if (expandoDict.ContainsKey(propertyName))
				expandoDict[propertyName] = propertyValue;
			else
				expandoDict.Add(propertyName, propertyValue);
		}

		public static bool SearchUsingScanner(string path, string searchQuery)
		{
			List<string> totalList = File.ReadAllLines(path).ToList();

			var total = new StringBuilder();
			foreach (string line in totalList) total.Append(line);

			return total.ToString().ToLower().Contains(searchQuery.ToLower());
		}

		public static void MarkInventoried(string path, string textToReplace)
		{
		}

		public static void ClearDir(string path)
		{
			var di = new DirectoryInfo(path);
			foreach (FileInfo file in di.GetFiles()) file.Delete();

			foreach (DirectoryInfo dir in di.GetDirectories()) dir.Delete(true);
		}

		public static string RandomString(int length)
		{
			const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
			return new string(Enumerable.Repeat(chars, length)
				.Select(s => s[Random.Next(s.Length)]).ToArray());
		}
	}
}