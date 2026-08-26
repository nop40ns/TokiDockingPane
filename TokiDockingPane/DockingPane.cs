using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using TokiDockingPane.Interfaces;
using TokiDockingPane.Models;
using TokiDockingPane.ViewModels;

namespace TokiDockingPane;

// ★【一撃必殺のインフラ】WPFの型解決バグを100%封殺するための、型安全なドラッグ専用ポインタコンテナ
public class TokiDragDropPayload
{
    public IPaneNode SourceNode { get; }
    public TabViewModel DraggedData { get; }

    public TokiDragDropPayload(IPaneNode sourceNode, TabViewModel draggedData)
    {
        SourceNode = sourceNode;
        DraggedData = draggedData;
    }
}

[TemplatePart(Name = "PART_TabItemBorder", Type = typeof(Border))]

[ObservableObject]
public partial class DockingPane : ContentControl
{
    private Grid? _tabContentContainer;
    private ScrollViewer? _headerScrollViewer;
    private Grid? _verticalSplitGrid;
    private Grid? _horizontalSplitGrid;

    private Point _dragStartPoint;
    private bool _isDragging;

    private Grid? _dockingIndicator{ get; set; }
    private Grid? _dockingOuterIndicator { get; set; }

    private static bool _isGloballyDragging;

    // DockingPane.cs の上部に、仕切り線ガイド用の Border ポインタを2本追加
    private Border? _verticalSplitterIndicator;
    private Border? _horizontalSplitterIndicator;

    private Grid? _toolHeader;

    private Border? _tabItemBorder;


    static DockingPane()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(DockingPane),
            new FrameworkPropertyMetadata(typeof(DockingPane)));
    }

    public DockingPane()
    {
        this.DataContextChanged += OnDataContextChanged;
        this.PreviewMouseWheel += OnPreviewMouseWheel;

        this.PreviewMouseLeftButtonDown += OnToolHeaderLeftButtonDown;
        this.PreviewMouseMove += OnToolHeaderMouseMove;


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


        if (_tabItemBorder != null)
        {
            _tabItemBorder.PreviewMouseLeftButtonDown -= OnTabHeaderMouseLeftButtonDown;
            _tabItemBorder.PreviewMouseMove -= OnTabHeaderMouseMove;
        }
         

        _tabContentContainer = GetTemplateChild("PART_TabContentContainer") as Grid;
        _headerScrollViewer = GetTemplateChild("PART_HeaderScrollViewer") as ScrollViewer;
        _verticalSplitGrid = GetTemplateChild("PART_VerticalSplitGrid") as Grid;
        _horizontalSplitGrid = GetTemplateChild("PART_HorizontalSplitGrid") as Grid;

        _dockingIndicator = GetTemplateChild("PART_DockingIndicator") as Grid;

        _dockingIndicator.Visibility = Visibility.Collapsed;

        _verticalSplitterIndicator = GetTemplateChild("PART_VerticalSplitterIndicator") as Border;
        _horizontalSplitterIndicator = GetTemplateChild("PART_HorizontalSplitterIndicator") as Border;

         

        QueueRefreshVisualState();
    }


   

    // ツール移動用のマウス座標キャッシュ変数
    private Point _toolDragStartPoint;
    private bool _isToolDragging = false;

    public void OnTabHeaderMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // ×ボタンがクリックされた時はドラッグを開始しないようにガード
        if (e.OriginalSource is DependencyObject source &&
            VisualTreeHelper.GetParent(source) is Button) return;

        System.Diagnostics.Debug.WriteLine($"タブが掴まれました！ターゲットのデータ: {this.DataContext}");

        // ここからマウスキャプチャ（CaptureMouse）などの引き抜き・ドラッグロジックを始動させます
    }

    public void OnTabHeaderMouseMove(object sender, MouseEventArgs e)
    {
        // ドラッグ中のインジケータ移動や青いプレビューバーの表示処理
    }
    private void OnToolHeaderLeftButtonDown(object sender, MouseButtonEventArgs e)
    {

        var hitElement = e.OriginalSource as DependencyObject;
        if (hitElement == null) return;

        ScrollViewer? activeScrollViewer = null;
        if (_headerScrollViewer != null && _headerScrollViewer.IsMouseOver) activeScrollViewer = _headerScrollViewer;
        else activeScrollViewer = FindParent<ScrollViewer>(hitElement);

        if (activeScrollViewer == null || activeScrollViewer.Name != "PART_HeaderScrollViewer") return;

        if (sender is Grid header && header.Tag is IPaneNode toolNode)
        {
            _toolDragStartPoint = e.GetPosition(this);
            _isToolDragging = true;

            e.Handled = true;
        }
    }

    /// <summary>
    /// ★ 核心：右ツールウィンドウをソリューションエクスプローラーのように引き抜いて空中戦を開始する
    /// </summary>
    private void OnToolHeaderMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isToolDragging || e.LeftButton != MouseButtonState.Pressed) return;
        if (!(sender is Grid header) || !(header.Tag is IPaneNode toolNode)) return;

        if (!(this.DataContext is PaneContentNode sourceNode)) return;

        Point currentPos = e.GetPosition(this);
        if (Math.Abs(currentPos.X - _toolDragStartPoint.X) > SystemParameters.MinimumHorizontalDragDistance ||
            Math.Abs(currentPos.Y - _toolDragStartPoint.Y) > SystemParameters.MinimumVerticalDragDistance)
        {
            _isToolDragging = false;

            TabViewModel? draggedData = toolNode.SelectedTab;
            if (draggedData == null) return;

            // 4画面中央の仕切り線バー（Indicator）たちを一斉に Visible 臨戦態勢へ！
            IPaneNode? rootNode = toolNode;
            while (rootNode?.Parent != null) rootNode = rootNode.Parent;
            rootNode?.ClearAllIndicators();

            // 144Hzでの空中戦用ペイロードのパッキング（ツールノード自身をSourceとして手渡す）
            var payload = new TokiDragDropPayload(toolNode, draggedData);

            // =========================================================================
            // 🛑 WPF同期ロック：仕切り線や4画面の中にドロップされるまでここでホールドされます
            // =========================================================================
            DragDropEffects result = DragDrop.DoDragDrop(header, payload, DragDropEffects.Move);

            // =========================================================================
            // 🚀【開発者様ビルドの空中射出エンジン連動】：
            // 4画面の中にも仕切り線の真上にも落とされず、何もない空中で指が離された（result == None）その瞬間に、
            // ツールペインの実体を保持する子ウィンドウ（FloatWindow）を一撃で空中に顕現させる！
            // =========================================================================
            if (result == DragDropEffects.None)
            {
                Point mouseScreenPos = PointToScreen(Mouse.GetPosition(this));
                int dragIndex = sourceNode.TabViewModels.FindIndex(x => x == draggedData);

                if (dragIndex >= 0)
                {
                    // 1. 【先攻引き抜き＆空ノード格上げクリーンアップ】（ここはそのまま）
                    sourceNode.RemoveTab(dragIndex);
                    // ... (前述のsiblingNodeを使った格上げ処理) ...

                    // 2. 【ポインタ移送】
                    // 💡 コンストラクタの型エラーを100%回避する安全な生成とプロパティコピー
                    var floatNode = new PaneContentNode(draggedData);
                    floatNode.IsToolPane = sourceNode.IsToolPane; // 元の部屋のツール属性を引き継ぐ

                    // 3. 【空中射出】
                    var floatWindow = new TokiDockingPane.Views.FloatWindow(floatNode);
                    floatWindow.Left = mouseScreenPos.X - 100;
                    floatWindow.Top = mouseScreenPos.Y - 15;
                    floatWindow.Show();
                    floatWindow.Focus();

                    // 4. 【ツリー構造の強制更新通知】
                    IPaneNode? _rootNode = sourceNode;
                    while (rootNode?.Parent != null) rootNode = _rootNode.Parent;
                    rootNode?.RaisePropertyChanged(string.Empty);
                }
            }
            // 空中戦の終了に伴い、すべての仕切り線バーを一斉消灯
            rootNode?.ClearAllIndicators();
        }
    }


    private void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (!(this.DataContext is IPaneNode node) || node.MainChild != null) return;

        var hitElement = e.OriginalSource as DependencyObject;
        if (hitElement == null) return;

        ContentPresenter? clickedPresenter = FindParent<ContentPresenter>(hitElement);
        if (clickedPresenter != null && node.TabViewModels != null)
        {
            int clickedIndex = node.TabViewModels
                        .FindIndex(x =>x == clickedPresenter.Content);
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

            TabViewModel ? draggedData = sourceNode.SelectedTab ;
            if (draggedData == null) return;

            // ➔ 【新章：仕切り線バーの一斉点火】
            // 自分が所属している「親コンテナ（GridSplitterを抱えている部屋）」をツリーから遡及探索し、
            // その親コンテナが内包している仕切り線ガイドバーをダイレクトに『Visible（表示）』へ叩き起こします！
            DockingPane? parentPane = FindParent<DockingPane>(this);
            if (parentPane != null)
            {
                parentPane.ShowSplitterIndicators(true);
            }

            // ➔ 【空中戦開始】：自分の部屋のインジケータを一瞬Visibleの準備状態にする
            if (_dockingIndicator != null)
            {
                _dockingIndicator.Visibility = Visibility.Visible;
                Debug.WriteLine($"➔ [DEBUG] (部屋Hash: {this.GetHashCode()}) _dockingIndicator.Visibility = Visibility.Visibility.Visible");
            }

            var payload = new TokiDragDropPayload(sourceNode, draggedData);

            // =========================================================================
            // 🛑 WPF同期ホールド：ドロップ先がすべての処理を終えるまで、この行でスレッドがロックされます
            // =========================================================================
            DragDropEffects result = DragDrop.DoDragDrop(this, payload, DragDropEffects.Move);

            // 🌟【完全ホールド】：開発者様ビルドの最強フローティングウィンドウ射出エンジン
            if (result == DragDropEffects.None)
            {
                // マウスの現在位置（スクリーン絶対座標）を0msキャプチャ
                Point mouseScreenPos = PointToScreen(Mouse.GetPosition(this));

                int dragIndex = 
                    sourceNode.TabViewModels.FindIndex(x => x == draggedData);

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

            // =========================================================================
            // 🎉 【ドロップ完了時の後攻クリーンアップ】
            // =========================================================================
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (_dockingIndicator != null)
                {
                    _dockingIndicator.Visibility = Visibility.Collapsed;

                    Debug.WriteLine($"➔ [DEBUG] (部屋Hash: {this.GetHashCode()}) _dockingIndicator.Visibility = Visibility.Visibility.Collapsed");

                    Debug.WriteLine("➔ [DEBUG] ★最終執行：WPFの遅延更新を完全に上書きして非表示化！");


                     
                    IPaneNode? rootNode = sourceNode;
                    while (rootNode?.Parent != null) rootNode = rootNode.Parent;
                    rootNode?.ClearAllIndicators();

                }

                // ★【究極の修正】：役目を終えた仕切り線バーを、親コンテナから安全に完全消滅（Collapsed）させる
                DockingPane? parentPane = FindParent<DockingPane>(this);
                if (parentPane != null)
                {
                    parentPane.ShowSplitterIndicators(false);
                }

                 
            }), DispatcherPriority.Render);
        }
    }

    // DockingPane.cs の内部へ追記
    /// <summary>
    /// ★ 外部（子ノード）のドラッグモーションと完全同期して、自身の仕切り線バーを浮かび上がらせる窓口
    /// </summary>
    public void ShowSplitterIndicators(bool show)
    {
        var targetVisibility = show ? Visibility.Visible : Visibility.Collapsed;

        if (_verticalSplitterIndicator != null) _verticalSplitterIndicator.Visibility = targetVisibility;
        if (_horizontalSplitterIndicator != null) _horizontalSplitterIndicator.Visibility = targetVisibility;
    }


    /// <summary>
    /// ★ 究極のUX：ドラッグ中にマウスが「本物の仕切り線」の真上に乗った瞬間だけ、青いバーを完全追従表示させる
    /// </summary>
    /// <summary>
    /// ★ 究極の自由分割UX：親の分割コンテナGrid（PART_VerticalSplitGrid等）をレイキャストで一本釣りし、
    /// マウスが仕切り線（GridSplitter）の付近に侵入した瞬間、吸い付くように青いバーを完全点火させる
    /// </summary>
    /// <summary>
    /// ★ 究極解決：マウスが GridSplitter 自体に重なった瞬間、同じ部屋のインジケーターを Visible に点火する
    /// </summary>
    private void OnDockingPaneDragOver(object sender, DragEventArgs e)
    {



        if (!e.Data.GetDataPresent(typeof(TokiDragDropPayload)) || !(this.DataContext is IPaneNode node)) return;

        // 1. 🎯【開発者様の大正解規律】：マウス直下にある本物の GridSplitter の名前をレイキャストで一本釣り！
        Point localPos = e.GetPosition(this);
        string hitSplitterName = string.Empty;
        FrameworkElement? hitSplitterElement = null;

        VisualTreeHelper.HitTest(this,
            null,
            new HitTestResultCallback(result =>
            {
                var element = result.VisualHit as FrameworkElement;
                while (element != null)
                {
                    // XAML側でGridSplitterに命名した本物の名前をピンポイントキャッチ！
                    if (!string.IsNullOrEmpty(element.Name) &&
                        (element.Name == "PART_VerticalSplitBar" || element.Name == "PART_HorizontalSplitBar"))
                    {
                        hitSplitterName = element.Name;
                        hitSplitterElement = element; // 同じ Grid 内の Indicator を探すためのポインタをホールド
                        return HitTestResultBehavior.Stop;
                    }
                    element = VisualTreeHelper.GetParent(element) as FrameworkElement;
                }
                return HitTestResultBehavior.Continue;
            }),
            new PointHitTestParameters(localPos));




        // 2. 🔥【判定執行】：GridSplitter の上に乗った瞬間だけ、同じ部屋の Indicator を最速点火！
        if (hitSplitterElement != null)
        {


            IPaneNode? rootNode = node;
            while (rootNode?.Parent != null)
            {
                rootNode = rootNode.Parent; // ツリーの「根（ルート）」に到達するまで高速逆引き
            }

            if (rootNode != null)
            {
                Debug.WriteLine($"{DateTime.Now}:Indicatorを全消去");
                // ルートノードから、全画面の全コンテナへ向けて一斉消灯命令を乱れ撃ち！
                rootNode.ClearAllIndicators();
            }


            // ヒットした GridSplitter が所属している「同じ Grid（パネル）」を親として取得
            var parentGrid = System.Windows.Media.VisualTreeHelper.GetParent(hitSplitterElement) as Grid;
            if (parentGrid != null)
            {
                if (hitSplitterName == "PART_VerticalSplitBar")
                {
                    // 同じGrid内にある縦用インジケーターを名前で発掘し、Visible（表示）へ！
                    var indicator = parentGrid.FindName("PART_VerticalSplitterIndicator") as Border;
                    if (indicator != null) indicator.Visibility = Visibility.Visible;
                }
                else if (hitSplitterName == "PART_HorizontalSplitBar")
                {
                    // 同じGrid内にある横用インジケーターを名前で発掘し、Visible（表示）へ！
                    var indicator = parentGrid.FindName("PART_HorizontalSplitterIndicator") as Border;
                    if (indicator != null) indicator.Visibility = Visibility.Visible;
                }
            }
        }
        //else
        //{
        //    Debug.WriteLine($"{DateTime.Now}:Indicatorを全消去?");

        //    // =========================================================================
        //    // 🛡️【最上位ルートノード駆動・一斉消灯インフラの執行】
        //    // マウスが境界線を越えて別のコンテナへ脱出した瞬間（緑の矢印の移動時）の、残像焼き付きを完全封殺！
        //    // 自分自身のデータ（node）からParentポインタを遡って最上位（Root）を一本釣りします。
        //    // =========================================================================
        //    IPaneNode? rootNode = node;
        //    while (rootNode?.Parent != null)
        //    {
        //        rootNode = rootNode.Parent; // ツリーの「根（ルート）」に到達するまで高速逆引き
        //    }

        //    if (rootNode != null)
        //    {
        //        Debug.WriteLine($"{DateTime.Now}:Indicatorを全消去");
        //        // ルートノードから、全画面の全コンテナへ向けて一斉消灯命令を乱れ撃ち！
        //        rootNode.ClearAllIndicators();
        //    }
        //}

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
                Debug.WriteLine($"➔ [DEBUG] (部屋Hash: {this.GetHashCode()}) _dockingIndicator.Visibility = Visibility.Visibility.Visible");

            }
        }
    }

 
    private void OnDockingPaneDragLeave(object sender, DragEventArgs e)
    {
        if (_dockingIndicator != null) _dockingIndicator.Visibility = Visibility.Collapsed;

        Debug.WriteLine("出た");


        // 部屋から去る時は、仕切り線バーのフラグを一斉に強制消滅させる
        var trueVisualParent = System.Windows.Media.VisualTreeHelper.GetParent(this);
        DockingPane? parentPane = FindParent<DockingPane>(trueVisualParent);
        if (parentPane != null)
        {
            parentPane.ShowSplitterIndicators(false);
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
        if (!(this.DataContext is IPaneNode targetNode)) return;
        if (!(e.Data.GetData(typeof(TokiDragDropPayload)) is TokiDragDropPayload payload)) return;

        IPaneNode sourceNode = payload.SourceNode;
        TabViewModel draggedData = payload.DraggedData;

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

        DependencyObject topVisual = this;
        while (VisualTreeHelper.GetParent(topVisual) != null)
        {
            topVisual = VisualTreeHelper.GetParent(topVisual);
        }

        // マウス直下のVisualを探索するレイキャストフィルター
        VisualTreeHelper.HitTest(this,
            null, // フィルターは使用しない
            new HitTestResultCallback(result =>
            {
                var element = result.VisualHit as FrameworkElement;
                while (element != null)
                {
                    // ★拡張規律：中央の十字インジケータに加え、新設した「仕切り線連動型バー」の名前も一緒に遡及一本釣り！
                    if (!string.IsNullOrEmpty(element.Name) &&
                        (element.Name.StartsWith("PART_Indicator") 
                        || element.Name.StartsWith("PART_SplitterIndicator")
                        || element.Name .EndsWith ("SplitterIndicator")
                        || element.Name.StartsWith("PART_IndicatorOuter")

                        ))
                    {
                        targetIndicatorName = element.Name;
                        return HitTestResultBehavior.Stop; // 発見したら即停止
                    }
                    element = VisualTreeHelper.GetParent(element) as FrameworkElement;
                }
                return HitTestResultBehavior.Continue;
            }),
            new PointHitTestParameters(localPos));

        if (targetIndicatorName == "") return;

        if (sourceNode == targetNode.MainChild || sourceNode == targetNode.SubChild) return;
    

        // 3. 【先攻引き抜き】：トポロジー変更前に、ドラッグ元からタブを消去しツリーを緊縮
        if (sourceNode != null && sourceNode.TabViewModels != null)
        {
            int dragIndex = sourceNode.TabViewModels.FindIndex(x => x == draggedData);
            if (dragIndex >= 0)
            {
                sourceNode.RemoveTab(dragIndex);
            }
        }
        // =========================================================================
        // 👑【開発者様インライン思想：仕切り線（GridSplitter）上ドロップの完全調停】
        // 親（trueParent）の有無に依存せず、targetNode（自分自身）をその場でコンテナへと昇華させる！
        // =========================================================================
        if (targetIndicatorName == "PART_HorizontalSplitterIndicator" || targetIndicatorName == "PART_VerticalSplitterIndicator")
        {
            // 🛠️ 1. 【最速消灯】：まずは表示を最速でクリア
            if (_dockingIndicator != null) _dockingIndicator.Visibility = Visibility.Collapsed;
            if (_verticalSplitterIndicator != null) _verticalSplitterIndicator.Visibility = Visibility.Collapsed;
            if (_horizontalSplitterIndicator != null) _horizontalSplitterIndicator.Visibility = Visibility.Collapsed;


            // ★【開発者様の規律】：targetNode が生存しているなら、親の有無に関係なく100%突入！
            if (targetNode != null)
            {
                // =========================================================================
                // 👑【開発者様設計：スプリッター上ドロップのインイン・トポロジー完全体】
                // =========================================================================
                var oldTreeSubClone = new PaneContentNode
                {
                    MainChild = targetNode.SubChild?.MainChild, // 安全にSubChild側の資産を引き継ぎ
                    SubChild = targetNode.SubChild?.SubChild,
                    Orientation = targetNode.SubChild?.Orientation ?? EnumOrientation.Horizontal,
                    SplitRatio = targetNode.SubChild?.SplitRatio ?? 0.5,
                    TabViewModels = targetNode.SubChild?.TabViewModels ?? new List<TabViewModel>(),
                    SelectedTabIndex = targetNode.SubChild?.SelectedTabIndex ?? 0
                };

                // 🛠️ 3. 【新設ペイン（Main用）の生成】
                var newTopMainNode = new PaneContentNode(draggedData);

                if (newTopMainNode == targetNode.MainChild || newTopMainNode == targetNode.SubChild)
                    return;


                // 🛠️ 4. 核心：targetNode.SubChild（標的の部屋）をコンテナへ昇華させるための型解決
                var targetSub = targetNode.SubChild;
                if (targetSub != null)
                {
                    // 自身のこれまでのタブ資産をクリーンに初期化
                    targetSub.TabViewModels = new List<TabViewModel>();
                    targetSub.ViewModel = null;

                    // 分割方向を指定（ドロップされたインジケーターに合わせて部屋を割る）
                    if (targetIndicatorName == "PART_HorizontalSplitterIndicator")
                    {
                        targetSub.Orientation = EnumOrientation.Horizontal; // 上下割
                    }
                    else if (targetIndicatorName == "PART_VerticalSplitterIndicator")
                    {
                        targetSub.Orientation = EnumOrientation.Vertical; // 左右割
                    }

                    // 🌟【開発者様大正解の差し込み】：Main（上）に新しい赤枠、Sub（下）に元いたペインを引っ越し！
                    targetSub.MainChild = newTopMainNode;
                    targetSub.SubChild = oldTreeSubClone;

                    // 親子関係の逆引きポインタも、新設された targetSub の配下へ完璧に再結線！
                    newTopMainNode.Parent = targetSub;
                    oldTreeSubClone.Parent = targetSub;
                }

                // 🛠️ 5. 【後攻引き抜き】：大移動が完了したあと、ドラッグ元の古い部屋からタブを消去
                if (sourceNode != null && sourceNode.TabViewModels != null)
                {
                    int dragIndex = sourceNode.TabViewModels.IndexOf(draggedData);
                    if (dragIndex >= 0)
                    {
                        sourceNode.RemoveTab(dragIndex);
                    }
                }

                // 🛠️ 6. 最終点火：最上位から画面全体へ一斉にプロパティ変更通知を撃ち、WPFGridを瞬間再描画！
                targetNode.RaisePropertyChanged(string.Empty);

            }

            e.Handled = true;
            return;
        }


        // =========================================================================
        // 🛡️【個別部屋防衛線】：通常のペイン（部屋の中）に対するドッキング処理に移行する直前で、
        // 開発者様が敷いた「すでに分割済みのコンテナ内部への誤ドロップを弾くガード」を安全に発動させます！
        // =========================================================================
        if (targetNode.MainChild != null)
        {
            if (_dockingIndicator != null) _dockingIndicator.Visibility = Visibility.Collapsed;
            e.Handled = true;
            return;
        }

        // 3. 【先攻引き抜き】：（通常部屋ドッキング用）トポロジー変更前に、ドラッグ元からタブを消去しツリーを緊縮
        if (sourceNode != null && sourceNode.TabViewModels != null)
        {
            int dragIndex = sourceNode.TabViewModels.IndexOf(draggedData);
            if (dragIndex >= 0)
            {
                sourceNode.RemoveTab(dragIndex);
            }
        }

        // 4. 【後攻着地】：ヒットしたインジケータの名前に基づき、0msトランスフォームを執行
        if (targetIndicatorName.StartsWith("PART_Indicator"))
        {
            if (targetIndicatorName == "PART_IndicatorCenter")
            {
              //  targetNode.AddTab(draggedData); // 中央は単純結合

                if (targetNode.IsToolPane || (sourceNode != null && sourceNode.IsToolPane))
                {
                    if (_dockingIndicator != null) _dockingIndicator.Visibility = Visibility.Collapsed;
                    e.Handled = true;
                    return; // ➔ 何もさせずに安全に処理を終了（ポイッ）
                }

            }
            else
            {
                // 🛠 ターゲットの親コンテナを特定し、新しい分割コンテナ(中間ノード)を挿入
                bool isHorizontal = targetIndicatorName.Contains("Top") || targetIndicatorName.Contains("Bottom");
                bool isInsertAsMain = targetIndicatorName.Contains("Top") || targetIndicatorName.Contains("Left");

                var newLeafNode = new PaneContentNode(draggedData); // 新規タブ用ノード
                var targetParent = targetNode.Parent;

                if (targetParent == null)
                {
                    newLeafNode.IsToolPane = true; // 画面の一番外側の端っこなのでツール化！
                    newLeafNode.CanAutoHide = true; // AutoHiddenの資格を付与！
                }
                else
                {
                    // 通常の中央分割領域ならエディタ属性（False）を維持
                    newLeafNode.IsToolPane = false;
                    newLeafNode.CanAutoHide = false;
                }



                var newContainer = new PaneContentNode(); // 引数なしで生成
                newContainer.IsToolPane = targetNode.IsToolPane; // 後からフラグをコピー


                newContainer.Orientation = isHorizontal ? EnumOrientation.Horizontal : EnumOrientation.Vertical;
                newContainer.Parent = targetParent;

                if (targetParent != null)
                {
                    if (targetParent.MainChild == targetNode) targetParent.MainChild = newContainer;
                    else if (targetParent.SubChild == targetNode) targetParent.SubChild = newContainer;
                }

                // ターゲットと新規ノードを新しい分割コンテナの子に設定
                if (isInsertAsMain)
                {
                    newContainer.MainChild = newLeafNode; newLeafNode.Parent = newContainer;
                    newContainer.SubChild = targetNode; targetNode.Parent = newContainer;
                }
                else
                {
                    newContainer.MainChild = targetNode; targetNode.Parent = newContainer;
                    newContainer.SubChild = newLeafNode; newLeafNode.Parent = newContainer;
                }
            }



            // 真のRootまで遡り、ツリー全体の構造変更をWPFへ通知
            IPaneNode? rootNode = targetNode;
            while (rootNode?.Parent != null) rootNode = rootNode.Parent;
            rootNode?.RaisePropertyChanged(string.Empty);
        }
        if (targetIndicatorName.StartsWith("PART_IndicatorOuter"))
        {
            bool isHorizontal = targetIndicatorName.Contains("Top") || targetIndicatorName.Contains("Bottom");
            bool isInsertAsMain = targetIndicatorName.Contains("Top") || targetIndicatorName.Contains("Left");

            // 1. ドラッグされた中身を新しいツールペインとして生成 (IsToolPane = true)
            var newToolNode = new PaneContentNode(draggedData, isToolPane: true);
            newToolNode.CanAutoHide = true;

            // 2. 現在の「真の最上位ルートノード」を引っ張ってくる
            IPaneNode? currentRoot = targetNode;
            while (currentRoot?.Parent != null) currentRoot = currentRoot.Parent;

            if (currentRoot != null && currentRoot is PaneContentNode oldRoot)
            {
                // 3. 画面全体を丸ごと包み込む「新しい最外殻コンテナ」を new する
                var newOuterRoot = new PaneContentNode(isToolPane: false);
                newOuterRoot.Orientation = isHorizontal ? EnumOrientation.Horizontal : EnumOrientation.Vertical;

                // 4. 新しい最外殻コンテナの下に、「これまでの画面全体」と「新しいツールペイン」をぶら下げる
                if (isInsertAsMain)
                {
                    newOuterRoot.MainChild = newToolNode; newToolNode.Parent = newOuterRoot;
                    newOuterRoot.SubChild = oldRoot; oldRoot.Parent = newOuterRoot;
                }
                else
                {
                    newOuterRoot.MainChild = oldRoot; oldRoot.Parent = newOuterRoot;
                    newOuterRoot.SubChild = newToolNode; newToolNode.Parent = newOuterRoot;
                }

                // 5. 【超重要】ViewModelが持っている大元のRootNodeプロパティを、新しい最外殻コンテナへ繋ぎ替える！
                if (this.DataContext is DockingPaneViewModel mainVM)
                {
                    mainVM.RootDocumentNode = newOuterRoot; // ➔ ここで真の頂点アドレスが完全にすり替わります
                }
            }

            // ツリー変更をWPFに電撃通知して、画面全体のレイアウトを一撃で再変形させる
            targetNode.RaisePropertyChanged(string.Empty);

            if (_dockingIndicator != null) _dockingIndicator.Visibility = Visibility.Collapsed;
             return; // 最外殻処理が終わったのでメソッドを抜ける
        }

        // 5. 【後攻完全クリーンアップ】
        if (_dockingIndicator != null) _dockingIndicator.Visibility = Visibility.Collapsed;
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

        // ★【核心の修正：キャスト先を具象型からインターフェース『IPaneNode』へ完全修正！】
        // これにより、PaneContentNodeでもToolPaneContentNodeでも、
        // どのようなデータノードが結合されても100%確実にイベントハンドラが結線されます。
        if (e.OldValue is IPaneNode oldNode)
        {
            oldNode.PropertyChanged -= OnNodePropertyChanged;
        }

        if (e.NewValue is IPaneNode newNode)
        {
            // 🔥【究極の防衛線】：新しく結線する「直前」に、念には念を入れて強制的に一度引き算する！！
            // もしWPFのリサイクルによって二重にイベントを抱え込んでしまっていても、
            // この1行によって過去の古いハンドラポインタがメモリから完全に消滅（リセット）されます。
            newNode.PropertyChanged -= OnNodePropertyChanged;

            // 2. その上で、新しくクリーンな1本だけをカチッと直結！
            newNode.PropertyChanged += OnNodePropertyChanged;
        }

        // 描画リフレッシュ（QueueRefreshVisualState）を最速で叩き起こす！
        QueueRefreshVisualState();
    }


    public void ClearIndicators()
    {
        if (_dockingIndicator != null)
        {
            _dockingIndicator.Visibility = Visibility.Collapsed;
            Debug.WriteLine($"➔ [DEBUG] (部屋Hash: {this.GetHashCode()}) _dockingIndicator.Visibility = Collapsed.Visibility.Collapsed");

        }
        if (_verticalSplitterIndicator != null)
        {
            //Debug.WriteLine($"{DateTime.Now}:COMMAND_CLEAR_INDICATORS：_verticalSplitterIndicatorを非表示");

            _verticalSplitterIndicator.Visibility = Visibility.Collapsed;
        }
        if (_horizontalSplitterIndicator != null)
        {
            //        Debug.WriteLine($"{DateTime.Now}:COMMAND_CLEAR_INDICATORS：_horizontalSplitterIndicatorを非表示");

            _horizontalSplitterIndicator.Visibility = Visibility.Collapsed;
        }
    }

    private void OnNodePropertyChanged(object sender, PropertyChangedEventArgs e)
    {
       // Debug.WriteLine($"{DateTime.Now}:OnNodePropertyChanged");

        if (e.PropertyName == "COMMAND_CLEAR_INDICATORS")
        {
            ClearIndicators();
            return; // 描画更新（Queue〜）までは走らせずに、消灯だけを最速執行してリターン
        }

        if (e.PropertyName == nameof(PaneContentNode.MainChild) 
            || e.PropertyName == nameof(PaneContentNode.Orientation)
                   ||     e.PropertyName == nameof(PaneContentNode.TabViewModels) // 👈 ★これを1行追記！
)
        {
            // 作り直される衝撃波の前に、仕切り線ガイドを強制成仏
            if (_verticalSplitterIndicator != null)
            {
　                _verticalSplitterIndicator.Visibility = Visibility.Collapsed;
            }
            if (_horizontalSplitterIndicator != null) _horizontalSplitterIndicator.Visibility = Visibility.Collapsed;


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
      //  DebugDebug.WriteLine("=== [DEBUG] QueueRefreshVisualState 開始 ===");

        this.Dispatcher.BeginInvoke(new Action(() =>
        {
            if (this.Template == null || !(this.DataContext is IPaneNode node)) return;

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

                if (_dockingIndicator != null) _dockingIndicator.Visibility = Visibility.Collapsed;

                // ★新章の点火線：自分が分割されたコンテナ（Parent持ち）の時、
                // その仕切り線バーのVisibleを最速で開けてドラッグの受け入れ態勢（臨戦態勢）を整える！
                if (node.Parent != null)
                {
                    if (_verticalSplitterIndicator != null)
                    {
                        //Debug.WriteLine("QueueRefreshVisualState：_verticalSplitterIndicatorを表示");
                        _verticalSplitterIndicator.Visibility = Visibility.Visible;
                    }
                    if (_horizontalSplitterIndicator != null)
                    {
                        //Debug.WriteLine("QueueRefreshVisualState：_horizontalSplitterIndicatorを表示");
                        _horizontalSplitterIndicator.Visibility = Visibility.Visible;
                    }
                }
            }
        }), DispatcherPriority.Render);
    }


    private void SyncTabVisibility()
    {
        return;
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
                    var targetViewModel = node.TabViewModels[i].ViewModel;

                    // ✨ 現在の中身と異なる場合のみ代入する（無駄なnullクリアを除去）
                    if (presenter.Content != targetViewModel)
                    {
                        presenter.Content = targetViewModel;
                    }
                }

                if (i == selectedIndex)
                {

                    node.RaisePropertyChanged("ActiveViewModel");


                    _tabContentContainer.Children[i].Visibility = Visibility.Visible;
                      
                    _dockingIndicator.Visibility = Visibility.Collapsed;

                }
          
                else
                {
                    _tabContentContainer.Children[i].Visibility = Visibility.Collapsed;

                }

                //_tabContentContainer.Children[i].Visibility = (i == selectedIndex) ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                _tabContentContainer.Children[i].Visibility = Visibility.Collapsed;
            }
        }
    }
    #endregion
}
