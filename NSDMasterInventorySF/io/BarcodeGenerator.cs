using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using ZXing;
using ZXing.Datamatrix;
using ZXing.Datamatrix.Encoder;

namespace NSDMasterInventorySF.io
{
	public class BarcodeGenerator
	{
		public static void CreateDmCodes(string dir, List<DataTable> dataTables)
		{
			Directory.CreateDirectory(dir + @"\BARCODES\");
			App.ClearDir(dir + @"\BARCODES\");

			using (var conn = new SqlConnection(App.ConnectionString))
			{
				var i = 0;
				foreach (string filePath in App.GetTableNames(conn))
				{
					foreach (DataRow row in dataTables[i].Rows)
					{
						string item = string.Join("\t", row.ItemArray);

						string itemFileName = item.Replace('\t', '_');
						itemFileName = itemFileName.Replace('/', '∕');
						itemFileName = itemFileName.Replace('\\', '∕');
						itemFileName = itemFileName.Replace(':', '꞉');

						if (string.IsNullOrEmpty(item) || string.IsNullOrEmpty(itemFileName)) continue;

						var datamatrixEncodingOptions = new DatamatrixEncodingOptions
						{
							Height = 300,
							Width = 300,
							PureBarcode = true,
							Margin = 0,
							SymbolShape = SymbolShapeHint.FORCE_SQUARE
						};
						var barcodeWriter = new BarcodeWriter {Format = BarcodeFormat.DATA_MATRIX, Options = datamatrixEncodingOptions};

						DirectoryInfo di = Directory.CreateDirectory($@"{dir}\BARCODES\{filePath}\");
						barcodeWriter.Write(item).Save(di.FullName + itemFileName + ".png");
					}

					i++;
				}
			}
		}
	}
}