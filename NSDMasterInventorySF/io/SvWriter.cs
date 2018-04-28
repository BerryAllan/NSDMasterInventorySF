using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;

namespace NSDMasterInventorySF.io
{
	public class SvWriter
	{
		public static void Write(DataSet dataTables, List<string> fileNames, string directoryName, string ext)
		{
			DirectoryInfo di = Directory.CreateDirectory(directoryName + @"\TABLES\");
			App.ClearDir(di.FullName);

			string commaOrTab = ext.Equals(".tsv") ? "\t" : ",";

			var index = 0;
			foreach (DataTable dataTable in dataTables.Tables)
			{
				var sb = new StringBuilder();

				IEnumerable<string> columnNames =
					dataTable.Columns.Cast<DataColumn>().Select(column => column.ColumnName);

				sb.AppendLine(string.Join(commaOrTab, columnNames));

				foreach (DataRow row in dataTable.Rows)
				{
					IEnumerable<string> fields = row.ItemArray.Select(field => field.ToString());
					sb.AppendLine(string.Join(commaOrTab, fields));
				}

				File.WriteAllText($@"{di.FullName}\{fileNames[index]}{ext}", sb.ToString());
				index++;
			}
		}
	}
}