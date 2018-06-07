using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Input;
using NSDMasterInventorySF.Properties;
using Syncfusion.SfSkinManager;
using Syncfusion.UI.Xaml.Grid;
using Cursors = System.Windows.Forms.Cursors;

namespace NSDMasterInventorySF
{
	/// <summary>
	///     Interaction logic for ComboBoxBuilder.xaml
	/// </summary>
	public partial class ComboBoxBuilder
	{
		public static RoutedCommand CloseWindow = new RoutedCommand();
		private readonly DataTable _prefabTable;
		public DataTable ComboTable;
		private readonly string _prefabName;
		private bool _wasTempTableCreated;

		private string _currentVisualStyle;

		public ComboBoxBuilder(DataTable prefabTable, string prefabName)
		{
			CloseWindow.InputGestures.Add(new KeyGesture(Key.Escape));
			CommandBindings.Add(new CommandBinding(CloseWindow, CloseCurrentWindow));

			InitializeComponent();

			_prefabTable = prefabTable;
			_prefabName = prefabName;
			ComboTable = new DataTable(_prefabName);

			FillComboTable();
		}

		private void FillComboTable()
		{
			ComboTable.RowChanged += (sender, args) =>
			{
				SaveButton.IsEnabled = true;
				//Debug.WriteLine("asdf");
				//if(args.Action == DataRowAction.Change)
				//_comboTable.AcceptChanges();
			};
			ComboTable.RowDeleted += (sender, args) =>
			{
				SaveButton.IsEnabled = true;
				ComboTable.AcceptChanges();
			};

			using (var conn =
				new SqlConnection(App.ConnectionString))
			{
				conn.Open();

				if (!App.GetTableNames(conn, $"{Settings.Default.Schema}_COMBOBOXES").Contains(_prefabName))
					return;

				using (var cmd = new SqlCommand($"SELECT * FROM [{Settings.Default.Schema}_COMBOBOXES].[{_prefabName}]",
					conn))
				{
					using (var sda = new SqlDataAdapter(cmd))
					{
						sda.Fill(ComboTable);
					}
				}

				conn.Close();
			}

			ComboGrid.ItemsSource = ComboTable;
			int j = 0;
			ComboGrid.Loaded += (sender, args) =>
			{
				if (j == 0)
				{
					GenerateColumns();
				}

				j++;
			};
		}

		public void GenerateColumns()
		{
			ComboGrid.Columns.Clear();
			for (var i = 0; i < ComboTable.Columns.Count; i++)
			{
				GridColumn column = new GridTextColumn
				{
					MappingName = ComboTable.Columns[i].ColumnName,
					HeaderText = ComboTable.Columns[i].ColumnName
				};
				ComboGrid.Columns.Add(column);
			}
		}

		public string CurrentVisualStyle
		{
			get => _currentVisualStyle;
			set
			{
				_currentVisualStyle = value;
				OnVisualStyleChanged();
			}
		}

		private void CloseCurrentWindow(object sender, EventArgs e)
		{
			Close();
		}

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			CurrentVisualStyle = Settings.Default.Theme;
		}

		private void OnVisualStyleChanged()
		{
			Enum.TryParse(CurrentVisualStyle, out VisualStyles visualStyle);
			if (visualStyle == VisualStyles.Default) return;
			SfSkinManager.ApplyStylesOnApplication = true;
			SfSkinManager.SetVisualStyle(this, visualStyle);
			SfSkinManager.ApplyStylesOnApplication = false;
		}

		private void AddColumnButton_OnClick(object sender, RoutedEventArgs e)
		{
			var columnChooser = new ColumnChooser(_prefabTable, ComboTable, this)
			{
				ShowInTaskbar = false,
				Owner = this,
				ResizeMode = ResizeMode.NoResize
			};
			columnChooser.ShowDialog();
		}

		private void SaveButton_OnClick(object sender, RoutedEventArgs e)
		{
			SaveComboBoxes();
			Close();
		}

		private void SaveComboBoxes()
		{
			System.Windows.Forms.Cursor.Current = Cursors.WaitCursor;
			ComboTable.AcceptChanges();
			using (var conn = new SqlConnection(App.ConnectionString))
			{
				conn.Open();
				if (App.GetTableNames(conn, $"{Settings.Default.Schema}_COMBOBOXES").Contains(_prefabName))
					using (var comm =
						new SqlCommand($"DROP TABLE [{Settings.Default.Schema}_COMBOBOXES].[{_prefabName}]", conn))
						comm.ExecuteNonQuery();
				using (var comm = new SqlCommand())
				{
					comm.Connection = conn;
					comm.CommandText = $"CREATE TABLE [{Settings.Default.Schema}_COMBOBOXES].[{_prefabName}] (";
					for (int i = 0; i < ComboTable.Columns.Count; i++)
					{
						if (i != ComboTable.Columns.Count - 1)
							comm.CommandText += $"[{ComboTable.Columns[i].ColumnName}] NVARCHAR(MAX), ";
						else
							comm.CommandText += $"[{ComboTable.Columns[i].ColumnName}] NVARCHAR(MAX)";
					}

					comm.CommandText += ")";
					comm.ExecuteNonQuery();
				}

				var bulkCopy = new SqlBulkCopy(conn)
				{
					DestinationTableName = $"[{Settings.Default.Schema}_COMBOBOXES].[{_prefabName}]"
				};
				bulkCopy.WriteToServer(ComboTable);
				conn.Close();
			}

			_wasTempTableCreated = false;
			System.Windows.Forms.Cursor.Current = Cursors.Default;
			Close();
		}

		private void ComboBoxBuilder_OnClosed(object sender, EventArgs e)
		{
			if (!_wasTempTableCreated) return;
			using (var conn = new SqlConnection(App.ConnectionString))
			{
				conn.Open();
				using (var comm = new SqlCommand($"DROP TABLE [{Settings.Default.Schema}_COMBOBOXES].[{_prefabName}]",
					conn))
					comm.ExecuteNonQuery();
				conn.Close();
			}
		}
	}
}