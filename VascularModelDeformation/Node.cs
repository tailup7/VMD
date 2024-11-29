using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VascularModelDeformation
{
    [Serializable]
    public class Node
    {
        public int Index { get; set; } 
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float XMoved { get; set; }
        public float YMoved { get; set; }
        public float ZMoved { get; set; }

        /// <summary>
        /// メッシュ変形に用いる移動する座標の総和を求めるための一時的な変数
        /// </summary>
        public float XMovedSum { get; set; } = 0.0f;
        public float YMovedSum { get; set; } = 0.0f;
        public float ZMovedSum { get; set; } = 0.0f;

        /// <summary>
        /// 中心線のNodeに対応するIndex
        /// </summary>
        public int CorrespondCenterlineIndex { get; set; } = -1;

        /// <summary>
        /// 複数のCorrespondIndexを保持するためのList
        /// CorrespondIndexは三角形パッチ（Cellと命名）に紐づいている
        /// これをNodeに対応させる場合、Nodeは複数のCellに対応しているので
        /// Nodeは複数のCorrespondIndexを保持する可能性がある
        /// </summary>
        public List<int> CorrespondIndexList { get; set; } = new List<int>();

        /// <summary>
        /// すでに移動したかを判定するための変数
        /// </summary>
        public int AlreadyMoved { get; set; } = 0;

        /// <summary>
        /// MeshMergeを作成する際にダブりがあるNodeをのぞくための変数
        /// デフォルトはfalseなので必要ない場合は変更しない
        /// 必要のある場合はtrueにする
        /// </summary>
        public bool Need { get; set; } = false;

        /// <summary>
        /// このNodeを含むEdge
        /// != このNodeを始点とするEdge
        /// </summary>
        private List<Edge> AroundEdge = new List<Edge>();

        /// <summary>
        /// このNodeを始点とするEdge
        /// != このNodeを含むEdge
        /// </summary>
        private List<Edge> AroundEdgeFromThisNode = new List<Edge>();

        /// <summary>
        /// 1リング近傍Node
        /// </summary>
        private HashSet<Node> OneRingNode = new HashSet<Node>();

        /// <summary>
        /// 2リング近傍Node
        /// </summary>
        private HashSet<Node> TwoRingNode = new HashSet<Node>();
        public float NearestDistance = 0.0f;

        /// <summary>
        /// constructor
        /// </summary>
        public Node()
        {
            //Debug.WriteLine($"This is Node constructor.");
        }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        public Node(float x, float y, float z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="index"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        public Node(int index, float x, float y, float z)
        {
            this.Index = index;         
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        // cf. https://www.mum-meblog.com/entry/tyr-utility/csharp-hashset 
        public override int GetHashCode()  
        {                                       
            int hash = 17;
            hash = hash * 23 + X.GetHashCode();
            hash = hash * 23 + Y.GetHashCode();
            hash = hash * 23 + Z.GetHashCode();
            return hash;
        }
        public override bool Equals(object obj) 
        {
            Node node = obj as Node; 
            if (node == null) 
                return false;
            return X == node.X && Y == node.Y && Z == node.Z; 
        }
        public string Print()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append($"({this.X}, {this.Y}, {this.Z})"); 
            return sb.ToString();
        }
    }
}
