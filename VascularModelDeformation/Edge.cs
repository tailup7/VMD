using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
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
        /// <summary>
        /// Edgeの左手側にあるCellを変更
        /// </summary>
        /// <param name="cell"></param>
        public void ChangeCell(Cell cell)
        {
            this.Cell = cell;
        }
        /// <summary>
        /// このEdgeをSwapするか判定
        /// </summary>
        public void DetermineEdgeSwap()
        {
            if (this.Pair is null)
                return;

            /** 図解
             *                     n2
             *                  /  |  \
             *                /    |    \
             *              /      |     \
             *            /        |      \
             *          /          |       \
             * v5↗ v1↙/          h |        \↖v4
             *       /             |        \
             *      n3 θ1 main    |↓       θ2 n4
             *       \             |        /
             *     v2↘\            |       /↗v3 ↙v6
             *          \          |     /
             *            \        |    /
             *              \      |   /
             *               \     |  /
             *                \    | /
             *                    n1
             */

            Node node1 = this.Start; 
            Node node2 = this.End;
            Node node3 = this.Next.End; 
            Node node4 = this.Pair.Next.End; 

            Vector3 v1 = new Vector3(node3.X - node2.X, node3.Y - node2.Y, node3.Z - node2.Z);
            Vector3 v2 = new Vector3(node1.X - node3.X, node1.Y - node3.Y, node1.Z - node3.Z);
            Vector3 v3 = new Vector3(node4.X - node1.X, node4.Y - node1.Y, node4.Z - node1.Z);
            Vector3 v4 = new Vector3(node2.X - node4.X, node2.Y - node4.Y, node2.Z - node4.Z);

            Vector3 v5 = -1.0f * v1;
            Vector3 v6 = -1.0f * v3;

            // calculate the angle between v2 and v5
            float theta1 = (float)Math.Acos(Vector3.Dot(v2, v5) / (v2.Length() * v5.Length()));
            // calculate the angle between v4 and v6
            float theta2 = (float)Math.Acos(Vector3.Dot(v4, v6) / (v4.Length() * v6.Length()));

            if (((theta1 * 180.0 / Math.PI) > (100.0f)) && ((theta1 * 180.0 / Math.PI) > (100.0f)))
            {
                //Debug.WriteLine($"=======================");
                //Debug.WriteLine($"{this.Index, 5} {this.Start.Index} {this.End.Index} {this.Cell.Index}");
                //Debug.WriteLine($"{this.Index,5} (theta1, theta2) = ({(theta1 * 180.0 / Math.PI).ToString("F1"),5}, {(theta2 * 180.0 / Math.PI).ToString("F1"),5})");
                this.DoEdgeSwap = true;
            }
        }
        /// <summary>
        /// Edgeが同一か判定
        /// </summary>
        /// <param name="edge1"></param>
        /// <param name="edge2"></param>
        /// <returns></returns>
        public static bool operator ==(Edge edge1, Edge edge2)
        {
            if (object.ReferenceEquals(edge1, edge2))
                return true;
            if (edge1.Start == edge2.End && edge1.End == edge2.Start)
                return true;

            // 上記2つに当てはまらなかったら確実にfalse?
            //if ((object)edge1 == null || (object)edge2 == null)
            //return false;
            return false;
        }
        /// <summary>
        /// Edgeが同一ではないことの判定
        /// </summary>
        /// <param name="edg1"></param>
        /// <param name="edge2"></param>
        /// <returns></returns>
        public static bool operator !=(Edge edg1, Edge edge2)
        {
            return !(edg1 == edge2);
        }
    }
}
