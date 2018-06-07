using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using NPOI.SS.UserModel;
using Syncfusion.UI.Xaml.Grid;
using Syncfusion.UI.Xaml.Grid.Converter;
using Syncfusion.XlsIO;
using DataColumn = System.Data.DataColumn;
using DataRow = System.Data.DataRow;
using DataTable = System.Data.DataTable;
using MessageBox = System.Windows.MessageBox;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace NSDMasterInventorySF.io
{
	public static class ExcelWriter
	{
		public static void Write(List<SfDataGrid> grids)
		{
			Cursor.Current = Cursors.AppStarting;

			var sfd = new SaveFileDialog
			{
				FilterIndex = 3,
				Filter =
					@"Excel 97 to 2003 Files(*.xls)|*.xls|Excel 2007 to 2010 Files(*.xlsx)|*.xlsx|Excel 2013 File(*.xlsx)|*.xlsx"
			};

			if (sfd.ShowDialog() == true)
			{
				ExcelEngine excelEngine = new ExcelEngine();
				string tempSheetName = App.RandomString(12);
				var workBook = excelEngine.Excel.Workbooks.Create(new[] {tempSheetName});

				using (Stream stream = sfd.OpenFile())
				{
					switch (sfd.FilterIndex)
					{
						case 1:
							workBook.Version = ExcelVersion.Excel97to2003;
							break;
						case 2:
							workBook.Version = ExcelVersion.Excel2010;
							break;
						default:
							workBook.Version = ExcelVersion.Excel2013;
							break;
					}

					foreach (var grid in grids)
					{
						DataTable itemsSource = (DataTable) grid.ItemsSource;
						var options = new ExcelExportingOptions
						{
							ExcelVersion = workBook.Version,
							AllowOutlining = true
						};
						switch (sfd.FilterIndex)
						{
							case 1:
								options.ExcelVersion = ExcelVersion.Excel97to2003;
								break;
							case 2:
								options.ExcelVersion = ExcelVersion.Excel2010;
								break;
							default:
								options.ExcelVersion = ExcelVersion.Excel2013;
								break;
						}

						if (grid.View != null && !grid.View.IsEmpty)
						{
							var tempExcelEngine = grid.ExportToExcel(grid.View, options);
							var workSheet = tempExcelEngine.Excel.Workbooks[0].Worksheets[0];
							workSheet.Name = itemsSource.TableName;
							workBook.Worksheets.AddCopy(workSheet);
						}
						else
						{
							var workSheet = workBook.Worksheets.Create(itemsSource.TableName);
							workSheet.ImportDataTable(itemsSource, true, 1, 1);
						}
					}

					workBook.Worksheets.Remove(tempSheetName);
					workBook.SaveAs(stream);
				}
				if (MessageBox.Show("Do you want to edit the spreadsheet?", "Spreadsheet has been created",
					    MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
				{
					new SpreadsheetEditor(sfd.FileName).Show();
				}
			}

/*

			IWorkbook workbook = isXssf ? (IWorkbook) new XSSFWorkbook() : new HSSFWorkbook();

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

						if (cell == null) continue;
						ICellStyle style = workbook.CreateCellStyle();

						style.FillForegroundColor = table.Rows[i - 2][0].ToString().ToLower().Equals("true")
							? IndexedColors.LightGreen.Index
							: IndexedColors.Rose.Index;

						style.FillPattern = FillPattern.SolidForeground;
						//System.out.println(itemSchools.get(i - counter).isInventoried());
						cell.CellStyle = style;
					}
			}

			workbook.Write(new FileStream(excelFilePath, FileMode.Create));
			workbook.Close();*/
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