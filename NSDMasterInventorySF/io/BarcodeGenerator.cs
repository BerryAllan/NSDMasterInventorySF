using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows;
using Syncfusion.Data.Extensions;
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
			foreach (DataRow row in table.Rows)
				totalItems++;
			foreach (DataTable table in dataTables.Tables)
			{
				foreach (DataRow row in table.Rows)
				{
					List<string> rowItemArray = new List<string>();
					foreach (var v in row.ItemArray)
						rowItemArray.Add(v.ToString());
					if (bool.TryParse(rowItemArray[0], out bool _) &&
					    table.Columns[0].ColumnName.ToLower().Equals("inventoried"))
						rowItemArray.RemoveAt(0);
					string item = string.Join("\t", rowItemArray);

					string itemFileName = item.Replace('\t', '_');
					itemFileName = itemFileName.Replace('/', '∕');
					itemFileName = itemFileName.Replace('\\', '∕');
					itemFileName = itemFileName.Replace(':', '꞉');
					itemFileName = itemFileName.Replace('?', '？');

					if (string.IsNullOrEmpty(item) || string.IsNullOrEmpty(itemFileName)) continue;

					var datamatrixEncodingOptions = new DatamatrixEncodingOptions
					{
						Height = 300,
						Width = 300,
						PureBarcode = true,
						Margin = 0,
						SymbolShape = SymbolShapeHint.FORCE_SQUARE
					};
					var barcodeWriter = new BarcodeWriter
					{
						Format = BarcodeFormat.DATA_MATRIX,
						Options = datamatrixEncodingOptions
					};

					DirectoryInfo di = Directory.CreateDirectory($@"{dir}\BARCODES\{table.TableName}\");
					Bitmap oldMap = barcodeWriter.Write(item);
					Bitmap newMap = ResizeCanvas(oldMap, oldMap.Width, oldMap.Height + 30, 0, 0);
					Graphics gfx = Graphics.FromImage(newMap);
					gfx.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
					Font arialFont = new Font("Calibri Light", 10, FontStyle.Regular);
					gfx.DrawString(itemFileName, arialFont, Brushes.Black, new Point(0, oldMap.Height + 5));
					newMap.Save(di.FullName + itemFileName + ".png");
					progress++;
					window.Dispatcher.Invoke(() =>
					{
						if (progress < totalItems)
						{
							if (window.ProgressGrid.Visibility != Visibility.Visible)
							{
								window.ProgressTextBlock.Text = "Barcode Generation In Progress...";
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

		static Bitmap ResizeCanvas(Bitmap imageToEmbed, int iconSizeX, int iconSizeY, int gridX, int gridY)
		{
			// Load the image and determine new dimensions
			System.Drawing.Image img = imageToEmbed;
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