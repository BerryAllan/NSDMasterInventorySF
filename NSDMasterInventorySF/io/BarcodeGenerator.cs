using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows;
using ZXing;
using ZXing.Datamatrix;
using ZXing.Datamatrix.Encoder;
using FontStyle = System.Drawing.FontStyle;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;

namespace NSDMasterInventorySF.io
{
	public class BarcodeGenerator
	{
		public static void CreateDmCodes(string dir, DataSet dataTables, MainWindow window)
		{
			Directory.CreateDirectory(dir + @"\BARCODES\");
			App.ClearDir(dir + @"\BARCODES\");

			double progress = 0;
			double totalItems = 0;
			foreach (DataTable table in dataTables.Tables)
				totalItems += table.Rows.Count;
			foreach (DataTable table in dataTables.Tables)
			{
				foreach (DataRow row in table.Rows)
				{
					string item = GetItemStringFromDataRow(row);

					DirectoryInfo di = Directory.CreateDirectory($@"{dir}\BARCODES\{table.TableName}\");
					string itemFileName = item.Replace('\t', '_');
					itemFileName = itemFileName.Replace('/', '∕');
					itemFileName = itemFileName.Replace('\\', '∕');
					itemFileName = itemFileName.Replace(':', '꞉');
					itemFileName = itemFileName.Replace('?', '？');
					itemFileName = itemFileName.Replace('>', '›');
					itemFileName = itemFileName.Replace('<', '‹');
					itemFileName = itemFileName.Replace('*', '✻');
					itemFileName = itemFileName.Replace('"', '\'');
					itemFileName = itemFileName.Replace('|', '│');

					if (string.IsNullOrEmpty(item) || string.IsNullOrEmpty(itemFileName)) continue;
					SaveBarcode(item, itemFileName, di.FullName + itemFileName + ".png");

					progress++;
					window.Dispatcher.Invoke(() =>
					{
						if (progress < totalItems)
						{
							if (window.ProgressGrid.Visibility != Visibility.Visible)
							{
								window.ProgressTextBlock.Text = "Barcodes Generating...";
								window.ProgressGrid.Visibility = Visibility.Visible;
							}

							window.ProgressBar.Value = progress / totalItems * 100.0;
						}
						else
						{
							window.ProgressBar.Value = 0;
							window.ProgressGrid.Visibility = Visibility.Hidden;
						}
					});
				}
			}
		}

		public static string GetItemStringFromDataRow(DataRow row)
		{
			List<string> rowItemArray = row.ItemArray.Select(v => v.ToString()).ToList();
			if (bool.TryParse(rowItemArray[0], out bool _) &&
			    row.Table.Columns[0].ColumnName.ToLower().Equals("inventoried"))
				rowItemArray.RemoveAt(0);
			return string.Join("\t", rowItemArray);
		}

		public static void SaveBarcode(string item, string fileName, string location)
		{
			var datamatrixEncodingOptions = new DatamatrixEncodingOptions
			{
				Height = 400,
				Width = 400,
				PureBarcode = true,
				Margin = 0,
				SymbolShape = SymbolShapeHint.FORCE_SQUARE
			};
			var barcodeWriter = new BarcodeWriter
			{
				Format = BarcodeFormat.DATA_MATRIX,
				Options = datamatrixEncodingOptions
			};

			Bitmap oldMap = barcodeWriter.Write(item);
			Bitmap newMap = ResizeCanvas(oldMap, oldMap.Width, oldMap.Height + 30, 0, 0);
			Graphics gfx = Graphics.FromImage(newMap);
			gfx.SmoothingMode = SmoothingMode.HighQuality;
			Font arialFont = new Font("Calibri Light", 10, FontStyle.Regular);
			gfx.DrawString(fileName, arialFont, Brushes.Black, new Point(0, oldMap.Height + 5));
			try
			{
				newMap.Save(location);
			}
			catch
			{
				// ignored
			}
		}

		private static Bitmap ResizeCanvas(Image imageToEmbed, int iconSizeX, int iconSizeY, int gridX, int gridY)
		{
			// Load the image and determine new dimensions
			Image img = imageToEmbed;
			// Define the new dimensions
			Size szDimensions = new Size(iconSizeX, iconSizeY);

			// Create blank canvas
			Bitmap resizedImg = new Bitmap(szDimensions.Width, szDimensions.Height);
			Graphics gfx = Graphics.FromImage(resizedImg);

			// Paste source image on blank canvas, then save it
			gfx.DrawImageUnscaled(img, gridX, gridY);

			return resizedImg;
		}
	}
}