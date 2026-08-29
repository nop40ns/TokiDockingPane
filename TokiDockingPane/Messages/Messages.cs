using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TokiDockingPane.Interfaces;
using TokiDockingPane.Models;
using TokiDockingPane.ViewModels;

namespace TokiDockingPane.Messages
{
    public class TreeContext
    {
        public IMessenger Messenger { get; } = new WeakReferenceMessenger();
    }

    public class AutoHiddenChangedMessage
    {
        // 状態が変わったノード自身
        public IPaneNode TargetNode { get; }
        public IPaneNode ParentNode { get; }

        // 新しい値（true: 自動非表示, false: 固定表示）
        public bool IsAutoHidden { get; } = false;

        public AutoHiddenChangedMessage(IPaneNode targetNode, IPaneNode parentNode, bool isAutoHidden)
        {
            TargetNode = targetNode;
            IsAutoHidden = isAutoHidden;
            ParentNode = parentNode;
        }
    }
     
    public class ChangeOverlay
    {
        public PaneContentNode TargetNode;

        public ChangeOverlay(PaneContentNode nd)
        {
            TargetNode = nd; 
        }
    }
}
