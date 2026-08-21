using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using TokiDockingPane.Models;

namespace TokiDockingPane.Views;

/// <summary>
/// 2026年基準：XAML不要、1バイトも無駄にしない空中射出用フロートウィンドウ
/// </summary>
public class FloatWindow : Window
{
    /// <summary>
    /// このウィンドウが保持するルートノードへのポインタ
    /// </summary>
    public PaneContentNode RootNode { get; private set; }

    public FloatWindow(PaneContentNode node)
    {
        // 1. 引数のノードポインタをガチッとホールド
        RootNode = node ?? throw new ArgumentNullException(nameof(node));

        // 2. ウィンドウの基本スタイリング（TokiFilerのダークテーマに完全同調）
        this.Title = "TokiFiler - Float Pane";
        this.Width = 800;   // 初期幅
        this.Height = 600;  // 初期高さ
        this.Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x1E));
        this.WindowStartupLocation = WindowStartupLocation.Manual;
        this.ShowInTaskbar = true; // タスクバーにも個別に表示させて操作性を確保

        // 3. ★【核心の1行】Contentに直接モデル（Node）を叩き込む
        // これにより、Generic.xaml の <DataTemplate DataType="{x:Type models:PaneContentNode}"> が
        // WPFのインフラによって100%自動適用され、一瞬でマルチタブUIへとトランスフォームします。
        this.Content = RootNode;

        RootNode.PropertyChanged += OnRootNodePropertyChanged;


        // 4. メモリゾンビ化・リークを地球上から根絶するクリーンアップイベントの結線
        this.Closed += OnWindowClosed;
    }

    private void OnRootNodePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // タブ一覧が変更されたタイミングを監視
        if (e.PropertyName == nameof(PaneContentNode.TabViewModels))
        {
            // 後攻非同期でモデルの状態が確定した後にジャッジ
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (RootNode == null) return;

                // 自分自身（ルート）が子を持たず、かつタブが0件になった＝すべてのタブが他へ帰還した
                if (RootNode.MainChild == null && RootNode.SubChild == null &&
                    (RootNode.TabViewModels == null || RootNode.TabViewModels.Count == 0))
                {
                    // 役割を終えたので、自分自身を閉じる（OnWindowClosedが走り綺麗に爆破されます）
                    this.Close();
                }
            }), System.Windows.Threading.DispatcherPriority.Render);
        }
    }




    private void OnWindowClosed(object? sender, EventArgs e)
    {
        this.Closed -= OnWindowClosed;

        // ウィンドウが閉じられた際、内部ノードの参照をクリーンに切断し再帰Disposeを執行
        if (RootNode != null)
        {
            RootNode.Dispose();
            RootNode = null!;
        }

        this.Content = null;

        // 明示的にGCへ回収を促す（1バイト思想の徹底）
        GC.Collect(0, GCCollectionMode.Optimized);
    }
}
