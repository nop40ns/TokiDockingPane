using System.Windows;
using System.Windows.Media;
using DependencyPropertyGenerator;

namespace TokiDockingPane.Models;

[DependencyProperty<Brush>("Background")]
[DependencyProperty<Brush>("Foreground")]
[DependencyProperty<FontWeight>("FontWeight")]
[DependencyProperty<FontStyle>("FontStyle")]
[DependencyProperty<TextDecorationCollection>("TextDecorations")]
public partial class TextAttr : Freezable // 💡 Freezable に変更
{
    // Freezable クラスを継承する際、このメソッドのオーバーライドが必須です
    protected override Freezable CreateInstanceCore() => new TextAttr();
}

[DependencyProperty<TextAttr>("Default")]
[DependencyProperty<TextAttr>("Mouseover")]
[DependencyProperty<TextAttr>("Selected")]
public partial class HeaderAttr : Freezable // 💡 Freezable に変更
{
    public HeaderAttr()
    {
        Default = new TextAttr();
        Mouseover = new TextAttr();
        Selected = new TextAttr();
    }

    protected override Freezable CreateInstanceCore() => new HeaderAttr();
}

// ※カスタムコントロール（MyCustomControl）本体は Control 継承のままで大丈夫です！
