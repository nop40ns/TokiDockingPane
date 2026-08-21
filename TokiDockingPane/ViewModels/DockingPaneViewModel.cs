using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TokiDockingPane.Interfaces;
using TokiDockingPane.Models;

namespace TokiDockingPane.ViewModels;

/// <summary>
/// 👑 【最外殻統治コントロール】
/// 画面全体を横断するアウタードッキング（青線・赤線）を完全に支配するカスタムコントロール
/// </summary>
[TemplatePart(Name = "PART_OuterDockingIndicator", Type = typeof(Grid))]
[TemplatePart(Name = "PART_OuterTop", Type = typeof(Border))]
[TemplatePart(Name = "PART_OuterRight", Type = typeof(Border))]
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
    private Grid? _outerDockingIndicator;
    private Border? _outerTop;
    private Border? _outerRight;

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
   


    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        // XAMLからアウタードッキング用のパーツアドレスをガチッと補獲
        _outerDockingIndicator = GetTemplateChild("PART_OuterDockingIndicator") as Grid;
        _outerTop = GetTemplateChild("PART_OuterTop") as Border;
        _outerRight = GetTemplateChild("PART_OuterRight") as Border;
    }

    // =========================================================================
    // 🎯 最外殻アウターインジケーター（青線・赤線用）のリアルタイム表示制御
    // =========================================================================
    private void OnOuterDragEnter(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(typeof(TokiDragDropPayload)))
        {
            if (_outerDockingIndicator != null)
            {
                // ドラッグ中、ウィンドウ内にマウスがいる間は、
                // セパレーターの真上に美しい半透明青の「横断ガイドバー」を常時パッと出現させてホールド！
                _outerDockingIndicator.Visibility = Visibility.Visible;
            }
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
        // マウスがウィンドウの完全に外へエスケープしたか、ドロップを諦めたら即座に成仏消滅
        if (_outerDockingIndicator != null && !this.IsMouseOver)
        {
            _outerDockingIndicator.Visibility = Visibility.Collapsed;
        }
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

        IPaneNode sourceNode = payload.SourceNode;
        TabViewModel  draggedData = payload.DraggedData;

        // マウスが落とされたインジケーターの境界判定
        if (_outerTop != null && _outerTop.IsMouseOver)
        {
            // A. 【青線の場所（アウター上）への着地】
            if (sourceNode != null && sourceNode.TabViewModels != null)
            {
                int dragIndex = sourceNode.TabViewModels.IndexOf(draggedData);
                if (dragIndex >= 0) sourceNode.RemoveTab(dragIndex);
            }

            // ★【核心修正】：具象クラスへのキャストを完全廃棄！
            // RootDocumentNode（IPaneNode）が持つアウター分割メソッドをダイレクトに叩き込みます
            if (RootDocumentNode != null)
            {
                RootDocumentNode.OuterSplitHorizontal(draggedData);
            }
        }
        else if (_outerRight != null && _outerRight.IsMouseOver)
        {
            // B. 【赤線の場所（アウター右）への着地】
            if (sourceNode != null && sourceNode.TabViewModels != null)
            {
                int dragIndex = sourceNode.TabViewModels.IndexOf(draggedData);
                if (dragIndex >= 0) sourceNode.RemoveTab(dragIndex);
            }

            if (RootDocumentNode != null)
            {
                RootDocumentNode.OuterSplitVertical(draggedData);
            }
        }

        // 役目を終えたアウターインジケーターを強制非表示化
        if (_outerDockingIndicator != null)
        {
            _outerDockingIndicator.Visibility = Visibility.Collapsed;
        }

        e.Handled = true;
    }


}
