using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using Syncfusion.UI.Xaml.Grid;

namespace NSDMasterInventorySF.ui
{
	public class SearchHelperExt : SearchHelper
	{
		private const string Quote = "\"";

		public SearchHelperExt(SfDataGrid datagrid)
			: base(datagrid)
		{
			AllowFiltering = false;
			AllowCaseSensitiveSearch = false;
			SearchType = SearchType.Contains;
			SearchBrush = Brushes.DeepSkyBlue;
		}

		protected override bool SearchCell(DataColumnBase column, object record, bool applySearchHighlightBrush)
		{
			if (column == null) return true;

			if (column.GridColumn == null || DataGrid.View == null) return false;

			var gridTemplateColumn = column.GridColumn as GridTemplateColumn;
			if (column.GridColumn.CellTemplate == null) return base.SearchCell(column, record, applySearchHighlightBrush);

			if (Provider == null) Provider = DataGrid.View.GetPropertyAccessProvider();

			object data = Provider.GetFormattedValue(record, column.GridColumn.MappingName);

			if (MatchSearchText(column.GridColumn, record)) return ApplyInline(column, data, applySearchHighlightBrush);

			ClearSearchCell(column, record);
			return false;
		}

		protected override bool MatchSearchText(GridColumn column, object record)
		{
			try
			{
				IEnumerable<string> searchStrings = GetSearchStrings().Select(_ => _.ToUpperInvariant());

				if (Provider.GetFormattedValue(record, column.MappingName) is DBNull)
					return false;

				string data = ((string) Provider.GetFormattedValue(record, column.MappingName)).ToUpperInvariant();
				return searchStrings.Any(data.Contains);
			}
			catch (Exception)
			{
				return false;
			}
		}

		protected override void ClearSearchCell(DataColumnBase column, object record)
		{
			if (column.GridColumn != null && string.IsNullOrEmpty(SearchText) && column.GridColumn.CellTemplate != null)
			{
				TextBlock[] textControls = FindObjectInVisualTreeDown<TextBlock>(column.ColumnElement).ToArray();
				if (textControls.Any())
				{
					TextBlock textBlock = textControls.First();
					if (textBlock != null && textBlock.Inlines.Count > 1)
					{
						textBlock.ClearValue(TextBlock.TextProperty);
						return;
					}
				}
			}

			base.ClearSearchCell(column, record);
		}

		protected override bool ApplyInline(DataColumnBase column, object data, bool ApplySearchHighlightBrush)
		{
			IEnumerable<string> searchTexts = GetSearchStrings().Select(Regex.Escape);
			var success = false;

			var regex = new Regex($"({string.Join("|", searchTexts)})", RegexOptions.IgnoreCase);

			TextBlock[] textControls = FindObjectInVisualTreeDown<TextBlock>(column.ColumnElement).ToArray();
			if (!textControls.Any()) return base.ApplyInline(column, data, ApplySearchHighlightBrush);

			TextBlock textBlock = textControls.First();
			Binding binding = BindingOperations.GetBinding(textBlock, TextBlock.TextProperty);

			string[] substrings = regex.Split(data.ToString());
			textBlock.Inlines.Clear();

			Binding binding1 = BindingOperations.GetBinding(textBlock, TextBlock.TextProperty);
			foreach (string item in substrings)
				if (regex.Match(item).Success)
				{
					var run = new Run(item);
					run.Background = SearchBrush;
					textBlock.Inlines.Add(run);
					success = true;
				}
				else
				{
					textBlock.Inlines.Add(item);
				}

			return success;
		}

		private IEnumerable<T> FindObjectInVisualTreeDown<T>(DependencyObject parent) where T : DependencyObject
		{
			if (parent == null) return null;

			var foundChildren = new List<T>();

			int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
			for (var i = 0; i < childrenCount; i++)
			{
				DependencyObject child = VisualTreeHelper.GetChild(parent, i);
				var childType = child as T;
				if (childType == null)
					foundChildren.AddRange(FindObjectInVisualTreeDown<T>(child));
				else
					foundChildren.Add(childType);
			}

			return foundChildren;
		}

		private IEnumerable<string> GetSearchStrings()
		{
			return GetAllStringVariants(SearchText);
		}

		private static IEnumerable<string> GetAllStringVariants(string text)
		{
			var strings = new List<string> {text};
			if (text.StartsWith(Quote, StringComparison.Ordinal) && text.EndsWith(Quote, StringComparison.Ordinal))
			{
				strings[0] = strings.First().Replace(Quote, string.Empty);
			}
			else if (text.StartsWith(Quote, StringComparison.Ordinal))
			{
				strings[0] = strings.First().Replace(Quote, string.Empty);
				text = text.Replace(Quote, string.Empty);
				strings.AddRange(text.Split(new[] {" "}, StringSplitOptions.RemoveEmptyEntries).ToList());
				strings.Remove(text);
			}
			else
			{
				//Split text filter into seperated filter texts 
				IEnumerable<string> splittedStrings = text.Split(new[] {" "}, StringSplitOptions.RemoveEmptyEntries).ToList()
					.Where(_ => !strings.Contains(_));
				strings.AddRange(splittedStrings);
			}

			return strings;
		}
	}
}