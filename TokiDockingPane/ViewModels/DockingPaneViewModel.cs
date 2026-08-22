using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using TokiDockingPane.Interfaces;
using TokiDockingPane.Models;

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


public class DockingPaneViewModel : Control
{
    public static readonly DependencyProperty RootDocumentNodeProperty =
        DependencyProperty.Register(nameof(RootDocumentNode), typeof(IPaneNode), typeof(DockingPaneViewModel), new PropertyMetadata(null));

    
    public IPaneNode RootDocumentNode
    {
        get => (IPaneNode)GetValue(RootDocumentNodeProperty);
        set => SetValue(RootDocumentNodeProperty, value);
    }
     

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

    public DockingPaneViewModel(IPaneNode rootDocument)
    {
        RootDocumentNode = rootDocument;
     }

    public DockingPaneViewModel()
    {
        // ★【最外殻D&Dインフラの起動】：
        // 自分自身（画面全体）に対してもWPFのD&D受け入れシグナルを直結します
        this.AllowDrop = true;
        this.DragEnter += OnOuterDragEnter;
        this.DragOver += OnOuterDragOver;
        this.DragLeave += OnOuterDragLeave;
        this.Drop += OnOuterDrop;
        this.PreviewMouseDown += OnPreviewMouseDown;

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
        var newToolLeaf = new PaneContentNode(dropPain, isToolPane: true);
        newToolLeaf.CanAutoHide = true;
        newToolLeaf.IsToolPane = true;

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
