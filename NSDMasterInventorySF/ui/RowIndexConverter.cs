using System;
using System.Globalization;
using System.Windows.Data;

namespace NSDMasterInventorySF.ui
{
	public class RowIndexConverter : IValueConverter
	{
		#region IValueConverter Members

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			// Default to 0. You may want to handle divide by zero 
			// and other issues differently than this.
			var result = 0;

			// Not the best code ever, but you get the idea.
			if (value == null) return result;
			try
			{
				var numerator = (int) value;

				result = numerator - 1 + 1;
			}
			catch
			{
				//ignored
			}

			//if (result == 0) return "＋";
			//if (result == 0) return "➕";

			return result;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			//ignored
			return null;
		}

		#endregion
	}
}