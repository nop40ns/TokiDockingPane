using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using TokiDockingPane.Interfaces;
using TokiDockingPane.Models;

namespace TokiDockingPane;

// ★【一撃必殺のインフラ】WPFの型解決バグを100%封殺するための、型安全なドラッグ専用ポインタコンテナ
public class TokiDragDropPayload
{
    public PaneContentNode SourceNode { get; }
    public object DraggedData { get; }

    public TokiDragDropPayload(PaneContentNode sourceNode, object draggedData)
    {
        SourceNode = sourceNode;
        DraggedData = draggedData;
    }
}

public class DockingPane : ContentControl
{
    private Grid? _tabContentContainer;
    private ScrollViewer? _headerScrollViewer;
    private Grid? _verticalSplitGrid;
    private Grid? _horizontalSplitGrid;

    private Point _dragStartPoint;
    private bool _isDragging;

    private Grid? _dockingIndicator;

    private static bool _isGloballyDragging;

    static DockingPane()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(DockingPane),
            new FrameworkPropertyMetadata(typeof(DockingPane)));
    }

    public DockingPane()
    {
        this.DataContextChanged += OnDataContextChanged;
        this.PreviewMouseWheel += OnPreviewMouseWheel;
        　
        this.PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown;
        this.PreviewMouseMove += OnPreviewMouseMove;　

        this.AllowDrop = true;
        this.DragEnter += OnDockingPaneDragEnter;
        this.DragOver += OnDockingPaneDragOver;
        this.DragLeave += OnDockingPaneDragLeave;
        this.Drop += OnDockingPaneDrop;
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _tabContentContainer = GetTemplateChild("PART_TabContentContainer") as Grid;
        _headerScrollViewer = GetTemplateChild("PART_HeaderScrollViewer") as ScrollViewer;
        _verticalSplitGrid = GetTemplateChild("PART_VerticalSplitGrid") as Grid;
        _horizontalSplitGrid = GetTemplateChild("PART_HorizontalSplitGrid") as Grid;

        _dockingIndicator = GetTemplateChild("PART_DockingIndicator") as Grid;

        _dockingIndicator.Visibility = Visibility.Collapsed;

        QueueRefreshVisualState();
    }

    private void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (!(this.DataContext is PaneContentNode node) || node.MainChild != null) return;

        var hitElement = e.OriginalSource as DependencyObject;
        if (hitElement == null) return;

        ContentPresenter? clickedPresenter = FindParent<ContentPresenter>(hitElement);
        if (clickedPresenter != null && node.TabViewModels != null)
        {
            int clickedIndex = node.TabViewModels.IndexOf(clickedPresenter.Content);
            if (clickedIndex >= 0)
            {
                node.SelectedTabIndex = clickedIndex;
                _dragStartPoint = e.GetPosition(this);
                _isDragging = true;
            }
        }
    }

    private void OnPreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDragging || e.LeftButton != MouseButtonState.Pressed || !(this.DataContext is PaneContentNode sourceNode)) return;

        Point currentPos = e.GetPosition(this);
        if (Math.Abs(currentPos.X - _dragStartPoint.X) > SystemParameters.MinimumHorizontalDragDistance ||
            Math.Abs(currentPos.Y - _dragStartPoint.Y) > SystemParameters.MinimumVerticalDragDistance)
        {
            _isDragging = false;

            object? draggedData = sourceNode.ActiveViewModel;
            if (draggedData == null) return;

            var payload = new TokiDragDropPayload(sourceNode, draggedData);

            // ➔ 【空中戦開始】：自分の部屋のインジケータを一瞬Visibleの準備状態にする
            if (_dockingIndicator != null)
            {
                _dockingIndicator.Visibility = Visibility.Visible;
                Debug.WriteLine("ここで表示");
            }
            // =========================================================================
            // 🛑 WPF同期ホールド：ドロップ先がすべての処理を終えるまで、この行でスレッドがロックされます
            // =========================================================================
            DragDropEffects result = DragDrop.DoDragDrop(this, payload, DragDropEffects.Move);

            if (result == DragDropEffects.None)
            {
                // マウスの現在位置（スクリーン絶対座標）を0msキャプチャ
                Point mouseScreenPos = PointToScreen(Mouse.GetPosition(this));

                int dragIndex = sourceNode.TabViewModels.IndexOf(draggedData);
                if (dragIndex >= 0)
                {
                    // 1. 【先攻引き抜き】完璧に直った親結線付きのRemoveTabで元ペインを0ms消滅・緊縮させる
                    sourceNode.RemoveTab(dragIndex);

                    // 2. 【ポインタ移送】引き抜いたデータのみを内包する新しい孤立ルートノードを生成
                    var floatNode = new PaneContentNode(draggedData);

                    // 3. 【空中射出】C#純粋駆動のFloatWindowに新ノードを流し込んで顕現させる
                    var floatWindow = new TokiDockingPane.Views.FloatWindow(floatNode);

                    // マウスカーソルの位置がウィンドウの左上ヘッダー付近に来るように座標をスライド配置
                    floatWindow.Left = mouseScreenPos.X - 100;
                    floatWindow.Top = mouseScreenPos.Y - 15;

                    floatWindow.Show();
                    floatWindow.Focus();
                }
            }

            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (_dockingIndicator != null)
                {
                    _dockingIndicator.Visibility = Visibility.Collapsed;
                    Debug.WriteLine("➔ [DEBUG] ★最終執行：WPFの遅延更新を完全に上書きして非表示化！");
                }
            }), DispatcherPriority.Render);

        }
    }

    private void OnDockingPaneDragOver(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(typeof(TokiDragDropPayload)) || !(this.DataContext is PaneContentNode node) || node.MainChild != null) return;
        e.Effects = DragDropEffects.Move;
        e.Handled = true;
    }

    private void OnDockingPaneDragEnter(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(typeof(TokiDragDropPayload)))
        {
            if (this.DataContext is PaneContentNode node && node.MainChild == null && _dockingIndicator != null)
            {
                Debug.WriteLine("中に入った");

                _dockingIndicator.Visibility = Visibility.Visible;
                Debug.WriteLine("_dockingIndicator Visible");

            }
        }
    }

    private void OnDockingPaneDragLeave(object sender, DragEventArgs e)
    {
        if (_dockingIndicator != null)
        {
            Debug.WriteLine("出た");

            _dockingIndicator.Visibility = Visibility.Collapsed;
        }
    }

    /// <summary>
    /// ★ 核心：専用型での一本釣りに切り替え、誤爆ガードを完全に突破するDrop処理
    /// </summary>
    /// <summary>
    /// ★ 最終調停：引き抜きを完全に先攻執行し、分割時のトポロジーねじれと幽霊ペインの居座りを完全封殺
    /// </summary>
    private void OnDockingPaneDrop(object sender, DragEventArgs e)
    {
        if (!(this.DataContext is PaneContentNode targetNode) || targetNode.MainChild != null) return;
        if (!(e.Data.GetData(typeof(TokiDragDropPayload)) is TokiDragDropPayload payload)) return;

        PaneContentNode sourceNode = payload.SourceNode;
        object draggedData = payload.DraggedData;

        // 1. 【自己破壊ガード】最後の1枚しか無い状態で同じ部屋に落とされた場合は完全スルー
        if (sourceNode == targetNode && sourceNode.TabViewModels.Count <= 1)
        {
            if (_dockingIndicator != null) _dockingIndicator.Visibility = Visibility.Collapsed;
            e.Handled = true;
            return;
        }

        // 2. 🎯【2026年基準：インジケータのピンポイント一本釣り（ヒットテスト）】
        Point localPos = e.GetPosition(this);
        string targetIndicatorName = string.Empty;

        // マウス直下のVisualを探索するレイキャストフィルター
        VisualTreeHelper.HitTest(this,
            null, // フィルターは使用しない
            new HitTestResultCallback(result =>
            {
                var element = result.VisualHit as FrameworkElement;
                while (element != null)
                {
                    // XAML側の名前（PART_IndicatorXXX）を遡及探索
                    if (!string.IsNullOrEmpty(element.Name) && element.Name.StartsWith("PART_Indicator"))
                    {
                        targetIndicatorName = element.Name;
                        return HitTestResultBehavior.Stop; // 発見したら即停止
                    }
                    element = VisualTreeHelper.GetParent(element) as FrameworkElement;
                }
                return HitTestResultBehavior.Continue;
            }),
            new PointHitTestParameters(localPos));

        // 3. 【先攻引き抜き】：トポロジー変更前に、ドラッグ元からタブを消去しツリーを緊縮
        if (sourceNode != null && sourceNode.TabViewModels != null)
        {
            int dragIndex = sourceNode.TabViewModels.IndexOf(draggedData);
            if (dragIndex >= 0)
            {
                sourceNode.RemoveTab(dragIndex);
            }
        }

        // 4. 【後攻着地】：ヒットしたインジケータの名前に基づき、0msトランスフォームを執行
        // ※もしインジケータの外（ペインの端など）に落とされた場合は、大まかな座標でフォールバック
        if (targetIndicatorName == "PART_IndicatorTop")
        {
            // 上分割：新データを「上(Main)」にし、自分を「下(Sub)」へ押し下げる
            targetNode.SplitHorizontal(draggedData);
            // 🌟上下を逆転させるため、MainとSubを瞬間スライド
            var temp = targetNode.MainChild;
            targetNode.MainChild = targetNode.SubChild;
            targetNode.SubChild = temp;
        }
        else if (targetIndicatorName == "PART_IndicatorBottom")
        {
            // 下分割：自分を「上(Main)」に残し、新データを「下(Sub)」へ配置
            targetNode.SplitHorizontal(draggedData); // 反転なしが正解
        }
        else if (targetIndicatorName == "PART_IndicatorLeft")
        {
            // 左分割：新データを「左(Main)」にし、自分を「右(Sub)」へ押し出す
            targetNode.SplitVertical(draggedData);
            // 🌟左右を逆転させるため、MainとSubを瞬間スライド
            var temp = targetNode.MainChild;
            targetNode.MainChild = targetNode.SubChild;
            targetNode.SubChild = temp;
        }
        else if (targetIndicatorName == "PART_IndicatorRight")
        {
            // 右分割：自分を「左(Main)」に残し、新データを「右(Sub)」へ配置
            targetNode.SplitVertical(draggedData); // 反転なしが正解
        }
        else if (targetIndicatorName == "PART_IndicatorCenter")
        {
            targetNode.AddTab(draggedData); // タブマージ
        }
        else
        {
            // 【フォールバック判定】：インジケータ外のペイン端に直接落とされた場合
            double width = this.ActualWidth;
            double height = this.ActualHeight;
            if (localPos.X > width * 0.8) targetNode.SplitVertical(draggedData);
            else if (localPos.Y > height * 0.8) targetNode.SplitHorizontal(draggedData);
            else targetNode.AddTab(draggedData);
        }

        // 5. 最終執行：インジケータを確実に圧殺
        if (_dockingIndicator != null)
        {
            _dockingIndicator.Visibility = Visibility.Collapsed;
        }

        e.Handled = true;
    }








    #region マウスホイール・再帰描画（そのまま完全に維持）
    private void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        var hitElement = e.OriginalSource as DependencyObject;
        if (hitElement == null) return;

        ScrollViewer? activeScrollViewer = null;
        if (_headerScrollViewer != null && _headerScrollViewer.IsMouseOver) activeScrollViewer = _headerScrollViewer;
        else activeScrollViewer = FindParent<ScrollViewer>(hitElement);

        if (activeScrollViewer == null || activeScrollViewer.Name != "PART_HeaderScrollViewer") return;

        if (e.Delta > 0) activeScrollViewer.ScrollToHorizontalOffset(activeScrollViewer.HorizontalOffset - 64);
        else if (e.Delta < 0) activeScrollViewer.ScrollToHorizontalOffset(activeScrollViewer.HorizontalOffset + 64);
        e.Handled = true;
    }

    private static T? FindParent<T>(DependencyObject? child) where T : DependencyObject
    {
        if (child == null) return null;
        DependencyObject parent = System.Windows.Media.VisualTreeHelper.GetParent(child);
        while (parent != null && !(parent is T)) parent = System.Windows.Media.VisualTreeHelper.GetParent(parent);
        return parent as T;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue == e.NewValue) return;
        if (e.OldValue is PaneContentNode oldNode) oldNode.PropertyChanged -= OnNodePropertyChanged;
        if (e.NewValue is PaneContentNode newNode) newNode.PropertyChanged += OnNodePropertyChanged;
        QueueRefreshVisualState();
    }

    private void OnNodePropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        Debug.WriteLine("OnNodePropertyChanged");


        if (e.PropertyName == nameof(PaneContentNode.MainChild) 
            || e.PropertyName == nameof(PaneContentNode.Orientation)
                   ||     e.PropertyName == nameof(PaneContentNode.TabViewModels) // 👈 ★これを1行追記！
)
        {
            QueueRefreshVisualState();
        }
        else if (e.PropertyName == nameof(PaneContentNode.SelectedTabIndex) || e.PropertyName == nameof(PaneContentNode.TabViewModels))
        {
            this.Dispatcher.BeginInvoke(new Action(() => SyncTabVisibility()), DispatcherPriority.Render);
        }
    }

    /// <summary>
    /// ★ 最終解決：空になった部屋のGrid寸法を一撃で 0 にして無駄な空間を消滅させる
    /// </summary>
    /// <summary>
    /// ★ 最終解決：空になった部屋のGrid寸法を一撃で 0 にして無駄な空間を消滅させる
    /// </summary>
    /// <summary>
    /// ★ デバッグ用：親Gridの捕捉とインデックス取得のプロセスを完全に可視化する
    /// </summary>
    /// <summary>
    /// ★ 最終確定：WPF視覚ツリーをダイレクトに遡り、空ペインのGrid寸法を 0 に完全消滅させる
    /// </summary>
    /// <summary>
    /// ★ 最終解決：VMSの巻き込みバグをねじ伏せ、初期表示時およびドロップ完了時のインジケータを100%消滅させる
    /// </summary>
    private void QueueRefreshVisualState()
    {
        Debug.WriteLine("=== [DEBUG] QueueRefreshVisualState 開始 ===");

        this.Dispatcher.BeginInvoke(new Action(() =>
        {
            if (this.Template == null || !(this.DataContext is PaneContentNode node)) return;

            if (node.MainChild != null)
            {
                if (_verticalSplitGrid != null && _verticalSplitGrid.ColumnDefinitions.Count >= 3)
                {
                    _verticalSplitGrid.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
                    _verticalSplitGrid.ColumnDefinitions[2].Width = new GridLength(1, GridUnitType.Star);
                }
                if (_horizontalSplitGrid != null && _horizontalSplitGrid.RowDefinitions.Count >= 3)
                {
                    _horizontalSplitGrid.RowDefinitions[0].Height = new GridLength(1, GridUnitType.Star);
                    _horizontalSplitGrid.RowDefinitions[2].Height = new GridLength(1, GridUnitType.Star);
                }

                VisualStateManager.GoToState(this, (node.Orientation == EnumOrientation.Vertical) ? "VerticalState" : "HorizontalState", false);
            }
            else
            {
                DependencyObject visualParent1 = System.Windows.Media.VisualTreeHelper.GetParent(this);
                if (visualParent1 != null)
                {
                    DependencyObject visualParent2 = System.Windows.Media.VisualTreeHelper.GetParent(visualParent1);
                    var parentGrid = visualParent2 as Grid;

                    if (parentGrid != null)
                    {
                        var myPresenter = visualParent1 as FrameworkElement;
                        if (myPresenter != null)
                        {
                            int myColumn = Grid.GetColumn(myPresenter);
                            int myRow = Grid.GetRow(myPresenter);

                            if (node.TabViewModels == null || node.TabViewModels.Count == 0)
                            {
                                if (myColumn < parentGrid.ColumnDefinitions.Count)
                                {
                                    parentGrid.ColumnDefinitions[myColumn].Width = new GridLength(0);
                                }
                                if (myRow < parentGrid.RowDefinitions.Count)
                                {
                                    parentGrid.RowDefinitions[myRow].Height = new GridLength(0);
                                }

                                parentGrid.InvalidateMeasure();
                                parentGrid.UpdateLayout();
                            }
                        }
                    }
                }

                // ★【修正：順番の完全反転】
                // まず先に、WPFに VisualState の状態遷移を完全に終わらせます。
                VisualStateManager.GoToState(this, "LeafState", false);
                SyncTabVisibility();

                // ★【核心の一撃】
                // VMSによる親要素の Visible 化が完全に全うされた『最後の最後』に、
                // C#のポインタからダイレクトにインジケータを『Collapsed（非表示）』に叩き落とします！
                // これにより、起動時のお節介な巻き込み表示が144Hzの1フレーム以下で完全に破壊・沈黙します。
                if (_dockingIndicator != null)
                {
                    _dockingIndicator.Visibility = Visibility.Collapsed;
                }
            }
        }), DispatcherPriority.Render);
    }


    private void SyncTabVisibility()
    {
        if (_tabContentContainer == null || !(this.DataContext is PaneContentNode node) || node.TabViewModels == null) return;

        int selectedIndex = node.SelectedTabIndex;
        int tabCount = node.TabViewModels.Count;

        while (_tabContentContainer.Children.Count < tabCount)
        {
            var presenter = new ContentPresenter();
            _tabContentContainer.Children.Add(presenter);
        }

        for (int i = 0; i < _tabContentContainer.Children.Count; i++)
        {
            if (i < tabCount)
            {
                var presenter = _tabContentContainer.Children[i] as ContentPresenter;
                if (presenter != null)
                {
                    presenter.Content = null;
                    presenter.Content = node.TabViewModels[i];
                }
                _tabContentContainer.Children[i].Visibility = (i == selectedIndex) ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                _tabContentContainer.Children[i].Visibility = Visibility.Collapsed;
            }
        }
    }
    #endregion
}
