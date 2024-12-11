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
        /// cellの種類と境界の番号でセル（エレメント）の様子数を調べて
        /// 新しくList<Cell>を作る
        /// </summary>
        /// <param name="cellType"></param>
        /// <param name="physicalID"></param>
        /// <returns></returns>
        public virtual (int, List<Cell>) GetNumberOfCells(CellType cellType, int physicalID)
        {
            int num = 0;
            List<Cell> cells = new List<Cell>();
            foreach (var cell in this.Cells)
            {
                if (cell.CellType == cellType && cell.PhysicalID == physicalID)
                {
                    cells.Add(cell);
                    num++;
                }
            }
            return (num, cells);
        }
        public virtual void GetNumberOfLayer()
        {
            int numberOfLayer = 0;
            try
            {
                numberOfLayer = this.NumberOfPrismLayerCells / this.NumberOfWallCells;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                Environment.Exit(0);
            }
            this.NumberOfLayer = numberOfLayer;
        }
        /// <summary>
        /// NumberOfWallCellsに、WALL上の三角形パッチの総数を格納
        /// CellsWallに、WALL上の三角形パッチの集合を格納
        /// </summary>
        public virtual void GetNumberOfWALLCells()
        {
            (var numberOfWallCells, List<Cell> cellsWall) = GetNumberOfCells(CellType.Triangle, 10); 
                                                                                                     
            this.NumberOfWallCells = numberOfWallCells;
            this.CellsWall = cellsWall;
        }
        public virtual void GetNumberOfInnerWallCells()
        {
            (var numberOfInnerWallCells, List<Cell> cellsInnerWall) = GetNumberOfCells(CellType.Triangle, 90);
            this.NumberOfInnerWallCells = numberOfInnerWallCells;
            this.CellsInnerWall = cellsInnerWall;
        }
        public virtual void GetNumberOfTetraCells()
        {
            (var numberOfTetrahedronCells, List<Cell> cellsTetra) = GetNumberOfCells(CellType.Tetrahedron, 100);
            this.NumberOfTetrahedronCells = numberOfTetrahedronCells;
            this.CellsTetra = cellsTetra;
        }
        /// <summary>
        /// NumberOfPrismLayerCellsにプリズムセルの総数を
        /// CellsPrismLayerにすべてのプリズムセルの集合を格納
        /// </summary>
        public virtual void GetNumberOfPrismLayerCells()
        {
            (var numberOfPrismLayerCells, List<Cell> cellsPrismLayer) = GetNumberOfCells(CellType.Prism, 100);
            this.NumberOfPrismLayerCells = numberOfPrismLayerCells;
            this.CellsPrismLayer = cellsPrismLayer;
        }

        /// <summary>
        /// NumberOfMostInnerPrismLayerCells に最も内側のプリズム層のプリズムセルの総数を
        /// CellsMostInnerPrismLayer　に最内側プリズム層のプリズムセルの集合           を格納
        /// </summary>
        public virtual void GetNumberOfMostInnerPrismLayer() 
        {
            int numberOfLayer = this.SurfaceCellCorrespondPrismCells[0].Count - 1;  
            Debug.WriteLine("====================================");
            Debug.WriteLine($"{numberOfLayer}");
            Debug.WriteLine("====================================");
            int numberOfPrismLayer = 0;
            int numberOfMostInnerCells = 0;
            List<Cell> cellsMostInnerPrismLayer = new List<Cell>();
            foreach (var cell in this.Cells)
            {
                if (cell.CellType == CellType.Prism) // (CellType.Prismの) cell 要素の並び方は、表面の5,6層ある三角柱を縦にひとまとまりで考えて、 
                {                  // 1つ目の三角柱群: cell index: 0(最も外側),1,2,3,4,5(最も内側), 2つめの三角柱群: cell index: 6(最も外側),7,8,9,10,11(最も内側) 
                    numberOfPrismLayer++;                           // 外側から数えて何番目の層か
                    if (numberOfPrismLayer % numberOfLayer == 0)    // 最も内側の層に属する cell ならば
                    {
                        cellsMostInnerPrismLayer.Add(cell);  
                        numberOfMostInnerCells++; 
                    }
                }
            }
            this.NumberOfMostInnerPrismLayerCells = numberOfMostInnerCells; 
            this.CellsMostInnerPrismLayer = cellsMostInnerPrismLayer; 
            Debug.WriteLine($"NumberOfMostInnerPrismLayerCells : {this.NumberOfMostInnerPrismLayerCells}");
        }

        public virtual void GetNumberOfInletQuadrilateralCells()
        {
            (var numberOfInletQuadrilateralCells, List<Cell> cellsInletQuadrilateral) = GetNumberOfCells(CellType.Quadrilateral, 11);
            this.NumberOfInletQuadrilateralCells = numberOfInletQuadrilateralCells;
            this.CellsInletQuadrilateral = cellsInletQuadrilateral;
        }
        public virtual void GetNumberOfOutletQuadrilateralCells()
        {
            (var numberOfOutletQuadrilateralCells, List<Cell> cellsOutletQuadrilateral) = GetNumberOfCells(CellType.Quadrilateral, 12);
            this.NumberOfOutletQuadrilateralCells = numberOfOutletQuadrilateralCells;
            this.CellsOutletQuadrilateral = cellsOutletQuadrilateral;
        }
        /// <summary>
        /// プリズムセルを、各層ごとにグループ分け
        /// </summary>
        public virtual void SplitPrismLayersIntoEachPrismLayer()
        {
            if (this.NumberOfWallCells == 0)
                return;                         

            int numberOfWallCells = this.NumberOfWallCells;                   // NumberOfWallCells: WALL上の三角形パッチの総数。
            int numberOfPrismLayerCells = this.NumberOfPrismLayerCells;       // NumberOfPrismLayerCells : この血管モデルのプリズムセルの総数。
            int numberOfLayer = (int)(numberOfPrismLayerCells / numberOfWallCells);   // プリズム層の数。
            this.NumberOfLayer = numberOfLayer;
            Debug.WriteLine($"{numberOfLayer}");

            this.CellsEachPrismLayer = new List<List<Cell>>();  
            for (int i = 0; i < numberOfLayer; i++)
            {
                this.CellsEachPrismLayer.Add(new List<Cell>()); 
            }
            int counter = 0;
            foreach (var cell in this.CellsPrismLayer)                                // CellsPrismLayerは、全てのプリズムセルの集合。
            {
                if (cell.CellType == CellType.Prism)
                {
                    this.CellsEachPrismLayer[counter % this.NumberOfLayer].Add(cell); // 最初の (index=0の)プリズムセルは最初のList<Cell>に格納される、
                    counter++;                                                         // 次の (index=1の)プリズムセルは次のList<Cell>に格納される、 ...
                }
            }
        }

        /// <summary>
        /// それぞれの三角柱群の構成プリズムセルに対して、その三角柱群の外側三角形パッチが持つ中心線との対応(surfeceCorrespondIndex[i] )を同様に与える。
        /// </summary>
        /// <param name="surfaceCorrespondIndex"></param>
        public void SetCellCorrespondCenterlineIndex(List<int> surfaceCorrespondIndex) 
        {                                                    // surfaceCorrespondIndex : すべての表面三角形パッチの、対応する中心線点群の点番号のリスト。
                                                                                      // 要素数は、表面上の三角形パッチの総数。
            int numberOfWallCells = this.NumberOfWallCells;
            int numberOfPrismLayerCells = this.NumberOfLayer;
            for (int i = 0; i < numberOfWallCells; i++)
            {
                int correspondIndex = surfaceCorrespondIndex[i];
                this.SurfaceCellCorrespondPrismCells[i][0].CorrespondIndex = correspondIndex; // SurfaceCellCorrespondPrismCells[i][0]:i番目の三角柱群で、0番目つまり最も外側のプリズムセル
                for (int j = 0; j < numberOfPrismLayerCells; j++)                          // その三角柱群のうち、外側から数えてj+1番目の三角柱
                {
                    this.SurfaceCellCorrespondPrismCells[i][j + 1].CorrespondIndex = correspondIndex;
                }
            }
        }

        /// <summary>
        /// assign face correspond index to node correspond index
        /// 中心線ノードは、Cellに対応している
        /// 実際に移動させるのはCellに所属しているノード
        /// なので、Cellに対応するノードに中心線のindexを割り当てる
        /// またNodeは複数のCellに所属しているが、一つしか採用せず値が小さいほうを用いる.   
        /// </summary>　　　　　　　　　　　　　
        /// すべての三角柱群の、プリズムセルを構成するすべてのNodeに対して、対応する中心線点番号を割り当てる。この際、表面三角形パッチと中心線点番号の対応をそのまま割り当てる。
        public void AssignFaceCorrespondIndexToNodeCorrespondIndex()
        {
            foreach (var cells in this.SurfaceCellCorrespondPrismCells)  
            {                                                              
                foreach (var cell in cells)                              
                {
                    int correspondIndex = cell.CorrespondIndex;   // public void SetCellCorrespondCenterlineIndex(List<int> surfaceCorrespondIndex) にて、各プリズムに対する
                    foreach (var node in cell.NodesIndex)           // 中心線上の点との対応を与えている。
                    {
                        if (this.Nodes[node - 1].CorrespondCenterlineIndex == -1)  // Nodes はList<Node>型 (このファイルの最上で定義)。ここでの要素数はおそらく6。
                        {                                               
                            this.Nodes[node - 1].CorrespondCenterlineIndex = correspondIndex;  // まだ割り当てていないなら、プリズムに対して付けられた中心線点番号を割り当てる。
                        }
                        else
                        {
                            if (this.Nodes[node - 1].CorrespondCenterlineIndex > correspondIndex)
                            {
                                this.Nodes[node - 1].CorrespondCenterlineIndex = correspondIndex;     // 中心線点番号のより小さい方を採用する
                            }
                            else
                            {

                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// assign face correspond index to node correspond index
        /// 中心線ノードは、Cellに対応している
        /// 実際に移動させるのはCellに所属しているノード
        /// なので、Cellに対応するノードに中心線のindexを割り当てる
        /// またNodeは複数のCellに所属しているので、Listで管理する
        /// </summary>
        public void AssignFaceCorrespondIndexToNodeCorrespondIndexList()
        {
            foreach (var cell in this.Cells)
            {
                int correspondIndex = cell.CorrespondIndex;

                if (correspondIndex == -1)
                    continue;   // 処理をスキップして次のcellに行く。テトラメッシュや三角形パッチは、cell.CorrespondIndexを割り当てられていないのため、スキップされる。
                foreach (var index in cell.NodesIndex)
                {
                    if (this.Nodes[index - 1].CorrespondIndexList == null) 
                        this.Nodes[index - 1].CorrespondIndexList = new List<int>(); 

                    this.Nodes[index - 1].CorrespondIndexList.Add(correspondIndex); 
                }                              // 1つのNodeは複数のcell (ここではプリズムセル)の構成要素になっているので、その分だけListに要素が追加される
            }
        }

        /// <summary>
        /// prismLayerに対しての処理
        /// SurfaceCellCorrespondPrismCells
        /// に、三角形パッチ、三角柱群 (List<Cell>) のセットを、プリズム層全体に対して
        /// リストとして格納
        /// 
        /// </summary>
        public void FetchPrismLayerData()
        {
            this.SurfaceCellCorrespondPrismCells = new List<List<Cell>>(); 
                                                                  
            for (int i = 0; i < this.NumberOfWallCells; i++)
            {
                this.SurfaceCellCorrespondPrismCells.Add(new List<Cell>());  
            }                                                        
            int counter = 0;
            foreach (var cellW in this.CellsWall)
            {
                this.SurfaceCellCorrespondPrismCells[counter % this.NumberOfWallCells].Add(cellW);
                counter++;                   
            }                       
            int counterTmp = 0;
            int index = 0;
            foreach (var cellB in this.CellsPrismLayer)   
            {
                this.SurfaceCellCorrespondPrismCells[index].Add(cellB); 
                counterTmp++;
                if (counterTmp >= this.NumberOfLayer) 
                {
                    index++;                          
                    counterTmp = 0;
                }
            }
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
        /// <summary>
        /// MeshクラスのCells[]を作る
        /// </summary>
        /// <param name="cellsJugArray"></param>
        /// <returns></returns>
        protected List<Cell> MakeCells(int[][] cellsJugArray)
        {
            List<Cell> cells = new List<Cell>();
            for (int i = 0; i < cellsJugArray.Length; i++)  
            {
                var line = cellsJugArray[i];
                Cell cell = MakeCell(i, line);
                cells.Add(cell);
            }
            return cells;
        }
        /// <summary>
        /// Cellを作る
        /// </summary>
        /// <param name="i"></param>
        /// <param name="line"></param>
        /// <returns></returns>
        public Cell MakeCell(int i, int[] line)
        {
            // ここで単体のCellを定義
            // どのセルでも共通の情報
            var cell = MakeCellCommon(i, line);
            // CellTypeによってNodesIndexに保存する数が異なるので
            // CellTypeによって場合分け
            if (cell.CellType == CellType.Triangle)
            {
                MakeCellTriangle(cell, line);
            }
            else if (cell.CellType == CellType.Quadrilateral)
            {
                MakeCellQuadrilateral(cell, line);
            }
            else if (cell.CellType == CellType.Tetrahedron)
            {
                MakeCellTetrahedron(cell, line);
            }
            else if (cell.CellType == CellType.Prism)
            {
                MakeCellPrism(cell, line);
            }
            return cell;
        }
        /// <summary>
        /// すべてのCellTypeに共通な部分はまとめた
        /// </summary>
        /// <param name="i"></param>
        /// <param name="line"></param>
        /// <returns></returns>
        private Cell MakeCellCommon(int i, int[] line)
        {
            var cell = new Cell()
            {
                Index = i,
                CellType = (CellType)line[1],
                Dummy = line[2],
                PhysicalID = line[3],
                EntityID = line[4]
            };
            return cell;
        }
        /// <summary>
        /// Triangle型のCellを作る
        /// </summary>
        /// <param name="cell"></param>
        /// <param name="line"></param>
        private void MakeCellTriangle(Cell cell, int[] line)
        {
            cell.NodesIndex = new int[]
            {
                line[5],
                line[6],
                line[7]
            };
        }
        /// <summary>
        /// Quadrilateral型のCellを作る
        /// </summary>
        /// <param name="cell"></param>
        /// <param name="line"></param>
        private void MakeCellQuadrilateral(Cell cell, int[] line)
        {
            cell.NodesIndex = new int[]
            {
                line[5],
                line[6],
                line[7],
                line[8]
            };
        }
        /// <summary>
        /// Tetrahedron型のCellを作る
        /// </summary>
        /// <param name="cell"></param>
        /// <param name="line"></param>
        private void MakeCellTetrahedron(Cell cell, int[] line)
        {
            cell.NodesIndex = new int[]
            {
                        line[5],
                        line[6],
                        line[7],
                        line[8]
            };
        }
        /// <summary>
        /// Prism型のCellを作る
        /// </summary>
        /// <param name="cell"></param>
        /// <param name="line"></param>
        private void MakeCellPrism(Cell cell, int[] line)
        {
            cell.NodesIndex = new int[]
            {
                line[5],
                line[6],
                line[7],
                line[8],
                line[9],
                line[10]
            };
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

    [Serializable]
    public class MeshSurfaceAndPrismLayer : Mesh
    {
        /// <summary>
        /// constructor
        /// </summary>
        public MeshSurfaceAndPrismLayer()
        {
            Debug.WriteLine($"SurfaceAndPrismLayerMesh() constructor");
        }
        /// <summary>
        /// deepcopy
        /// </summary>
        /// <returns></returns>
        public new MeshSurfaceAndPrismLayer DeepCopy()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(ms, this);
                ms.Position = 0;
                return (MeshSurfaceAndPrismLayer)bf.Deserialize(ms);
            }
        }

        public override List<List<Cell>> CellsEachPrismLayer { get; set; }
        public override List<Cell> CellsPrismLayer { get; set; }
        public override List<Cell> CellsMostInnerPrismLayer { get; set; }
        public override List<Cell> CellsTetra { get; set; }
        public override List<Cell> CellsWall { get; set; }
        public override List<Cell> CellsInnerWall { get; set; }
        public override List<Cell> CellsInletQuadrilateral { get; set; }
        public override List<Cell> CellsOutletQuadrilateral { get; set; }
        public override int NumberOfPrismLayerCells { get; set; }
        public override int NumberOfMostInnerPrismLayerCells { get; set; }
        public override int NumberOfInnerWallCells { get; set; }
        public override int NumberOfInletQuadrilateralCells { get; set; }
        public override int NumberOfOutletQuadrilateralCells { get; set; }
        public override int NumberOfLayer { get; set; }
        public List<Triangle> TriangleList { get; set; }
        public HalfEdge HalfEdge { get; set; } = new HalfEdge();

        public void EdgeSwap()
        {
            foreach (var edge in HalfEdge.EdgeList)
            {
                if (edge.DoEdgeSwap)
                {
                    try
                    {
                        HalfEdge.EdgeSwapTrianglePrism(edge, this.Nodes, this.SurfaceCellCorrespondPrismCells);
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine($"{e.Message}");
                        Environment.Exit(0);
                    }
                }
            }
        }
        public void AllEdgeSwap()
        {
            HalfEdge.Create(this.Nodes, this.CellsWall);
            this.EdgeSwap();
        }
        public override void AnalyzeMesh()
        {
            Debug.WriteLine("AnalyzeMesh.");
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
        /// cellの種類と境界の番号でセル（エレメント）の様子数を調べて
        /// 新しくList<Cell>を作る
        /// </summary>
        /// <param name="cellType"></param>
        /// <param name="physicalID"></param>
        /// <returns></returns>
        public override (int, List<Cell>) GetNumberOfCells(CellType cellType, int physicalID)
        {
            int num = 0;
            List<Cell> cells = new List<Cell>();
            foreach (var cell in this.Cells)
            {
                if (cell.CellType == cellType && cell.PhysicalID == physicalID)
                {
                    cells.Add(cell);
                    num++;
                }
            }
            return (num, cells);
        }
        public override void GetNumberOfLayer()
        {
            int numberOfLayer = 0;
            numberOfLayer = this.NumberOfPrismLayerCells / this.NumberOfWallCells;
            this.NumberOfLayer = numberOfLayer;
        }
        public override void GetNumberOfWALLCells()
        {
            (var numberOfWallCells, List<Cell> cellsWall) = GetNumberOfCells(CellType.Triangle, 10);
            this.NumberOfWallCells = numberOfWallCells;
            this.CellsWall = cellsWall;
        }
        public override void GetNumberOfInnerWallCells()
        {
            (var numberOfInnerWallCells, List<Cell> cellsInnerWall) = GetNumberOfCells(CellType.Triangle, 90);
            this.NumberOfInnerWallCells = numberOfInnerWallCells;
            this.CellsInnerWall = cellsInnerWall;
        }
        public override void GetNumberOfTetraCells()
        {
            (var numberOfTetrahedronCells, List<Cell> cellsTetra) = GetNumberOfCells(CellType.Tetrahedron, 100);
            this.NumberOfTetrahedronCells = numberOfTetrahedronCells;
            this.CellsTetra = cellsTetra;
        }
        public override void GetNumberOfPrismLayerCells()
        {
            (var numberOfPrismLayerCells, List<Cell> cellsPrismLayer) = GetNumberOfCells(CellType.Prism, 100);
            this.NumberOfPrismLayerCells = numberOfPrismLayerCells;
            this.CellsPrismLayer = cellsPrismLayer;
        }
        public override void GetNumberOfMostInnerPrismLayer()
        {
            if (this.SurfaceCellCorrespondPrismCells.Count == 0)
                return;

            int numberOfLayer = this.SurfaceCellCorrespondPrismCells[0].Count - 1;
            Debug.WriteLine("====================================");
            Debug.WriteLine($"{numberOfLayer}");
            Debug.WriteLine("====================================");
            int numberOfPrismLayer = 0;
            int numberOfMostInnerCells = 0;
            List<Cell> cellsMostInnerPrismLayer = new List<Cell>();
            foreach (var cell in this.Cells)
            {
                if (cell.CellType == CellType.Prism)
                {
                    numberOfPrismLayer++;
                    if (numberOfPrismLayer % numberOfLayer == 0)
                    {
                        cellsMostInnerPrismLayer.Add(cell);
                        numberOfMostInnerCells++;
                    }
                }
            }
            this.NumberOfMostInnerPrismLayerCells = numberOfMostInnerCells;
            this.CellsMostInnerPrismLayer = cellsMostInnerPrismLayer;
            Debug.WriteLine($"NumberOfMostInnerPrismLayerCells : {this.NumberOfMostInnerPrismLayerCells}");
        }
        public override void GetNumberOfInletQuadrilateralCells()
        {
            (var numberOfInletQuadrilateralCells, List<Cell> cellsInletQuadrilateral) = GetNumberOfCells(CellType.Quadrilateral, 11);
            this.NumberOfInletQuadrilateralCells = numberOfInletQuadrilateralCells;
            this.CellsInletQuadrilateral = cellsInletQuadrilateral;
        }
        public override void GetNumberOfOutletQuadrilateralCells()
        {
            (var numberOfOutletQuadrilateralCells, List<Cell> cellsOutletQuadrilateral) = GetNumberOfCells(CellType.Quadrilateral, 12);
            this.NumberOfOutletQuadrilateralCells = numberOfOutletQuadrilateralCells;
            this.CellsOutletQuadrilateral = cellsOutletQuadrilateral;
        }
        public override void SplitPrismLayersIntoEachPrismLayer()
        {
            if (this.NumberOfWallCells == 0)
                return;

            int numberOfWallCells = this.NumberOfWallCells;
            int numberOfPrismLayerCells = this.NumberOfPrismLayerCells;
            int numberOfLayer = (int)(numberOfPrismLayerCells / numberOfWallCells);
            this.NumberOfLayer = numberOfLayer;
            Debug.WriteLine($"{numberOfLayer}");

            this.CellsEachPrismLayer = new List<List<Cell>>();
            for (int i = 0; i < numberOfLayer; i++)
            {
                this.CellsEachPrismLayer.Add(new List<Cell>());
            }
            int counter = 0;
            foreach (var cell in this.CellsPrismLayer)
            {
                if (cell.CellType == CellType.Prism)
                {
                    this.CellsEachPrismLayer[counter % this.NumberOfLayer].Add(cell);
                    counter++;
                }
            }
        }

    }
}
