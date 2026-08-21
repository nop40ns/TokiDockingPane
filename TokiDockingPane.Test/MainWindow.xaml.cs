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
        // 🔷 1. 中央領域（エディタ）の末端ペインたちを生成 (isToolPane: false)
        var node左上 = new PaneContentNode(new TabViewModel(s[0], new FileViewModel(s[0])), isToolPane: false);
        node左上.AddTab(new TabViewModel(s[1], new FileViewModel(s[1])));

        var node右上 = new PaneContentNode(new TabViewModel(s[2], new FileViewModel(s[2])), isToolPane: false);

        var node左下 = new PaneContentNode(new TabViewModel(s[3], new FileViewModel(s[3])), isToolPane: false);
        node左下.AddTab(new TabViewModel(s[4], new FileViewModel(s[4])));
        node左下.AddTab(new TabViewModel(s[5], new FileViewModel(s[5])));

        var node右下 = new PaneContentNode(new TabViewModel(s[6], new FileViewModel(s[6])), isToolPane: false);

        // 🌲 2. 中央領域の結合（ここは既存のトポロジーと同じ）
        var nodeTop = new PaneContentNode(isToolPane: false) { Orientation = EnumOrientation.Vertical };
        nodeTop.MainChild = node左上; node左上.Parent = nodeTop;
        nodeTop.SubChild = node右上; node右上.Parent = nodeTop;

        var nodeBottom = new PaneContentNode(isToolPane: false) { Orientation = EnumOrientation.Vertical };
        nodeBottom.MainChild = node左下; node左下.Parent = nodeBottom;
        nodeBottom.SubChild = node右下; node右下.Parent = nodeBottom;

        var rootDocument = new PaneContentNode(isToolPane: false) { Orientation = EnumOrientation.Horizontal };
        rootDocument.MainChild = nodeTop; nodeTop.Parent = rootDocument;
        rootDocument.SubChild = nodeBottom; nodeBottom.Parent = rootDocument;

        // 🔷 3. 右端のツール領域の末端ペインを生成 (isToolPane: true)
        var solutionExplorerNode = new PaneContentNode(new TabViewModel(l[0], new LuncherViewModelWPF(l[0])), isToolPane: true);
        solutionExplorerNode.AddTab(new TabViewModel(l[1], new LuncherViewModelWPF(l[1])));

        // 🚀 4. 【ここが案Aのキモ！】「中央領域全体」と「右ツール」を Vertical（縦割り）で結合し、真の最上位ルートを作る
        var trueRoot = new PaneContentNode(isToolPane: false) { Orientation = EnumOrientation.Vertical };

        // 左側（Main）に中央のエディタ領域すべてをぶら下げる
        trueRoot.MainChild = rootDocument;
        rootDocument.Parent = trueRoot;

        // 右側（Sub）にソリューションエクスプローラー（ツール）をぶら下げる
        trueRoot.SubChild = solutionExplorerNode;
        solutionExplorerNode.Parent = trueRoot;

        // 🎯 5. 頂点ノードだけを ViewModel に渡して DataContext に直結
        var mainVM = new DockingPaneViewModel(trueRoot);
        this.DataContext = mainVM;
    }
    　
}
