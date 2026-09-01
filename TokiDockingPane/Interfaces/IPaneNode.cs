using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TokiDockingPane.Messages;
using TokiDockingPane.Models;
using TokiDockingPane.ViewModels;

namespace TokiDockingPane.Interfaces;



[ObservableObject]
public partial class TabViewModel
{
    [ObservableProperty]
    string _title = "";


    [ObservableProperty]
    private object? _viewModel;

    public TabViewModel()
    {

    }

    public TabViewModel(string title, object vm)
    {
        _title = title;
        _viewModel = vm;

    }

}
/// <summary>
/// ペインの分割方向（WPF Gridの行・列駆動に直結）
/// </summary>
public enum EnumOrientation : byte
{
    Vertical = 0,   // 縦割（左右にペインが並ぶ）
    Horizontal = 1  // 横割（上下にペインが並ぶ）
}


public enum EnumToolPainPosition:byte
{
    None = 0,
    Top = 1,
    Bottom = 2,
    Left = 3,
    Right=4


}
public interface IPaneNode : IDisposable, INotifyPropertyChanged
{
    public string ID { get; set; }

    // 親への参照（メインもツールも、ツリーを遡るために必須）
    public IPaneNode? MainChild { get; set; }
    public IPaneNode? SubChild { get; set; }
    public IPaneNode? Parent { get; set; }

    DockingSideContext DockingSide { get; set; }

    /// <summary>
    ///  public DockingPaneViewModel RootVM { get; set; }
    /// </summary>

    public string Title { get; set; }

    TreeContext? Context { get; set; }

    public bool IsToolPane { get; set; }

    EnumToolPainPosition ToolPainPosition { get; set; }

    public bool IsAutoHidden { get; set; }

    public bool IsPinned { get; set; }

    public EnumOrientation Orientation { get; set; }



    public double SplitRatio { get; set; }    // Gridの * 寸法（Width/Height）にダイレクト連動

    public object? ViewModel { get; set; }          // 葉ノード（末端ペイン）が持つ実際のファイラーデータ

    // 現在選択されているアクティブなタブのインデックス（0ms切り替えのインデックス）
    public int SelectedTabIndex { get; set; }

    // タブに内包されている実体データ（ViewModel）の一覧
    public List<TabViewModel> TabViewModels { get; set; }

    // 現在アクティブな画面の中身
    object? ActiveViewModel { get; }

    public TabViewModel? SelectedTab { get; }


    public void AddNode(IPaneNode node);

    public void AddTab(TabViewModel tab);
    // タブを引き抜く（Remove）ための共通駆動
    void RemoveTab(int index);

    void RemoveMe();


    void RaisePropertyChanged(string propertyName);

    public void SplitHorizontal(TabViewModel newViewModel);

    public void SplitVertical(TabViewModel newViewModel);


    void OuterSplitHorizontal(TabViewModel newViewModel);
    void OuterSplitVertical(TabViewModel newViewModel);

    void ClearAllIndicators();

}
