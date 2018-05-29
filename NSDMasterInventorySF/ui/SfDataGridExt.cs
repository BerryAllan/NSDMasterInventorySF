using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Syncfusion.UI.Xaml.Grid;
using Syncfusion.UI.Xaml.ScrollAxis;

namespace NSDMasterInventorySF.ui
{
	public class SfDataGridExt : SfDataGrid
	{
		protected override void OnTextInput(TextCompositionEventArgs e)
		{
			if (!SelectionController.CurrentCellManager.HasCurrentCell)
			{
				base.OnTextInput(e);
				return;
			}

			//Get the Current Row and Column index from the CurrentCellManager
			RowColumnIndex rowColumnIndex = SelectionController.CurrentCellManager.CurrentRowColumnIndex;
			if (e.OriginalSource is SfDataGridExt dataGrid)
			{
				RowGenerator rowGenerator = dataGrid.RowGenerator;
				DataRowBase dataRow = rowGenerator.Items.FirstOrDefault(item => item.RowIndex == rowColumnIndex.RowIndex);
				List<DataColumnBase> visiblecolumn = dataRow?.VisibleColumns;
				if (dataRow is DataRow)
				{
					//Get the column from the VisibleColumn collection based on the column index
					DataColumnBase dataColumn = visiblecolumn.FirstOrDefault(column => column.ColumnIndex == rowColumnIndex.ColumnIndex);
					//Convert the input text to char type 
					char.TryParse(e.Text, out char text);
					//Skip if the column is GridTemplateColumn and the column is not already in editing 
					//Allow Editing only pressed letters digits and Minus sign key
					if (dataColumn != null && !dataColumn.IsEditing &&
					    SelectionController.CurrentCellManager.BeginEdit() &&
					    char.IsLetterOrDigit(text) || char.IsPunctuation(text))
						dataColumn?.Renderer.PreviewTextInput(e);
				}
			}

			base.OnTextInput(e);
		}
	}
}