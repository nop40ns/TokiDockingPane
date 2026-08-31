using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace TokiDockingPane.CustomControls;


public class DockPanelSplitter : Border
{
    private bool _isDragging;
    private Point _previousMousePositionInScreen; // 画面全体を基準にした前回位置

    public DockPanelSplitter()
    {
        this.MouseDown += DockPanelSplitter_MouseDown;
        this.MouseMove += DockPanelSplitter_MouseMove;
        this.MouseUp += DockPanelSplitter_MouseUp;
    }

    private void DockPanelSplitter_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            _isDragging = true;
            this.CaptureMouse();

            // ★重要：自分自身ではなく、アプリのウィンドウ（画面全体）を基準にした座標を取得する
            Window window = Window.GetWindow(this);
            if (window != null)
            {
                _previousMousePositionInScreen = e.GetPosition(window);
            }

            e.Handled = true;
        }
    }

    private void DockPanelSplitter_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDragging) return;

        Window window = Window.GetWindow(this);
        if (window == null) return;

        if (VisualTreeHelper.GetParent(this) is not DockPanel parent) return;

        int myIndex = parent.Children.IndexOf(this);
        if (myIndex <= 0) return;

        if (parent.Children[myIndex - 1] is FrameworkElement target)
        {
            // ★重要：画面全体基準の現在位置を取得
            Point currentMousePositionInScreen = e.GetPosition(window);

            // 固定された画面に対する正確な移動量を計算（これでブレがゼロになります）
            double horizontalChange = currentMousePositionInScreen.X - _previousMousePositionInScreen.X;
            double verticalChange = currentMousePositionInScreen.Y - _previousMousePositionInScreen.Y;

            Dock dock = DockPanel.GetDock(this);

            if (dock == Dock.Left)
            {
                target.Width = Math.Max(0, target.ActualWidth + horizontalChange);
            }
            else if (dock == Dock.Right)
            {
                target.Width = Math.Max(0, target.ActualWidth - horizontalChange);
            }
            else if (dock == Dock.Top)
            {
                target.Height = Math.Max(0, target.ActualHeight + verticalChange);
            }
            else if (dock == Dock.Bottom)
            {
                target.Height = Math.Max(0, target.ActualHeight - verticalChange);
            }

            // 次回計算用に、今回の画面基準位置を保存
            _previousMousePositionInScreen = currentMousePositionInScreen;
        }
        e.Handled = true;
    }

    private void DockPanelSplitter_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_isDragging && e.ChangedButton == MouseButton.Left)
        {
            _isDragging = false;
            this.ReleaseMouseCapture();
            e.Handled = true;
        }
    }
}
