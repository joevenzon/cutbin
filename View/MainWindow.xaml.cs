using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CutBin
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ViewModel _viewModel;
        private Point _lastMouseDown;
        private TreeViewItem _selectedItem;
        private TreeViewItem _dragTargetItem;

        public MainWindow()
        {
            try
            {
                InitializeComponent();
                _viewModel = new ViewModel(this);
                DataContext = _viewModel;
            }
            catch (Exception e)
            {
                Debug.Assert(false, e.Message);
            }
        }

        private void TreeView_OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue == null)
                _viewModel.DeselectCut();
            else
                _viewModel.SelectCut(((TreeViewItem)e.NewValue).Name.ToString(), (TreeViewItem)e.NewValue);

            _selectedItem = (TreeViewItem)e.NewValue;
        }

        private void NewCutButton(object sender, RoutedEventArgs e)
        {
            _viewModel.NewCut();
        }

        private void DeleteCutButton(object sender, RoutedEventArgs e)
        {
            _viewModel.DeleteCut();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _viewModel.Save();
        }

        private void TreeView_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                _lastMouseDown = e.GetPosition(cutTree);
            }
        }

        private void TreeView_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Point currentPosition = e.GetPosition(cutTree);

                // Note: This should be based on some accessibility number and not just 2 pixels
                if ((Math.Abs(currentPosition.X - _lastMouseDown.X) > 2.0) ||
                    (Math.Abs(currentPosition.Y - _lastMouseDown.Y) > 2.0))
                {
                    if (cutTree.SelectedItem != null)
                    {
                        if (_selectedItem != null)
                        {
                            DragDropEffects finalDropEffect = DragDrop.DoDragDrop(_selectedItem, _selectedItem.Name.ToString(), DragDropEffects.Move);
                            if ((finalDropEffect == DragDropEffects.Move) && (_dragTargetItem != null))
                            {
                                // A Move drop was accepted
                                _viewModel.DragCut(_selectedItem.Name.ToString(), _dragTargetItem.Name.ToString());
                                _dragTargetItem = null;
                            }
                        }
                    }
                }
            }
        }

        private void TheTreeView_Drop(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.None;
            e.Handled = true;


            // Verify that this is a valid drop and then store the drop target
            TreeViewItem container = GetNearestContainer(e.OriginalSource as UIElement);
            if (container != null)
            {
                // todo: more validation here?
                _dragTargetItem = container;
                e.Effects = DragDropEffects.Move;
            }
        }

        private void TheTreeView_CheckDropTarget(object sender, DragEventArgs e)
        {
            if (!IsValidDropTarget(e.OriginalSource as UIElement))
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private bool IsValidDropTarget(UIElement target)
        {
            if (target != null)
            {
                TreeViewItem container = GetNearestContainer(target);
                
                return (container != null);
            }

            return false;
        }

        private TreeViewItem GetNearestContainer(UIElement element)
        {
            // Walk up the element tree to the nearest tree view item.
            TreeViewItem container = element as TreeViewItem;
            while ((container == null) && (element != null))
            {
                element = VisualTreeHelper.GetParent(element) as UIElement;
                container = element as TreeViewItem;
            }

            return container;
        }
    }
}
