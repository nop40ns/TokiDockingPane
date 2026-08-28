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

public partial class ToolPaneContentNode : PaneContentNode
{
    [ObservableProperty] private EnumToolState _state = EnumToolState.Docked;
    [ObservableProperty] private bool _isPopupOpened = false;
     
    [ObservableProperty] private double _paneSize = 280; // 幅または高さ




    public ToolPaneContentNode() : base()
    {
    }

    public ToolPaneContentNode(TabViewModel tab)
    {
        TabViewModels = new List<TabViewModel> { tab };
        SelectedTabIndex = 0;
    }


 

    // 🔥【新章】ホバー時に外からポインタ駆動される開閉コマンド
    [RelayCommand] private void OpenPopup() => IsPopupOpened = true;
    [RelayCommand] private void ClosePopup() => IsPopupOpened = false;

    public void RemoveTab(int index)
    {
        // まずは親クラス（PaneContentNode）の安全なタブ削除ロジックを執行
        base.RemoveTab(index);

        // もし最後の1枚が引き抜かれてタブがゼロになったら、外殻ごと非表示（AutoHidden）にスイッチ！
        if (this.TabViewModels.Count == 0)
        {
            State = EnumToolState.AutoHidden; // 0msでバーへ自動格納
        }
    }
} // クラスの閉じ
