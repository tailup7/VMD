using KdTree; // for kdtree
using KdTree.Math;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;

namespace VascularModelDeformation
{
    public static class Algorithm
    {
        public static void HausdorffDistance(List<Node> aa, List<Node> bb)
        {
            Debug.WriteLine("start HausdorffDistance");
            float ave = 0.0f;
            float max = float.MinValue;
            float min = float.MaxValue;

            List<Node> uniqueNodeList1 = aa;
            List<Node> uniqueNodeList2 = bb;
            var indexPair = new Dictionary<int, int>();
            var tree = new KdTree<float, int>(3, new FloatMath());
            // KDTreeへの登録
            foreach (var node1 in uniqueNodeList1)
            {
                tree.Add(new[] { node1.X, node1.Y, node1.Z }, node1.Index);
            }
            // KDTree内での検索
            foreach (var node2 in uniqueNodeList2)
            {
                if (node2.Index % 1000 == 0)
                {
                    Debug.WriteLine($"{node2.Index}");
                }
                var n = tree.GetNearestNeighbours(new[] { node2.X, node2.Y, node2.Z }, 1);
                indexPair.Add(node2.Index, n[0].Value);
            }
            // calculate each most near distance
            foreach (var node2 in uniqueNodeList2)
            {
                //Node nearestNode = uniqueNodeList2.First(n => n.Index)
                Node nearestInNode1 = uniqueNodeList1[indexPair[node2.Index]];
                Vector3 vector = node2.Difference(nearestInNode1);
                float distance = vector.Length();
                node2.NearestDistance = distance;
                if (distance > max)
                    max = distance;
                if (distance < min)
                    min = distance;
                ave += distance;
            }
            ave /= uniqueNodeList2.Count;

            Debug.WriteLine($"ave = {ave}");
            Debug.WriteLine($"max = {max}");
            Debug.WriteLine($"min = {min}");

            Debug.WriteLine("finish HausdorffDistance");
        }
        /// <summary>
        /// KDtreeを用いてPrimsLayerMeshの最も内側表面のNodeとMeshInnerの表面のNodeの対応を求める
        /// ぴったり重なり合うことを前提としており、
        ///   つまり、MeshInnerはPrismLayerMeshの最も内側表面のSTLを参照して作っている
        /// ぴったり重なり合わない場合は、例外を返す
        /// ぴったり重なりあうので、それぞれの最近傍Nodeを求めることで対応を求める
        /// </summary>
        /// <param name="mesh1"></param>
        /// <param name="mesh2"></param>
        /// <returns></returns>
        public static Dictionary<int, int> KDTree(Mesh mesh1, MeshInner mesh2)
        {
            int numberOfMeshMostInnserPrismLayer = mesh1.NumberOfMostInnerPrismLayerCells;
            int numberOfWallCells = mesh2.NumberOfWallCells;
            if (numberOfMeshMostInnserPrismLayer != numberOfWallCells)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append($"二つのメッシュの接合部のNodeの数が一致していません！！！").Append(Environment.NewLine);
                sb.Append($"プリズム層の最も内側の表面のNode数:{numberOfMeshMostInnserPrismLayer}").Append(Environment.NewLine);
                sb.Append($"MeshInnerの表面のNode数:{numberOfWallCells}").Append(Environment.NewLine);
                throw new Exception($"{sb.ToString()}");
            }

            Debug.WriteLine($"{numberOfWallCells}");

            List<Node> uniqueNodesList1 = new List<Node>();
            List<Node> uniqueNodesList2 = new List<Node>();
            HashSet<Node> nodesHashSet1 = new HashSet<Node>();
            HashSet<Node> nodesHashSet2 = new HashSet<Node>();
            for (int i = 0; i < numberOfMeshMostInnserPrismLayer; i++)
            {
                int a = mesh1.CellsMostInnerPrismLayer[i].NodesIndex[0];
                int b = mesh1.CellsMostInnerPrismLayer[i].NodesIndex[1];
                int c = mesh1.CellsMostInnerPrismLayer[i].NodesIndex[2];
                int d = mesh1.CellsMostInnerPrismLayer[i].NodesIndex[3];
                int e = mesh1.CellsMostInnerPrismLayer[i].NodesIndex[4];
                int f = mesh1.CellsMostInnerPrismLayer[i].NodesIndex[5];
                Node node1 = mesh1.Nodes[d - 1];
                Node node2 = mesh1.Nodes[e - 1];
                Node node3 = mesh1.Nodes[f - 1];
                /* 点の追加アルゴリズム
                 * nodeHashSet1に追加する際に、追加できたかどうかがboolで返ってくる
                 * trueの時は追加できたので、uniqueNodesList1にも追加する
                 * falseの時は追加できなかったので、uniqueNodesList1には追加しない
                 */
                if (nodesHashSet1.Add(node1))
                {
                    uniqueNodesList1.Add(node1);
                }
                if (nodesHashSet1.Add(node2))
                {
                    uniqueNodesList1.Add(node2);
                }
                if (nodesHashSet1.Add(node3))
                {
                    uniqueNodesList1.Add(node3);
                }

            }
            for (int i = 0; i < numberOfWallCells; i++)
            {
                int a = mesh2.CellsWall[i].NodesIndex[0];
                int b = mesh2.CellsWall[i].NodesIndex[1];
                int c = mesh2.CellsWall[i].NodesIndex[2];
                Node node1 = mesh2.Nodes[a - 1];
                Node node2 = mesh2.Nodes[b - 1];
                Node node3 = mesh2.Nodes[c - 1];
                /* 点の追加アルゴリズム
                 * nodeHashSet2に追加する際に、追加できたかどうかがboolで返ってくる
                 * trueの時は追加できたので、uniqueNodesList2にも追加する
                 * falseの時は追加できなかったので、uniqueNodesList2には追加しない
                 */
                if (nodesHashSet2.Add(node1))
                {
                    uniqueNodesList2.Add(node1);
                }
                if (nodesHashSet2.Add(node2))
                {
                    uniqueNodesList2.Add(node2);
                }
                if (nodesHashSet2.Add(node3))
                {
                    uniqueNodesList2.Add(node3);
                }
            }
            Debug.WriteLine($"nodesHashSet1 number is {nodesHashSet1.Count}");
            Debug.WriteLine($"nodesHashSet2 number is {nodesHashSet2.Count}");
            Debug.WriteLine($"uniqueNodesList1 number is {uniqueNodesList1.Count}");
            Debug.WriteLine($"uniqueNodesList2 number is {uniqueNodesList2.Count}");
            if (uniqueNodesList1.Count != uniqueNodesList2.Count)
            {
                Debug.WriteLine($"KDTree make error!");
            }

            var indexPair = new Dictionary<int, int>();
            var tree = new KdTree<float, int>(3, new FloatMath());
            // KDTreeへの登録
            foreach (var node in uniqueNodesList1)
            {
                float x = node.X;
                float y = node.Y;
                float z = node.Z;
                tree.Add(new[] { x, y, z }, node.Index);
            }
            // KDTree内での検索
            foreach (var node in uniqueNodesList2)
            {
                float x = node.X;
                float y = node.Y;
                float z = node.Z;
                var n = tree.GetNearestNeighbours(new[] { x, y, z }, 1);
                // ここで1-indexに戻す
                indexPair.Add(node.Index + 1, n[0].Value + 1);
            }
            return indexPair;
        }
        /// <summary>
        /// 中心線Nodeと血管内腔曲面Faceの対応を求める
        /// </summary>
        /// <param name="nodesCenterline"></param>
        /// <param name="triangles"></param>
        public static void CorrespondenceBetweenCenterlineNodeAndLumenalSurfaceTriangle(List<NodeCenterline> nodesCenterline, List<Triangle> triangles)
        {
            foreach (var triangle in triangles)
            {
                int index = -1;
                float minDistance = float.MaxValue;
                Vector3 vec = new Vector3(0.0f, 0.0f, 0.0f);
                for (int i = 0; i < nodesCenterline.Count; i++)
                {
                    Node node = nodesCenterline[i];
                    float distance = triangle.Center.Distance(node);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        index = i;
                        vec = node.Difference(triangle.Center);
                    }
                }
                triangle.CorrespondCenterlineIndex = index;
                triangle.CorrespondTriangleVector = vec;
            }
        }
        /// <summary>
        /// 中心線Nodeと血管内宮表面Nodeの対応を求める
        /// 多分使わない
        /// </summary>
        /// <param name="nodesCenterline"></param>
        /// <param name="stl"></param>
        public static void CorrespondenceBetweenCenterlineNodeAndLumenalSurfaceNode(List<Node> nodesCenterline, STL stl)
        {
            foreach (var node in stl.Nodes)
            {
                int index = -1;
                float minDistance = float.MaxValue;
                for (int i = 0; i < nodesCenterline.Count; i++)
                {
                    Node nC = nodesCenterline[i];
                    float distance = node.Distance(nC);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        index = i;
                    }
                }
                node.CorrespondCenterlineIndex = index;
                ;
            }
        }
    }
}

