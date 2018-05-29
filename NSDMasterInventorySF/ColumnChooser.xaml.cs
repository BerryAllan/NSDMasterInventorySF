using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Syncfusion.Linq;

namespace NSDMasterInventorySF
{
	/// <summary>
	/// Interaction logic for ColumnChooser.xaml
	/// </summary>
	public partial class ColumnChooser
	{
		public static RoutedCommand CloseWindow = new RoutedCommand();
		private string _chosenColumn;
		private ComboBoxBuilder _builder;

		public ColumnChooser(DataTable prefabTable, DataTable comboTable, ComboBoxBuilder builder)
		{
			_builder = builder;
			CloseWindow.InputGestures.Add(new KeyGesture(Key.Escape));
			CommandBindings.Add(new CommandBinding(CloseWindow, CloseCurrentWindow));

			InitializeComponent();

			List<string> columnNames = new List<string>();
			foreach(DataColumn col in comboTable.Columns)
				columnNames.Add(col.ColumnName);

			foreach (DataRow row in prefabTable.Rows)
				if ((row[1].ToString().ToLower().Equals("autocomplete") || row[1].ToString().ToLower().Equals("combobox")) &&
				    !comboTable.Columns.ToList<DataColumn>().Select(column => column.ColumnName).Contains(row[0]))
					ViableColumnList.Items.Add(row[0].ToString());
		}

		private void DoneButton_OnClick(object sender, RoutedEventArgs e)
		{
			_chosenColumn = ViableColumnList.SelectedItem.ToString();
			if (!string.IsNullOrEmpty(_chosenColumn))
				_builder.ComboTable.Columns.Add(_chosenColumn);
			_builder.ComboGrid.ItemsSource = _builder.ComboTable;
			_builder.GenerateColumns();
			Close();
		}

		private void CloseCurrentWindow(object sender, EventArgs e)
		{
			Close();
		}

		private void ViableColumnList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			DoneButton.IsEnabled = ViableColumnList.SelectedItems.Count > 0;
		}
	}
}