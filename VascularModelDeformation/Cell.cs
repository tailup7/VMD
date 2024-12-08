using System;
using System.Collections.Generic;
using System.Numerics;


namespace VascularModelDeformation
{
    [Serializable]
    public class Cell: IComparable<Cell>
    {
        public int Index { get; set; }
        /// <summary>
        /// gmshと同じように1-indexで格納されている
        /// Nodeにアクセスするときは-1する必要がある
        /// </summary>
        public int[] NodesIndex { get; set; }
        public CellType CellType { get; set; }
        public CellTypeVTK CellTypeVTK { get; set; }
        public int Dummy { get; set; } = 2;
        public int PhysicalID { get; set; } = -1;
        public int EntityID { get; set; } = -1;
        /// <summary>
        /// CorrespondIndexが-1のときは対応する中心線Nodeがない
        /// </summary>
        public int CorrespondIndex { get; set; } = -1;
        public bool Need { get; set; } = false;
        /// <summary>
        /// Cellを構成するEdge
        /// </summary>
        private List<Edge> Edges = new List<Edge>();

        public Cell()
        {
        }
        public Cell(int index)
        {
            this.Index = index;
        }
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="index"></param>
        /// <param name="index1"></param>
        /// <param name="index2"></param>
        /// <param name="index3"></param>
        /// <param name="correspondIndex"></param>
        public Cell(int index, int index1, int index2, int index3, int correspondIndex)
        {
            this.Index = index;
            this.NodesIndex = new int[] { index1, index2, index3 };
            this.CorrespondIndex = correspondIndex;
        }
        public Cell(int index, CellType cellType, int dummy, int physicalID, int entityID, int correspondIndex)
        {
            this.Index = index;
            this.CellType = cellType;
            this.Dummy = dummy;
            this.PhysicalID = physicalID;
            this.EntityID = entityID;
            this.CorrespondIndex = correspondIndex;
        }
        public Cell(int index, CellType cellType, int dummy, int physicalID, int entityID, int correspondIndex, int[] nodeIndex)
        {
            this.Index = index;
            this.CellType = cellType;
            this.Dummy = dummy;
            this.PhysicalID = physicalID;
            this.EntityID = entityID;
            this.CorrespondIndex = correspondIndex;
            this.NodesIndex = this.MakeNodesIndex(this.CellType, nodeIndex);
        }
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="edge1"></param>
        /// <param name="edge2"></param>
        /// <param name="edge3"></param>
        /// <param name="index"></param>
        public Cell(Edge edge1, Edge edge2, Edge edge3, int index)
        {
            this.SetEdge(edge1, edge2, edge3);
            this.Index = index;
        }
        public void CellTriangleReallocate(int index, int index1, int index2, int index3)  
        {
            this.Index = index;
            this.NodesIndex[0] = index1;
            this.NodesIndex[1] = index2;
            this.NodesIndex[2] = index3;
        }
        public void CellPrismReallocate(int index, int index1, int index2, int index3, int index4, int index5, int index6)  
        {
            this.Index = index;
            this.NodesIndex[0] = index1;
            this.NodesIndex[1] = index2;
            this.NodesIndex[2] = index3;
            this.NodesIndex[3] = index4;
            this.NodesIndex[4] = index5;
            this.NodesIndex[5] = index6;
        }

        private int[] MakeNodesIndex(CellType cellType, int[] nodeIndex)
        {
            if (cellType == CellType.Triangle)
            {
                int[] nodeIndexes = MakeCellTriangle(nodeIndex);
                return nodeIndexes;
            }
            if (cellType == CellType.Quadrilateral)
            {
                int[] nodeIndexes = MakeCellQuadrilateral(nodeIndex);
                return nodeIndexes;
            }
            if (cellType == CellType.Tetrahedron)
            {
                int[] nodeIndexes = MakeCellTetrahedron(nodeIndex);
                return nodeIndexes;
            }
            if (cellType == CellType.Prism)
            {
                int[] nodeIndexes = MakeCellPrism(nodeIndex);
                return nodeIndexes;
            }
            // ここに来ることはないはず
            // ここまで来たら例外を投げる
            throw new Exception("このプログラムでは認識できないCellTypeです" + Environment.NewLine + "Triangle Quadrilateral Tetrahedron Prismしか登録できません。");
        }
        private int[] MakeCellTriangle(int[] nodeIndex)
        {
            int[] res = new int[3] { nodeIndex[0], nodeIndex[1], nodeIndex[2] };
            return res;
        }
        private int[] MakeCellQuadrilateral(int[] nodeIndex)
        {
            int[] res = new int[4] { nodeIndex[0], nodeIndex[1], nodeIndex[2], nodeIndex[3] };
            return res;
        }
        private int[] MakeCellTetrahedron(int[] nodeIndex)
        {
            int[] res = new int[4] { nodeIndex[0], nodeIndex[1], nodeIndex[2], nodeIndex[3] };
            return res;
        }
        private int[] MakeCellPrism(int[] nodeIndex)
        {
            int[] res = new int[6] { nodeIndex[0], nodeIndex[1], nodeIndex[2], nodeIndex[3], nodeIndex[4], nodeIndex[5] };
            return res;
        }

        /// <summary>
        /// このCellに含まれるEdgeを取得する
        /// IEnumerable<Edge>
        /// </summary>
        public IEnumerable<Edge> GetEdge
        {
            get
            {
                if (this.Edges == null)
                    yield break;

                foreach (var edge in this.Edges)
                    yield return edge;
            }
        }
        /// <summary>
        /// このCellを構成するEdgeを取得する
        /// 戻り値はList<Edge>
        /// </summary>
        public List<Edge> GetEdges
        {
            get
            {
                return this.Edges;
            }
        }
        /// <summary>
        /// cellを3つのedgeから構成する
        /// 三角形Cellのみに適用できる
        /// </summary>
        /// <param name="edge1"></param>
        /// <param name="edge2"></param>
        /// <param name="edge3"></param>
        public void SetEdge(Edge edge1, Edge edge2, Edge edge3)
        {
            this.Edges.Clear();
            this.Edges.Add(edge1);
            this.Edges.Add(edge2);
            this.Edges.Add(edge3);
        }
        /// <summary>
        /// sortをするために必要
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public int CompareTo(Cell other)
        {
            int cellType = CellType.CompareTo(other.CellType);
            if (cellType != 0)
            {
                return cellType;
            }

            int physicalIDComparison = PhysicalID.CompareTo(other.PhysicalID);
            if (physicalIDComparison != 0)
            {
                return physicalIDComparison;
            }

            return EntityID.CompareTo(other.EntityID);
        }
    }

    [Serializable]
    public class CellVTK : Cell
    {
        /// <summary>
        /// cell type vtk format
        /// </summary>
        public CellTypeVTK CellTypeVTK { get; set; }
        // 自分の情報はどこから始まるか
        public int Offset { get; set; }
        /// <summary>
        /// VTKで出力したものに
        /// </summary>
        public Vector3 Centroid { get; set; }
        public Vector3 RealOuterCentroid { get; set; }
        /// <summary>
        /// velocity
        /// </summary>
        public Vector3 U { get; set; }
        /// <summary>
        /// correct velocity
        /// </summary>
        public Vector3 UVali { get; set; }
        public Vector3 UError { get; set; }
        public Vector3 URelativeError { get; set; }
        public float SmallRbyR { get; set; }
        public float Length { get; set; }
        /// <summary>
        /// pressure
        /// </summary>
        public float P { get; set; }

        // これ以降は実際には必要ない
        // ただ、デバッグのために残しておく
        /// <summary>
        /// 自分の一番外側のプリズムのインデックス
        /// </summary>
        public int ParentPrismIndex { get; set; }
    }
    /// <summary>
    /// gmshのCellType。
    /// コメントアウトしたものは, paraviewのCellType
    /// </summary>
    public enum CellType
    {
        Triangle = 2, // Triangle 5
        Quadrilateral = 3, // Quad 9
        Tetrahedron = 4, // Tetra 10
        Prism = 6 // Wedge 13
    }
    public enum CellTypeVTK
    {
        Triangle = 5, // Triangle 5
        Polygons = 7, // Polygons 7
        Quadrilateral = 9, // Quad 9
        Tetrahedron = 10, // Tetra 10
        Prism = 13 // Wedge 13
    }
    public enum CellTypeVTKHeaderCount
    {
        Triangle = 3 + 1,
        Polygons = 3 + 1,
        Quadrilateral = 4 + 1,
        Tetrahedron = 4 + 1,
        Prism = 6 + 1
    }
}
