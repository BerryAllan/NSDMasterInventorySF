#region Copyright Syncfusion Inc. 2001-2018.

// Copyright Syncfusion Inc. 2001-2018. All rights reserved.
// Use of this code is subject to the terms of our license.
// A copy of the current license can be obtained at any time by e-mailing
// licensing@syncfusion.com. Any infringement will be prosecuted under
// applicable laws. 

#endregion

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace NSDMasterInventorySFUWP
{
	/// <inheritdoc cref="Page" />
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class MainPage
	{
		public MainPage()
		{
			InitializeComponent();
			ContentFrame.Navigate(typeof(MasterTablePage), new MasterTablePage());
		}

		private void MasterNavView_OnItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
		{
		}

		private void MasterNavView_OnLoaded(object sender, RoutedEventArgs e)
		{
			MasterNavView.SelectedItem = MainTables;
		}

		private void MasterNavView_OnBackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
		{
		}
	}
}