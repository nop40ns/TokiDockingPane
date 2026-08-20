using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace TokiDockingPane.Converters;



/// <summary>
/// 🌟 【双方向リサイズ調停インフラ】
/// モデルの double（PaneSize）と、WPF最外殻Gridの GridLength（列幅）を一瞬で相互変換するコンバーター。
/// これにより、GridSplitterの移動量が1ピクセルの遅延もなくモデルへ吸い込まれ、ツールの独立リサイズが完全大開通します！
/// </summary>
public class DoubleToGridLengthConverter : IValueConverter
{
    /// <summary>
    /// A面【モデル ➔ 画面】：double値（280.0など）を、WPFが理解できる GridLength（Pixel指定）に変換
    /// </summary>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double doubleValue)
        {
            // 与えられた数値をそのまま物理ピクセル（GridUnitType.Pixel）としてパッキングして画面へ流す
            return new GridLength(doubleValue, GridUnitType.Pixel);
        }
        // フォールバック：初期値として安全な280pxを返す
        return new GridLength(280, GridUnitType.Pixel);
    }

    /// <summary>
    /// B面【画面 ➔ モデル】：ユーザーが GridSplitter をマウスで動かした際の、変化した GridLength の数値を
    /// ピュアな double 型に逆算抽出して、モデル（RightToolPane.PaneSize）へリアルタイムに双方向書き戻し！
    /// </summary>
    /// <summary>
    /// 👑【リサイズ ＆ 格納完全調停】：
    /// トリガーから流れてきた「Auto（非表示化）」のシグナルを検知した時は、
    /// モデルの数値を上書き破壊せず、安全にスルーさせて黒い空間を完全圧殺する！
    /// </summary>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is GridLength gridLength)
        {
            // 🌟【大核心修正】：もしトリガーによって列幅が「Auto」に叩き落とされているなら、
            // モデルの PaneSize を 280px に無理やり上書き戻しするのを完全にストップ！
            // そのまま現在の数値を維持（または 0 を送還）して、WPFに領土を完全に譲渡します。
            if (gridLength.IsAuto)
            {
                return 0.0; // ➔ 土地のサイズを 0 にして完全成仏させる！
            }

            return gridLength.Value; // 通常のリサイズ時は、動いた数値をそのままモデルへ書き戻し
        }
        return 280.0;
    }

}

