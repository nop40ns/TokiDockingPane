using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TokiDockingPane.Models;

namespace TokiDockingPane.Interfaces;



/// <summary>
/// ペインの分割方向（WPF Gridの行・列駆動に直結）
/// </summary>
public enum EnumOrientation : byte
{
    Vertical = 0,   // 縦割（左右にペインが並ぶ）
    Horizontal = 1  // 横割（上下にペインが並ぶ）
}

public interface IPaneNode : IDisposable, INotifyPropertyChanged
{
    // 親への参照（メインもツールも、ツリーを遡るために必須）

 
    public EnumOrientation Orientation { get; set; } 

    public double SplitRatio { get; set; }    // Gridの * 寸法（Width/Height）にダイレクト連動

    public object? ViewModel { get; set; }          // 葉ノード（末端ペイン）が持つ実際のファイラーデータ

    // 現在選択されているアクティブなタブのインデックス（0ms切り替えのインデックス）
    public  int SelectedTabIndex { get; set; }

    // タブに内包されている実体データ（ViewModel）の一覧
    public List<object> TabViewModels { get; set; }

    // 現在アクティブな画面の中身
    object? ActiveViewModel { get; }

    // タブを引き抜く（Remove）ための共通駆動
    void RemoveTab(int index);
     

}
