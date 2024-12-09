using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace VascularModelDeformation
{
    [Serializable]
    public class Mesh
    {
        public List<Node> Nodes { get; set; }
        public List<Node> NodesNeed { get; set; }
        public List<Cell> Cells { get; set; }   
        public List<Cell> CellsNeed { get; set; }
        private Dictionary<int, int> NodeIndexCorrespond { get; set; }
        public Dictionary<int, int> InletEntityIDCorrespond { get; set; }
        public Dictionary<int, int> OutletEntityIDCorrespond { get; set; }
        public List<PhysicalInfo> PhysicalInfos { get; set; }
        public List<EntityInfo> EntityInfos { get; set; }
        public HashSet<int> SOMETHINGEntityIDTriangleHashSet { get; set; }
        public HashSet<int> SOMETHINGEntityIDQuadrilateralHashSet { get; set; }
        public List<HashSet<int>> SomethingEntityIDTriangleNodesHashSetList { get; set; }
        public List<HashSet<int>> SomethingEntityIDQuadrilateralNodesHashSetList { get; set; }
        public virtual List<List<Cell>> CellsEachPrismLayer { get; set; }
        public virtual List<Cell> CellsPrismLayer { get; set; }
        public virtual List<Cell> CellsMostInnerPrismLayer { get; set; }
        public virtual List<Cell> CellsTetra { get; set; }
        public virtual List<Cell> CellsWall { get; set; }
        public virtual List<Cell> CellsInnerWall { get; set; }
        public virtual List<Cell> CellsInletQuadrilateral { get; set; }
        public virtual List<Cell> CellsOutletQuadrilateral { get; set; }
        public virtual int NumberOfPrismLayerCells { get; set; }
        public virtual int NumberOfMostInnerPrismLayerCells { get; set; }
        public virtual int NumberOfInnerWallCells { get; set; }
        public virtual int NumberOfInletQuadrilateralCells { get; set; }
        public virtual int NumberOfOutletQuadrilateralCells { get; set; }
        public virtual int NumberOfLayer { get; set; }
        public List<List<Cell>> SurfaceCellCorrespondPrismCells { get; set; } 
        public int NumberOfTetrahedronCells { get; set; }
        public int NumberOfPrismCells { get; set; }
        public int NumberOfWallCells { get; set; }

        /// <summary>
        /// constructor 
        /// </summary>
        public Mesh()
        {
            Debug.WriteLine($"Mesh() constructor");
        }

        /// <summary>
        /// constructor for Mesh class
        /// </summary>
        /// <param name="lines"></param>
        public Mesh(string[] lines)
        {
            Debug.WriteLine($"Mesh(string[] lines) constructor");
            // 素の.mshファイルに関して取得できる情報を登録---------------------------------------------------
            try
            {
                LoadMesh(lines);
            }
            catch (Exception e)
            {
                Debug.WriteLine($"{e.Message}");
                Environment.Exit(0);
            }
            // 元の.mshファイルに関して取得できる情報を登録終了---------------------------------------------------

            // meshで必要な部分を決定する
            //SearchMesh();

            //AnalyzeMesh();
        }
        public Mesh(List<Node> nodes, List<Cell> cells)
        {
            this.Nodes = new List<Node>();
            this.Nodes = nodes;
            this.Cells = new List<Cell>();
            this.Cells = cells;
        }
        /// <summary>
        /// deepcopyするための関数
        /// https://programming.pc-note.net/csharp/copy.html
        /// </summary>
        /// <returns></returns>
        public Mesh DeepCopy()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(ms, this);
                ms.Position = 0;
                return (Mesh)bf.Deserialize(ms);
            }
        }
        public MeshSurface MakeOuterSurface(Mesh meshIn)
        {
            MeshSurface meshOut = new MeshSurface();
            meshOut.Nodes = meshIn.Nodes;
            meshOut.Cells = new List<Cell>();
            meshOut.EntityInfos = meshIn.EntityInfos;
            meshOut.PhysicalInfos = meshIn.PhysicalInfos;
            meshOut.TriangleList = new List<Triangle>();
            for (int i = 0; i < meshIn.Cells.Count; i++)
            {
                if (meshIn.Cells[i].CellType == CellType.Triangle && meshIn.Cells[i].PhysicalID == 10)
                {
                    meshOut.Cells.Add(meshIn.Cells[i]);
                }
            }
            return meshOut;
        }
        public MeshSurface MakeInnerSurfaceMesh(Mesh meshIn)
        {
            MeshSurface meshOut = new MeshSurface();
            meshOut.Nodes = meshIn.Nodes;
            meshOut.Cells = meshIn.Cells;
            meshOut.EntityInfos = meshIn.EntityInfos;
            meshOut.PhysicalInfos = meshIn.PhysicalInfos;
            meshOut.NumberOfLayer = meshIn.NumberOfLayer;
            meshOut.TriangleList = new List<Triangle>();
            int allCounter = 1;
            for (int i = 0; i < meshOut.Cells.Count; i++)
            {
                if (meshOut.Cells[i].CellType == CellType.Prism)
                {
                    if (allCounter % meshOut.NumberOfLayer == 0)
                    {
                        // prismなので6点あるうちの
                        // 必要な最後の3点のみ取り出す
                        int d = meshOut.Cells[i].NodesIndex[3] - 1;
                        int e = meshOut.Cells[i].NodesIndex[4] - 1;
                        int f = meshOut.Cells[i].NodesIndex[5] - 1;
                        // 出力されるstlが外向きに法線を持つように
                        // d->e->f
                        // の順番ではなく
                        // d->f->e
                        Triangle triangle = new Triangle(meshOut.Nodes[d], meshOut.Nodes[f], meshOut.Nodes[e]);
                        meshOut.TriangleList.Add(triangle);
                    }
                    allCounter++;
                }
            }
            return meshOut;
        }



    }
    [Serializable]
    public class MeshSurface : Mesh
    {
        public MeshSurface()
        {
            Debug.WriteLine($"MeshSurface() constructor");
        }
        public int NumberOfMostOuterSurfaceCell { get; set; }
        public int NumberOfPrismLayerCell { get; set; }
        public int NumberOfQuadCell { get; set; }
        public override int NumberOfLayer { get; set; }
        public List<Triangle> TriangleList { get; set; }
    }
}
