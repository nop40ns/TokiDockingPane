using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using TokiDockingPane.Models;

namespace TokiDockingPane
{
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

        static DockingPane()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(DockingPane),
                new FrameworkPropertyMetadata(typeof(DockingPane)));
        }

        public DockingPane()
        {
            this.DataContextChanged += OnDataContextChanged;
            this.PreviewMouseWheel += OnPreviewMouseWheel;

            this.AllowDrop = true;
            this.PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown;
            this.PreviewMouseMove += OnPreviewMouseMove;
            this.Drop += OnDockingPaneDrop;
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _tabContentContainer = GetTemplateChild("PART_TabContentContainer") as Grid;
            _headerScrollViewer = GetTemplateChild("PART_HeaderScrollViewer") as ScrollViewer;
            _verticalSplitGrid = GetTemplateChild("PART_VerticalSplitGrid") as Grid;
            _horizontalSplitGrid = GetTemplateChild("PART_HorizontalSplitGrid") as Grid;
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

                // ★【修正】専用の確定型パッケージとしてDoDragDropへ射出する
                var payload = new TokiDragDropPayload(sourceNode, draggedData);
                DragDrop.DoDragDrop(this, payload, DragDropEffects.Move);
            }
        }

        /// <summary>
        /// ★ 核心：専用型での一本釣りに切り替え、誤爆ガードを完全に突破するDrop処理
        /// </summary>
        private void OnDockingPaneDrop(object sender, DragEventArgs e)
        {
            if (!(this.DataContext is PaneContentNode targetNode) || targetNode.MainChild != null) return;

            // ★ 専用型『TokiDragDropPayload』で厳格にデータポインタを抽出（100%開通）
            if (!(e.Data.GetData(typeof(TokiDragDropPayload)) is TokiDragDropPayload payload)) return;

            PaneContentNode sourceNode = payload.SourceNode;
            object draggedData = payload.DraggedData;

            // 自分自身（ドロップされた部屋）のサイズとローカル座標を算出
            Point localPos = e.GetPosition(this);
            double width = this.ActualWidth;
            double height = this.ActualHeight;

            // 1. 【安全着地】：移動データをドロップ先にカチッと先に結合
            if (localPos.X > width * 0.75) targetNode.SplitVertical(draggedData);
            else if (localPos.Y > height * 0.75) targetNode.SplitHorizontal(draggedData);
            else targetNode.AddTab(draggedData);

            // 2. 【後攻引き抜き】：結合が全うされた後にドラッグ元から古いタブを安全消去
            int dragIndex = sourceNode.TabViewModels.IndexOf(draggedData);
            if (dragIndex >= 0)
            {
                sourceNode.RemoveTab(dragIndex);
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
            if (e.PropertyName == nameof(PaneContentNode.MainChild) || e.PropertyName == nameof(PaneContentNode.Orientation))
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
        private void QueueRefreshVisualState()
        {
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (this.Template == null || !(this.DataContext is PaneContentNode node)) return;

                if (node.MainChild != null)
                {
                    // コンテナ状態のときは、Gridの比率を星寸法（1* : 1*）に正しく復元して表示する
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
                    // ★【右上の無駄な空間を完全に消し去る核心のロジック】
                    // 自分が末端の実体ペイン（LeafState）へ縮んだ瞬間、親コンテナであるGridのColumn定義を直接スキャンし、
                    // 引き抜かれて空っぽになった側（自分自身の配置されている列インデックス）の幅を『0』に書き換えます。
                    // これにより、データツリー構造を物理破壊することなく、WPFの画面上から一瞬（0ms）で無駄な空間が完全消滅します。
                    var parentGrid = this.Parent as Grid;
                    if (parentGrid != null)
                    {
                        int myColumn = Grid.GetColumn(this);
                        if (myColumn < parentGrid.ColumnDefinitions.Count)
                        {
                            parentGrid.ColumnDefinitions[myColumn].Width = new GridLength(0);
                        }
                    }

                    VisualStateManager.GoToState(this, "LeafState", false);
                    SyncTabVisibility();
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
}
