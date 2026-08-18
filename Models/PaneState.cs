using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TokiDockingPane.Models;

/// <summary>
/// 各ペインの物理配置スロットを定義（メモリ効率のためバイト列挙型）
/// </summary>
public enum PaneSlot : byte
{
    None = 0,
    LeftTop = 1,     // 左上
    RightTop = 2,    // 右上
    LeftBottom = 3,  // 左下
    RightBottom = 4  // 右下
}

/// <summary>
/// 4画面の1画面分の状態を司る基本Model。
/// ObservableObjectを継承し、ヒープアロケーションのオーバーヘッドを最小化。
/// </summary>
public class PaneState : ObservableObject, IDisposable
{
    // インデックス（0〜3固定・イミュータブル）
    public byte Index { get; }

    private string _currentPath = string.Empty;
    private PaneSlot _slot = PaneSlot.None;
    private bool _isActive = false;
    private bool _isVisible = false;

    // 各ペイン固有の非同期ロード用キャンセルスロット（ゾンビタスク成仏用）
    private CancellationTokenSource _cts;
    private readonly object _ctsLock = new object();

    public PaneState(byte index)
    {
        this.Index = index;
    }

    /// <summary>
    /// カレントパス（インプレイスで文字列参照を切り替え）
    /// </summary>
    public string CurrentPath
    {
        get => _currentPath;
        set
        {
            // 内部で値比較を行い、変更がある場合のみPropertyChangedを安全にキック
            // 【WPFバインディング切断ギミック】を挟む余地を残すため標準セッター構造を維持
            SetProperty(ref _currentPath, value ?? string.Empty);
        }
    }

    /// <summary>
    /// 現在の配置スロット（GridのRow/Column駆動用）
    /// </summary>
    public PaneSlot Slot
    {
        get => _slot;
        set => SetProperty(ref _slot, value);
    }

    /// <summary>
    /// フォーカス（アクティブ）状態
    /// </summary>
    public bool IsActive
    {
        get => _isActive;
        set => SetProperty(ref _isActive, value);
    }

    /// <summary>
    /// ペインの表示・非表示
    /// </summary>
    public bool IsVisible
    {
        get => _isVisible;
        set => SetProperty(ref _isVisible, value);
    }

    /// <summary>
    /// このペイン専用のCancellationTokenを取得・安全に再生成する
    /// </summary>
    public CancellationToken RefreshToken()
    {
        lock (_ctsLock)
        {
            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
            }
            _cts = new CancellationTokenSource();
            return _cts.Token;
        }
    }

    /// <summary>
    /// 安全な解放処理
    /// </summary>
    public void Dispose()
    {
        lock (_ctsLock)
        {
            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts = null;
            }
        }
    }
}
