using System.Windows;
using System.Windows.Controls;
using Syncfusion.UI.Xaml.Grid.Cells;

namespace NSDMasterInventorySF.ui
{
	public class GridCellComboBoxRendererExt : GridCellComboBoxRenderer
	{
		protected override void OnEditElementLoaded(object sender, RoutedEventArgs e)
		{
			base.OnEditElementLoaded(sender, e);
			var combobox = sender as ComboBox;
			combobox.IsDropDownOpen = true;
		}
	}
}