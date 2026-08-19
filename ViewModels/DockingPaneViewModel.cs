using System.Windows;
using System.Windows.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using TokiDockingPane.Interfaces;
using TokiDockingPane.Models;

namespace TokiDockingPane.ViewModels;

/// <summary>
/// 👑 【最外殻統治コントロール】
/// 5列のVS風レイアウトをライブラリ側で安全に駆動するための、外殻カスタムコントロール
/// </summary>
public class DockingPaneViewModel : Control
{
    // 依存関係プロパティ（DependencyProperty）として定義することで、WPFのXAMLと100%直結します
    public static readonly DependencyProperty RootDocumentNodeProperty =
        DependencyProperty.Register(nameof(RootDocumentNode), typeof(IPaneNode), typeof(DockingPaneViewModel), new PropertyMetadata(null));

    public static readonly DependencyProperty RightToolPaneProperty =
        DependencyProperty.Register(nameof(RightToolPane), typeof(ToolPaneContentNode), typeof(DockingPaneViewModel), new PropertyMetadata(null));

    public IPaneNode RootDocumentNode
    {
        get => (IPaneNode)GetValue(RootDocumentNodeProperty);
        set => SetValue(RootDocumentNodeProperty, value);
    }

    public ToolPaneContentNode RightToolPane
    {
        get => (ToolPaneContentNode)GetValue(RightToolPaneProperty);
        set => SetValue(RightToolPaneProperty, value);
    }

    static DockingPaneViewModel()
    {
        // WPFに対し、Themes/Generic.xaml 内のスタイル（設計図）を見に行くように強制マーク
        DefaultStyleKeyProperty.OverrideMetadata(typeof(DockingPaneViewModel),
            new FrameworkPropertyMetadata(typeof(DockingPaneViewModel)));
    }

    public DockingPaneViewModel(IPaneNode rootDocument, ToolPaneContentNode rightTool)
    {
        RootDocumentNode = rootDocument;
        RightToolPane = rightTool;
    }

    // 引数なしコンストラクタ（WPFのXAMLデザイナー・初期化用）
    public DockingPaneViewModel() { }
}
