using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NSDMasterInventorySF.Properties;
using Syncfusion.SfSkinManager;
using Syncfusion.Windows.Controls.Input;
using Cursors = System.Windows.Forms.Cursors;

namespace NSDMasterInventorySF
{
	/// <summary>
	///     Interaction logic for PrefabBuilder.xaml
	/// </summary>
	public partial class PrefabBuilder : Window
	{
		public static List<WrapPanel> WrapPanels;
		public static List<SfTextBoxExt> ColumnNames;
		public static List<ComboBox> Types;
		public static List<Button> Edits;
		public static List<TextBox> SortBys;
		public static List<TextBox> Groups;
		public static List<Button> Deletes;
		public static List<Button> Adds;
		public static List<DataTable> TempTables;
		public static RoutedCommand CloseWindow = new RoutedCommand();

		private readonly string _originalPrefab;

		private readonly MainWindow _window;

		private string _currentVisualStyle;
		private bool _editButtonsEnabled;

		private Dictionary<string, string> _originalToChangedComboBoxes;

		public PrefabBuilder()
		{
			CloseWindow.InputGestures.Add(new KeyGesture(Key.Escape));
			CommandBindings.Add(new CommandBinding(CloseWindow, CloseCurrentWindow));

			InitializeComponent();
			_editButtonsEnabled = false;

			ClearAllLists();

			InsertEmptyFields(0);
		}

		public PrefabBuilder(string prefabName, MainWindow window)
		{
			InitializeComponent();

			_window = window;
			_originalPrefab = prefabName;
			//Debug.WriteLine(prefabName);

			ClearAllLists();

			foreach (DataTable table in MainWindow.MasterDataTables.Tables) TempTables.Add(table.Copy());
			using (var conn = new SqlConnection(App.ConnectionString))
			{
				conn.Open();
				ShowInventoriedColumn.IsChecked =
					App.GetPrefabDataTable(conn, "PREFABS", prefabName).Rows[0]["COLUMNS"].Equals("Inventoried") &&
					App.GetPrefabDataTable(conn, "PREFABS", prefabName).Rows[0]["TYPES"].Equals("CheckBox");
				conn.Close();
			}

			if (!string.IsNullOrWhiteSpace(prefabName))
				InitializePrefabEditFields(prefabName);
			else
				InsertEmptyFields(0);
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

		private void InitializePrefabEditFields(string prefab)
		{
			using (var conn = new SqlConnection(App.ConnectionString))
			{
				conn.Open();
				DataTable prefabTable = App.GetPrefabDataTable(conn, "PREFABS", prefab);

				if ((bool) ShowInventoriedColumn.IsChecked)
					for (var i = 1; i < prefabTable.Rows.Count; i++)
						InsertEmptyFields(i - 1);
				else
					for (var i = 0; i < prefabTable.Rows.Count; i++)
						InsertEmptyFields(i);

				if ((bool) ShowInventoriedColumn.IsChecked)
					for (var i = 1; i < prefabTable.Rows.Count; i++)
						ColumnNames[i - 1].Text = prefabTable.Rows[i]["COLUMNS"].ToString();
				else
					for (var i = 0; i < prefabTable.Columns.Count; i++)
						ColumnNames[i].Text = prefabTable.Rows[i]["COLUMNS"].ToString();

				if ((bool) ShowInventoriedColumn.IsChecked)
					for (var i = 1; i < prefabTable.Rows.Count; i++)
						Types[i - 1].Text = prefabTable.Rows[i]["TYPES"].ToString();
				else
					for (var i = 0; i < prefabTable.Rows.Count; i++)
						Types[i].Text = prefabTable.Rows[i]["TYPES"].ToString();

				if ((bool) ShowInventoriedColumn.IsChecked)
					for (var i = 1; i < prefabTable.Rows.Count; i++)
						SortBys[i - 1].Text = prefabTable.Rows[i]["SORTBYS"].ToString();
				else
					for (var i = 0; i < prefabTable.Rows.Count; i++)
						SortBys[i].Text = prefabTable.Rows[i]["SORTBYS"].ToString();

				if ((bool) ShowInventoriedColumn.IsChecked)
					for (var i = 1; i < prefabTable.Rows.Count; i++)
						Groups[i - 1].Text = prefabTable.Rows[i]["GROUPS"].ToString();
				else
					for (var i = 0; i < prefabTable.Rows.Count; i++)
						Groups[i].Text = prefabTable.Rows[i]["GROUPS"].ToString();

				var j = 0;
				foreach (SfTextBoxExt column in ColumnNames)
				{
					if (Types[j].Text.Trim().ToLower().Equals("combobox") || Types[j].Text.Trim().ToLower().Equals("autocomplete"))
						_originalToChangedComboBoxes.Add(column.Text, column.Text);

					j++;
				}

				ItemNameField.Text = prefab;
				conn.Close();
			}
		}

		private void InsertEmptyFields(int i)
		{
			var panel = new WrapPanel();

			var columnName = new SfTextBoxExt
			{
				Width = 100,
				Margin = new Thickness(7.5),
				Name = $"name{ColumnNames.Count}"
			};
			columnName.PreviewTextInput += ColumnValidationTextBox;
			try
			{
				ColumnNames.Insert(i, columnName);
			}
			catch (ArgumentOutOfRangeException)
			{
				ColumnNames.Add(columnName);
			}

			columnName.Watermark = $"Field{ColumnNames.IndexOf(columnName) + 1}";

			var fieldType = new ComboBox
			{
				Width = 110,
				Margin = new Thickness(7.5),
				Name = $"type{Types.Count}"
			};
			fieldType.Items.Add("TextField");
			fieldType.Items.Add("Numeric");
			fieldType.Items.Add("AutoComplete");
			fieldType.Items.Add("DatePicker");
			fieldType.Items.Add("ComboBox");
			fieldType.Items.Add("CheckBox");
			fieldType.Items.Add("Currency");
			fieldType.Items.Add("Hyperlink");
			fieldType.Items.Add("Percentage");
			fieldType.SelectedValue = fieldType.Items[0];
			try
			{
				Types.Insert(i, fieldType);
			}
			catch (ArgumentOutOfRangeException)
			{
				Types.Add(fieldType);
			}

			string oldColumnName = string.Empty;
			var q = 0;
			columnName.Loaded += (sender, args) =>
			{
				if (q == 0)
					oldColumnName = columnName.Text;
				q++;
			};
			columnName.TextChanged += (sender, args) =>
			{
				if (fieldType.SelectedValue == null) return;

				if (((string) fieldType.SelectedValue).ToLower().Equals("combobox") ||
				    ((string) fieldType.SelectedValue).ToLower().Equals("autocomplete"))
				{
					if (_originalToChangedComboBoxes.Keys.Contains(oldColumnName))
						_originalToChangedComboBoxes[oldColumnName] = columnName.Text;
					else
						_originalToChangedComboBoxes.Add(oldColumnName, columnName.Text);
				}
			};

			fieldType.SelectionChanged += (sender, args) =>
			{
				if (((ComboBox) sender).SelectedItem == null) return;

				if ((((string) ((ComboBox) sender).SelectedItem).ToLower().Equals("combobox") ||
				     ((string) ((ComboBox) sender).SelectedItem).ToLower().Equals("autocomplete")) && _editButtonsEnabled)
					Edits[Types.IndexOf(fieldType)].IsEnabled = true;
				else
					Edits[Types.IndexOf(fieldType)].IsEnabled = false;
			};

			var edit = new Button
			{
				Content = "Edit",
				Margin = new Thickness(7.5),
				Name = $"edit{Edits.Count}",
				IsEnabled = false
			};
			edit.Click += (sender1, args1) =>
			{
				Debug.WriteLine(oldColumnName);
				var boxBuilder =
					new ComboBoxBuilder(string.IsNullOrEmpty(oldColumnName) ? columnName.Text : oldColumnName,
						!string.IsNullOrEmpty(_originalPrefab) ? _originalPrefab : ItemNameField.Text)
					{
						Owner = this,
						ShowInTaskbar = false
					};
				boxBuilder.ShowDialog();

				//oldColumnName = columnName.Text;
			};
			try
			{
				Edits.Insert(i, edit);
			}
			catch (ArgumentOutOfRangeException)
			{
				Edits.Add(edit);
			}

			var sortBy = new TextBox
			{
				Width = 20,
				Margin = new Thickness(7.5),
				Name = $"sortBy{SortBys.Count}"
			};
			sortBy.PreviewTextInput += NumberValidationTextBox;
			try
			{
				SortBys.Insert(i, sortBy);
			}
			catch (ArgumentOutOfRangeException)
			{
				SortBys.Add(sortBy);
			}

			var group = new TextBox
			{
				Width = 20,
				Margin = new Thickness(7.5),
				Name = $"group{Groups.Count}"
			};
			group.PreviewTextInput += NumberValidationTextBox;
			try
			{
				Groups.Insert(i, group);
			}
			catch (ArgumentOutOfRangeException)
			{
				Groups.Add(group);
			}

			var delete = new Button
			{
				Content = "-",
				Width = 20,
				Margin = new Thickness(7.5),
				Name = $"delete{Deletes.Count}"
			};
			try
			{
				Deletes.Insert(i, delete);
			}
			catch (ArgumentOutOfRangeException)
			{
				Deletes.Add(delete);
			}

			var add = new Button
			{
				Content = "+",
				Width = 20,
				Margin = new Thickness(7.5),
				Name = $"add{Adds.Count}"
			};
			add.Click += (sender, args) =>
			{
				int index = FieldsStackPanel.Children.IndexOf(panel);

				index += (bool) ShowInventoriedColumn.IsChecked ? 1 : 0;

				foreach (DataTable table in TempTables)
					try
					{
						table.Columns.Add($"C-_-O-_-L-_-U-_-M-_-N-_-{App.RandomString(24)}").SetOrdinal(index + 1);
						//Debug.WriteLine(table.ColumnNames[index].ColumnName);
					}
					catch
					{
						table.Columns.Add($"C-_-O-_-L-_-U-_-M-_-N-_-{App.RandomString(24)}");
					}

				InsertEmptyFields(index);

				RefreshLists();
			};
			try
			{
				Adds.Insert(i, add);
			}
			catch (ArgumentOutOfRangeException)
			{
				Adds.Add(add);
			}

			panel.Children.Add(columnName);
			panel.Children.Add(fieldType);
			panel.Children.Add(edit);
			panel.Children.Add(sortBy);
			panel.Children.Add(group);
			panel.Children.Add(delete);
			panel.Children.Add(add);
			try
			{
				WrapPanels.Insert(i, panel);
			}
			catch (ArgumentOutOfRangeException)
			{
				WrapPanels.Add(panel);
			}

			FieldsStackPanel.Children.Insert(i, panel);

			delete.Click += (sender, args) =>
			{
				int index = FieldsStackPanel.Children.IndexOf(panel);

				index += (bool) ShowInventoriedColumn.IsChecked ? 1 : 0;

				foreach (DataTable table in TempTables)
					try
					{
						//Debug.WriteLine(table.ColumnNames[index].ColumnName);
						table.Columns.RemoveAt(index);
					}
					catch
					{
						//table.ColumnNames.RemoveAt(table.ColumnNames.Count - 1);
					}

				//try
				//{
				//	using (var conn = new SqlConnection(App.ConnectionString))
				//	{
				//		using (var comm = new SqlCommand("ALTER TABLE PREFABS.[" + _originalPrefab + "] DROP COLUMN [" + columnName + "]",
				//			conn))
				//		{
				//			conn.Open();
				//			comm.ExecuteNonQuery();
				//			conn.Close();
				//		}
				//	}
				//}
				//catch
				//{
				//	//ignored
				//}

				ColumnNames.Remove(columnName);
				Types.Remove(fieldType);
				Edits.Remove(edit);
				SortBys.Remove(sortBy);
				Groups.Remove(group);
				Deletes.Remove(delete);
				Adds.Remove(add);

				if (FieldsStackPanel.Children.Count > 1)
					FieldsStackPanel.Children.Remove(panel);

				RefreshLists();
			};

			RefreshLists();
		}

		private void ClearAllLists()
		{
			CloseWindow.InputGestures.Add(new KeyGesture(Key.Escape));
			CommandBindings.Add(new CommandBinding(CloseWindow, CloseCurrentWindow));

			_editButtonsEnabled = true;
			WrapPanels = new List<WrapPanel>();
			ColumnNames = new List<SfTextBoxExt>();
			Types = new List<ComboBox>();
			Edits = new List<Button>();
			SortBys = new List<TextBox>();
			Groups = new List<TextBox>();
			Deletes = new List<Button>();
			Adds = new List<Button>();

			_originalToChangedComboBoxes = new Dictionary<string, string>();

			TempTables = new List<DataTable>();
		}

		private static void RefreshLists()
		{
			foreach (SfTextBoxExt v in ColumnNames)
			{
				v.Watermark = $"Field{ColumnNames.IndexOf(v) + 1}";
				v.Name = $"name{ColumnNames.IndexOf(v)}";
			}

			foreach (ComboBox v in Types) v.Name = $"type{Types.IndexOf(v)}";

			foreach (Button v in Edits) v.Name = $"edit{Edits.IndexOf(v)}";

			foreach (TextBox v in SortBys) v.Name = $"sortBy{SortBys.IndexOf(v)}";

			foreach (TextBox v in Groups) v.Name = $"group{Groups.IndexOf(v)}";

			foreach (Button v in Deletes) v.Name = $"delete{Deletes.IndexOf(v)}";

			foreach (Button v in Adds) v.Name = $"add{Adds.IndexOf(v)}";
		}

		private void SavePrefab(object sender, RoutedEventArgs e)
		{
			System.Windows.Forms.Cursor.Current = Cursors.WaitCursor;

			using (var conn = new SqlConnection(App.ConnectionString))
			{
				conn.Open();

				var shouldTruncate = true;
				DataTable prefabTable = App.GetPrefabDataTable(conn, "PREFABS", _originalPrefab);
				if (prefabTable.Rows.Count < 1)
					shouldTruncate = false;
				if (prefabTable.Columns.Count < 4)
				{
					try
					{
						prefabTable.Columns.Add("COLUMNS");
					}
					catch
					{
					}

					try
					{
						prefabTable.Columns.Add("TYPES");
					}
					catch
					{
					}

					try
					{
						prefabTable.Columns.Add("SORTBYS");
					}
					catch
					{
					}

					try
					{
						prefabTable.Columns.Add("GROUPS");
					}
					catch
					{
					}
				}

				if (!ItemNameField.Text.Equals(_originalPrefab) && !string.IsNullOrEmpty(_originalPrefab))
				{
					using (var comm =
						new SqlCommand($"sp_rename \'PREFABS.{_originalPrefab}\', \'{ItemNameField.Text}\'", conn))
					{
						comm.ExecuteNonQuery();
					}

					prefabTable.TableName = ItemNameField.Text;
				}

				if (string.IsNullOrEmpty(prefabTable.TableName))
					prefabTable.TableName = ItemNameField.Text;

				List<DataTable> tablesOfPrefab = App.GetDatatablesOfPrefab(MainWindow.MasterDataTables, ItemNameField.Text);

				foreach (SfTextBoxExt v in ColumnNames)
				{
					if (!string.IsNullOrEmpty(v.Text)) continue;

					MessageBox.Show("Please fill out all column names before saving.", "Cannot save", MessageBoxButton.OK,
						MessageBoxImage.Warning);
					return;
				}

				var fieldNames = new List<string>();
				foreach (SfTextBoxExt v in ColumnNames) fieldNames.Add(v.Text);

				if (fieldNames.Count != fieldNames.Distinct().Count())
				{
					MessageBox.Show("There are duplicate column names. Please rename said columns.", "Cannot save",
						MessageBoxButton.OK,
						MessageBoxImage.Warning);
					return;
				}

				var sortFields = new List<string>();
				foreach (TextBox v in SortBys)
					if (!string.IsNullOrEmpty(v.Text) && v.Text != "0")
						sortFields.Add(v.Text);

				if (sortFields.Count != sortFields.Distinct().Count())
				{
					MessageBox.Show("There are duplicate sorting integers. Please re-enter said integers.", "Cannot save",
						MessageBoxButton.OK,
						MessageBoxImage.Warning);
					return;
				}

				var groupFields = new List<string>();
				foreach (TextBox v in Groups)
					if (!string.IsNullOrEmpty(v.Text) && v.Text != "0")
						groupFields.Add(v.Text);

				if (groupFields.Count != groupFields.Distinct().Count())
				{
					MessageBox.Show("There are duplicate grouping integers. Please re-enter said integers.", "Cannot save",
						MessageBoxButton.OK,
						MessageBoxImage.Warning);
					return;
				}

				if (MessageBox.Show(
					    "Are you sure you would like to save? ColumnNames (and their data) may be deleted (permanently) as a result of this action.",
					    "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
					return;

				var inventoriedIsChecked = (bool) ShowInventoriedColumn.IsChecked;

				foreach (DataColumn column in prefabTable.Columns)
					foreach (DataRow row in prefabTable.Rows)
						row[column.ColumnName] = null;

				if (inventoriedIsChecked)
				{
					if (prefabTable.Rows.Count > 0)
					{
						prefabTable.Rows[0]["COLUMNS"] = "Inventoried";
					}
					else
					{
						DataRow row = prefabTable.NewRow();
						row["COLUMNS"] = "Inventoried";
						prefabTable.Rows.Add(row);
					}

					for (var i = 1; i <= ColumnNames.Count; i++)
						if (i < prefabTable.Rows.Count)
						{
							prefabTable.Rows[i]["COLUMNS"] = ColumnNames[i - 1].Text;
						}
						else
						{
							DataRow row = prefabTable.NewRow();
							row["COLUMNS"] = ColumnNames[i - 1].Text;
							prefabTable.Rows.Add(row);
						}
				}
				else
				{
					for (var i = 0; i < ColumnNames.Count; i++)
						if (i < prefabTable.Rows.Count)
						{
							prefabTable.Rows[i]["COLUMNS"] = ColumnNames[i].Text;
						}
						else
						{
							DataRow row = prefabTable.NewRow();
							row["COLUMNS"] = ColumnNames[i].Text;
							prefabTable.Rows.Add(row);
						}
				}

				if (inventoriedIsChecked)
				{
					prefabTable.Rows[0]["TYPES"] = "CheckBox";
					for (var i = 1; i <= Types.Count; i++)
						prefabTable.Rows[i]["TYPES"] = Types[i - 1].SelectedValue.ToString();
				}
				else
				{
					for (var i = 0; i < Types.Count; i++)
						prefabTable.Rows[i]["TYPES"] = Types[i].SelectedValue.ToString();
				}

				if (inventoriedIsChecked)
				{
					prefabTable.Rows[0]["SORTBYS"] = string.Empty;
					for (var i = 1; i <= SortBys.Count; i++)
						prefabTable.Rows[i]["SORTBYS"] = SortBys[i - 1].Text;
				}
				else
				{
					for (var i = 0; i < SortBys.Count; i++)
						prefabTable.Rows[i]["SORTBYS"] = SortBys[i].Text;
				}

				if (inventoriedIsChecked)
				{
					prefabTable.Rows[0]["GROUPS"] = string.Empty;
					for (var i = 1; i <= Groups.Count; i++)
						prefabTable.Rows[i]["GROUPS"] = Groups[i - 1].Text;
				}
				else
				{
					for (var i = 0; i < Groups.Count; i++)
						prefabTable.Rows[i]["GROUPS"] = Groups[i].Text;
				}

				DataTable comboTable = App.GetPrefabDataTable(conn, "COMBOBOXES", prefabTable.TableName);
				foreach (string key in _originalToChangedComboBoxes.Keys)
					for (var i = 0; i < comboTable.Rows.Count; i++)
						if (App.GetAllColumnsOfTable(conn, "COMBOBOXES", comboTable.TableName)
							.Contains(_originalToChangedComboBoxes[key]))
							using (var comm =
								new SqlCommand(
									$"sp_rename \'COMBOBOXES.{comboTable.TableName}.{key}\', \'{_originalToChangedComboBoxes[key]}\', \'COLUMN\'", conn))
							{
								comm.ExecuteNonQuery();
							}
						else
							using (var comm =
								new SqlCommand(
									$"ALTER TABLE COMBOBOXES.{comboTable.TableName} ADD [{_originalToChangedComboBoxes[key]}] TEXT",
									conn))

							{
								comm.ExecuteNonQuery();
							}

				if (shouldTruncate)
					using (var comm = new SqlCommand($"TRUNCATE TABLE PREFABS.[{prefabTable.TableName}]",
						conn))
					{
						comm.ExecuteNonQuery();
					}

				var bulkCopy = new SqlBulkCopy(conn)
				{
					DestinationTableName =
						$"PREFABS.[{prefabTable.TableName}]"
				};
				try
				{
					bulkCopy.WriteToServer(prefabTable);
				}
				catch
				{
					Debug.WriteLine(e);
				}

				var indices = new List<int>();
				foreach (DataTable table in tablesOfPrefab) indices.Add(MainWindow.MasterDataTables.Tables.IndexOf(table));

				//TODO: Make sure this frikin' works
				foreach (int i in indices)
				{
					MainWindow.MasterDataTables.Tables[i].Rows.Clear();
					MainWindow.MasterDataTables.Tables[i].Columns.Clear();

					DataTable copy = TempTables[i].Copy();
					foreach (DataColumn column in copy.Columns)
					{
						MainWindow.MasterDataTables.Tables[i].Columns.Add(column);
					}

					foreach (DataRow row in copy.Rows)
					{
						MainWindow.MasterDataTables.Tables[i].Rows.Add(row);
					}
				}

				foreach (int i in indices)
				{
					for (var j = 0; j < MainWindow.MasterDataTables.Tables[i].Columns.Count; j++)
						MainWindow.MasterDataTables.Tables[i].Columns[j].ColumnName = $"C_-_O_-_L_-_U_-_M_-_N_-_{j}";

					for (var j = 0; j < MainWindow.MasterDataTables.Tables[i].Columns.Count; j++)
						MainWindow.MasterDataTables.Tables[i].Columns[j].ColumnName = prefabTable.Rows[j]["COLUMNS"].ToString();
				}

				if (_window != null)
					_window.SaveToDb(true);

				foreach (object window in Application.Current.Windows)
					if (window is PrefabManager pm)
						pm.PopulateListBox();
				conn.Close();
			}

			Close();

			System.Windows.Forms.Cursor.Current = Cursors.Default;
		}

		private void NameFieldTextChanged(object sender, TextChangedEventArgs e)
		{
			bool buttonIsEnabled = !string.IsNullOrEmpty(ItemNameField.Text);
			SaveButton.IsEnabled = buttonIsEnabled;
			_editButtonsEnabled = buttonIsEnabled;

			var i = 0;
			foreach (Button edit in Edits)
			{
				if (Types[i].SelectedValue == null) return;
				edit.IsEnabled = buttonIsEnabled && (Types[i].SelectedValue.ToString().ToLower().Equals("autocomplete") ||
				                                     Types[i].SelectedValue.ToString().ToLower().Equals("combobox"));
				i++;
			}
		}

		private void CloseCurrentWindow(object sender, EventArgs e)
		{
			Close();
		}

		private void ShowInventoriedColumn_OnClick(object sender, RoutedEventArgs e)
		{
			if ((bool) ShowInventoriedColumn.IsChecked)
				foreach (DataTable table in TempTables)
					table.Columns.Add($"C-_-O-_-L-_-U-_-M-_-N-_-{App.RandomString(24)}").SetOrdinal(0);
			else
				foreach (DataTable table in TempTables)
					table.Columns.RemoveAt(0);
		}

		private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
		{
			var regex = new Regex("[^0-9]+");

			e.Handled = regex.IsMatch(e.Text);
		}

		private void ColumnValidationTextBox(object sender, TextCompositionEventArgs e)
		{
			var allow = true;
			foreach (char c in e.Text)
			{
				if (!(char.IsLetterOrDigit(c) || char.IsSymbol(c) || c.Equals('!') || c.Equals('@') || c.Equals('#') ||
				      c.Equals('$') || c.Equals('%') || c.Equals('^') || c.Equals('&') || c.Equals('*') || c.Equals('(') ||
				      c.Equals(')') || c.Equals('/') || c.Equals('\\') || c.Equals(':') || c.Equals('<') || c.Equals('>') ||
				      c.Equals('?') || c.Equals('"') || c.Equals(';') || c.Equals('{') || c.Equals('}') || c.Equals('~') ||
				      c.Equals(' ') || c.Equals('_'))) continue;
				allow = false;
				break;
			}

			e.Handled = allow;
		}

		private void OnVisualStyleChanged()
		{
			Enum.TryParse(CurrentVisualStyle, out VisualStyles visualStyle);
			if (visualStyle == VisualStyles.Default) return;
			SfSkinManager.ApplyStylesOnApplication = true;
			SfSkinManager.SetVisualStyle(this, visualStyle);
			SfSkinManager.ApplyStylesOnApplication = false;
		}

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			CurrentVisualStyle = Settings.Default.Theme;
		}
	}
}