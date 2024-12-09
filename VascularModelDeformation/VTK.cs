using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

namespace VascularModelDeformation
{
    public class VTK
    {
        public string Name { get; set; }
        public float Time { get; set; }
        public int NumberOfNodes { get; set; }
        public int NumberOfCells { get; set; }
        public int NumberOfCellsInfo { get; set; }
        public int NumberOfPolygons { get; set; }
        public int NumberOfPolygonsInfo { get; set; }
        public List<Node> Nodes { get; set; }
        public List<CellVTK> CellVTKs { get; set; }

        public VTK()
        {
            Debug.WriteLine($"VTK constructor");
        }
        //private void VTKVersionCheker(string[] lines)
        //{

        //    if (versionString == "5.1")
        //    {

        //    }
        //    else
        //    {

        //    }
        //}
        public void InterpretUNSTRUCTUREDGRID(string[] lines)
        {
            string[] firstLineParts = lines[0].Split(' ');
            string versionString = firstLineParts[4];

            if (versionString == "5.1")
            {
                this.InterpretUNSTRUCTUREDGRID51(lines);
            }
            else
            {
                this.InterpretUNSTRUCTUREDGRID20(lines);
            }

            return;
        }
        public void InterpretUNSTRUCTUREDGRID51(string[] lines)
        {
            for (int currentLine = 0; currentLine < lines.Length; currentLine++)
            {
                string[] parts = lines[currentLine].Split(' ');

                if (parts.Length == 0)
                    continue;
                if (parts[0] == "METADATA")
                    continue;
                if (parts[0] == "INFORMATION")
                    continue;
                if (parts[0] == "NAME")
                    continue;
                if (parts[0] == "DATA")
                    continue;
                // 頂点の情報はいらないので、これ以降は無視
                if (parts[0] == "POINT_DATA")
                    break;

                if (parts[0] == "TimeValue")
                {
                    string[] timeValue = lines[currentLine + 1].Split(' ');
                    this.Time = float.Parse(timeValue[0]);
                }

                if (parts[0] == "POINTS")
                {
                    this.NumberOfNodes = int.Parse(parts[1]);
                    this.Nodes = new List<Node>(this.NumberOfNodes);
                    currentLine++;
                    int firstLineNumber = currentLine;
                    int endLineNumber = 0;
                    bool continueFlag = true;
                    while (continueFlag)
                    {
                        if (StopFlag(lines[currentLine]))
                        {
                            continueFlag = false;
                            endLineNumber = currentLine - 1;
                        }
                        currentLine++;
                    }

                    // POINTSに関する行を連結
                    currentLine = firstLineNumber;
                    StringBuilder sbPoints = new StringBuilder();
                    for (int i = 0; i < (endLineNumber - firstLineNumber + 1); i++)
                    {
                        sbPoints.Append(lines[currentLine + i]);
                        if (i < (endLineNumber - firstLineNumber + 1 - 1))
                        {
                            //sbPoints.Append(" ");
                        }
                    }
                    string[] pointsString = sbPoints.ToString().Split(' ');
                    pointsString = RemoveLast(pointsString);

                    for (int i = 0; i < this.NumberOfNodes; i++)
                    {
                        Node newNode = new Node();
                        newNode.Index = i;
                        newNode.X = float.Parse(pointsString[i * 3 + 0]);
                        newNode.Y = float.Parse(pointsString[i * 3 + 1]);
                        newNode.Z = float.Parse(pointsString[i * 3 + 2]);
                        this.Nodes.Add(newNode);
                    }
                    currentLine = endLineNumber;
                }

                if (parts[0] == "CELLS")
                {
                    // CELLS 1429 8568
                    // OFFSETS vtktypeint64
                    // 0 6 12 18 24 30 36 42 48
                    // ...
                    // ...
                    // ...
                    // CONNECTIVITY vtktypeint64
                    // 0 1 2 3 4 5 6 7 8
                    // 0 1 2 9 10 11 6 7 8
                    this.NumberOfCells = int.Parse(parts[1]) - 1;
                    this.CellVTKs = new List<CellVTK>(this.NumberOfCells);
                    currentLine++;
                    // CELLS 1429 8568
                    // OFFSETS vtktypeint64
                    // 0 6 12 18 24 30 36 42 48
                    // の記述があるので
                    int firstLineNumber = currentLine + 1;
                    int endLineNumber = 0;
                    bool continueFlag = true;
                    while (continueFlag)
                    {
                        if (lines[currentLine].Contains("CONNECTIVITY"))
                        {
                            continueFlag = false;
                            endLineNumber = currentLine - 1;
                        }
                        currentLine++;
                    }

                    // CELLS OFFSETに関する行を連結
                    currentLine = firstLineNumber;
                    StringBuilder sbCellsOffset = new StringBuilder();
                    for (int i = 0; i < (endLineNumber - firstLineNumber + 1); i++)
                    {
                        sbCellsOffset.Append(lines[currentLine + i]);
                        if (i < (endLineNumber - firstLineNumber + 1 - 1))
                        {
                            //sbCells.Append(" ");
                        }
                    }
                    string[] cellsString = sbCellsOffset.ToString().Split(' ');
                    cellsString = RemoveLast(cellsString);

                    int numberOfReadedCells = 0;
                    for (int i = 0; i < cellsString.Length;)
                    {
                        int offset = int.Parse(cellsString[i]);
                        CellVTK cellVTK = new CellVTK();
                        cellVTK.Offset = offset;
                        cellVTK.Index = i;
                        this.CellVTKs.Add(cellVTK);
                        numberOfReadedCells++;
                        i++;
                        if (numberOfReadedCells == this.NumberOfCells)
                            break;
                    }
                    currentLine = endLineNumber;
                    currentLine++;

                    firstLineNumber = currentLine + 1;
                    continueFlag = true;




                    // connectivity read part
                    while (continueFlag)
                    {
                        if (StopFlag(lines[currentLine]))
                        {
                            continueFlag = false;
                            endLineNumber = currentLine - 1;
                        }
                        currentLine++;
                    }
                    // CELLSに関する行を連結
                    currentLine = firstLineNumber;
                    StringBuilder sbCellsConnectivity = new StringBuilder();
                    for (int i = 0; i < (endLineNumber - firstLineNumber + 1); i++)
                    {
                        sbCellsConnectivity.Append(lines[currentLine + i]);
                        if (i < (endLineNumber - firstLineNumber + 1 - 1))
                        {
                            //sbCells.Append(" ");
                        }
                    }
                    string[] cellsConnectivityString = sbCellsConnectivity.ToString().Split(' ');
                    cellsConnectivityString = RemoveLast(cellsConnectivityString);

                    for (int i = 0; i < this.CellVTKs.Count; i++)
                    {
                        // offsetとendsetを求める
                        // 次のセルのOffsetの一個前が自分の分の領域
                        // 最後のセルだけ次のセルがないので、cellsConnectivityStringの長さの-1が自分の分
                        int offset = this.CellVTKs[i].Offset;
                        int endset = -1;
                        if (i == (this.CellVTKs.Count - 1))
                        {
                            endset = cellsConnectivityString.Length - 1;
                        }
                        else
                        {
                            endset = this.CellVTKs[i + 1].Offset - 1;
                        }

                        List<int> indexes = new List<int>();
                        for (int j = offset; j < (endset + 1); j++)
                        {
                            indexes.Add(int.Parse(cellsConnectivityString[j]));
                        }
                        this.CellVTKs[i].NodesIndex = indexes.ToArray();
                    }


                    currentLine = endLineNumber;
                }

                if (parts[0] == "CELL_TYPES")
                {
                    currentLine++;
                    int firstLineNumber = currentLine;
                    int endLineNumber = 0;
                    bool continueFlag = true;
                    while (continueFlag)
                    {
                        if (StopFlag(lines[currentLine]))
                        {
                            continueFlag = false;
                            endLineNumber = currentLine - 1;
                        }
                        currentLine++;
                    }

                    // CELL_TYPESに関する行を連結
                    currentLine = firstLineNumber;
                    StringBuilder sbCellTypes = new StringBuilder();
                    for (int i = 0; i < (endLineNumber - firstLineNumber + 1); i++)
                    {
                        sbCellTypes.Append(lines[currentLine + i]);
                        if (i < (endLineNumber - firstLineNumber + 1 - 1))
                        {
                            sbCellTypes.Append(" ");
                        }
                    }
                    string[] cellTypesString = sbCellTypes.ToString().Split(' ');

                    for (int i = 0; i < cellTypesString.Length; i++)
                    {
                        int cellType = int.Parse(cellTypesString[i]);
                        if (cellType == 10)
                        {
                            this.CellVTKs[i].CellTypeVTK = CellTypeVTK.Quadrilateral;
                        }
                        if (cellType == 13)
                        {
                            this.CellVTKs[i].CellTypeVTK = CellTypeVTK.Prism;
                        }
                    }
                    currentLine = endLineNumber;
                }

                if (parts[0] == "C" && parts[1] == "3" && (int.Parse(parts[2]) == this.NumberOfCells))
                {
                    currentLine++;
                    int firstLineNumber = currentLine;
                    int endLineNumber = 0;
                    bool continueFlag = true;
                    while (continueFlag)
                    {
                        if (StopFlag(lines[currentLine]))
                        {
                            continueFlag = false;
                            endLineNumber = currentLine - 1;
                        }
                        currentLine++;
                    }

                    // Uに関する行を連結
                    currentLine = firstLineNumber;
                    StringBuilder sbC = new StringBuilder();
                    for (int i = 0; i < (endLineNumber - firstLineNumber + 1); i++)
                    {
                        sbC.Append(lines[currentLine + i]);
                        if (i < (endLineNumber - firstLineNumber + 1 - 1))
                        {
                            //sbC.Append(" ");
                        }
                    }
                    string[] CString = sbC.ToString().Split(' ');
                    CString = RemoveLast(CString);

                    for (int i = 0; i < this.NumberOfCells; i++)
                    {
                        Vector3 centroids = new Vector3(
                            float.Parse(CString[i * 3 + 0]),
                            float.Parse(CString[i * 3 + 1]),
                            float.Parse(CString[i * 3 + 2])
                        );
                        this.CellVTKs[i].Centroid = centroids;
                    }
                    currentLine = endLineNumber;
                }

                if (parts[0] == "U" && parts[1] == "3" && (int.Parse(parts[2]) == this.NumberOfCells))
                {
                    Debug.WriteLine($"this.NumberOfCells = {this.NumberOfCells}");
                    Debug.WriteLine($"int.Parse(parts[2]) = {int.Parse(parts[2])}");

                    currentLine++;
                    int firstLineNumber = currentLine;
                    int endLineNumber = 0;
                    bool continueFlag = true;
                    while (continueFlag)
                    {
                        if (StopFlag(lines[currentLine]))
                        {
                            continueFlag = false;
                            endLineNumber = currentLine - 1;
                        }
                        currentLine++;
                    }

                    // Uに関する行を連結
                    currentLine = firstLineNumber;
                    StringBuilder sbU = new StringBuilder();
                    for (int i = 0; i < (endLineNumber - firstLineNumber + 1); i++)
                    {
                        sbU.Append(lines[currentLine + i]);
                        if (i < (endLineNumber - firstLineNumber + 1 - 1))
                        {
                            //sbU.Append(" ");
                        }
                    }
                    string[] UString = sbU.ToString().Split(' ');
                    UString = RemoveLast(UString);

                    for (int i = 0; i < this.NumberOfCells; i++)
                    {
                        Vector3 u = new Vector3(
                            float.Parse(UString[i * 3 + 0]),
                            float.Parse(UString[i * 3 + 1]),
                            float.Parse(UString[i * 3 + 2])
                        );
                        this.CellVTKs[i].U = u;
                    }
                    currentLine = endLineNumber;
                }

            }
        }
        public void InterpretUNSTRUCTUREDGRID20(string[] lines)
        {
            for (int currentLine = 0; currentLine < lines.Length; currentLine++)
            {
                string[] parts = lines[currentLine].Split(' ');

                if (parts.Length == 0)
                    continue;

                // 頂点の情報はいらないので、これ以降は無視
                if (parts[0] == "POINT_DATA")
                    break;

                if (parts[0] == "TimeValue")
                {
                    string[] timeValue = lines[currentLine + 1].Split(' ');
                    this.Time = float.Parse(timeValue[0]);
                }

                if (parts[0] == "POINTS")
                {
                    this.NumberOfNodes = int.Parse(parts[1]);
                    this.Nodes = new List<Node>(this.NumberOfNodes);
                    currentLine++;
                    int firstLineNumber = currentLine;
                    int endLineNumber = 0;
                    bool continueFlag = true;
                    while (continueFlag)
                    {
                        if (lines[currentLine].Contains("CELLS"))
                        {
                            continueFlag = false;
                            endLineNumber = currentLine - 1;
                        }
                        currentLine++;
                    }

                    // POINTSに関する行を連結
                    currentLine = firstLineNumber;
                    StringBuilder sbPoints = new StringBuilder();
                    for (int i = 0; i < (endLineNumber - firstLineNumber + 1); i++)
                    {
                        sbPoints.Append(lines[currentLine + i]);
                        if (i < (endLineNumber - firstLineNumber + 1 - 1))
                        {
                            sbPoints.Append(" ");
                        }
                    }
                    string[] pointsString = sbPoints.ToString().Split(' ');
                    //pointsString = RemoveLast(pointsString);

                    for (int i = 0; i < this.NumberOfNodes; i++)
                    {
                        Node newNode = new Node();
                        newNode.Index = i;
                        newNode.X = float.Parse(pointsString[i * 3 + 0]);
                        newNode.Y = float.Parse(pointsString[i * 3 + 1]);
                        newNode.Z = float.Parse(pointsString[i * 3 + 2]);
                        this.Nodes.Add(newNode);
                    }
                    currentLine = endLineNumber;
                }

                if (parts[0] == "CELLS")
                {
                    this.NumberOfCells = int.Parse(parts[1]);
                    this.NumberOfCellsInfo = int.Parse(parts[2]);
                    this.CellVTKs = new List<CellVTK>(this.NumberOfCells);
                    currentLine++;
                    int firstLineNumber = currentLine;
                    int endLineNumber = 0;
                    bool continueFlag = true;
                    while (continueFlag)
                    {
                        if (lines[currentLine].Contains("CELL_TYPES"))
                        {
                            continueFlag = false;
                            endLineNumber = currentLine - 1;
                        }
                        currentLine++;
                    }

                    // CELLSに関する行を連結
                    currentLine = firstLineNumber;
                    StringBuilder sbCells = new StringBuilder();
                    for (int i = 0; i < (endLineNumber - firstLineNumber + 1); i++)
                    {
                        sbCells.Append(lines[currentLine + i]);
                        if (i < (endLineNumber - firstLineNumber + 1 - 1))
                        {
                            sbCells.Append(" ");
                        }
                    }
                    string[] cellsString = sbCells.ToString().Split(' ');
                    cellsString = RemoveLast(cellsString);

                    int numberOfReadedCells = 0;
                    for (int i = 0; i < cellsString.Length;)
                    {
                        int count = int.Parse(cellsString[i]);
                        i++;
                        CellVTK cellVTK = new CellVTK();
                        List<int> indexes = new List<int>();
                        for (int j = 0; j < count; j++)
                        {
                            indexes.Add(int.Parse(cellsString[i + j]));
                        }
                        i += count;
                        cellVTK.Index = numberOfReadedCells;
                        cellVTK.NodesIndex = indexes.ToArray();
                        this.CellVTKs.Add(cellVTK);
                        numberOfReadedCells++;
                    }
                    currentLine = endLineNumber;
                }

                if (parts[0] == "CELL_TYPES")
                {
                    currentLine++;
                    int firstLineNumber = currentLine;
                    int endLineNumber = 0;
                    bool continueFlag = true;
                    while (continueFlag)
                    {
                        if (lines[currentLine].Contains("CELL_DATA"))
                        {
                            continueFlag = false;
                            endLineNumber = currentLine - 1;
                        }
                        currentLine++;
                    }

                    // CELL_TYPESに関する行を連結
                    currentLine = firstLineNumber;
                    StringBuilder sbCellTypes = new StringBuilder();
                    for (int i = 0; i < (endLineNumber - firstLineNumber + 1); i++)
                    {
                        sbCellTypes.Append(lines[currentLine + i]);
                        if (i < (endLineNumber - firstLineNumber + 1 - 1))
                        {
                            sbCellTypes.Append(" ");
                        }
                    }
                    string[] cellTypesString = sbCellTypes.ToString().Split(' ');
                    cellTypesString = RemoveLast(cellTypesString);

                    for (int i = 0; i < cellTypesString.Length; i++)
                    {
                        int cellType = int.Parse(cellTypesString[i]);
                        if (cellType == 10)
                        {
                            this.CellVTKs[i].CellTypeVTK = CellTypeVTK.Quadrilateral;
                        }
                        if (cellType == 13)
                        {
                            this.CellVTKs[i].CellTypeVTK = CellTypeVTK.Prism;
                        }
                    }
                    currentLine = endLineNumber;
                }

                if (parts[0] == "C" && parts[1] == "3" && (int.Parse(parts[2]) == this.NumberOfCells))
                {
                    currentLine++;
                    int firstLineNumber = currentLine;
                    int endLineNumber = 0;
                    bool continueFlag = true;
                    while (continueFlag)
                    {
                        if (StopFlag(lines[currentLine]))
                        {
                            continueFlag = false;
                            endLineNumber = currentLine - 1;
                        }
                        currentLine++;
                    }

                    // Uに関する行を連結
                    currentLine = firstLineNumber;
                    StringBuilder sbC = new StringBuilder();
                    for (int i = 0; i < (endLineNumber - firstLineNumber + 1); i++)
                    {
                        sbC.Append(lines[currentLine + i]);
                        if (i < (endLineNumber - firstLineNumber + 1 - 1))
                        {
                            sbC.Append(" ");
                        }
                    }
                    string[] CString = sbC.ToString().Split(' ');
                    //CString = RemoveLast(CString);

                    for (int i = 0; i < this.NumberOfCells; i++)
                    {
                        Vector3 centroids = new Vector3(
                            float.Parse(CString[i * 3 + 0]),
                            float.Parse(CString[i * 3 + 1]),
                            float.Parse(CString[i * 3 + 2])
                        );
                        this.CellVTKs[i].Centroid = centroids;
                    }
                    currentLine = endLineNumber;
                }

                if (parts[0] == "U" && parts[1] == "3" && (int.Parse(parts[2]) == this.NumberOfCells))
                {
                    Debug.WriteLine($"this.NumberOfCells = {this.NumberOfCells}");
                    Debug.WriteLine($"int.Parse(parts[2]) = {int.Parse(parts[2])}");

                    currentLine++;
                    int firstLineNumber = currentLine;
                    int endLineNumber = 0;
                    bool continueFlag = true;
                    while (continueFlag)
                    {
                        if (StopFlag(lines[currentLine]))
                        {
                            continueFlag = false;
                            endLineNumber = currentLine - 1;
                        }
                        currentLine++;
                    }

                    // Uに関する行を連結
                    currentLine = firstLineNumber;
                    StringBuilder sbU = new StringBuilder();
                    for (int i = 0; i < (endLineNumber - firstLineNumber + 1); i++)
                    {
                        sbU.Append(lines[currentLine + i]);
                        if (i < (endLineNumber - firstLineNumber + 1 - 1))
                        {
                            sbU.Append(" ");
                        }
                    }
                    string[] UString = sbU.ToString().Split(' ');
                    //UString = RemoveLast(UString);

                    for (int i = 0; i < this.NumberOfCells; i++)
                    {
                        Vector3 u = new Vector3(
                            float.Parse(UString[i * 3 + 0]),
                            float.Parse(UString[i * 3 + 1]),
                            float.Parse(UString[i * 3 + 2])
                        );
                        this.CellVTKs[i].U = u;
                    }
                    currentLine = endLineNumber;
                }

                if (parts[0] == "CELL_TYPES")
                    continue;

            }
        }

        /// <summary>
        /// string[]の最後に空白が入っているのを除く関数
        /// </summary>
        /// <param name="inputStringsArray"></param>
        /// <returns></returns>
        private string[] RemoveLast(string[] inputStringsArray)
        {
            List<string> tmpStringsList = inputStringsArray.ToList();
            tmpStringsList.RemoveAt(tmpStringsList.Count - 1);
            string[] outputStrinsArray = tmpStringsList.ToArray();
            return outputStrinsArray;
        }
        /// <summary>
        /// vktの書式で、ある物理量が羅列されてから、どこまでがその物理量の羅列かを判断するためのもの
        /// </summary>
        /// <param name="lines"></param>
        /// <returns></returns>
        private bool StopFlag(string lines)
        {
            if (lines == "")
            {
                return true;
            }
            string[] flagKeyword = new string[] { "p", "U", "wallShearStress", "C", "Cx", "Cy", "Cz", "METADATA" };
            string[] parts = lines.Split(' ');
            foreach (var key in flagKeyword)
            {
                foreach (var part in parts)
                {
                    if (key == part)
                        return true;
                }
            }
            return false;
        }

        public void InterpretPolygon(string[] lines)
        {
            for (int currentLine = 0; currentLine < lines.Length; currentLine++)
            {
                string[] parts = lines[currentLine].Split(' ');

                if (parts.Length == 0) continue;

                if (parts[0] == "POINT_DATA")
                    break;

                // TimeValue 1 1 float
                // 193
                if (parts[0] == "TimeValue")
                {
                    string[] timeValue = lines[currentLine + 1].Split(' ');
                    this.Time = float.Parse(timeValue[0]);
                }

                // POINTS 359 float
                //　-0.012713 -0.000682174 0.0900003 -0.0130484 -0.000759695 0.0899938 -0.0128429 -0.00102139 0.0899939
                //　-0.0139837 0.000685621 0.0899933 -0.014149 0.000958309 0.0899932 -0.0142349 0.000656937 0.0899932
                //　-0.016217 -0.00171846 0.0899928
                if (parts[0] == "POINTS")
                {
                    string[] points = lines[currentLine].Split(' ');
                    this.NumberOfNodes = int.Parse(points[1]);
                    this.Nodes = new List<Node>(this.NumberOfNodes);
                    bool continueFlag = true;
                    int readedNumberOfNodes = 0;
                    while (continueFlag)
                    {
                        string[] pp = lines[currentLine + 1].Split(' ');
                        int thisLineNumberOfNodes = pp.Length / 3;
                        for (int i = 0; i < thisLineNumberOfNodes; i++)
                        {
                            Node newNode = new Node();
                            newNode.Index = this.Nodes.Count;
                            newNode.X = float.Parse(pp[i * 3 + 0]);
                            newNode.Y = float.Parse(pp[i * 3 + 1]);
                            newNode.Z = float.Parse(pp[i * 3 + 2]);
                            this.Nodes.Add(newNode);
                            readedNumberOfNodes++;
                        }
                        currentLine++;

                        if (readedNumberOfNodes >= this.NumberOfNodes)
                            continueFlag = false;
                    }
                }

                // POLYGONS 506 2192
                // 3 0 1 2  3 3 4 5  3
                // 6 7 8  3 9 10 11  3 12
                // 13 14  3 15 16 17  3 18 19
                // 20  3 21 22 23  3 24 25 26
                // polygonの情報が2行にまたがっていてデータ処理が面倒なので、
                // 戦略としてはpolygonの情報をすべてi行にする
                if (parts[0] == "POLYGONS")
                {
                    string[] pp = lines[currentLine].Split(' ');
                    this.NumberOfPolygons = int.Parse(pp[1]);
                    this.NumberOfPolygonsInfo = int.Parse(pp[2]);
                    this.CellVTKs = new List<CellVTK>(this.NumberOfPolygons);
                    for (int i = 0; i < this.NumberOfPolygons; i++)
                    {
                        CellVTK newCell = new CellVTK();
                        newCell.Index = this.CellVTKs.Count;
                        newCell.CellTypeVTK = CellTypeVTK.Polygons;
                        this.CellVTKs.Add(newCell);
                    }
                    int numberOfReadedPolygons = 0;
                    currentLine++;
                    int firstLineNumber = currentLine;
                    int endLineNumber = 0;
                    bool continueFlag = true;
                    while (continueFlag)
                    {
                        if (lines[currentLine] == "")
                        {
                            continueFlag = false;
                            endLineNumber = currentLine - 1;
                        }
                        currentLine++;
                    }

                    // POLYGONSに関する行を連結
                    currentLine = firstLineNumber;
                    StringBuilder sbPolygons = new StringBuilder();
                    for (int i = 0; i < (endLineNumber - firstLineNumber + 1); i++)
                    {
                        if (i == (endLineNumber - firstLineNumber + 1 - 1))
                        {
                            sbPolygons.Append(lines[currentLine + i]);
                        }
                        else
                        {
                            sbPolygons.Append(lines[currentLine + i] + " ");
                        }
                    }
                    string[] polygonsInfo = sbPolygons.ToString().Split(' ');
                    for (int i = 0; i < polygonsInfo.Length;)
                    {
                        int count = int.Parse(polygonsInfo[i++]);
                        List<int> polygonNode = new List<int>();
                        for (int j = 0; j < count; j++)
                        {
                            polygonNode.Add(int.Parse(polygonsInfo[i++]));
                        }
                        this.CellVTKs[numberOfReadedPolygons].NodesIndex = polygonNode.ToArray();
                        numberOfReadedPolygons++;
                    }
                    currentLine = endLineNumber + 1;
                }

                // C 3 506 float
                // -0.0128681 -0.000821088 0.089996 -0.0141225 0.000766955 0.0899932 -0.0163237 -0.00155073 0.0899928
                // -0.0156797 -0.00220229 0.0899926 -0.0151806 9.89388e-05 0.089993 -0.0149691 -0.00185775 0.0899932
                // -0.0138898 0.000273925 0.0899934 -0.0167388 0.0010071 0.0899923 -0.0156409 0.00104272 0.0899927
                // -0.0138206 -5.32213e-06 0.0899934 -0.0140844 0.000580505 0.0899933 -0.0143323 0.00110554 0.0899931
                if (parts[0] == "C" && parts[1] == "3")
                {
                    bool continueFlag = true;
                    int numberOfReadedPolygons = 0;
                    while (continueFlag)
                    {
                        string[] pp = lines[currentLine + 1].Split(' ');
                        int thisLineNumberOfCells = pp.Length / 3;
                        for (int i = 0; i < thisLineNumberOfCells; i++)
                        {
                            Vector3 centroid = new Vector3();
                            centroid.X = float.Parse(pp[i * 3 + 0]);
                            centroid.Y = float.Parse(pp[i * 3 + 1]);
                            centroid.Z = float.Parse(pp[i * 3 + 2]);
                            this.CellVTKs[numberOfReadedPolygons].Centroid = centroid;
                            numberOfReadedPolygons++;
                        }
                        currentLine++;

                        if (numberOfReadedPolygons >= this.NumberOfPolygons)
                            continueFlag = false;
                    }
                }

                // U 3 506 float
                // -5.85293e-05 0.000800132 0.168109 0.000558547 -0.000117624 0.339662 0.00023643 -0.000307535 0.213628
                // 0.000656494 0.00153103 0.161881 -1.44446e-05 -0.000337353 0.396743 -0.000456451 0.000211513 0.237023
                // -0.000327721 0.000319281 0.338841 -0.000585838 -0.000195177 0.216896 0.00117179 0.00054296 0.326737
                if (parts[0] == "U" && parts[1] == "3")
                {
                    bool continueFlag = true;
                    int numberOfReadedPolygons = 0;
                    while (continueFlag)
                    {
                        string[] pp = lines[currentLine + 1].Split(' ');
                        int thisLineNumberOfCells = pp.Length / 3;
                        for (int i = 0; i < thisLineNumberOfCells; i++)
                        {
                            Vector3 u = new Vector3();
                            u.X = float.Parse(pp[i * 3 + 0]);
                            u.Y = float.Parse(pp[i * 3 + 1]);
                            u.Z = float.Parse(pp[i * 3 + 2]);
                            this.CellVTKs[numberOfReadedPolygons].U = u;
                            numberOfReadedPolygons++;
                        }
                        currentLine++;

                        if (numberOfReadedPolygons >= this.NumberOfPolygons)
                            continueFlag = false;
                    }
                }
            }
        }

    }
}
