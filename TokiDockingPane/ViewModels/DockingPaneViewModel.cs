using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Messaging;
using TokiDockingPane.Interfaces;
using TokiDockingPane.Messages;
using TokiDockingPane.Models;
using System.Diagnostics;

namespace TokiDockingPane.ViewModels;

/// <summary>
/// 👑 【最外殻統治コントロール】
/// 画面全体を横断するアウタードッキング（青線・赤線）を完全に支配するカスタムコントロール
/// </summary>
[TemplatePart(Name = "PART_IndicatorOuter", Type = typeof(Grid))]
[TemplatePart(Name = "PART_IndicatorOuterTop", Type = typeof(Grid))]
[TemplatePart(Name = "PART_IndicatorOuterBottom", Type = typeof(Border))]
[TemplatePart(Name = "PART_IndicatorOuterLeft", Type = typeof(Border))]
[TemplatePart(Name = "PART_IndicatorOuterRight", Type = typeof(Border))]

[ObservableObject]
public partial class DockingPaneViewModel : Control
{
 


#region HiddenPanes

    [ObservableProperty]
    private ObservableCollection<PaneContentNode> _RightHiddenPanes = new();

    private static void OnRightHiddenPanesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DockingPaneViewModel vm && e.NewValue is IPaneNode newNode)
        {
            newNode.ToolPainPosition = EnumToolPainPosition.Right;
            vm.SetToolPainPosition(newNode);
            vm.AttachContextToTree(newNode, vm._context);
        }
    }

    [ObservableProperty]
    private ObservableCollection<PaneContentNode> _LeftHiddenPanes = new();

    private static void OnLeftHiddenPanesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DockingPaneViewModel vm && e.NewValue is IPaneNode newNode)
        {
            newNode.ToolPainPosition = EnumToolPainPosition.Left;
            vm.SetToolPainPosition(newNode);
            vm.AttachContextToTree(newNode, vm._context);
        }
    }

    [ObservableProperty]
    private ObservableCollection<PaneContentNode> _TopHiddenPanes = new();

    private static void OnTopHiddenPanesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DockingPaneViewModel vm && e.NewValue is IPaneNode newNode)
        {
            newNode.ToolPainPosition = EnumToolPainPosition.Top;
            vm.SetToolPainPosition(newNode);
            vm.AttachContextToTree(newNode, vm._context);
        }
    }

    [ObservableProperty]
    private ObservableCollection<PaneContentNode> _BottomHiddenPanes = new();
    private static void OnBottomHiddenPanesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DockingPaneViewModel vm && e.NewValue is IPaneNode newNode)
        {
            newNode.ToolPainPosition = EnumToolPainPosition.Bottom;
            vm.SetToolPainPosition(newNode);
            vm.AttachContextToTree(newNode, vm._context);
        }
    }


    #endregion
     
    public static readonly DependencyProperty RootDocumentNodeProperty =
    DependencyProperty.Register(nameof(RootDocumentNode), typeof(IPaneNode), typeof(DockingPaneViewModel),
        new PropertyMetadata(null, OnToolRootChanged));


    public IPaneNode RootDocumentNode
    {
        get => (IPaneNode)GetValue(RootDocumentNodeProperty);
        set =>  SetValue(RootDocumentNodeProperty, value);  
    }

    #region RootDocument

    // --- 依存関係プロパティの定義（すべて同じコールバックを指定） ---

    public static readonly DependencyProperty LeftToolRootProperty =
        DependencyProperty.Register(nameof(LeftToolRoot), typeof(IPaneNode), typeof(DockingPaneViewModel),
            new PropertyMetadata(null, OnToolRootChanged));

    public static readonly DependencyProperty RightToolRootProperty =
        DependencyProperty.Register(nameof(RightToolRoot), typeof(IPaneNode), typeof(DockingPaneViewModel),
            new PropertyMetadata(null, OnToolRootChanged));

    public static readonly DependencyProperty TopToolRootProperty =
        DependencyProperty.Register(nameof(TopToolRoot), typeof(IPaneNode), typeof(DockingPaneViewModel),
            new PropertyMetadata(null, OnToolRootChanged));

    public static readonly DependencyProperty BottomToolRootProperty =
        DependencyProperty.Register(nameof(BottomToolRoot), typeof(IPaneNode), typeof(DockingPaneViewModel),
            new PropertyMetadata(null, OnToolRootChanged));


    // --- 各プロパティのラッパー ---
    public IPaneNode? LeftToolRoot { get => (IPaneNode?)GetValue(LeftToolRootProperty); set => SetValue(LeftToolRootProperty, value); }
    public IPaneNode? RightToolRoot { get => (IPaneNode?)GetValue(RightToolRootProperty); set => SetValue(RightToolRootProperty, value); }
    public IPaneNode? TopToolRoot { get => (IPaneNode?)GetValue(TopToolRootProperty); set => SetValue(TopToolRootProperty, value); }
    public IPaneNode? BottomToolRoot { get => (IPaneNode?)GetValue(BottomToolRootProperty); set => SetValue(BottomToolRootProperty, value); }


    // 🎯 完全に共通化されたコールバックメソッド
 
      
    void SetToolPainPosition(IPaneNode nd)
    {
        if (nd == null) return;

        if (nd.MainChild != null)
        {
            nd.MainChild.IsToolPane = nd.IsToolPane;
            nd.MainChild.ToolPainPosition = nd.ToolPainPosition;

            SetToolPainPosition(nd.MainChild);
        }

        if (nd.SubChild != null)
        {
            nd.SubChild.IsToolPane = nd.IsToolPane;
            nd.SubChild.ToolPainPosition = nd.ToolPainPosition;

            SetToolPainPosition(nd.SubChild);
        }
    }

    #endregion

    private static void OnToolRootChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DockingPaneViewModel vm && e.NewValue is IPaneNode newNode)
        {
            // 1. どのプロパティ（DependencyProperty）が変わったかに応じて位置を決定
            EnumToolPainPosition position = e.Property switch
            {
                _ when e.Property == LeftToolRootProperty => EnumToolPainPosition.Left,
                _ when e.Property == RightToolRootProperty => EnumToolPainPosition.Right,
                _ when e.Property == TopToolRootProperty => EnumToolPainPosition.Top,
                _ when e.Property == BottomToolRootProperty => EnumToolPainPosition.Bottom,
                _ when e.Property == LeftOverlayPaneNodeProperty => EnumToolPainPosition.Left,
                _ when e.Property == RightOverlayPaneNodeProperty => EnumToolPainPosition.Right,
                _ when e.Property == TopOverlayPaneNodeProperty => EnumToolPainPosition.Top,
                _ when e.Property == BottomOverlayPaneNodeProperty => EnumToolPainPosition.Bottom,
                _ => EnumToolPainPosition.None // デフォルトや想定外の場合
            };

            // 2. 決定した位置をノードに設定
            newNode.ToolPainPosition = position;

            // 3. 共通ロジックの実行
            vm.SetToolPainPosition(newNode);
            vm.AttachContextToTree(newNode, vm._context);


            Debug.WriteLine("Peopety");

            Debug.WriteLine(vm.GetHashCode());

            Debug.WriteLine(vm._context.GetHashCode());
        }
    }



    #region OverlayPaneNode

    public IPaneNode? LeftOverlayPaneNode { get => (IPaneNode?)GetValue(LeftOverlayPaneNodeProperty); set => SetValue(LeftOverlayPaneNodeProperty, value); }
    public IPaneNode? RightOverlayPaneNode { get => (IPaneNode?)GetValue(RightOverlayPaneNodeProperty); set => SetValue(RightOverlayPaneNodeProperty, value); }
    public IPaneNode? TopOverlayPaneNode { get => (IPaneNode?)GetValue(TopOverlayPaneNodeProperty); set => SetValue(TopOverlayPaneNodeProperty, value); }
    public IPaneNode? BottomOverlayPaneNode { get => (IPaneNode?)GetValue(BottomOverlayPaneNodeProperty); set => SetValue(BottomOverlayPaneNodeProperty, value); }

    public static readonly DependencyProperty LeftOverlayPaneNodeProperty =
        DependencyProperty.Register(nameof(LeftOverlayPaneNode), typeof(IPaneNode), typeof(DockingPaneViewModel),
            new PropertyMetadata(null, OnToolRootChanged));

    public static readonly DependencyProperty RightOverlayPaneNodeProperty =
        DependencyProperty.Register(nameof(RightOverlayPaneNode), typeof(IPaneNode), typeof(DockingPaneViewModel),
            new PropertyMetadata(null, OnToolRootChanged));

    public static readonly DependencyProperty TopOverlayPaneNodeProperty =
        DependencyProperty.Register(nameof(TopOverlayPaneNode), typeof(IPaneNode), typeof(DockingPaneViewModel),
        new PropertyMetadata(null, OnToolRootChanged));

    public static readonly DependencyProperty BottomOverlayPaneNodeProperty =
        DependencyProperty.Register(nameof(BottomOverlayPaneNode), typeof(IPaneNode), typeof(DockingPaneViewModel),
            new PropertyMetadata(null, OnToolRootChanged));

 
     
    #endregion

    //partial void OnBottomOverlayPaneNodeChanged(PaneContentNode? value)
    //{
    //    value.Parent = _bottomOverlayPaneNode;
    //}


    TreeContext? _context { get; set; } = new();

    // XAML側の最外殻パーツを一本釣りするためのプライベートポインタ
    private Grid? _indicatorOuter;
    private Border? _outerTop;
    private Border? _outerRight;
    private Border? _outerBottom;
    private Border? _outerLeft;


    static DockingPaneViewModel()
    {

        DefaultStyleKeyProperty.OverrideMetadata(typeof(DockingPaneViewModel),
            new FrameworkPropertyMetadata(typeof(DockingPaneViewModel)));
    }

     


    void AttachContextToTree(IPaneNode? node, TreeContext context)
    {
        // 1. nullチェック（ガード節）
        if (node == null) return;

        // 2. インターフェース（IPaneNode）から具象クラス（PaneContentNode）へ安全にキャスト
        if (node is PaneContentNode contentNode)
        {
            // 具象クラスが持つ internal な Context プロパティに、世界に1つのインスタンスをセット
            contentNode._context = context;
        }

        // 3. 【最重要】再帰駆動（バケツリレー）
        // 左（上）の子ノードとその子孫すべてに context を配る
        AttachContextToTree(node.MainChild, context);

        // 右（下）の子ノードとその子孫すべてに context を配る
        AttachContextToTree(node.SubChild, context);
    }

    public DockingPaneViewModel(IPaneNode rootDocument)
    {


        Debug.WriteLine( "引数付き");

        Debug.WriteLine(this.GetHashCode());

        Debug.WriteLine(_context.GetHashCode());

        RootDocumentNode = rootDocument;

     
    }

    public DockingPaneViewModel()
    {

        _context.Messenger.Register<AutoHiddenChangedMessage>(this, (recipient, message) =>
        {
            if (recipient is DockingPaneViewModel vm)
            {
                // 安全にUIスレッド（または同期処理）で実行
                vm.ChangeOverlay(message.TargetNode);
            }
        });

        // ★【最外殻D&Dインフラの起動】：
        // 自分自身（画面全体）に対してもWPFのD&D受け入れシグナルを直結します
        this.AllowDrop = true;
        this.DragEnter += OnOuterDragEnter;
        this.DragOver += OnOuterDragOver;
        this.DragLeave += OnOuterDragLeave;
        this.Drop += OnOuterDrop;
        this.PreviewMouseDown += OnPreviewMouseDown;

        Debug.WriteLine("引数なし");

        Debug.WriteLine(this.GetHashCode());

        Debug.WriteLine(_context.GetHashCode());
        //this.DataContext = this;
    }
    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();


        var _r = GetTemplateChild("PART_DockingOuterIndicator") as Grid;

        // XAMLからアウタードッキング用のパーツアドレスをガチッと補獲
        _indicatorOuter = GetTemplateChild("PART_IndicatorOuter") as Grid;

         

        _outerTop = GetTemplateChild("PART_IndicatorOuterTop") as Border;

        _outerBottom  = GetTemplateChild("PART_IndicatorOuterBottom") as Border;

        _outerLeft = GetTemplateChild("PART_IndicatorOuterLeft") as Border;
        _outerRight = GetTemplateChild("PART_IndicatorOuterRight") as Border;

        _outerRight.Drop += OnOuterDrop;

    }

    public void Refresh()
    {
        RefreshSub(TopToolRoot);
        RefreshSub(BottomToolRoot);
        RefreshSub(LeftToolRoot);
        RefreshSub(RightToolRoot);
    }

    void RefreshSub(IPaneNode nd )
    {
        if (nd == null) return;

        if(nd.MainChild != null )
        {
            RefreshSub(nd.MainChild);
        }

        if (nd.SubChild != null)
        {
            RefreshSub(nd.SubChild);
        }
    }



    public void ChangeOverlay(PaneContentNode node)
    {
        switch (node.ToolPainPosition)
        {
            case EnumToolPainPosition.Right:

                RightOverlayPaneNode = node;
               
                break;

            case EnumToolPainPosition.Left:
                LeftOverlayPaneNode = node;

                break;

            case EnumToolPainPosition.Top:
                TopOverlayPaneNode = node;


                break;

            case EnumToolPainPosition.Bottom:
                BottomOverlayPaneNode = node;

                break;

        }


    }

    //[RelayCommand]
    //private void ToggleOverlay(object parameter)
    //{
    //    if (parameter is PaneContentNode clickedNode)
    //    {
    //        OverlayPaneNode = clickedNode;

    //        if (OverlayPaneNode.IsAutoHidden  == true)
    //        {
    //            OverlayPaneNode.IsAutoHidden = false;
    //            System.Diagnostics.Debug.WriteLine("[AutoHidden] ポップアップを閉じました。");
    //        }
    //        else
    //        {
    //            OverlayPaneNode.IsAutoHidden = true;
    //            System.Diagnostics.Debug.WriteLine($"[AutoHidden] ポップアップを一時展開しました: {clickedNode.SelectedTab?.Title}");
    //        }
    //    }
    //}






    // 【裏方のツリー巡回ロジック】隠れている末端ノードをすべて抽出する
    private List<PaneContentNode> GetHiddenPanes(
        IPaneNode? root )
    {
        if (root == null) return null;

        var list = new List<PaneContentNode>();

        //var sss = HiddenPanes;

        list.AddRange(GetHiddenPanesSub(root));
        return list;
    }

    private List<PaneContentNode> GetHiddenPanesSub(IPaneNode? root)
    {
        var list = new List<PaneContentNode>();
        if (root is PaneContentNode node)
        {
            if (node.IsAutoHidden && node.IsToolPane)
            {
                list.Add(node);


                //    node.RemoveMe();


            }
            list.AddRange(GetHiddenPanesSub(node.MainChild));
            list.AddRange(GetHiddenPanesSub(node.SubChild));
        }
        return list;
    }


    public void AddHiddenPane(PaneContentNode node)
    {
        if(node == null || node.IsToolPane == false) return;

        switch( node.ToolPainPosition  )
        {
            case EnumToolPainPosition.Right:

                RightHiddenPanes.Add(node);
                this.PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(RightHiddenPanes)));

                break;

            case EnumToolPainPosition.Left:

                LeftHiddenPanes.Add(node);
                this.PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(LeftHiddenPanes)));

                break;

            case EnumToolPainPosition.Top:

                TopHiddenPanes.Add(node);
                this.PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(TopHiddenPanes)));
                

                break;

            case EnumToolPainPosition.Bottom:

                BottomHiddenPanes.Add(node);

                

                this.PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(BottomHiddenPanes)));
                break;

        }
    }

    public void RefreshHiddenPanes(PaneContentNode node=null)
    {




        //// 💡 自分の内部からであれば、PropertyChangedイベントを安全に100%発火させることができます！

     
        System.Diagnostics.Debug.WriteLine("[ViewModel] 隠しペインサイドバーの再計算通知を代理発射しました。");
    }

 








    private bool IsLeftOuter(PaneContentNode node)
    {
        // ツリーの構造を上へと辿り、RootDocumentNode の MainChild 側の血統にいるか確認
        IPaneNode? current = node;
        while (current?.Parent != null && current.Parent != RootDocumentNode) current = current.Parent;
        return RootDocumentNode?.MainChild == current;
    }

    // 【方向判定】大元のルートの SubChild 側にいれば「右側」と判定するシンプルな規律
    private bool IsRightOuter(PaneContentNode node)
    {
        // ツリーの構造を上へと辿り、RootDocumentNode の SubChild 側の血統にいるか確認
        IPaneNode? current = node;
        while (current?.Parent != null && current.Parent != RootDocumentNode) current = current.Parent;
        return RootDocumentNode?.SubChild == current;
    }

　

    private void OnPreviewMouseDown(object sender, MouseEventArgs  e)
    {
        //   if (this.RightToolPane == null || this.RightToolPane.IsPopupOpened == false) return;



        // 🌟【大核心】：クリックされた最末尾の具象要素（TextBlock等）を正確に一本釣り！
        if (e.OriginalSource is System.Windows.DependencyObject clickedElement)
        {
            bool isClickInsidePopup = false;

            // クリックされた要素から、ビジュアルツリーを上に向かって親玉（Parent）へ遡上
            System.Windows.DependencyObject? current = clickedElement;
            while (current != null)
            {
                // 遡る途中で、右ツールペインのポップアップ外枠である Grid（Name="PART_RightToolPopup"）に到達した場合
                if (current is System.Windows.Controls.Grid grid && grid.Name == "PART_RightToolPopup")
                {
                    isClickInsidePopup = true; // ポップアップの内側（中身）をクリックしたと確定！
                    break;
                }

                // 🌟【WPF絶対規則】：ビジュアルツリーの親要素を1階層上に安全に登る
                current = System.Windows.Media.VisualTreeHelper.GetParent(current);
            }

            // 🎯【外側確定クリック】：もしクリックされた座標が、ポップアップの領土の外（4画面やタブ）だった場合！
            //if (!isClickInsidePopup)
            //{
            //    // 0msでデータモデル側の IsPopupOpened を False へ直接叩き落とす！
            //    // これにより最外殻の通常トリガーが連動し、Popupが「シュッ」と安全格納されます！
            //    this.RightToolPane.IsPopupOpened = false;
            //}
        }
    }
   



    // =========================================================================
    // 🎯 最外殻アウターインジケーター（青線・赤線用）のリアルタイム表示制御
    // =========================================================================
    private void OnOuterDragEnter(object sender, DragEventArgs e)
    {
        _indicatorOuter.Visibility = Visibility.Visible;

        if (e.Data.GetDataPresent(typeof(TokiDragDropPayload)))
        {

     
        }
    }

    private void OnOuterDragOver(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(typeof(TokiDragDropPayload))) return;

        // マウスの現在座標が、上バーの真上か、右バーの真上かをOSレベルで一本釣り
        if (_outerTop != null && _outerTop.IsMouseOver)
        {
            // 上バーの上に乗っている時は、ネオンのように高輝度化させるなどの視覚演出フック
            _outerTop.Opacity = 1.0;
            if (_outerRight != null) _outerRight.Opacity = 0.4; // 反対側をうっすら暗くして集中させる
        }
        else if (_outerRight != null && _outerRight.IsMouseOver)
        {
            _outerRight.Opacity = 1.0;
            if (_outerTop != null) _outerTop.Opacity = 0.4;
        }
        else
        {
            // どっちのバーの上でもないときは、両方等しくうっすら表示
            if (_outerTop != null) _outerTop.Opacity = 0.7;
            if (_outerRight != null) _outerRight.Opacity = 0.7;
        }

        e.Effects = DragDropEffects.Move;
        e.Handled = true;
    }

    private void OnOuterDragLeave(object sender, DragEventArgs e)
    {

        _indicatorOuter.Visibility = Visibility.Collapsed;

    }

    /// <summary>
    /// 🚨【新章・アウター横断分割の執行】：
    /// 最外殻の「上（青線）」または「右（赤線）」のボタン上にドロップされた瞬間、
    /// ツリー全体のトポロジーを一撃でバキッと組み替える
    /// </summary>
    /// <summary>
    /// 🚨【新章・アウター横断分割の執行確定版】：
    /// インターフェース（IPaneNode）の窓口をダイレクトに貫通させ、トポロジーをミリ秒で大分割する
    /// </summary>
    private void OnOuterDrop(object sender, DragEventArgs e)
    {
        if (!(e.Data.GetData(typeof(TokiDragDropPayload)) is TokiDragDropPayload payload)) return;

        _indicatorOuter.Visibility = Visibility.Collapsed;

    //    var ctrl = _outerTop.InputHitTest();

        IPaneNode sourceNode = payload.SourceNode;
        TabViewModel  draggedData = payload.DraggedData;

        Point dropPos = e.GetPosition(this); // インジケータ全体の Grid を基準にした座標
        IInputElement hitElement = this.InputHitTest(dropPos);

        bool isTopHit = (dropPos.Y >= 0 && dropPos.Y <= 24);
        bool isBottomHit = (dropPos.Y >= (this.ActualHeight - 24) && dropPos.Y <= this.ActualHeight);
        bool isLeftHit = (dropPos.X >= 0 && dropPos.X <= 24);
        bool isRightHit = (dropPos.X >= (this.ActualWidth - 24) && dropPos.X <= this.ActualWidth);

        if (isTopHit)
        {
            // 👆 上端：上下分割（Vertical）、新しいツールを上（Main）に挿入
            ExecuteOuterDock(sourceNode, draggedData, EnumOrientation.Horizontal, insertAsMain: true);
        }
        else if (isBottomHit)
        {
            // 👇 下端：上下分割（Vertical）、新しいツールを下（Sub）に挿入
            ExecuteOuterDock(sourceNode, draggedData, EnumOrientation.Horizontal, insertAsMain: false);
        }
        else if (isLeftHit)
        {
            // 👈 左端：左右分割（Horizontal）、新しいツールを左（Main）に挿入
            ExecuteOuterDock(sourceNode, draggedData, EnumOrientation.Vertical, insertAsMain: true);
        }
        else if (isRightHit)
        {
            // 👉 右端：左右分割（Horizontal）、新しいツールを右（Sub）に挿入
            ExecuteOuterDock(sourceNode, draggedData, EnumOrientation.Vertical, insertAsMain: false);
        }

        e.Handled = true;
    }

    // ⭕ DockingPaneViewModel.cs 内に追加する共通トポロジー組み替えメソッド
    private void ExecuteOuterDock(IPaneNode sourceNode, TabViewModel draggedData, EnumOrientation orientation, bool insertAsMain)
    {
        int dragIndex = sourceNode.TabViewModels.IndexOf(draggedData);
        if (dragIndex < 0) return;

        var dropPain = sourceNode.TabViewModels[dragIndex];
        sourceNode.RemoveTab(dragIndex);

        // 💡 1. 引き抜いたタブを詰め込んだ、独立した新しい「ツールペイン（葉）」を生成
        var newToolLeaf = new PaneContentNode(dropPain, isToolPane: sourceNode.IsToolPane);
        newToolLeaf.CanAutoHide = true;
        newToolLeaf.IsToolPane = sourceNode.IsToolPane;

        // 💡 2. これまでの画面全体のルート（ツリー全体）を退避して確保
        var oldRoot = RootDocumentNode;

        // 💡 3. 画面全体を丸ごと外側から包み込む「新しい最外殻コンテナ」を生成
        var newOuterRoot = new PaneContentNode(isToolPane: false);
        newOuterRoot.Orientation = orientation; // 引数から縦割り・横割りを指定

        // 💡 4. ドロップされた方角（引数）に応じて、新旧のノードを「Main」と「Sub」に賢く振り分ける
        if (insertAsMain)
        {
            // 👈 左（Left）または 👆 上（Top）にドロップされた場合：新しいツールが左（上）に来る
            newOuterRoot.MainChild = newToolLeaf; newToolLeaf.Parent = newOuterRoot;
            newOuterRoot.SubChild = oldRoot; if (oldRoot != null) oldRoot.Parent = newOuterRoot;
        }
        else
        {
            // 👉 右（Right）または 👇 下（Bottom）にドロップされた場合：新しいツールが右（下）に来る
            newOuterRoot.MainChild = oldRoot; if (oldRoot != null) oldRoot.Parent = newOuterRoot;
            newOuterRoot.SubChild = newToolLeaf; newToolLeaf.Parent = newOuterRoot;
        }

        // 💡 5. 頂点（RootDocumentNode）のアドレスを、新しく組み立てた巨大なコンテナへと完全繋ぎ替え！
        RootDocumentNode = newOuterRoot;

        // 💡 6. ツリー全体に構造変更をWPFへ電撃通知して一撃で再レンダリングさせる
        this.RootDocumentNode?.RaisePropertyChanged(string.Empty);
        if (oldRoot != null) oldRoot.RaisePropertyChanged(string.Empty);
    }

}

 