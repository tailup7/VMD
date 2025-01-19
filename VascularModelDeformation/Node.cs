using System;
using System.Collections.Generic;
using System.Numerics;
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
        /// <summary>
        /// 2つのNodeが同じかどうか判定する
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj) 
        {
            Node node = obj as Node; 
            if (node == null) 
                return false;
            return X == node.X && Y == node.Y && Z == node.Z; 
        }
        /// <summary>
        /// 座標を出力する
        /// </summary>
        /// <returns></returns>
        public string Print()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append($"({this.X}, {this.Y}, {this.Z})"); 
            return sb.ToString();
        }
        /// <summary>
        /// このNode と引数のNode の距離を求める
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public float Distance(Node node)
        {
            float distance = (float)Math.Sqrt(Math.Pow(this.X - node.X, 2) + Math.Pow(this.Y - node.Y, 2) + Math.Pow(this.Z - node.Z, 2));
            return distance;
        }

        public Vector3 Difference(Node node)
        {
            Vector3 vec = new Vector3(this.X - node.X, this.Y - node.Y, this.Z - node.Z);
            return vec;
        }

        public IEnumerable<Edge> GetAroundEdge 
        {
            get
            {
                if (this.AroundEdge == null)
                    yield break;

                foreach (var edge in this.AroundEdge)
                    yield return edge;
            }
        }
        public IEnumerable<Edge> GetAroundEdgeFromThisNode
        {
            get
            {
                if (this.AroundEdgeFromThisNode == null)
                    yield break;

                foreach (var edge in this.AroundEdgeFromThisNode)
                    yield return edge;
            }
        }
        /// <summary>
        /// このEdgeを含むFace
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Cell> GetAroundCell
        {
            get
            {
                if (this.AroundEdge == null)
                    yield break;

                foreach (var edge in this.AroundEdge)
                    yield return edge.Cell;    
            }

        }
        public void AddAroundEdge(Edge edge)
        {
            if (!this.AroundEdge.Contains(edge))
                this.AroundEdge.Add(edge);
        }
        /// <summary>
        /// このNodeを含むEdgeを追加する
        /// </summary>
        /// <param name="edge"></param>
        public void AddAroundEdgeFromThisNode(Edge edge)
        {
            if (!this.AroundEdgeFromThisNode.Contains(edge))
                this.AroundEdgeFromThisNode.Add(edge);
        }
        public void ClearAroundEdgeFromThisNode()  
        {
            AroundEdgeFromThisNode.Clear();
        }
        public void RemoveAroundEdgeFromThisNode(Edge edge)
        {
            AroundEdgeFromThisNode.Remove(edge);
        }
        /// <summary>
        /// このNodeの1リング近傍Nodeを取得する
        /// </summary>
        public IEnumerable<Node> GetOneRingNode
        {
            get
            {
                if (this.AroundEdge == null)
                    yield break;

                foreach (var edge in this.AroundEdge)
                    yield return edge.End;
            }
        }
        /// <summary>
        /// このNodeの2リング近傍Nodeを取得する
        /// </summary>
        public IEnumerable<Node> GetTwoRingNode
        {
            get
            {
                if (this.AroundEdge == null)
                    yield break;

                foreach (var edge1 in this.AroundEdge)
                    foreach (var edge2 in edge1.End.AroundEdge)
                        yield return edge2.End;
            }
        }
    }

    /// <summary>
    /// NodeCenterline
    /// </summary>
    public class NodeCenterline : Node
    {
        public float XTangent { get; set; }
        public float YTangent { get; set; }
        public float ZTangent { get; set; }
        public float XMovedTangent { get; set; }
        public float YMovedTangent { get; set; }
        public float ZMovedTangent { get; set; }
        public float XTangentSmoothed { get; set; }
        public float YTangentSmoothed { get; set; }
        public float ZTangentSmoothed { get; set; }
        public float XMovedTangentSmoothed { get; set; }
        public float YMovedTangentSmoothed { get; set; }
        public float ZMovedTangentSmoothed { get; set; }
        public float[] Difference { get; set; } = new float[3] { 0.0f, 0.0f, 0.0f };
        public float[,] RodriguesMatrix { get; set; } = new float[3, 3];
        public float[,] RotationMatrix { get; set; } = new float[3, 3];

        /// <summary>
        /// constructor
        /// </summary>
        public NodeCenterline()
        {
            //Debug.WriteLine($"This is NodeCenterLine constructor.");
        }
        /// <summary>
        /// constructor
        /// </summary>
        public NodeCenterline(float x, float y, float z)
        {
            //Debug.WriteLine($"This is NodeCenterLine constructor.");
            this.X = x;
            this.Y = y;
            this.Z = z;
        }
    }
    public class NodeSurface : Node
    {
        public int CorrespondCenterlineIndices { get; set; }

        /// <summary>
        /// constructor
        /// </summary>
        public NodeSurface()
        {
            //Debug.WriteLine($"This is NodeSurface constructor.");
        }
    }
}
