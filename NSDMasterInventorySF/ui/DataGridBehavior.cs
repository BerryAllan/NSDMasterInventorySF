using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace NSDMasterInventorySF.ui
{
	public class DataGridBehavior
	{
		#region DisplayRowNumber

		public static DependencyProperty DisplayRowNumberProperty =
			DependencyProperty.RegisterAttached("DisplayRowNumber",
				typeof(bool),
				typeof(DataGridBehavior),
				new FrameworkPropertyMetadata(false, OnDisplayRowNumberChanged));

		public static bool GetDisplayRowNumber(DependencyObject target)
		{
			return (bool) target.GetValue(DisplayRowNumberProperty);
		}

		public static void SetDisplayRowNumber(DependencyObject target, bool value)
		{
			target.SetValue(DisplayRowNumberProperty, value);
		}

		private static void OnDisplayRowNumberChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
		{
			var dataGrid = target as DataGrid;
			if ((bool) e.NewValue)
			{
				void LoadedRowHandler(object sender, DataGridRowEventArgs ea)
				{
					if (GetDisplayRowNumber(dataGrid) == false)
					{
						dataGrid.LoadingRow -= LoadedRowHandler;
						return;
					}

					ea.Row.Header = ea.Row.GetIndex() + 1;
				}

				if (dataGrid != null)
				{
					dataGrid.LoadingRow += LoadedRowHandler;

					void ItemsChangedHandler(object sender, ItemsChangedEventArgs ea)
					{
						if (GetDisplayRowNumber(dataGrid) == false)
						{
							dataGrid.ItemContainerGenerator.ItemsChanged -= ItemsChangedHandler;
							return;
						}

						GetVisualChildCollection<DataGridRow>(dataGrid).ForEach(d => d.Header = d.GetIndex() + 1);
					}

					dataGrid.ItemContainerGenerator.ItemsChanged += ItemsChangedHandler;
				}
			}
		}

		#endregion // DisplayRowNumber

		#region Get Visuals

		private static List<T> GetVisualChildCollection<T>(object parent) where T : Visual
		{
			var visualCollection = new List<T>();
			GetVisualChildCollection(parent as DependencyObject, visualCollection);
			return visualCollection;
		}

		private static void GetVisualChildCollection<T>(DependencyObject parent, List<T> visualCollection) where T : Visual
		{
			int count = VisualTreeHelper.GetChildrenCount(parent);
			for (var i = 0; i < count; i++)
			{
				DependencyObject child = VisualTreeHelper.GetChild(parent, i);
				if (child is T) visualCollection.Add(child as T);

				if (child != null) GetVisualChildCollection(child, visualCollection);
			}
		}

		#endregion // Get Visuals
	}
}