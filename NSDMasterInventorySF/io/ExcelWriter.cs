using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Windows.Forms;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Syncfusion.Linq;
using Syncfusion.UI.Xaml.Grid;
using DataColumn = System.Data.DataColumn;
using DataRow = System.Data.DataRow;

namespace NSDMasterInventorySF.io
{
	public class ExcelWriter
	{
		public static void Write(DataSet dataTables, string excelFilePath, bool isXssf)
		{
			Cursor.Current = Cursors.AppStarting;
			IWorkbook workbook;
			workbook = isXssf ? (IWorkbook) new XSSFWorkbook() : new HSSFWorkbook();

			DataTable recycledTable = Recycled.RecycledDataTable.Copy();
			recycledTable.TableName = "RECYCLED";
			List<DataTable> absoluteDataTables =
				new List<DataTable>(dataTables.Tables.ToList<DataTable>()) {recycledTable};

			foreach (DataTable table in absoluteDataTables)
			{
				string sheetName = table.TableName;

				ISheet sheet = workbook.GetSheet(sheetName) ?? workbook.CreateSheet(sheetName);

				IRow row0 = sheet.CreateRow(0);
				WritePrefabRow(table, row0);
				foreach (ICell cell in row0)
					if (cell != null)
					{
						ICellStyle style = workbook.CreateCellStyle();
						IFont font = workbook.CreateFont();
						font.Boldweight = (short) FontBoldWeight.Bold;
						style.SetFont(font);
						cell.CellStyle = style;
					}

				foreach (DataRow dataRow in table.Rows)
				{
					IRow row = sheet.CreateRow(table.Rows.IndexOf(dataRow) + 2);
					WriteRow(dataRow, row);
				}

				//Debug.WriteLine(row0.GetCell(0).ToString());
				if (row0.GetCell(0).ToString().Equals("Inventoried") &&
				    MainWindow.MasterDataGrids[dataTables.Tables.IndexOf(table)].Columns[0] is GridCheckBoxColumn)
					for (var i = 2; i <= sheet.LastRowNum; i++)
					{
						IRow row = sheet.GetRow(i);
						ICell cell = row.GetCell(0);

						if (cell != null)
						{
							ICellStyle style = workbook.CreateCellStyle();

							if (table.Rows[i - 2][0].ToString().ToLower().Equals("true"))
								style.FillForegroundColor = IndexedColors.LightGreen.Index;
							else
								style.FillForegroundColor = IndexedColors.Rose.Index;

							style.FillPattern = FillPattern.SolidForeground;
							//System.out.println(itemSchools.get(i - counter).isInventoried());
							cell.CellStyle = style;
						}
					}
			}

			workbook.Write(new FileStream(excelFilePath, FileMode.Create));
			workbook.Close();
			Cursor.Current = Cursors.Default;
		}

		private static void WriteRow(DataRow item, IRow row)
		{
			for (var i = 0; i < item.ItemArray.Length; i++)
			{
				ICell cell = row.CreateCell(i);
				cell.SetCellValue(item[i].ToString());
			}
		}

		private static void WritePrefabRow(DataTable table, IRow row)
		{
			var i = 0;
			foreach (DataColumn column in table.Columns)
			{
				ICell cell = row.CreateCell(i);
				cell.SetCellValue(column.ColumnName);
				i++;
			}
		}
	}
}