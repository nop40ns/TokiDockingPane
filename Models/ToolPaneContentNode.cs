using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
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
}
