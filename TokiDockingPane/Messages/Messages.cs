using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TokiDockingPane.Models;
using TokiDockingPane.ViewModels;

namespace TokiDockingPane.Messages
{
    internal class TreeContext
    {
        public IMessenger Messenger { get; } = new WeakReferenceMessenger();
    }

    internal class AutoHiddenChangedMessage
    {
        // 状態が変わったノード自身
        public PaneContentNode TargetNode { get; }

        // 新しい値（true: 自動非表示, false: 固定表示）
        public bool IsAutoHidden { get; }

        public AutoHiddenChangedMessage(PaneContentNode targetNode, bool isAutoHidden)
        {
            TargetNode = targetNode;
            IsAutoHidden = isAutoHidden;
        }
    }

    // 2. メッセージ自体も internal にする（DLL外からは見えない）
    internal class ChangeOverlay
    {
        public PaneContentNode TargetNode;

        public ChangeOverlay(PaneContentNode nd)
        {
            TargetNode = nd; 
        }
    }
}
