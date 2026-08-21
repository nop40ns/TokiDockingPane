using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using TokiDockingPane.Interfaces;
using TokiDockingPane.Models;
using TokiDockingPane.Test.ViewModels;
using TokiDockingPane.ViewModels; // 上記のコンテナを使う場合

namespace TokiDockingPane.Test;

public partial class MainWindow : Window
{
    private bool _isRightToolPinned = true; // 初期状態はピン留め固定(Docked)

    public MainWindow()
    {
        InitializeComponent();

        // =========================================================================
        // 📁 1. メイン領域：マルチペイン ＆ マルチタブ のテストデータ構築（完全維持）
        // =========================================================================

        string[] s =  {
            "C:\\Users\\nop40ns\\Documents",
            "C:\\Users\\nop40ns\\Downloads",
            "D:\\Source\\Repos\\TokiFiler",
            "E:\\M32R\\Project_Alpha",
            "E:\\M32R\\Interrupt_Vectors",
            "E:\\M32R\\Assembler_Core",
            "F:\\NAS\\Backup_2026"
        };

        string[] l =  {
            "C:\\xxx",
            "C:\\yyy", 
        };


        var node左上 = new PaneContentNode
            ( new TabViewModel ( s[0],new FileViewModel(s[0])));

        node左上.AddTab(new TabViewModel( s[1],new FileViewModel(s[1])));

        var node右上 = new PaneContentNode
            (new TabViewModel(s[2], new FileViewModel(s[2])));

        var node左下 = new PaneContentNode
            (new TabViewModel(s[3], new FileViewModel(s[3])));
         

        node左下.AddTab(new TabViewModel(s[4], new FileViewModel(s[4]))); 
        node左下.AddTab(new TabViewModel(s[5], new FileViewModel(s[5]))); 

        var node右下 = new PaneContentNode
            (new TabViewModel(s[6], new FileViewModel(s[6])));

        // 🌲 再帰ツリーの結合（トポロジー構築）
        var nodeTop = new PaneContentNode();
        nodeTop.Orientation = EnumOrientation.Vertical;
        nodeTop.MainChild = node左上; node左上.Parent = nodeTop;
        nodeTop.SubChild = node右上; node右上.Parent = nodeTop;

        var nodeBottom = new PaneContentNode();
        nodeBottom.Orientation = EnumOrientation.Vertical;
        nodeBottom.MainChild = node左下; node左下.Parent = nodeBottom;
        nodeBottom.SubChild = node右下; node右下.Parent = nodeBottom;

        var rootDocument = new PaneContentNode();
        rootDocument.Orientation = EnumOrientation.Horizontal;
        rootDocument.MainChild = nodeTop; nodeTop.Parent = rootDocument;
        rootDocument.SubChild = nodeBottom; nodeBottom.Parent = rootDocument;

        // =========================================================================
        // 🎯 2. ツール領域：ソリューションエクスプローラー風ノードの配備
        // =========================================================================
        // 実体の中身としてダミーの文字列を流し込み、タイトルをセット
  
        var solutionExplorerNode 
            = new ToolPaneContentNode( new TabViewModel(l[0], new LuncherViewModelWPF(l[0]))
            );

        solutionExplorerNode.AddTab(new TabViewModel(l[1], new LuncherViewModelWPF(l[1])));

        // 最上位コンテナに「メイン」と「ツール」をまとめて直結
        var mainVM = new DockingPaneViewModel(rootDocument, solutionExplorerNode);
        this.DataContext = mainVM;
    }
    　
}
