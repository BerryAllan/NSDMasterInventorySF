using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NSDMasterInventorySF.Properties;
using Syncfusion.SfSkinManager;

namespace NSDMasterInventorySF
{
	/// <inheritdoc cref="Window" />
	/// <summary>
	/// Interaction logic for DatabaseManager.xaml
	/// </summary>
	public partial class DatabaseManager
	{
		public static RoutedCommand CloseWindow = new RoutedCommand();
		public static RoutedCommand CopyItem = new RoutedCommand();
		public static RoutedCommand PasteItem = new RoutedCommand();
		private readonly MainWindow _window;
		private TreeViewItem _copiedItem;

		private string _currentVisualStyle;

		public string CurrentVisualStyle
		{
			get => _currentVisualStyle;
			set
			{
				_currentVisualStyle = value;
				OnVisualStyleChanged();
			}
		}

		public DatabaseManager(MainWindow window)
		{
			CloseWindow.InputGestures.Add(new KeyGesture(Key.Escape));
			CommandBindings.Add(new CommandBinding(CloseWindow, CloseCurrentWindow));
			CopyItem.InputGestures.Add(new KeyGesture(Key.C, ModifierKeys.Control));
			CommandBindings.Add(new CommandBinding(CopyItem, CopyCurrentItem));
			PasteItem.InputGestures.Add(new KeyGesture(Key.V, ModifierKeys.Control));
			CommandBindings.Add(new CommandBinding(PasteItem, PasteCurrentItem));

			InitializeComponent();
			_window = window;

			PopulateTreeView();
		}

		private void PasteCurrentItem(object sender, ExecutedRoutedEventArgs e)
		{
			if (_copiedItem == null) return;
			using (var conn = new SqlConnection(App.ConnectionString))
			{
				conn.Open();
				TreeViewItem pasteDestItem = (TreeViewItem) DbTreeView.SelectedItem;
				//Debug.WriteLine(_copiedItem.Header);
				if (!(pasteDestItem.Parent is TreeViewItem))
				{
					//Debug.WriteLine(pasteDestItem.Header);
					if (_copiedItem.Parent != null && _copiedItem.Parent is TreeViewItem parent &&
					    !App.GetTableNames(conn, $"{pasteDestItem.Header}").Contains(_copiedItem.HeaderStringFormat))
					{
						using (var comm =
							new SqlCommand(
								$"SELECT * INTO [{pasteDestItem.Header}].[{_copiedItem.Header}] FROM [{parent.Header}].[{_copiedItem.Header}]",
								conn))
						{
							//Debug.WriteLine(comm.CommandText);
							comm.ExecuteNonQuery();
						}
					}
				}

				conn.Close();
			}

			PopulateTreeView();
		}

		private void CopyCurrentItem(object sender, ExecutedRoutedEventArgs e)
		{
			if (((TreeViewItem) DbTreeView.SelectedItem).Parent is TreeViewItem)
			{
				_copiedItem = (TreeViewItem) DbTreeView.SelectedItem;
				//Debug.WriteLine(_copiedItem.Header);
			}
		}

		private void CloseCurrentWindow(object sender, ExecutedRoutedEventArgs e)
		{
			Close();
		}

		private void DeleteButtonClick(object sender, RoutedEventArgs e)
		{
			DeleteSelectedItem();
		}

		private void TreeGridOnKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Delete) DeleteSelectedItem();
		}

		private void DeleteSelectedItem()
		{
			if (MessageBox.Show(
				    "Are you sure? This will result in a permanent loss of data, including any tables that are belong to this schema.",
				    "Confirm",
				    MessageBoxButton.YesNo, MessageBoxImage.Exclamation) != MessageBoxResult.Yes) return;

			string itemToDelete = ((TreeViewItem) DbTreeView.SelectedItem).Header.ToString();
			if (itemToDelete.Equals("dbo") || itemToDelete.Equals("db_accessadmin") ||
			    itemToDelete.Equals("db_backupoperator") || itemToDelete.Equals("db_datareader") ||
			    itemToDelete.Equals("db_datawriter") || itemToDelete.Equals("db_ddladmin") ||
			    itemToDelete.Equals("db_denydatareader") || itemToDelete.Equals("db_denydatawriter") ||
			    itemToDelete.Equals("db_owner") || itemToDelete.Equals("db_securityadmin") ||
			    itemToDelete.Equals("guest") || itemToDelete.Equals("sys") || itemToDelete.Equals("INFORMATION_SCHEMA"))
				return;
			using (var conn = new SqlConnection(App.ConnectionString))
			{
				conn.Open();
				var item = (TreeViewItem) DbTreeView.SelectedItem;
				if (item.Parent != null && item.Parent is TreeViewItem parent)
				{
					using (var comm = new SqlCommand($"DROP TABLE [{parent.Header}].[{item.Header}]", conn))
						comm.ExecuteNonQuery();
				}
				else
				{
					foreach (var table in App.GetTableNames(conn, item.Header.ToString()))
					{
						using (var comm = new SqlCommand($"DROP TABLE [{item.Header}].[{table}]", conn))
							comm.ExecuteNonQuery();
					}

					using (var comm = new SqlCommand($"DROP SCHEMA [{item.Header}]", conn))
						comm.ExecuteNonQuery();
					if (item.Header.ToString().Equals(Settings.Default.Schema))
					{
						new ConnectionManager(_window)
						{
							ShowInTaskbar = false,
							Owner = this
						}.ShowDialog();
					}
				}

				conn.Close();
			}

			PopulateTreeView();
		}

		private void SheetListBox_OnSelectionChanged(object sender,
			RoutedPropertyChangedEventArgs<object> propertyChangedEventArgs)
		{
			DeleteButton.IsEnabled = DbTreeView.SelectedItem != null;
		}

		private void AddSchemaClick(object sender, RoutedEventArgs e)
		{
			var choosePrefab = new EditTable(false, string.Empty, false, true)
			{
				Owner = this,
				ShowInTaskbar = false
			};
			choosePrefab.ShowDialog();
			PopulateTreeView();
		}

		public void PopulateTreeView()
		{
			List<string> expandeds = (from TreeViewItem item in DbTreeView.Items
				where item.IsExpanded
				select item.Header.ToString()).ToList();
			DbTreeView.Items.Clear();
			using (var conn = new SqlConnection(App.ConnectionString))
			{
				conn.Open();
				foreach (var schema in App.GetAllNames(conn, "schemas"))
				{
					if (schema.Equals("dbo") || schema.Equals("db_accessadmin") ||
					    schema.Equals("db_backupoperator") || schema.Equals("db_datareader") ||
					    schema.Equals("db_datawriter") || schema.Equals("db_ddladmin") ||
					    schema.Equals("db_denydatareader") || schema.Equals("db_denydatawriter") ||
					    schema.Equals("db_owner") || schema.Equals("db_securityadmin") ||
					    schema.Equals("guest") || schema.Equals("sys") || schema.Equals("INFORMATION_SCHEMA"))
						continue;
					TreeViewItem schemaItem = new TreeViewItem {Header = schema};
					if (expandeds.Contains(schemaItem.Header.ToString()))
						schemaItem.IsExpanded = true;
					foreach (var table in App.GetTableNames(conn, schema))
					{
						schemaItem.Items.Add(new TreeViewItem {Header = table});
					}

					DbTreeView.Items.Add(schemaItem);
				}

				conn.Close();
			}
		}

		private void OnVisualStyleChanged()
		{
			Enum.TryParse(CurrentVisualStyle, out VisualStyles visualStyle);
			if (visualStyle == VisualStyles.Default) return;
			SfSkinManager.ApplyStylesOnApplication = true;
			SfSkinManager.SetVisualStyle(this, visualStyle);
			SfSkinManager.ApplyStylesOnApplication = false;
		}

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			CurrentVisualStyle = Settings.Default.Theme;
		}

		private void DatabaseManager_OnClosed(object sender, EventArgs e)
		{
			_window.InitializeOrRefreshEverything(_window.MasterTabControl.SelectedIndex);
		}
	}
}