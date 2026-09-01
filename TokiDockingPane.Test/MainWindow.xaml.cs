using System.Windows;
using TokiDockingPane.Interfaces;
using TokiDockingPane.Models;
using TokiDockingPane.Test.ViewModels;
using TokiDockingPane.ViewModels;

namespace TokiDockingPane.Test;

public partial class MainWindow : Window
{

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
            "C:\\zzz",
            "C:\\aaa",
            "C:\\bbb",
        };
        // 🔷 1. 中央領域（エディタ）の末端ペインたちを生成 (isToolPane: false)
        var node左上 
            = new PaneContentNode(new TabViewModel(s[0], new FileViewModel(s[0])), isToolPane: false);
        node左上.AddTab(new TabViewModel(s[1], new FileViewModel(s[1])));

        var node右上 = new PaneContentNode(new TabViewModel(s[2], new FileViewModel(s[2])), isToolPane: false);

        var node左下 = new PaneContentNode(new TabViewModel(s[3], new FileViewModel(s[3])), isToolPane: false);
        node左下.AddTab(new TabViewModel(s[4], new FileViewModel(s[4])));
        node左下.AddTab(new TabViewModel(s[5], new FileViewModel(s[5])));

        var node右下 = new PaneContentNode(new TabViewModel(s[6], new FileViewModel(s[6])), isToolPane: false);

        // 🌲 2. 中央領域の結合（ここは既存のトポロジーと同じ）
        var nodeTop = new PaneContentNode(isToolPane: false) { Orientation = EnumOrientation.Vertical };


        nodeTop.ID = "nodeTop";
        node左上.ID = "node左上";

        nodeTop.MainChild = node左上;  
        nodeTop.SubChild = node右上;  

        var nodeBottom = new PaneContentNode(isToolPane: false) { Orientation = EnumOrientation.Vertical };
        nodeBottom.MainChild = node左下;  
        nodeBottom.SubChild = node右下;  

        var rootDocument = new PaneContentNode(isToolPane: false) { Orientation = EnumOrientation.Horizontal };
        rootDocument.MainChild = nodeTop;  
        rootDocument.SubChild = nodeBottom;  



        var t0 = new PaneContentNode(new TabViewModel(l[0], new LuncherViewModelWPF(l[0])), isToolPane: true);
        var t1 = new PaneContentNode(new TabViewModel(l[1], new LuncherViewModelWPF(l[1])), isToolPane: true);
        var t2 = new PaneContentNode(new TabViewModel(l[2], new LuncherViewModelWPF(l[2])), isToolPane: true);
        var t3 = new PaneContentNode(new TabViewModel(l[3], new LuncherViewModelWPF(l[3])), isToolPane: true);
        var t4 = new PaneContentNode(new TabViewModel(l[4], new LuncherViewModelWPF(l[4])), isToolPane: true);

        t0.ID = "t0";

        t1.ID = "t1";
        t2.ID = "t2";
        t3.ID = "t3";
        t4.ID = "t4";

        // 🔷 3. 右端のツール領域の末端ペインを生成 (isToolPane: true)
        var solutionExplorerNode = new PaneContentNode() { Orientation = EnumOrientation.Horizontal };
        solutionExplorerNode.IsToolPane = true;

        solutionExplorerNode.ID = "solutionExplorerNode";


        solutionExplorerNode.MainChild = t0;
        solutionExplorerNode.SubChild = t1;

        // 🎯 5. 頂点ノードだけを ViewModel に渡して DataContext に直結
        // var mainVM = new DockingPaneViewModel(rootDocument );
        DP.RootDocumentNode = rootDocument;

        DP.BottomToolPain.BasePane = solutionExplorerNode;

         

        DP.RightToolPain.BasePane = t2;
        

        t3.ID = "t3";
        DP.LeftToolPain.BasePane = t3;
        


        DP.TopToolPain.BasePane = t4;
        //DP.TopToolRoot.ToolPainPosition = EnumToolPainPosition.Top;

        //  DP.Refresh();

        //  this.DataContext = mainVM;

    }


    　
}
