using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VascularModelDeformation
{
    public class Edge
    {
        public int Index { get; set; }
        /// <summary>
        /// Start Node
        /// </summary>
        public Node Start { get; set; }
        /// <summary>
        /// End Node
        /// </summary>
        public Node End { get; set; }
        /// <summary>
        /// Edge の左手側にあるCell
        /// </summary>
        public Cell Cell { get; set; }
        public Edge Next { get; set; } 
        public Edge Prev { get; set; }
        public Edge Pair { get; set; }
        public bool DoEdgeSwap { get; set; } = false;

        /// <summary>
        /// constructor
        /// </summary>
        public Edge()
        { 
        }
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="cell"></param>
        /// <param name="index"></param>
        public Edge(Node start, Node end, Cell cell, int index) 
        {                                  
            this.Start = start;            
            this.End = end;
            this.Cell = cell;
            this.Index = index;
        }
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="index"></param>
        public Edge(Node start, Node end, int index)
        {
            this.Start = start;
            this.End = end;
            this.Index = index;
        }
    }
}
