using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using TokiDockingPane.Interfaces;

namespace TokiDockingPane.Models;

public enum EnumToolState : byte
{
    Docked = 0,
    AutoHidden = 1,
    Floating = 2
}
 
public partial class ToolPaneContentNode : ObservableObject, IPaneNode
{
    [ObservableProperty]
    private IPaneNode? _parent;

    [ObservableProperty]
    private IPaneNode? _mainChild; // Verticalなら「左」、Horizontalなら「上」

    [ObservableProperty]
    private IPaneNode? _subChild;  // Verticalなら「右」、Horizontalなら「下」

    [ObservableProperty]
    private EnumOrientation _orientation;

    [ObservableProperty]
    private double _splitRatio = 0.5;    // Gridの * 寸法（Width/Height）にダイレクト連動

    [ObservableProperty]
    private object? _viewModel;          // 葉ノード（末端ペイン）が持つ実際のファイラーデータ

    // 現在選択されているアクティブなタブのインデックス（0ms切り替えのインデックス）
    [ObservableProperty]
    private int _selectedTabIndex = 0;

    [ObservableProperty] private string _title = "ツール";
    [ObservableProperty] private EnumToolState _state = EnumToolState.Docked;
    [ObservableProperty] private double _paneSize = 280; // 幅または高さ

    [ObservableProperty] private bool _isPopupOpened = false;



    // インターフェース規定のマルチタブ資産
    [ObservableProperty] private List<object> _tabViewModels = new();
 
    public object? ActiveViewModel
    {
        get
        {
            if (_tabViewModels == null || _tabViewModels.Count == 0 || _selectedTabIndex < 0 || _selectedTabIndex >= _tabViewModels.Count)
                return null;
            return _tabViewModels[_selectedTabIndex];
        }
    }

    public ToolPaneContentNode(object vm, string title)
    {
        _title = title;
        _tabViewModels.Add(vm);
        _selectedTabIndex = 0;
    }

    public void AddTab(object vm)
    {
        var newList = new List<object>(_tabViewModels) { vm };
        TabViewModels = newList;
        SelectedTabIndex = TabViewModels.Count - 1;
    }

    public void RemoveTab(int index)
    {
        if (_tabViewModels == null || index < 0 || index >= _tabViewModels.Count) return;

        var newList = new List<object>(_tabViewModels);
        newList.RemoveAt(index);
        TabViewModels = newList;

        // ★【ツールペイン独自の収縮】
        // もし最後の1枚が引き抜かれてタブがゼロになったら、外殻スロットごと非表示にするため、
        // 結線されている親（MainWindowなど）に通知を送るか、Stateを隠蔽状態にする
        if (TabViewModels.Count == 0)
        {
            State = EnumToolState.AutoHidden; // 0msでバーへ自動格納
            return;
        }

        SelectedTabIndex = Math.Max(0, TabViewModels.Count - 1);
    }

    public void Dispose()
    {
        _tabViewModels?.Clear();
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


        leftNode.Parent = this;
        rightNode.Parent = this;

        // 3. 自身のペインデータをクリーンに切断（自身はコンテナ枠へ昇華するため）
        //this.TabViewModels = null!;
        this.TabViewModels = new List<object>();


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

        topNode.Parent = this;
        bottomNode.Parent = this;

        //this.TabViewModels = null!;
        this.TabViewModels = new List<object>();



        // 3. トポロジーの書き換え ➔ 上下分割へ
        this.Orientation = EnumOrientation.Horizontal;
        this.MainChild = topNode;
        this.SubChild = bottomNode;

        OnPropertyChanged(nameof(MainChild));
        OnPropertyChanged(nameof(SubChild));
        OnPropertyChanged(nameof(Orientation));
    }




    [RelayCommand]
    private void TogglePin()
    {
        if (State == EnumToolState.Docked)
        {
            State = EnumToolState.AutoHidden;
        }
        else if (State == EnumToolState.AutoHidden)
        {
            State = EnumToolState.Docked;
        }
    }


    // 🔥【新章】ホバー時に外からポインタ駆動される開閉コマンド
    [RelayCommand] private void OpenPopup() => IsPopupOpened = true;
    [RelayCommand] private void ClosePopup() => IsPopupOpened = false;

    public void RaisePropertyChanged(string propertyName)
    {
        // CommunityToolkit.Mvvm が裏で自動生成している本物の通知メソッドへそのままアドレスを横流しする
        this.OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
    }
}
