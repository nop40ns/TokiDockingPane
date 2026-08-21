using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TokiDockingPane.Interfaces;

namespace TokiDockingPane.Models
{
    public class TokiDragDropPayload
    {
        /// <summary>
        /// ドラッグ元のノード（画面分割の起点）
        /// </summary>
        public IPaneNode SourceNode { get; set; }

        /// <summary>
        /// ドラッグされているタブ（ViewModel）のデータ実体
        /// </summary>
        public TabViewModel DraggedData { get; set; }

        public TokiDragDropPayload(IPaneNode sourceNode, TabViewModel draggedData)
        {
            SourceNode = sourceNode;
            DraggedData = draggedData;
        }
    }
}
