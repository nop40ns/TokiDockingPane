using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TokiDockingPane.Models;

namespace TokiDockingPane.Manager;

public class MultiPaneManager
{
    // 4画面分の固定スロット（要素数は絶対に4から増減しない）
    private readonly PaneState[] _panes = new PaneState[4];

    public MultiPaneManager()
    {
        // 初期化時に4インスタンスを先行生成。以降、アプリ終了までnewは叩かない
        for (byte i = 0; i < 4; i++)
        {
            _panes[i] = new PaneState(i)
            {
                Slot = (PaneSlot)(i + 1), // 初期配置を1〜4にマッピング
                IsVisible = i == 0       // 初期状態は1画面目のみ表示など
            };
        }
    }

    /// <summary>
    /// 特定インデックスのペイン状態をダイレクト参照（ポインタ高速アクセス）
    /// </summary>
    public PaneState GetPane(byte index)
    {
        if (index >= 4) throw new ArgumentOutOfRangeException(nameof(index));
        return _panes[index];
    }

    /// <summary>
    /// ペインの位置（スロット）を入れ替える（DandD時の超高速データ置換）
    /// </summary>
    public void SwapPaneSlot(byte indexA, byte indexB)
    {
        if (indexA >= 4 || indexB >= 4) return;

        // インスタンスの配置を換えるのではなく、Slotプロパティの値だけを入れ替える
        // これにより、WPFのUI側（CustomControl）がバインディング経由で位置を検知して動く
        PaneSlot temp = _panes[indexA].Slot;
        _panes[indexA].Slot = _panes[indexB].Slot;
        _panes[indexB].Slot = temp;
    }
}
