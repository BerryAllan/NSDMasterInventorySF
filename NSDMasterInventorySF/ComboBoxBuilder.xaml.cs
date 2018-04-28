using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NSDMasterInventorySF.Properties;
using Syncfusion.SfSkinManager;

namespace NSDMasterInventorySF
{
	/// <summary>
	///     Interaction logic for ComboBoxBuilder.xaml
	/// </summary>
	public partial class ComboBoxBuilder
	{
		public static RoutedCommand CloseWindow = new RoutedCommand();
		private readonly string _columnName;
		private readonly string _prefab;

		private string _currentVisualStyle;

		public ComboBoxBuilder()
		{
			CloseWindow.InputGestures.Add(new KeyGesture(Key.Escape));
			CommandBindings.Add(new CommandBinding(CloseWindow, CloseCurrentWindow));

			InitializeComponent();
		}

		public ComboBoxBuilder(string columnName, string prefab)
		{
			CloseWindow.InputGestures.Add(new KeyGesture(Key.Escape));
			CommandBindings.Add(new CommandBinding(CloseWindow, CloseCurrentWindow));

			InitializeComponent();

			_columnName = columnName;
			_prefab = prefab;

			PopulateListBox();
		}

		private ObservableCollection<string> DataItemList { get; set; }

		public string CurrentVisualStyle
		{
			get => _currentVisualStyle;
			set
			{
				_currentVisualStyle = value;
				OnVisualStyleChanged();
			}
		}

		private void PopulateListBox()
		{
			DataItemList = new ObservableCollection<string>();

			using (var conn = new SqlConnection(App.ConnectionString))
			{
				conn.Open();
				if (App.GetTableNames(conn, "COMBOBOXES").Contains(_prefab))
				{
					DataTable comboTable = App.GetPrefabDataTable(conn, "COMBOBOXES", _prefab);
					if (comboTable.Columns.Contains(_columnName))
						for (var i = 0; i < comboTable.Rows.Count; i++)
							if (!string.IsNullOrEmpty(comboTable.Rows[i][_columnName].ToString()))
								DataItemList.Add(comboTable.Rows[i][_columnName].ToString());
				}

				conn.Close();
			}

			ChoiceList.ItemsSource = DataItemList;
		}

		private void ChoiceList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (ChoiceList.SelectedItems.Count > 0)
			{
				RemoveButton.IsEnabled = true;
				MoveDownButton.IsEnabled = true;
				MoveUpButton.IsEnabled = true;
			}
			else
			{
				RemoveButton.IsEnabled = false;
				MoveDownButton.IsEnabled = false;
				MoveUpButton.IsEnabled = false;
			}
		}

		private void SaveButton_OnClick(object sender, RoutedEventArgs e)
		{
			using (var conn = new SqlConnection(App.ConnectionString))
			{
				conn.Open();

				DataTable comboTable = App.GetPrefabDataTable(conn, "COMBOBOXES", _prefab);

				if (!comboTable.Columns.Contains(_columnName))
					comboTable.Columns.Add(_columnName);

				for (var i = 0; i < comboTable.Rows.Count; i++) comboTable.Rows[i][_columnName] = null;

				for (var i = 0; i < DataItemList.Count; i++)
					if (i < comboTable.Rows.Count)
					{
						comboTable.Rows[i][_columnName] = DataItemList[i];
					}
					else
					{
						DataRow row = comboTable.NewRow();
						row[_columnName] = DataItemList[i];
						comboTable.Rows.Add(row);
					}

				if (!App.GetAllColumnsOfTable(conn, "COMBOBOXES", _prefab).Contains(_columnName))
					using (var comm = new SqlCommand($"ALTER TABLE COMBOBOXES.[{_prefab}] ADD [{_columnName}] TEXT",
						conn))
					{
						comm.ExecuteNonQuery();
					}

				using (var comm = new SqlCommand($"TRUNCATE TABLE COMBOBOXES.[{_prefab}]",
					conn))
				{
					comm.ExecuteNonQuery();
				}

				var bulkCopy = new SqlBulkCopy(conn)
				{
					DestinationTableName = $"COMBOBOXES.[{_prefab}]"
				};
				try
				{
					bulkCopy.WriteToServer(comboTable);
				}
				catch (Exception a)
				{
					Debug.WriteLine(a);
				}

				conn.Close();
			}

			Close();
		}

		private void MoveUpButton_OnClick(object sender, RoutedEventArgs e)
		{
			MoveItem(true);
		}

		private void MoveDownButton_OnClick(object sender, RoutedEventArgs e)
		{
			MoveItem(false);
		}

		private void MoveItem(bool up)
		{
			try
			{
				int currentIndex = ChoiceList.SelectedIndex;

				//Index of the selected item
				if (currentIndex < 0 || currentIndex >= DataItemList.Count) return;
				int moveIndex = up ? currentIndex - 1 : currentIndex + 1;

				//move the items
				DataItemList.Move(moveIndex, currentIndex);
			}
			catch (Exception)
			{
				// ignored
			}
		}

		private void UIElement_OnKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key != Key.Enter) return;
			AddItem();
		}

		private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
		{
			AddItem();
		}

		private void AddItem()
		{
			if (!string.IsNullOrEmpty(ChoiceNameTextBox.Text))
				DataItemList.Add(ChoiceNameTextBox.Text);
			ChoiceNameTextBox.Clear();
		}

		private void RemoveButton_OnClick(object sender, RoutedEventArgs e)
		{
			RemoveItem();
		}

		private void RemoveItem()
		{
			int currentIndex = ChoiceList.SelectedIndex;

			if (currentIndex >= 0) DataItemList.RemoveAt(currentIndex);
		}

		private void ChoiceList_OnKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Delete)
				RemoveItem(); /*
			else if (e.Key == Key.Up)
				MoveItem(true);
			else if (e.Key == Key.Down)
				MoveItem(false);*/
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
	}
}