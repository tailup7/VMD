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
        public virtual int NumberOfLayer { get; set; }      // これは固定値で、5層なら5, 6層なら6
        public List<List<Cell>> SurfaceCellCorrespondPrismCells { get; set; } // 三角柱が6層程度重なったものを1つの List<Cell> だと考えて、それが血管表面を構成している
                                                                              // と考えると、List<List<Cell>> の要素数は数万程度、その中のList<Cell> は5か6
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
        /// <summary>
        /// WALL面の三角形パッチの集合を作る
        /// </summary>
        /// <param name="meshIn"></param>
        /// <returns></returns>
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
        /// <summary>
        /// プリズムメッシュの部分を除いた場合の、内側(テトラの部分のみ)の表面の三角形パッチの集まりを作る
        /// </summary>
        /// <param name="meshIn"></param>
        /// <returns></returns>
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
                    allCounter++;    // 三角柱のindexの番号付けが、縦に外から数えるものだから、次のCells[i]は、ひとつ内側の三角柱
                }
            }
            return meshOut;
        }

        public virtual void AnalyzeMesh()
        {
            if (this.Cells == null)
                return;

            GetNumberOfWALLCells();
            GetNumberOfInnerWallCells();
            GetNumberOfTetraCells();
            GetNumberOfPrismLayerCells();
            GetNumberOfInletQuadrilateralCells();
            GetNumberOfOutletQuadrilateralCells();
            SplitPrismLayersIntoEachPrismLayer();
            FetchPrismLayerData();
            GetNumberOfMostInnerPrismLayer();
            GetNumberOfLayer();
        }






        /// <summary>
        /// 引数は、ファイルの中身が一行ずつ格納された文字列配列
        /// gmshから出力されたファイル「.msh」を読み込んで、ノードやエレメントを抜き出す
        /// </summary>
        /// <param name="lines"></param>
        /// <returns>
        /// </returns>
        public virtual void LoadMesh(string[] lines)
        {
            if (lines == null)
                return;

            int[][] elements = null;
            Dictionary<int, string> PhysicalNamesCorrespondence = null;
            var physicalInfos = new List<PhysicalInfo>();
            // Interpret lines.
            for (int currentLine = 0; currentLine < lines.Length; currentLine++)
            {
                if (lines[currentLine] == "$MeshFormat")
                {
                    //Debug.WriteLine("This is MeshFormat.");
                    currentLine += 2;
                }
                else if (lines[currentLine] == "$PhysicalNames")
                {
                    // TODO: PhysicalNamesが定義されていないときには対応できていない
                    currentLine += 1;
                    var physicalNameNumber = int.Parse(lines[currentLine]);
                    PhysicalNamesCorrespondence = new Dictionary<int, string>();
                    for (int index = 0; index < physicalNameNumber; index++)
                    {
                        currentLine += 1;
                        string[] cols = lines[currentLine].Split(' ');
                        var dimension = int.Parse(cols[0]);
                        var id = int.Parse(cols[1]);
                        var name = cols[2].Replace("\"", "");
                        PhysicalNamesCorrespondence.Add(id, name);
                        PhysicalInfo physicalInfo = new PhysicalInfo(dimension, id, name);
                        physicalInfos.Add(physicalInfo);
                    }
                }
                else if (lines[currentLine] == "$Nodes")
                {
                    //Debug.WriteLine($"Nodes");
                    currentLine += 1;
                    var nodesNumber = int.Parse(lines[currentLine]);
                    this.Nodes = new List<Node>();
                    for (int index = 0; index < nodesNumber; index++)
                    {
                        currentLine += 1;
                        string[] cols = lines[currentLine].Split(' ');
                        float x = float.Parse(cols[1]);
                        float y = float.Parse(cols[2]);
                        float z = float.Parse(cols[3]);
                        Node node = new Node(index, x, y, z);
                        this.Nodes.Add(node);
                    }
                }
                else if (lines[currentLine] == "$Elements")
                {
                    // elementは1-index
                    currentLine += 1;
                    var elementsNumber = int.Parse(lines[currentLine]);
                    elements = new int[elementsNumber][];
                    for (int index = 0; index < elementsNumber; index++)
                    {
                        currentLine += 1;
                        string[] splittedLine = lines[currentLine].Split(' ');
                        var array = new int[splittedLine.Length];
                        for (int c = 0; c < splittedLine.Length; c++)
                        {
                            array[c] = int.Parse(splittedLine[c]);
                        }
                        elements[index] = array;
                    }
                }
            }

            if (elements == null)
                throw new Exception("elementsが読み込めませんでした");

            this.Cells = MakeCells(elements);
            this.PhysicalInfos = physicalInfos;
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
