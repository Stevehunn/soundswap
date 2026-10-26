using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using SoundSwap.Desktop.ViewModels;

namespace SoundSwap.Desktop.Views;

/// <summary>Drag & drop reordering of the configuration cards.</summary>
public partial class MainWindow
{
    private Point _dragOrigin;
    private ConfigItemViewModel? _dragItem;

    private MainViewModel ViewModel => (MainViewModel)DataContext;

    private void OnListMouseDown(object sender, MouseButtonEventArgs e)
    {
        _dragItem = null;
        if (e.OriginalSource is not DependencyObject source) return;
        if (FindAncestor<ButtonBase>(source) is not null) return; // pressing a button is not a drag

        var container = ItemsControl.ContainerFromElement(ItemsList, source);
        if (container is null) return;

        _dragItem = ItemsList.ItemContainerGenerator.ItemFromContainer(container) as ConfigItemViewModel;
        _dragOrigin = e.GetPosition(null);
    }

    private void OnListMouseMove(object sender, MouseEventArgs e)
    {
        if (_dragItem is null || e.LeftButton != MouseButtonState.Pressed) return;

        var delta = e.GetPosition(null) - _dragOrigin;
        if (Math.Abs(delta.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(delta.Y) < SystemParameters.MinimumVerticalDragDistance) return;

        var item = _dragItem;
        _dragItem = null;
        item.IsDragging = true;
        try
        {
            DragDrop.DoDragDrop(ItemsList, new DataObject(typeof(ConfigItemViewModel), item), DragDropEffects.Move);
        }
        finally
        {
            item.IsDragging = false;
            ClearDropMarkers();
        }
    }

    private void OnListDragOver(object sender, DragEventArgs e)
    {
        e.Handled = true;
        if (e.Data.GetData(typeof(ConfigItemViewModel)) is not ConfigItemViewModel item)
        {
            e.Effects = DragDropEffects.None;
            return;
        }

        e.Effects = DragDropEffects.Move;
        AutoScroll(e);
        ShowDropMarker(item, GetInsertIndex(e));
    }

    private void OnListDrop(object sender, DragEventArgs e)
    {
        e.Handled = true;
        if (e.Data.GetData(typeof(ConfigItemViewModel)) is not ConfigItemViewModel item) return;

        var insertIndex = GetInsertIndex(e);
        var source = ViewModel.Items.IndexOf(item);
        ClearDropMarkers();

        // insertIndex is a position in the current list; the final index shifts by one when moving down.
        var target = insertIndex > source ? insertIndex - 1 : insertIndex;
        if (source >= 0 && target != source) ViewModel.Move(item, target);
    }

    /// <summary>Index (0..Count) before which the dragged card would be inserted, from the pointer position.</summary>
    private int GetInsertIndex(DragEventArgs e)
    {
        var y = e.GetPosition(ItemsList).Y;
        for (var i = 0; i < ItemsList.Items.Count; i++)
        {
            if (ItemsList.ItemContainerGenerator.ContainerFromIndex(i) is not FrameworkElement card) continue;
            var top = card.TranslatePoint(new Point(0, 0), ItemsList).Y;
            if (y < top + card.ActualHeight / 2) return i;
        }
        return ItemsList.Items.Count;
    }

    private void ShowDropMarker(ConfigItemViewModel dragged, int insertIndex)
    {
        ClearDropMarkers();
        var items = ViewModel.Items;
        var source = items.IndexOf(dragged);
        if (insertIndex == source || insertIndex == source + 1) return; // would not change the order

        if (insertIndex < items.Count) items[insertIndex].DropBefore = true;
        else items[^1].DropAfter = true;
    }

    private void ClearDropMarkers()
    {
        foreach (var item in ViewModel.Items)
        {
            item.DropBefore = false;
            item.DropAfter = false;
        }
    }

    /// <summary>Scrolls the list when the pointer is close to its top or bottom edge.</summary>
    private void AutoScroll(DragEventArgs e)
    {
        const double edge = 36, step = 14;
        var y = e.GetPosition(ListScroll).Y;
        if (y < edge) ListScroll.ScrollToVerticalOffset(ListScroll.VerticalOffset - step);
        else if (y > ListScroll.ActualHeight - edge) ListScroll.ScrollToVerticalOffset(ListScroll.VerticalOffset + step);
    }

    private static T? FindAncestor<T>(DependencyObject? element) where T : DependencyObject
    {
        while (element is not null)
        {
            if (element is T match) return match;
            // Run / Hyperlink etc. are not visuals: use the logical tree for them.
            element = element is Visual or System.Windows.Media.Media3D.Visual3D
                ? VisualTreeHelper.GetParent(element)
                : LogicalTreeHelper.GetParent(element);
        }
        return null;
    }
}
