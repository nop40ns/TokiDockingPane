using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using DependencyPropertyGenerator;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TokiDockingPane.Interfaces;
using TokiDockingPane.Messages;
using TokiDockingPane.Models;

namespace TokiDockingPane.ViewModels;


[DependencyProperty<IPaneNode>("OverlayPaneNode", OnChanged = nameof(OnSidePropertyChanged))]
[DependencyProperty<IPaneNode>("BasePane", OnChanged = nameof(OnSidePropertyChanged))]
[DependencyProperty<ObservableCollection<IPaneNode>>("HiddenPanes" , OnChanged = nameof(OnChangeHiddenPanes))]
[DependencyProperty<bool>("IsContentVisible", IsReadOnly = true, DefaultValue = true)]
[DependencyProperty<bool>("IsSplitterVisible", IsReadOnly = true, DefaultValue = true)]
[DependencyProperty<bool>("IsHedderVisible",DefaultValue = false)]
[DependencyProperty<bool>("IsHiddnVisible", DefaultValue = false , OnChanged = nameof(OnIsHiddnVisibleChanged))]

public partial class DockingSideContext : DependencyObject
{
    // 💡 親コントロール（DockingPaneViewModel）への参照
    private readonly DockingPaneViewModel _parent;

    // 💡 自分が「左・右・上・下」のどれなのか
    public EnumToolPainPosition Position { get; set;}

    public TreeContext Context;

    // コンストラクタで親と位置を受け取る
    public DockingSideContext(DockingPaneViewModel parent, TreeContext context, EnumToolPainPosition position)
    {
        _parent = parent;
        Position = position;
        Context = context;
        HiddenPanes = new ObservableCollection<IPaneNode>();

   

    }

    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        //Debug.WriteLine($"{e.Property.Name}:{Position}" );


    
        if (e.Property.Name == nameof(IsHedderVisible))
        {

        }

        if ( e.NewValue is IPaneNode newNode)             
        {
            newNode.ToolPainPosition = Position;
            newNode.Context = Context;



            if (e.Property.Name == nameof(BasePane))
            {
             }

            if (e.Property.Name == nameof(OverlayPaneNode))
            {
                Debug.WriteLine($"OverlayPaneNode.IsAutoHidden:{OverlayPaneNode.IsAutoHidden}");

             //   UpdateIsContentVisible();

            }


        }
     

    }

    private void OnIsHiddnVisibleChanged(bool newValue)
    {
        Debug.WriteLine($"OverlayPaneNode:{OverlayPaneNode}");
        Debug.WriteLine($"IsHiddnVisible:{IsHiddnVisible}");
    }

    // 💡 ここで変更イベントを直接処理！
    private static void OnSidePropertyChanged(
        DockingSideContext side,
        DependencyPropertyChangedEventArgs e)
    {
        // 1. 表示状態の自動計算
        side.UpdateIsContentVisible();

        // 2. 元の OnToolRootChanged のロジックをここで実行
        if (e.NewValue is IPaneNode newNode)
        {
            newNode.ToolPainPosition = side.Position;

            // 親（DockingPaneViewModel）のメソッドを呼び出す
            side._parent.SetToolPainPosition(newNode);

             side._parent.AttachContextToTree(newNode, side.Context);
             
            

            Debug.WriteLine($"{newNode.ID}:{newNode.GetHashCode()}");
        }

        // 3. 親コントロールのレイアウトを更新
        side._parent.InvalidateArrange();
        side._parent.InvalidateVisual();

        Debug.WriteLine($"Property changed on side inner: {side.Position}");
    }

    private static void OnChangeHiddenPanes(DockingSideContext side, DependencyPropertyChangedEventArgs e)
    {
        // 古いコレクションのイベント購読を解除（メモリリーク対策）
        if (e.OldValue is ObservableCollection<IPaneNode> oldCollection)
        {
            oldCollection.CollectionChanged -= side.OnHiddenPanesCollectionChanged;
        }

        // 新しいコレクションのイベントを購読
        if (e.NewValue is ObservableCollection<IPaneNode> newCollection)
        {
            newCollection.CollectionChanged += side.OnHiddenPanesCollectionChanged;

            // 初回読み込み時にも判定を走らせる
            side.UpdateIsContentVisible();
        }
    }

    private void OnHiddenPanesCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        // 以前コメントアウトにあった「隠されているペインが0個になったら…」のロジックや、
        // 表示状態（Visibility）の更新ロジックをここに記述します！

        System.Diagnostics.Debug.WriteLine($"要素が変更されました。現在のカウント: {HiddenPanes.Count}");

        // 例: 要素数の変化に応じて表示状態を再計算する
        this.UpdateIsContentVisible();

        // 親（DockingPaneViewModel）へ通知やリレイアウトが必要なら、親のメソッドを叩く
        this._parent?.InvalidateArrange();
    }

    public void ChangeFlag(  IPaneNode node )
    {
        if (BasePane is null)
        {
            IsContentVisible = false;
            IsSplitterVisible = false;
            IsHedderVisible = false;
            IsHiddnVisible = false;
            return;
        }
        var s = BasePane;
        if (HiddenPanes.Count() > 0)
        {
            if( BasePane.MainChild == null && BasePane.SubChild == null)
            {
                IsContentVisible = false;
                IsSplitterVisible = false;
                IsHedderVisible = true;
            }
            else
            {
                IsSplitterVisible = true;

                IsContentVisible = true;
                IsHedderVisible = true;
            }
        }

       // IsHiddnVisible = node.IsAutoHidden;


    }



    private void UpdateIsContentVisible()
    {

        if (BasePane is null)
        {
            IsContentVisible = false;
            IsHedderVisible = false;
            IsHiddnVisible = false;
            return;
        }

        if(HiddenPanes.Count()>=0)
        {
            IsContentVisible = false;
            IsHedderVisible = true;
        }

        if (OverlayPaneNode is IPaneNode overlay   )
        {
            //IsContentVisible = false;

            //if(IsHiddnVisible == overlay.IsPinned)
            //    IsHiddnVisible = overlay.IsPinned==false ;
            //IsContentVisible = true;
            return;
        }
  //      IsContentVisible = true;


         


    }

    public void AddHiddenPanes( IPaneNode node)
    {
        HiddenPanes.Add(node);

        ChangeFlag(node);

//        UpdateIsContentVisible();
    }
}

/// <summary>
/// 👑 【最外殻統治コントロール】
/// 画面全体を横断するアウタードッキング（青線・赤線）を完全に支配するカスタムコントロール
/// </summary>
[TemplatePart(Name = "PART_IndicatorOuter", Type = typeof(Grid))]
[TemplatePart(Name = "PART_IndicatorOuterTop", Type = typeof(Grid))]
[TemplatePart(Name = "PART_IndicatorOuterBottom", Type = typeof(Border))]
[TemplatePart(Name = "PART_IndicatorOuterLeft", Type = typeof(Border))]
[TemplatePart(Name = "PART_IndicatorOuterRight", Type = typeof(Border))]


[DependencyProperty<DockingSideContext>("LeftToolPain")]
[DependencyProperty<DockingSideContext>("RightToolPain")]
[DependencyProperty<DockingSideContext>("TopToolPain")]
[DependencyProperty<DockingSideContext>("BottomToolPain")]

 

     

[ObservableObject]
public partial class DockingPaneViewModel : Control
{





    #region RootDocument
    public static readonly DependencyProperty RootDocumentNodeProperty =
DependencyProperty.Register(nameof(RootDocumentNode), typeof(IPaneNode), typeof(DockingPaneViewModel),
new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsRender, OnToolRootChanged));


    public IPaneNode RootDocumentNode
    {
        get => (IPaneNode)GetValue(RootDocumentNodeProperty);
        set => SetValue(RootDocumentNodeProperty, value);
    }


    // 🎯 完全に共通化されたコールバックメソッド


    public void SetToolPainPosition(IPaneNode nd)
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


        if (d is DockingPaneViewModel vm)
        {
            // 1. どのプロパティ（DependencyProperty）が変わったかに応じて位置を決定
            //EnumToolPainPosition position = e.Property switch
            //{
            //    _ when e.Property ==  LeftToolRootProperty => EnumToolPainPosition.Left,
            //    _ when e.Property == RightToolRootProperty => EnumToolPainPosition.Right,
            //    _ when e.Property == TopToolRootProperty => EnumToolPainPosition.Top,
            //    _ when e.Property == BottomToolRootProperty => EnumToolPainPosition.Bottom,
            //    _ when e.Property == LeftOverlayPaneNodeProperty => EnumToolPainPosition.Left,
            //    _ when e.Property == RightOverlayPaneNodeProperty => EnumToolPainPosition.Right,
            //    _ when e.Property == TopOverlayPaneNodeProperty => EnumToolPainPosition.Top,
            //    _ when e.Property == BottomOverlayPaneNodeProperty => EnumToolPainPosition.Bottom,
            //    _ => EnumToolPainPosition.None // デフォルトや想定外の場合
            //};

            if (e.NewValue is IPaneNode newNode)
            {
               // newNode.ToolPainPosition = position;

                // 💡 既存のレイアウト構築処理を走らせる
                vm.SetToolPainPosition(newNode);

                // 💡 ここで深い階層（MainChildやSubChild）まで完璧に共通Contextをバケツリレーする
                vm.AttachContextToTree(newNode, vm.Context);


                Debug.WriteLine($"{newNode.ID}:{newNode.GetHashCode()}");


            }

            // 3. 💡 追加の対策：WPFにビジュアルツリーの更新とリレイアウトを強制コマンドで叩き込む
            vm.InvalidateArrange();
            vm.InvalidateVisual();

            Debug.WriteLine("Peopety");
 
       
        }
    }



    #region OverlayPaneNode






    #endregion

    //partial void OnBottomOverlayPaneNodeChanged(PaneContentNode? value)
    //{
    //    value.Parent = _bottomOverlayPaneNode;
    //}


    public TreeContext? Context { get; set; } = new();

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


    private readonly HashSet<IPaneNode> _visitedNodes = new();


    public void AttachContextToTree(IPaneNode? node, TreeContext context)
    {
        // 1. nullチェック（ガード節）
        if (node == null) return;


        // 1. 【無限ループガード】すでにこのメソッドを通ったノードなら処理をスキップする
        if (_visitedNodes.Contains(node))
        {
            System.Diagnostics.Debug.WriteLine($"⚠️ 警告: 循環参照を検知したためスキップしました -> {node.ID}");
            return;
        }
        _visitedNodes.Add(node);

        try
        {
            // 2. ノードにContextを注入
            if (node is PaneContentNode paneNode)
            {
                paneNode.Context = context;
    //            System.Diagnostics.Debug.WriteLine($"【デバッグ】Context注入成功: {paneNode.ID} (Hash: {context.GetHashCode()})");
            }

            // 3. 子階層を巡回（nullチェックを厳重に行う）
            if (node.MainChild != null && node.MainChild != node)
            {
                AttachContextToTree(node.MainChild, context);
            }

            if (node.SubChild != null && node.SubChild != node)
            {
                AttachContextToTree(node.SubChild, context);
            }
        }
        catch (Exception ex)
        {
            // 4. 万が一エラーが起きていたら、ここでキャッチして出力する
            System.Diagnostics.Debug.WriteLine($"❌ エラー発生 (AttachContextToTree): {ex.Message}");
        }
        finally
        {
            // 1つのルート処理が終わったらクリアする
            // (プロパティ変更コールバックの最初でクリアを呼ぶ必要があります)
            _visitedNodes.Clear();

        }
    }

 

    public DockingPaneViewModel()
    {
        LeftToolPain = new(this, Context, EnumToolPainPosition.Left);
        RightToolPain  = new(this, Context, EnumToolPainPosition.Right);
        TopToolPain = new(this, Context, EnumToolPainPosition.Top);
        BottomToolPain  = new(this, Context, EnumToolPainPosition.Bottom);
         

        Debug.WriteLine("引数なし");

        Debug.WriteLine($"{this.GetHashCode()}:this.GetHashCode()");

        Debug.WriteLine($"{Context.GetHashCode()}:Context.GetHashCode()");
        //this.DataContext = this;
    }
    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();


        Context.Messenger.Register<AutoHiddenChangedMessage>(this, (recipient, message) =>
        {
            if (recipient is DockingPaneViewModel vm)
            {
                // 安全にUIスレッド（または同期処理）で実行
                vm.ChangeOverlay(message );
            }
        });

        var _r = GetTemplateChild("PART_DockingOuterIndicator") as Grid;

        // XAMLからアウタードッキング用のパーツアドレスをガチッと補獲
        _indicatorOuter = GetTemplateChild("PART_IndicatorOuter") as Grid;



        _outerTop = GetTemplateChild("PART_IndicatorOuterTop") as Border;

        _outerBottom = GetTemplateChild("PART_IndicatorOuterBottom") as Border;

        _outerLeft = GetTemplateChild("PART_IndicatorOuterLeft") as Border;
        _outerRight = GetTemplateChild("PART_IndicatorOuterRight") as Border;

    //    _outerRight.Drop += OnOuterDrop;

        //***************


        // ★【最外殻D&Dインフラの起動】：
        // 自分自身（画面全体）に対してもWPFのD&D受け入れシグナルを直結します
        this.AllowDrop = true;
        this.DragEnter += OnOuterDragEnter;
        this.DragOver += OnOuterDragOver;
        this.DragLeave += OnOuterDragLeave;
        this.Drop += OnOuterDrop;
        this.PreviewMouseDown += OnPreviewMouseDown;


        // 🧪 確定した本物のハッシュコードをログ出力
        Debug.WriteLine($"★[OnApplyTemplate] 本物のコントロール起動成功");
        Debug.WriteLine($"This Hash: {this.GetHashCode()}");
        Debug.WriteLine($"Context Hash: {Context.GetHashCode()}");

        //if( )
        //// 💡 画面が確定したので、もし初期データがすでに入っていればContextを再配備する
        //if (RootDocumentNode != null) AttachContextToTree(RootDocumentNode, Context);
        //if (LeftToolRoot != null) AttachContextToTree(LeftToolRoot, Context);
        //if (RightToolRoot != null) AttachContextToTree(RightToolRoot, Context);
        //if (TopToolRoot != null) AttachContextToTree(TopToolRoot, Context);
        //if (BottomToolRoot != null) AttachContextToTree(BottomToolRoot, Context);
    }

 

    void RefreshSub(IPaneNode nd)
    {
        if (nd == null) return;

        if (nd.MainChild != null)
        {
            RefreshSub(nd.MainChild);
        }

        if (nd.SubChild != null)
        {
            RefreshSub(nd.SubChild);
        }
    }



    public void ChangeOverlay( AutoHiddenChangedMessage msg )
    {
        IPaneNode node = msg.TargetNode;

        IPaneNode parent = msg.ParentNode;
        bool IsPinned = msg.IsAutoHidden;


        switch (node.ToolPainPosition)
        {
            case EnumToolPainPosition.Right:


                RightToolPain.OverlayPaneNode = node;
                 
                RightToolPain.IsHiddnVisible = !IsPinned; 
        
 
                //RightToolPain.ChangeFlag(   node   );
                break;

            case EnumToolPainPosition.Left:

                LeftToolPain.OverlayPaneNode = node;

                LeftToolPain.IsHiddnVisible = !IsPinned;

                Debug.WriteLine(LeftToolPain.OverlayPaneNode.IsAutoHidden);


           //     LeftToolPain.IsHiddnVisible = IsPinned;

                break;

            case EnumToolPainPosition.Top:
                TopToolPain.OverlayPaneNode = node;

                TopToolPain.IsHiddnVisible = !IsPinned;

                break;

            case EnumToolPainPosition.Bottom:


                BottomToolPain.OverlayPaneNode = node;

                //BottomToolPain.ChangeFlag(node);

                BottomToolPain.IsHiddnVisible = !IsPinned;
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
        IPaneNode? root)
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
        if (node == null || node.IsToolPane == false) return;

        switch (node.ToolPainPosition)
        {
            case EnumToolPainPosition.Right:

                RightToolPain.AddHiddenPanes (node);

                break;

            case EnumToolPainPosition.Left:

                LeftToolPain.AddHiddenPanes(node);
                 
                break;

            case EnumToolPainPosition.Top:
                 
                TopToolPain.AddHiddenPanes(node);


                break;

            case EnumToolPainPosition.Bottom:

                BottomToolPain.AddHiddenPanes(node);
                 
                break;

        }
    }

    public void RefreshHiddenPanes(PaneContentNode node = null)
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



    private void OnPreviewMouseDown(object sender, MouseEventArgs e)
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
        TabViewModel draggedData = payload.DraggedData;

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

