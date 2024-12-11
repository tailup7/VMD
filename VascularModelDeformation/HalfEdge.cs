using Microsoft.SqlServer.Server;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.VisualStyles;

namespace VascularModelDeformation
{
    public class HalfEdge
    {
        public static string FileExtension = ".heds";
        public List<Cell> CellList = new List<Cell>();
        public List<Edge> EdgeList = new List<Edge>();
        public List<Node> NodeList = new List<Node>();


        /// <summary>
        ///　HalfEdgeDataStructureを作成する
        /// </summary>
        /// <param name="mesh"></param>
        public void Create(Mesh mesh)
        {
            this.CreateHalfEdgeDataStructure(mesh);
            this.SetPairEdge(mesh.Nodes);
            //this.NotPairEdgeEndNodeAddPairForcely();
            if (!this.CheckPairEdge())
                throw new Exception("PairEdgeが正しく設定されていません");
            this.CheckCanEdgeSwap();
        }
        /// <summary>
        /// HalfEdgeDataStructureを作成する
        /// </summary>
        /// <param name="nodes"></param>
        /// <param name="cells"></param>
        /// <exception cref="Exception"></exception>
        public void Create(List<Node> nodes, List<Cell> cells)
        {
            this.CreateHalfEdgeDataStructure(nodes, cells);
            this.SetPairEdge(nodes);
            if (!this.CheckPairEdge())
                throw new Exception("PairEdgeが正しく設定されていません");
            this.CheckCanEdgeSwap();
        }
        /// <summary>
        /// EdgeList全体に対してEdgeSwapを実行する
        /// </summary>
        public void ExecuteEdgeSwap()
        {
            foreach (var edge in this.EdgeList)
            {
                if (edge.DoEdgeSwap)
                {
                    this.EdgeSwap(edge);
                    edge.DoEdgeSwap = false;
                    edge.Pair.DoEdgeSwap = false;
                }
            }

        }
        private void CreateHalfEdgeDataStructure(List<Node> nodes, List<Cell> cells)
        {
            foreach (var cell in cells)
            {
                if (cell.CellType == CellType.Triangle && cell.PhysicalID == 10)
                {

                    this.CreateCell(cell, nodes[cell.NodesIndex[0] - 1], nodes[cell.NodesIndex[1] - 1], nodes[cell.NodesIndex[2] - 1]);
                }
            }
        }
        /// <summary>
        /// HalfEdgeDataStructureを作成する
        /// </summary>
        /// <param name="mesh"></param>
        private void CreateHalfEdgeDataStructure(Mesh mesh)
        {
            foreach (var cell in mesh.Cells)
            {
                // PhysicalID == 10の三角形パッチのみを取得
                // PhysicalID == 10はWALLを表す
                if (cell.CellType == CellType.Triangle && cell.PhysicalID == 10)
                {
                    this.CreateCell(cell, mesh.Nodes[cell.NodesIndex[0] - 1], mesh.Nodes[cell.NodesIndex[1] - 1], mesh.Nodes[cell.NodesIndex[2] - 1]);
                }
            }
        }
        private void CreateCell(Cell cell, Node node1, Node node2, Node node3)
        {
            Edge edge1 = new Edge(node1, node2, cell, this.EdgeList.Count + 0);
            Edge edge2 = new Edge(node2, node3, cell, this.EdgeList.Count + 1);
            Edge edge3 = new Edge(node3, node1, cell, this.EdgeList.Count + 2);
            cell.SetEdge(edge1, edge2, edge3);

            edge1.Next = edge2;
            edge2.Next = edge3;
            edge3.Next = edge1;

            edge1.Prev = edge3;
            edge2.Prev = edge1;
            edge3.Prev = edge2;

            // そのNodeを始点とするEdgeを設定する
            node1.AddAroundEdgeFromThisNode(edge1);
            node2.AddAroundEdgeFromThisNode(edge2);
            node3.AddAroundEdgeFromThisNode(edge3);

            this.EdgeList.Add(edge1);
            this.EdgeList.Add(edge2);
            this.EdgeList.Add(edge3);
        }
        private void SetPairEdge(List<Node> nodes)
        {
            foreach (var node in nodes)
            {
                foreach (var edge in node.GetAroundEdgeFromThisNode)
                {
                    this.SetPairEdgeCore(edge);
                }
            }
        }
        private void SetPairEdgeCore(Edge edge)
        {
            Node start = edge.Start;
            Node end = edge.End;
            // 反対の頂点のエッジを取得する
            foreach (var pairPossibleEdge in end.GetAroundEdgeFromThisNode)
            {
                // 一致しない場合はスキップ
                if (pairPossibleEdge.Start != end || pairPossibleEdge.End != start)
                    continue;

                // ここの条件をパスしたということは
                // つまり、このエッジのペアであるということ
                // 一致したら、このエッジのペアを設定する
                edge.Pair = pairPossibleEdge;
                pairPossibleEdge.Pair = edge;
                break;
            }
        }
        private bool CheckPairEdge()
        {
            foreach (var edge in this.EdgeList)
            {
                // 境界にあるものはPairがnullであるので
                // この後の処理はスキップ
                if (Object.ReferenceEquals(edge.Pair, null))
                    continue;

                Edge pair = edge.Pair;
                if (pair != pair.Pair)
                    return false;
            }
            return true;
        }
        public void CheckCanEdgeSwap()
        {
            foreach (var edge in this.EdgeList)
            {
                if (edge.Pair is null)
                    continue;

                // ここでエッジスワップができるかどうかを判定する
                edge.DetermineEdgeSwap();
            }
        }
        public void EdgeSwapTrianglePrism(Edge edge, List<Node> nodes, List<List<Cell>> surfaceCellCorrespondPrismCells)
        {
            //Debug.WriteLine($"EdgeSwapTrianglePrism");
            //Debug.WriteLine($"EdgeSwap");
            Edge LT = null;
            Edge RT = null;
            Edge LB = null;
            Edge RB = null;
            if (!Utility.IsNull(edge.Prev.Pair))
                RT = edge.Next.Pair;
            if (!Utility.IsNull(edge.Next.Pair))
                LT = edge.Prev.Pair;
            if (!Utility.IsNull(edge.Pair))
            {
                LB = edge.Pair.Next.Pair;
                RB = edge.Pair.Prev.Pair;
            }

            edge.Start.RemoveAroundEdgeFromThisNode(edge);
            edge.Pair.Start.RemoveAroundEdgeFromThisNode(edge.Pair);

            // ここからSwap

            //Debug.WriteLine($"{}");

            // 一時保存
            Node node1 = edge.Start;
            Node node2 = edge.End;
            Node node3 = edge.Next.End;
            Node node4 = edge.Pair.Next.End;
            Triangle t1 = new Triangle(node1, node2, node3);
            Triangle t2 = new Triangle(node1, node4, node2);
            Triangle t3 = new Triangle(node3, node4, node2);
            Triangle t4 = new Triangle(node3, node1, node4);

            // ここでエッジスワップを行うかどうかを判定する
            // Cellのクオリティが両方のセルで改善する場合のみスワップを行う
            // 両方のセルで改善しない場合はスワップを行わない
            // 下の条件は「「両方のセルで改善する」の否定」で、この後の処理を行わずreturn
            // 値が小さいほどクオリティは良い
            if (!((t1.QualityAspectRatio > t3.QualityAspectRatio) && (t2.QualityAspectRatio > t4.QualityAspectRatio)))
                return;

            //Debug.WriteLine($"{edge.Cell.Index} {edge.Pair.Cell.Index}");
            //Debug.WriteLine($"{node1.Index} {node2.Index} {node3.Index} {node4.Index}");
            //Debug.WriteLine($"{t1.QualityAspectRatio}, {t2.QualityAspectRatio}");
            //Debug.WriteLine($"{t3.QualityAspectRatio}, {t4.QualityAspectRatio}");


            //Edge edge1 = edge;
            Edge edge2 = edge.Next;
            Edge edge3 = edge.Prev;
            //Edge edge4 = edge.Pair;
            Edge edge5 = edge.Pair.Next;
            Edge edge6 = edge.Pair.Prev;
            Cell cellTriangle1 = edge.Cell;
            Cell cellTriangle2 = edge.Pair.Cell;

            // 層の数
            int numberOfLayer = surfaceCellCorrespondPrismCells[0].Count - 1;
            int test1 = -1; // surfaceCellCorrespondPrismCellsにおいてCell1に該当するIndex
            int test2 = -1; // surfaceCellCorrespondPrismCellsにおいてCell2に該当するIndex
            for (int i = 0; i < surfaceCellCorrespondPrismCells.Count; i++)
            {
                if (surfaceCellCorrespondPrismCells[i][0].Index == cellTriangle1.Index)
                {
                    test1 = i;
                    int a = surfaceCellCorrespondPrismCells[i][0].NodesIndex[0];
                    int b = surfaceCellCorrespondPrismCells[i][0].NodesIndex[1];
                    int c = surfaceCellCorrespondPrismCells[i][0].NodesIndex[2];
                    //Debug.WriteLine($"{a} {b} {c}");
                }
            }
            for (int i = 0; i < surfaceCellCorrespondPrismCells.Count; i++)
            {
                if (surfaceCellCorrespondPrismCells[i][0].Index == cellTriangle2.Index)
                {
                    test2 = i;
                    int a = surfaceCellCorrespondPrismCells[i][0].NodesIndex[0];
                    int b = surfaceCellCorrespondPrismCells[i][0].NodesIndex[1];
                    int c = surfaceCellCorrespondPrismCells[i][0].NodesIndex[2];
                    //Debug.WriteLine($"{a} {b} {c}");
                }
            }
            //Debug.WriteLine($"============================");
            for (int k = 1; k < numberOfLayer + 1; k++)
            {
                Cell cellPrismA = surfaceCellCorrespondPrismCells[test1][k];
                Cell cellPrismB = surfaceCellCorrespondPrismCells[test2][k];

                //// まず共有する辺を検索
                //Debug.WriteLine($"{cellPrismA.Index} {cellPrismB.Index}");
                //Debug.WriteLine($"{cellPrismA.NodesIndex[0]} {cellPrismA.NodesIndex[1]} {cellPrismA.NodesIndex[2]} {cellPrismA.NodesIndex[3]} {cellPrismA.NodesIndex[4]} {cellPrismA.NodesIndex[5]}");
                //Debug.WriteLine($"{cellPrismB.NodesIndex[0]} {cellPrismB.NodesIndex[1]} {cellPrismB.NodesIndex[2]} {cellPrismB.NodesIndex[3]} {cellPrismB.NodesIndex[4]} {cellPrismB.NodesIndex[5]}");

                bool[] flagA = new bool[] { false, false, false };
                bool[] flagB = new bool[] { false, false, false };
                int flagAIndex = -1;
                int flagBIndex = -1;
                for (int i = 0; i < 3; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        if ((cellPrismA.NodesIndex[i] - 1) == (cellPrismB.NodesIndex[j] - 1))
                        {
                            flagA[i] = true;
                            flagB[j] = true;
                        }
                    }
                }
                for (int i = 0; i < 3; i++)
                {
                    if (!flagA[i])
                        flagAIndex = i;
                    if (!flagB[i])
                        flagBIndex = i;
                }
                //Debug.WriteLine($"{flagA[0]} {flagA[1]} {flagA[2]}");
                //Debug.WriteLine($"{flagB[0]} {flagB[1]} {flagB[2]}");
                //Debug.WriteLine($"{flagAIndex} {flagBIndex}");

                int A0 = cellPrismA.NodesIndex[(flagAIndex + 0) % 3];
                int A1 = cellPrismA.NodesIndex[(flagAIndex + 1) % 3];
                int A2 = cellPrismA.NodesIndex[(flagAIndex + 2) % 3];
                int A3 = cellPrismA.NodesIndex[(flagAIndex + 0) % 3 + 3];
                int A4 = cellPrismA.NodesIndex[(flagAIndex + 1) % 3 + 3];
                int A5 = cellPrismA.NodesIndex[(flagAIndex + 2) % 3 + 3];
                int B0 = cellPrismB.NodesIndex[(flagBIndex + 0) % 3];
                int B1 = cellPrismB.NodesIndex[(flagBIndex + 1) % 3];
                int B2 = cellPrismB.NodesIndex[(flagBIndex + 2) % 3];
                int B3 = cellPrismB.NodesIndex[(flagBIndex + 0) % 3 + 3];
                int B4 = cellPrismB.NodesIndex[(flagBIndex + 1) % 3 + 3];
                int B5 = cellPrismB.NodesIndex[(flagBIndex + 2) % 3 + 3];

                if ((A2 != B1) || (A5 != B4) || (A1 != B2) || (A4 != B5))
                {
                    return;
                    //Debug.WriteLine($"-----------------------------------");
                    //Debug.WriteLine($"{cellPrismA.Index} {cellPrismB.Index}");
                    //throw new Exception($"{edge.Index}において{cellPrismA.Index}の{cellPrismB.Index}の接合部がうまく行かないようです。");
                }

                if ((flagAIndex != -1) && (flagBIndex != -1))
                {
                    cellPrismA.CellPrismReallocate(
                        cellPrismA.Index,
                        A0,
                        B0,
                        A2,
                        A3,
                        B3,
                        A5
                    );
                    cellPrismB.CellPrismReallocate(
                        cellPrismB.Index,
                        B0,
                        A0,
                        B2,
                        B3,
                        A3,
                        B5
                    );
                    // もとに戻す
                    surfaceCellCorrespondPrismCells[test1][k] = cellPrismA;
                    surfaceCellCorrespondPrismCells[test2][k] = cellPrismB;
                }
                else
                {
                    throw new Exception($"prismのエッジフリップがうまくいかないよ");
                }
            }

            edge.Start = node3;
            edge.End = node4;
            edge.Pair.Start = node4;
            edge.Pair.End = node3;

            edge.Next = edge6;
            edge.Prev = edge2;
            edge.Pair.Next = edge3;
            edge.Pair.Prev = edge5;

            edge2.Next = edge;
            edge6.Prev = edge;
            edge3.Prev = edge.Pair;
            edge5.Next = edge.Pair;

            edge.Start.AddAroundEdgeFromThisNode(edge);
            edge.Pair.Start.AddAroundEdgeFromThisNode(edge.Pair);

            // nodeは0index
            // cellのIndexは1index
            // なので+1する
            cellTriangle1.CellTriangleReallocate(cellTriangle1.Index, (edge.Start.Index + 1), (edge.End.Index + 1), (edge.Next.End.Index + 1));
            cellTriangle2.CellTriangleReallocate(cellTriangle2.Index, (edge.Pair.Start.Index + 1), (edge.Pair.End.Index + 1), (edge.Pair.Next.End.Index + 1));

            edge.ChangeCell(cellTriangle1);
            edge.Next.ChangeCell(cellTriangle1);
            edge.Prev.ChangeCell(cellTriangle1);

            edge.Pair.ChangeCell(cellTriangle2);
            edge.Pair.Next.ChangeCell(cellTriangle2);
            edge.Pair.Prev.ChangeCell(cellTriangle2);
        }
        public void EdgeSwap(Edge edge)
        {
            Debug.WriteLine($"EdgeSwap");
            Edge LT = null;
            Edge RT = null;
            Edge LB = null;
            Edge RB = null;
            if (!Utility.IsNull(edge.Prev.Pair))
                RT = edge.Next.Pair;
            if (!Utility.IsNull(edge.Next.Pair))
                LT = edge.Prev.Pair;
            if (!Utility.IsNull(edge.Pair))
            {
                LB = edge.Pair.Next.Pair;
                RB = edge.Pair.Prev.Pair;
            }

            edge.Start.RemoveAroundEdgeFromThisNode(edge);
            edge.Pair.Start.RemoveAroundEdgeFromThisNode(edge.Pair);

            // ここからSwap

            // 一時保存
            Node node1 = edge.Start;
            Node node2 = edge.End;
            Node node3 = edge.Next.End;
            Node node4 = edge.Pair.Next.End;
            Debug.WriteLine($"{node1.Index} {node2.Index} {node3.Index} {node4.Index}");
            //Edge edge1 = edge;
            Edge edge2 = edge.Next;
            Edge edge3 = edge.Prev;
            //Edge edge4 = edge.Pair;
            Edge edge5 = edge.Pair.Next;
            Edge edge6 = edge.Pair.Prev;
            Cell cell1 = edge.Cell;
            Cell cell2 = edge.Pair.Cell;

            edge.Start = node3;
            edge.End = node4;
            edge.Pair.Start = node4;
            edge.Pair.End = node3;

            edge.Next = edge6;
            edge.Prev = edge2;
            edge.Pair.Next = edge3;
            edge.Pair.Prev = edge5;

            edge2.Next = edge;
            edge6.Prev = edge;
            edge3.Prev = edge.Pair;
            edge5.Next = edge.Pair;

            edge.Start.AddAroundEdgeFromThisNode(edge);
            edge.Pair.Start.AddAroundEdgeFromThisNode(edge.Pair);

            // nodeは0index
            // cellのIndexは1index
            // なので+1する
            cell1.CellTriangleReallocate(cell1.Index, (edge.Start.Index + 1), (edge.End.Index + 1), (edge.Next.End.Index + 1));
            cell2.CellTriangleReallocate(cell2.Index, (edge.Pair.Start.Index + 1), (edge.Pair.End.Index + 1), (edge.Pair.Next.End.Index + 1));

            edge.ChangeCell(cell1);
            edge.Next.ChangeCell(cell1);
            edge.Prev.ChangeCell(cell1);
            edge.Pair.ChangeCell(cell2);
            edge.Pair.Next.ChangeCell(cell2);
            edge.Pair.Prev.ChangeCell(cell2);
        }
        public void Write(string dirPath)
        {
            string fileName = "HalfEdgeDataStructure" + FileExtension;
            this.Write(dirPath, fileName);
        }
        public void Write(string dirPath, string fileName)
        {
            string index = "index";
            string start = "start";
            string end = "end";
            string prev = "prev";
            string next = "next";
            string pair = "pair";
            string quality = "quality";
            string swap = "swap";
            string cell = "cell";
            Debug.WriteLine($"HalfEdge.Write()");
            string filePath1 = Path.Combine(dirPath, (fileName + 1.ToString()));
            string filePath2 = Path.Combine(dirPath, (fileName + 2.ToString()));
            Debug.WriteLine($"filePath: {filePath1}");
            Debug.WriteLine($"filePath: {filePath2}");
            using (StreamWriter sw = new StreamWriter(filePath1))
            {
                sw.WriteLine($"Edge");
                sw.WriteLine($"{index,5} {start,5} {end,5} {prev,5} {next,5} {cell,5} {pair,5} {quality,5} {swap,5}");
                foreach (var edge in this.EdgeList)
                {
                    if (Utility.IsNull(edge.Pair))
                    {
                        sw.WriteLine($"{edge.Index,5} {edge.Start.Index,5} {edge.End.Index,5} {edge.Prev.Index,5} {edge.Next.Index,5} {edge.Cell.Index,5} {-1,5} {edge.DoEdgeSwap,5} ");
                        //Console.WriteLine($"{edge.Index, 5} {edge.Start.Index, 5} {edge.End.Index, 5} {edge.Prev.Index, 5} {edge.Next.Index, 5} {edge.Cell.Index, 5} {edge.DoEdgeSwap,5} {-1, 5}");
                    }
                    else
                    {
                        Triangle triangle = new Triangle(edge.Start, edge.End, edge.Next.End);
                        sw.WriteLine($"{edge.Index,5} {edge.Start.Index,5} {edge.End.Index,5} {edge.Prev.Index,5} {edge.Next.Index,5} {edge.Cell.Index,5} {edge.Pair.Index,5} {triangle.QualityAspectRatio,5} {edge.DoEdgeSwap,5}");
                        //Console.WriteLine($"{edge.Index, 5} {edge.Start.Index, 5} {edge.End.Index, 5} {edge.Prev.Index, 5} {edge.Next.Index, 5} {edge.Cell.Index, 5} {edge.Pair.Index, 5} {edge.DoEdgeSwap,5} {triangle.QualityAspectRatio}");
                    }
                }
            }
            this.ExecuteEdgeSwap();
            using (StreamWriter sw = new StreamWriter(filePath2))
            {
                sw.WriteLine($"Edge");
                //EdgeSwap(this.EdgeList[32]);
                sw.WriteLine($"{index,5} {start,5} {end,5} {prev,5} {next,5} {cell,5} {pair,5} {quality,5}");
                foreach (var edge in this.EdgeList)
                {
                    if (Utility.IsNull(edge.Pair))
                    {
                        sw.WriteLine($"{edge.Index,5} {edge.Start.Index,5} {edge.End.Index,5} {edge.Prev.Index,5} {edge.Next.Index,5} {edge.Cell.Index,5} {-1,5} {edge.DoEdgeSwap,5} ");
                        //Console.WriteLine($"{edge.Index, 5} {edge.Start.Index, 5} {edge.End.Index, 5} {edge.Prev.Index, 5} {edge.Next.Index, 5} {edge.Cell.Index, 5} {edge.DoEdgeSwap,5} {-1, 5}");
                    }
                    else
                    {
                        Triangle triangle = new Triangle(edge.Start, edge.End, edge.Next.End);
                        sw.WriteLine($"{edge.Index,5} {edge.Start.Index,5} {edge.End.Index,5} {edge.Prev.Index,5} {edge.Next.Index,5} {edge.Cell.Index,5} {edge.Pair.Index,5} {triangle.QualityAspectRatio,5} {edge.DoEdgeSwap,5}");
                        //Console.WriteLine($"{edge.Index, 5} {edge.Start.Index, 5} {edge.End.Index, 5} {edge.Prev.Index, 5} {edge.Next.Index, 5} {edge.Cell.Index, 5} {edge.Pair.Index, 5} {edge.DoEdgeSwap,5} {triangle.QualityAspectRatio}");
                    }
                }
            }
        }

        private void NotPairEdgeEndNodeAddPairForcely()
        {
            var edgeListCopy = new List<Edge>(this.EdgeList);
            foreach (var edge in edgeListCopy)
            {
                // ペアである場合はスキップ
                if (edge.Pair != null)
                    continue;

                Edge edgeNew = new Edge(edge.End, edge.Start, edge.Cell, this.EdgeList.Count);
                edgeNew.Pair = edge;
                edge.Pair = edgeNew;

                // ここは循環参照になる可能性がある
                edgeNew.Prev = edgeNew;
                edgeNew.Next = edgeNew;
                // ここも応急処置的
                // これだと右手側にあるCellになる
                edgeNew.Cell = edge.Cell;

                this.EdgeList.Add(edgeNew);

                // これが実際の目的
                // これをすることでnode.GetAroundNodeでedgeでつながっているNodeが探れるようになる
                edge.End.AddAroundEdge(edgeNew);
            }
        }
    }
}
