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
 

// ※カスタムコントロール（MyCustomControl）本体は Control 継承のままで大丈夫です！
