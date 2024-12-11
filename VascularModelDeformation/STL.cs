using System;
using System.Collections.Generic;
using System.Drawing.Text;

namespace VascularModelDeformation
{
    public class STL
    {
        public List<Node> Nodes { get; set; } = new List<Node>();
        public List<Triangle> Triangles { get; set; } = new List<Triangle>();
        private Dictionary<Node, int> dictionaryNodeInt { get; set; } = new Dictionary<Node, int>();
        private HashSet<Node> hashSetNode { get; set; } = new HashSet<Node>();
        public List<Node> UniqueNodes { get; set; } = new List<Node>();

        /// <summary>
        /// constructor
        /// </summary>
        public STL()
        {
            //Debug.WriteLine("STL constructor");
        }
        /// <summary>
        /// triangleのリストを受け取って、STLを作成する
        /// これの何がいいかというと、STLのノードのリストを作成するときに、
        /// 頂点の重複をなくすことができる
        /// </summary>
        /// <param name="triangles"></param>
        public STL(List<Triangle> triangles)
        {
            /** ;
             * 1. 三角形の頂点をハッシュセットに追加する
             *  1.1 追加された場合は、Dictionryに何番目に追加されたかを保存
             *  1.2 追加されなかった場合は、Dictionaryによって何番目に追加されたかを取得
             * 2. 三角形の頂点のインデックスを三角形に保存する
             */
            foreach (var triangle in triangles)
            {
                int a, b, c = 0;
                if (hashSetNode.Add(triangle.N0))
                {
                    a = dictionaryNodeInt.Count;
                    Nodes.Add(triangle.N0);
                    dictionaryNodeInt.Add(triangle.N0, a);
                }
                else
                {
                    a = dictionaryNodeInt[triangle.N0];
                }
                if (hashSetNode.Add(triangle.N1))
                {
                    b = dictionaryNodeInt.Count;
                    Nodes.Add(triangle.N1);
                    dictionaryNodeInt.Add(triangle.N1, b);
                }
                else
                {
                    b = dictionaryNodeInt[triangle.N1];
                }
                if (hashSetNode.Add(triangle.N2))
                {
                    c = dictionaryNodeInt.Count;
                    Nodes.Add(triangle.N2);
                    dictionaryNodeInt.Add(triangle.N2, c);
                }
                else
                {
                    c = dictionaryNodeInt[triangle.N2];
                }

                triangle.NodeIndexes[0] = a;
                triangle.NodeIndexes[1] = b;
                triangle.NodeIndexes[2] = c;
                this.Triangles.Add(triangle);
            }
        }
        public STL(List<Triangle> triangles, int dummy)
        {
            HashSet<Node> nodes = new HashSet<Node>();
            foreach (var triangle in triangles)
            {
                if (nodes.Add(triangle.N0))
                {
                    Node node = new Node();
                    node.X = triangle.N0.X;
                    node.Y = triangle.N0.Y;
                    node.Z = triangle.N0.Z;
                    node.Index = UniqueNodes.Count;
                    UniqueNodes.Add(node);
                }
                if (nodes.Add(triangle.N1))
                {
                    Node node = new Node();
                    node.X = triangle.N1.X;
                    node.Y = triangle.N1.Y;
                    node.Z = triangle.N1.Z;
                    node.Index = UniqueNodes.Count;
                    UniqueNodes.Add(node);
                }
                if (nodes.Add(triangle.N2))
                {
                    Node node = new Node();
                    node.X = triangle.N2.X;
                    node.Y = triangle.N2.Y;
                    node.Z = triangle.N2.Z;
                    node.Index = UniqueNodes.Count;
                    UniqueNodes.Add(node);
                }
            }
        }
    }

    public class MostInnerSTL : STL
    {
        public MostInnerSTL()
        {
            //Debug.WriteLine($"MostInnerSTL() constructor");
        }
    }
}
