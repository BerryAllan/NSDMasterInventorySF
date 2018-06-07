using System.Diagnostics;
using System.Threading;
using System.Windows.Input;

namespace NSDMasterInventorySF
{
	/// <summary>
	/// Interaction logic for SpreadsheetEditor.xaml
	/// </summary>
	public partial class SpreadsheetEditor
	{
		private readonly RoutedCommand _saveCommand = new RoutedCommand();
		private readonly RoutedCommand _saveAsCommand = new RoutedCommand();

		public SpreadsheetEditor(string fileName)
		{
			_saveCommand.InputGestures.Add(new KeyGesture(Key.S, ModifierKeys.Control));
			CommandBindings.Add(new CommandBinding(_saveCommand, Save));
			_saveAsCommand.InputGestures.Add(new KeyGesture(Key.S, ModifierKeys.Control | ModifierKeys.Shift));
			CommandBindings.Add(new CommandBinding(_saveAsCommand, SaveAs));

			InitializeComponent();
			Spreadsheet.Open(fileName);
			Spreadsheet.AllowCellContextMenu = true;
			Spreadsheet.AllowExtendRowColumnCount = true;
			Spreadsheet.AllowFiltering = true;
			Spreadsheet.AllowZooming = true;
			Spreadsheet.AllowFormulaRangeSelection = true;
			Spreadsheet.DisplayAlerts = true;
			Spreadsheet.Loaded += (sender, args) => Spreadsheet.ActiveGrid.CurrentCellValueChanged +=
				(sender1, args1) =>
				{
					Debug.WriteLine("asdf");
					Title += " *";
				};
		}

		private void SaveAs(object sender, ExecutedRoutedEventArgs e)
		{
			Spreadsheet.SaveAs();
		}

		private void Save(object sender, ExecutedRoutedEventArgs e)
		{
			System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.WaitCursor;
			Spreadsheet.Save();
			Title = Title.Replace(" *", string.Empty);
			Thread.Sleep(150);
			System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.Default;
		}
	}
}