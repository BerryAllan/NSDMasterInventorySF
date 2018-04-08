using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Syncfusion.UI.Xaml.Grid;

namespace NSDMasterInventorySF.ui
{
	public class CellStyleSelector : StyleSelector
	{
		private readonly int _index;

		public CellStyleSelector(int index)
		{
			_index = index;
		}

		public override Style SelectStyle(object item, DependencyObject container)
		{
			var gridCell = container as GridCell;

			if (gridCell?.ColumnBase?.GridColumn == null)
				base.SelectStyle(item, container);

			var editedCellStyle = new Style(typeof(GridCell));
			editedCellStyle.Setters.Add(new Setter(Control.BackgroundProperty, Brushes.Tomato));

			if (MainWindow.EditedCells[_index].ContainsKey(gridCell.ColumnBase.RowIndex))
				if (MainWindow.EditedCells[_index][gridCell.ColumnBase.RowIndex].Contains(gridCell.ColumnBase.ColumnIndex))
					return editedCellStyle;

			return base.SelectStyle(item, container);
		}
	}
}