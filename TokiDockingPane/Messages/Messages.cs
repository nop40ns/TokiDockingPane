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
