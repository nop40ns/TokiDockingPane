using CommunityToolkit.Mvvm.ComponentModel;

namespace TokiDockingPane.Models;

/// <summary>
/// テストプログラム用エントリー検証クラス
/// </summary>
 
/// <summary>
/// ペインの分割方向（WPF Gridの行・列駆動に直結）
/// </summary>
public enum EnumOrientation : byte
{
    Vertical = 0,   // 縦割（左右にペインが並ぶ）
    Horizontal = 1  // 横割（上下にペインが並ぶ）
}

/// <summary>
/// 分割方向と2つの子ノードを再帰ネスト保持する、1バイト無駄のないレイアウトModel
/// </summary>
[ObservableObject]
public partial class PaneContentNode : IDisposable
{
    [ObservableProperty]
    private PaneContentNode? _parent;

    [ObservableProperty]
    private PaneContentNode? _mainChild; // Verticalなら「左」、Horizontalなら「上」

    [ObservableProperty]
    private PaneContentNode? _subChild;  // Verticalなら「右」、Horizontalなら「下」

    [ObservableProperty]
    private EnumOrientation _orientation;

    [ObservableProperty]
    private double _splitRatio = 0.5;    // Gridの * 寸法（Width/Height）にダイレクト連動

    [ObservableProperty]
    private object? _viewModel;          // 葉ノード（末端ペイン）が持つ実際のファイラーデータ

    // 現在選択されているアクティブなタブのインデックス（0ms切り替えのインデックス）
    [ObservableProperty]
    private int _selectedTabIndex = 0;

    // 各タブの実体データ（ViewModelポインタ）を保持するリスト
    // ※ 1バイト管理思想に基づき、初期化時に一定数をプールするアプローチも可能
    [ObservableProperty]
    private List<object> _tabViewModels = new();


    /// <summary>
    /// 現在アクティブな画面の中身（互換性維持用のショートカットプロパティ）
    /// </summary>
    public object? ActiveViewModel
    {
        get
        {
            if (_tabViewModels == null || _tabViewModels.Count == 0 || _selectedTabIndex < 0 || _selectedTabIndex >= _tabViewModels.Count)
                return null;
            return _tabViewModels[_selectedTabIndex];
        }
    }



    /// <summary>
    /// 実体ペイン（葉ノード）用コンストラクタ
    /// </summary>

    public PaneContentNode(object vm)
    {
        _tabViewModels.Add(vm);
        _selectedTabIndex = 0;
    }

    /// <summary>
    /// レイアウトコンテナ（親ノード）用コンストラクタ
    /// </summary>
    public PaneContentNode()
    {
    }



    /// <summary>
    /// このペインに新しくタブを追加する（1本道非同期パイプラインへの直結用）
    /// </summary>
    public void AddTab(object vm)
    {
        // 1. 既存のリストのポインタを新しいインスタンス（ shallow copy ）へ一発置換
        //    これによりWPFのバインディングエンジンは「全く新しいリストが来た」と検知し、
        //    ListBoxのヘッダーを一瞬（0ms）で再描画します。
        var newList = new List<object>(_tabViewModels) { vm };
        TabViewModels = newList; // 自動生成されたプロパティセッター経由でアドレス変更をWPFへ叩き込む

        // 2. 追加された新規タブへ自動的にフォーカス（選択）を移動させる
        SelectedTabIndex = TabViewModels.Count - 1;
    }
    /// <summary>
    /// 【診断用暫定コード】画面の収縮（消滅）ロジックを完全にオミットし、
    /// 純粋に「リストからポインタを引き抜いてインデックスを合わせるだけ」の処理に固定する
    /// </summary>
    public void RemoveTab(int index)
    {
        if (_tabViewModels == null || index < 0 || index >= _tabViewModels.Count) return;

        // 純粋に要素を1枚引き抜くだけ（画面を閉じる・詰める処理は1行も走らせない）
        var newList = new List<object>(_tabViewModels);
        newList.RemoveAt(index);
        TabViewModels = newList;

        // インデックスとアクティブ画面の同期
        int targetIndex = Math.Max(0, TabViewModels.Count - 1);
        _selectedTabIndex = -1;
        SelectedTabIndex = targetIndex;

        OnPropertyChanged(nameof(SelectedTabIndex));
        OnPropertyChanged(nameof(ActiveViewModel));
    }

    /// <summary>
    /// タブ引き抜き処理（トポロジーの自動自動クリーンアップ内包）
    /// </summary>
    //public void RemoveTab(int index)
    //{
    //    if (_tabViewModels == null || index < 0 || index >= _tabViewModels.Count) return;

    //    var newList = new List<object>(_tabViewModels);
    //    newList.RemoveAt(index);
    //    TabViewModels = newList;

    //    // ---------------------------------------------------------------------
    //    // 🔥【最深部・最終調停】中身を破壊せず、アドレス（参照ポインタ）だけを繋ぎ替える
    //    // ---------------------------------------------------------------------
    //    if (TabViewModels.Count == 0 && Parent != null)
    //    {
    //        PaneContentNode currentParent = Parent;     // 自身の親（分割コンテナ）
    //        PaneContentNode? grandParent = currentParent.Parent; // 自身の祖父

    //        // 自分の相方（兄弟ノード：消されずに生き残る本物のデータノード）を特定
    //        PaneContentNode? sibling = (currentParent.MainChild == this)
    //            ? currentParent.SubChild
    //            : currentParent.MainChild;

    //        if (sibling != null)
    //        {
    //            if (grandParent != null)
    //            {
    //                // 【パターンA：上にさらに親（祖父）がいる階層構造の場合】
    //                // 祖父から見た古い親（currentParent）へのポインタを一度 null にしてキャッシュを破砕
    //                bool isMainChild = (grandParent.MainChild == currentParent);
    //                if (isMainChild)
    //                {
    //                    grandParent.MainChild = null;
    //                    grandParent.OnPropertyChanged(nameof(MainChild));
    //                }
    //                else
    //                {
    //                    grandParent.SubChild = null;
    //                    grandParent.OnPropertyChanged(nameof(SubChild));
    //                }

    //                // 役割を終えた中間のコンテナと自分のリンクを完全切断
    //                currentParent.MainChild = null;
    //                currentParent.SubChild = null;
    //                currentParent.Parent = null;
    //                this.Parent = null;

    //                // 生き残った相方（sibling）のポインタを祖父へダイレクトに直結（バイパス）
    //                sibling.Parent = grandParent;
    //                if (isMainChild)
    //                {
    //                    grandParent.MainChild = sibling;
    //                    grandParent.OnPropertyChanged(nameof(MainChild));
    //                }
    //                else
    //                {
    //                    grandParent.SubChild = sibling;
    //                    grandParent.OnPropertyChanged(nameof(SubChild));
    //                }

    //                // 相方の全プロパティ通知をキックしてWPFを強制再描画
    //                sibling.OnPropertyChanged(string.Empty);
    //            }
    //            else
    //            {
    //                // 【パターンB：自分がルート直下の分割だった場合（上がWindowなど、grandParentがnull）】
    //                // 祖父がいない場合は、親コンテナ（currentParent）を完全に『sibling』のクローンにするのではなく、
    //                // 親コンテナの MainChild と SubChild 自体を sibling の持っていた子へ差し替えます。
    //                // 1バイトのデータ破壊も起こさないよう、プロパティの「参照ポインタ」だけを安全にスライドさせます。
    //                currentParent.MainChild = sibling.MainChild;
    //                currentParent.SubChild = sibling.SubChild;
    //                currentParent.Orientation = sibling.Orientation;
    //                currentParent.SplitRatio = sibling.SplitRatio;

    //                // 最重要：中身のデータ（ポインタ）をそのままアドレス移送
    //                currentParent.TabViewModels = sibling.TabViewModels;
    //                currentParent.SelectedTabIndex = sibling.SelectedTabIndex;
    //                currentParent.ViewModel = sibling.ViewModel;

    //                // 再結線
    //                if (currentParent.MainChild != null) currentParent.MainChild.Parent = currentParent;
    //                if (currentParent.SubChild != null) currentParent.SubChild.Parent = currentParent;

    //                sibling.Parent = null;
    //                this.Parent = null;

    //                // 親コンテナ側の通知を撃ち、Gridを詰め直させる
    //                currentParent.OnPropertyChanged(string.Empty);
    //            }
    //        }
    //        return;
    //    }

    //    // まだタブが残っている場合の通常フォーカス同期
    //    int targetIndex = Math.Max(0, TabViewModels.Count - 1);
    //    _selectedTabIndex = -1;
    //    SelectedTabIndex = targetIndex;

    //    OnPropertyChanged(nameof(SelectedTabIndex));
    //    OnPropertyChanged(nameof(ActiveViewModel));
    //}


    /// <summary>
    /// 階層ツリー全体の再帰的完全クリーンアップ（メモリゾンビ化の完全阻止）
    /// </summary>
    public void Dispose()
    {
        _mainChild?.Dispose();
        _subChild?.Dispose();
        if (_tabViewModels != null)
        {
            _tabViewModels.Clear();
        }
        _mainChild = null;
        _subChild = null;
        _parent = null;
        _tabViewModels = null!;
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// このペインをその場で縦割（左右）コンテナへとトランスフォームさせる（アロケーション最小化）
    /// </summary>
    public void SplitVertical(object newViewModel)
    {
        // 1. 自身の現在の全タブ資産を引き継ぐ「左側（Main）」のクローンノードを生成
        var leftNode = new PaneContentNode
        {
            TabViewModels = this.TabViewModels, // ポインタの一発譲渡
            SelectedTabIndex = this.SelectedTabIndex
        };

        // 2. 新しくドロップされたViewModelを保持する「右側（Sub）」のノードを生成
        var rightNode = new PaneContentNode(newViewModel);

        // 3. 自身のペインデータをクリーンに切断（自身はコンテナ枠へ昇華するため）
        this.TabViewModels = null!;

        // 4. トポロジーの書き換え ➔ WPF側が感知して一瞬で画面が割れる
        this.Orientation = EnumOrientation.Vertical;
        this.MainChild = leftNode;
        this.SubChild = rightNode;

        OnPropertyChanged(nameof(MainChild));
        OnPropertyChanged(nameof(SubChild));
        OnPropertyChanged(nameof(Orientation));
    }

    /// <summary>
    /// このペインをその場で横割（上下）コンテナへとトランスフォームさせる
    /// </summary>
    public void SplitHorizontal(object newViewModel)
    {
        // 1. 自身の現在の全タブ資産を引き継ぐ「上側（Main）」のクローンノードを生成
        var topNode = new PaneContentNode
        {
            TabViewModels = this.TabViewModels,
            SelectedTabIndex = this.SelectedTabIndex
        };

        // 2. 新しくドロップされたViewModelを保持する「下側（Sub）」のノードを生成
        var bottomNode = new PaneContentNode(newViewModel);

        this.TabViewModels = null!;

        // 3. トポロジーの書き換え ➔ 上下分割へ
        this.Orientation = EnumOrientation.Horizontal;
        this.MainChild = topNode;
        this.SubChild = bottomNode;

        OnPropertyChanged(nameof(MainChild));
        OnPropertyChanged(nameof(SubChild));
        OnPropertyChanged(nameof(Orientation));
    }

}