using System.Windows;
using System.Windows.Input;

namespace NSDMasterInventorySF
{
	/// <summary>
	/// Interaction logic for AboutWindow.xaml
	/// </summary>
	public partial class AboutWindow : Window
	{
		public static RoutedCommand CloseCommand = new RoutedCommand();

		public AboutWindow()
		{
			CloseCommand.InputGestures.Add(new KeyGesture(Key.Escape));
			CommandBindings.Add(new CommandBinding(CloseCommand, CloseEvent));

			InitializeComponent();
		}

		private void CloseEvent(object sender, ExecutedRoutedEventArgs e)
		{
			Close();
		}
	}
}
