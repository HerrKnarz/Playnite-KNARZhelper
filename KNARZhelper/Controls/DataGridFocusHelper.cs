using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace KNARZhelper.Controls
{
    public interface IRequestFocus
    {
        event EventHandler<FocusRequestedEventArgs> FocusRequested;
    }

    public static class DataGridFocusHelper
    {
        public static T FindVisualChild<T>(DependencyObject obj) where T : DependencyObject
        {
            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                var child = VisualTreeHelper.GetChild(obj, i);

                if (child is T t)
                {
                    return t;
                }
                else
                {
                    var childOfChild = FindVisualChild<T>(child);

                    if (childOfChild != null)
                    {
                        return childOfChild;
                    }
                }
            }

            return null;
        }

        public static DataGridCell GetCell(DataGrid dataGrid, DataGridRow rowContainer, int column)
        {
            if (rowContainer != null)
            {
                var presenter = FindVisualChild<DataGridCellsPresenter>(rowContainer);

                if (presenter == null)
                {
                    /* if the row has been virtualized away, call its ApplyTemplate() method
                     * to build its visual tree in order for the DataGridCellsPresenter
                     * and the DataGridCells to be created */
                    rowContainer.ApplyTemplate();
                    presenter = FindVisualChild<DataGridCellsPresenter>(rowContainer);
                }

                if (presenter != null)
                {
                    if (presenter.ItemContainerGenerator.ContainerFromIndex(column) is DataGridCell cell)
                    {
                        return cell;
                    }

                    /* bring the column into view
                     * in case it has been virtualized away */
                    dataGrid.ScrollIntoView(rowContainer, dataGrid.Columns[column]);
                    cell = presenter.ItemContainerGenerator.ContainerFromIndex(column) as DataGridCell;
                    return cell;
                }
            }

            return null;
        }

        public static void SelectRowByIndex(DataGrid dataGrid, int rowIndex)
        {
            if (!dataGrid.SelectionUnit.Equals(DataGridSelectionUnit.FullRow))
            {
                throw new ArgumentException("The SelectionUnit of the DataGrid must be set to FullRow.");
            }

            if (rowIndex < 0 || rowIndex > (dataGrid.Items.Count - 1))
            {
                throw new ArgumentException(string.Format("{0} is an invalid row index.", rowIndex));
            }

            dataGrid.SelectedItems.Clear();
            /* set the SelectedItem property */
            var item = dataGrid.Items[rowIndex]; // = Product X
            dataGrid.SelectedItem = item;

            if (!(dataGrid.ItemContainerGenerator.ContainerFromIndex(rowIndex) is DataGridRow row))
            {
                /* bring the data item (Product object) into view
                 * in case it has been virtualized away */
                dataGrid.ScrollIntoView(item);
                dataGrid.UpdateLayout();
                row = dataGrid.ItemContainerGenerator.ContainerFromIndex(rowIndex) as DataGridRow;
            }

            if (row != null)
            {
                var cell = GetCell(dataGrid, row, 0);
                cell?.Focus();
            }
        }
    }

    public class FocusRequestedEventArgs : EventArgs
    {
        public FocusRequestedEventArgs(string propertyName)
        {
            PropertyName = propertyName;
        }

        public string PropertyName
        {
            get;
            private set;
        }
    }
}
