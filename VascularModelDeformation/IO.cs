using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Numerics;

namespace VascularModelDeformation
{
    public class IO
    {
        // VTK Format
        private const int VTK_LINE = 3;
        private const int VTK_TRIANGLE = 5;
        private const int VTK_QUAD = 9;
        private const int VTK_TETRA = 10;
        private const int VTK_WEDGE = 13;
        // GMSH Mesh Format
        private const int GMSH_LINE = 1;
        private const int GMSH_TRIANGLE = 2;
        private const int GMSH_QUAD = 3;
        private const int GMSH_TETRA = 4;
        private const int GMSH_PRISM = 6;
        // FLUENT Mesh Format
        private const int HEADER_COMMNET = 0;
        private const int HEADER_POINTS = 10;
        private const int HEADER_CELLS = 12;
        private const int HEADER_FACES = 13;
        private const int FLUENT_CELL_MIXED = 0;
        private const int FLUENT_CELL_TETRAHEDRAL = 2;
        private const int FLUENT_CELL_WEDGE = 6;
        private const int FLUENT_FACE_MIXED = 0;
        private const int FLUENT_FACE_TRIANGLE = 3;
        private const int FLUNET_FACE_QUADRILATERAL = 4;
        private const int FLUENT_BC_WALL = 3;
        private const int FLUENT_BC_INLET = 4;
        private const int FLUENT_BC_VELOCITY_INLET = 10;
        private const int FLUENT_BC_OUTLET = 5;
        /// <summary>
        /// constructor
        /// </summary>
        public IO()
        {
            Debug.WriteLine($"This is IO constructor.");
        }
        /// <summary>
        /// destructor
        /// </summary>
        ~IO()
        {
            Debug.WriteLine($"This is IO destructor");
        }

        /// <summary>
        /// Read GMSH22
        /// </summary>
        /// <returns></returns>
        public (Mesh, string) ReadGMSH22Ori()    // ファイルを読み込んでメッシュデータ (mesh) とディレクトリパス (dirPath) を返す
        {
            string filePath = null;
            string dirPath = null;
            using (var ofd = new OpenFileDialog())
            {
                ofd.Filter = "msh files|*.msh"; 
                if (ofd.ShowDialog() == DialogResult.OK) 
                {
                    var onlyFileName = Path.GetFileName(ofd.FileName);
                    if (".msh" == Path.GetExtension(onlyFileName))
                    {
                        filePath = ofd.FileName; 
                    }
                }
            }
            Mesh mesh = new Mesh();
            (mesh, dirPath) = this.ReadGMSH22Ori(filePath); 
            return (mesh, dirPath);
        }
        public (Mesh, string) ReadGMSH22Ori(string filePath) // 素の.mshファイルに関して取得できる情報 (ファイルの中身(各行))を登録
        {
            string[] lines = File.ReadAllLines(filePath);   
            string dirPath = Path.GetDirectoryName(filePath);
            Mesh mesh = new Mesh(lines); 
            return (mesh, dirPath);
        }

        public (MeshInner, string) ReadGMSH22Inner()
        {
            string filePath = null;
            string dirPath = null;
            using (var ofd = new OpenFileDialog())
            {
                ofd.Filter = "msh files|*.msh";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    var onlyFileName = Path.GetFileName(ofd.FileName);
                    if (".msh" == Path.GetExtension(onlyFileName))
                    {
                        filePath = ofd.FileName;
                    }
                }
            }
            MeshInner mesh = new MeshInner();
            (mesh, dirPath) = this.ReadGMSH22Inner(filePath);
            return (mesh, dirPath);
        }
        public (MeshInner, string) ReadGMSH22Inner(string filePath)
        {
            string dirPath = Path.GetDirectoryName(filePath);
            string[] lines = File.ReadAllLines(filePath);
            MeshInner mesh = new MeshInner(lines);    
            return (mesh, dirPath);
        }






        public List<int> ReadPLY()
        {
            string dirPath = null;
            string fileName = null;
            string[] lines = null;
            using (var ofd = new OpenFileDialog())
            {
                ofd.Filter = "ply file|*.ply";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    Debug.WriteLine($"{ofd.FileName}");
                    dirPath = Path.GetDirectoryName(ofd.FileName);
                    fileName = Path.GetFileName(ofd.FileName);
                    lines = File.ReadAllLines(ofd.FileName);
                }
            }
            List<int> correspondIndexList = ReadPLY(dirPath, fileName);
            return correspondIndexList;
        }
        public List<int> ReadPLY(string filePath)
        {
            Debug.WriteLine($"ReadPLY");
            List<Triangle> triangles = new List<Triangle>();
            using (StreamReader sr = new StreamReader(filePath))
            {
                string line = null;
                int vertexCount = 0;
                int faceCount = 0;
                List<Node> nodes = new List<Node>();
                while ((line = sr.ReadLine()) != null)
                {
                    if (line.StartsWith("element vertex"))
                    {
                        vertexCount = int.Parse(line.Split(' ')[2]);
                    }
                    else if (line.StartsWith("element face"))
                    {
                        faceCount = int.Parse(line.Split(' ')[2]);
                    }
                    else if (line == "end_header")
                    {
                        Debug.WriteLine($"{line}");
                        for (int i = 0; i < vertexCount; i++)
                        {
                            string[] vertexLine = sr.ReadLine().Split(' ');
                            float x = float.Parse(vertexLine[0]);
                            float y = float.Parse(vertexLine[1]);
                            float z = float.Parse(vertexLine[2]);
                            Node node = new Node(x, y, z);
                            nodes.Add(node);
                        }
                        for (int i = 0; i < faceCount; i++)
                        {
                            string[] faceLine = sr.ReadLine().Split(' ');
                            int dummy = int.Parse(faceLine[0]);
                            int n0 = int.Parse(faceLine[1]);
                            int n1 = int.Parse(faceLine[2]);
                            int n2 = int.Parse(faceLine[3]);
                            int correspondCenterlineIndex = int.Parse(faceLine[4]);
                            Triangle triangle = new Triangle(i, nodes[n0], nodes[n1], nodes[n2], correspondCenterlineIndex);
                            triangles.Add(triangle);
                        }
                    }
                }
            }
            STL stl = new STL(triangles);
            Debug.WriteLine($"{stl.Nodes.Count}");
            List<int> surfaceCorrespondIndex = new List<int>();
            foreach (var triangle in stl.Triangles)
            {
                surfaceCorrespondIndex.Add(triangle.CorrespondCenterlineIndex);
            }
            return surfaceCorrespondIndex;
        }
        public List<int> ReadPLY(string dirPath, string fileName)
        {
            Debug.WriteLine($"ReadPLY");
            string filePath = Path.Combine(dirPath, fileName);
            var surfaceCorrespondIndex = this.ReadPLY(filePath);
            return surfaceCorrespondIndex;
        }










        /// <summary>
        /// plyを出力する
        /// ファイル名のデフォルトはWritePLY.ply
        /// </summary>
        /// <param name="stl"></param>
        /// <param name="dirPath"></param>
        public void WritePLY(STL stl, string dirPath)
        {
            WritePLY(stl, dirPath, "WritePLY.ply");
        }
        /// <summary>
        /// plyを出力する
        /// stlクラスを受け取って、
        /// </summary>
        /// <param name="stl"></param>
        /// <param name="dirPath"></param>
        /// <param name="fileName"></param>
        public void WritePLY(STL stl, string dirPath, string fileName)
        {
            Debug.WriteLine($"WritePLY");
            string filePath = Path.Combine(dirPath, fileName);
            Debug.WriteLine($"{filePath}");
            using (StreamWriter sw = new StreamWriter(filePath))
            {
                string fixHeader = WritePLYFixHeader();
                string vertexHeader = WritePLYVertexHeader(stl);
                string faceHeader = WritePLYFaceHeader(stl);
                string vertexList = WritePLYVertexList(stl);
                string faceList = WritePLYFaceList(stl);
                sw.Write(fixHeader);
                sw.Write(vertexHeader);
                sw.Write(faceHeader);
                sw.Write(vertexList);
                sw.Write(faceList);
            }
        }
        private string WritePLYFixHeader()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("ply").Append(Environment.NewLine);
            sb.Append("format ascii 1.0").Append(Environment.NewLine);
            sb.Append("comment author: onoue").Append(Environment.NewLine);
            return sb.ToString();
        }
        private string WritePLYVertexHeader(STL stl)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append($"element vertex {stl.Nodes.Count}").Append(Environment.NewLine);
            sb.Append("property float x").Append(Environment.NewLine);
            sb.Append("property float y").Append(Environment.NewLine);
            sb.Append("property float z").Append(Environment.NewLine);
            return sb.ToString();
        }
        private string WritePLYFaceHeader(STL stl)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append($"element face {stl.Triangles.Count}").Append(Environment.NewLine);
            sb.Append("property list uchar int vertex_indices").Append(Environment.NewLine);
            sb.Append("property uchar face_correspond_node_index").Append(Environment.NewLine);
            sb.Append("end_header").Append(Environment.NewLine);
            return sb.ToString();
        }
        private string WritePLYVertexList(STL stl)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var node in stl.Nodes)
            {
                sb.Append($"{node.X} {node.Y} {node.Z}").Append(Environment.NewLine);
            }
            return sb.ToString();
        }
        private string WritePLYFaceList(STL stl)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var triangle in stl.Triangles)
            {
                sb.Append($"3 {triangle.NodeIndexes[0]} {triangle.NodeIndexes[1]} {triangle.NodeIndexes[2]} {triangle.CorrespondCenterlineIndex}").Append(Environment.NewLine);
            }
            return sb.ToString();
        }

        /// <summary>
        /// read centerline with openFileDialog
        /// </summary>
        public (Centerline, string) ReadCenterline()
        {
            Debug.WriteLine("ReadCenterLine");

            string filePath = null;
            using (var ofd = new OpenFileDialog())
            {
                ofd.Filter = "centerline files|*.txt";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    filePath = ofd.FileName;
                }
                else
                {
                    return (null, null);
                }
            }
            (Centerline centerline, string dirPath) = this.ReadCenterline(filePath);
            return (centerline, dirPath);
        }
        /// <summary>
        /// read centerline with string filePath
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public (Centerline, string) ReadCenterline(string filePath)
        {
            Debug.WriteLine($"ReadCenterline");
            string dirPath = Path.GetDirectoryName(filePath);
            string[] lines = File.ReadAllLines(filePath);
            Centerline centerline = new Centerline();
            centerline.InterpretCenterline(lines);
            return (centerline, dirPath);
        }


        /// <summary>
        /// read targetRadius.txt 
        /// </summary>
        /// <returns></returns>
        public List<float> ReadRadius()
        {
            Debug.WriteLine($"ReadRadius");

            string filePath = null;
            using (var ofd = new OpenFileDialog())
            {
                 ofd.Filter = "radius files|*.txt";
                 if (ofd.ShowDialog() == DialogResult.OK)
                 {
                    filePath = ofd.FileName;
                 }
            }
            // ファイルが選択されなかった場合
            if (string.IsNullOrEmpty(filePath))
            {
                return new List<float> { 0 };
            }

            string[] lines = File.ReadAllLines(filePath);
            List<float> radius = new List<float>();
            try
            {
                foreach (string line in lines)
                {
                    if (!string.IsNullOrWhiteSpace(line) && !line.Trim().StartsWith("#"))
                    {
                        float value;
                        if (float.TryParse(line.Trim(), out value)) 
                        {
                            radius.Add(value);  
                        }
                        else
                        {
                            Console.WriteLine($"無効な値が検出されました: {line}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ファイル読み込み中にエラーが発生しました: {ex.Message}");
            }
            return radius;
        }


        public void WriteRadius (List<float> radius, string dirPath, string fileName)
        {
            Debug.WriteLine($"WritePLY");
            string filePath = Path.Combine(dirPath, fileName);
            Debug.WriteLine($"{filePath}");
            using (StreamWriter sw = new StreamWriter(filePath))
            {
                sw.WriteLine($"# {radius.Count}");
                foreach (var r in radius)
                {
                    sw.WriteLine(r);
                }
            }
        }


        public (List<Triangle>, string) ReadSTLASCIITemp()
        {
            string dirPath = null;
            string stlFilePath = null;
            string[] lines = null;
            using (var ofd = new OpenFileDialog())
            {
                ofd.Filter = "stl file|*.stl";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    Debug.WriteLine($"{ofd.FileName}");
                    dirPath = Path.GetDirectoryName(ofd.FileName);
                    lines = File.ReadAllLines(ofd.FileName);
                    stlFilePath = ofd.FileName;
                }
            }
            List<Triangle> triangles = ReadSTLASCII(stlFilePath);
            return (triangles, dirPath);
        }
        public List<Triangle> ReadSTLASCII()
        {
            string dirPath = null;
            string stlFilePath = null;
            string[] lines = null;
            using (var ofd = new OpenFileDialog())
            {
                ofd.Filter = "stl file|*.stl";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    Debug.WriteLine($"{ofd.FileName}");
                    dirPath = Path.GetDirectoryName(ofd.FileName);
                    lines = File.ReadAllLines(ofd.FileName);
                    stlFilePath = ofd.FileName;
                }
            }
            List<Triangle> triangles = ReadSTLASCII(stlFilePath);
            return triangles;
        }
        /// <summary>
        /// read ascii stl
        /// </summary>
        /// <param name="stlFilePath"></param>
        /// <returns></returns>
        public List<Triangle> ReadSTLASCII(string stlFilePath)
        {
            List<Triangle> triangles = new List<Triangle>();

            using (StreamReader reader = new StreamReader(stlFilePath))
            {
                string line;
                int numberOfTriangles = 0;

                // ファイルの先頭から"solid"が現れるまで読み込む
                // 読み込んだ部分は無視するのでbreak
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.StartsWith("solid"))
                    {
                        break;
                    }
                }

                // "facet"から始まる行を読み込んで、三角形を作成する
                while ((line = reader.ReadLine()) != null)
                {
                    line = line.Trim();
                    if (string.IsNullOrEmpty(line))
                    {
                        continue;
                    }
                    if (line.StartsWith("facet"))
                    {
                        // 法線ベクトルを読み込む
                        string[] normalLine = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        float nx = float.Parse(normalLine[2]);
                        float ny = float.Parse(normalLine[3]);
                        float nz = float.Parse(normalLine[4]);

                        while ((line = reader.ReadLine()) != null)
                        {
                            line = line.Trim();
                            if (line.StartsWith("outer loop"))
                            {
                                break;
                            }
                        }

                        // 三角形の頂点を読み込む
                        line = reader.ReadLine().Trim();
                        string[] vertexLine1 = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        float x1 = float.Parse(vertexLine1[1]);
                        float y1 = float.Parse(vertexLine1[2]);
                        float z1 = float.Parse(vertexLine1[3]);

                        line = reader.ReadLine().Trim();
                        string[] vertexLine2 = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        float x2 = float.Parse(vertexLine2[1]);
                        float y2 = float.Parse(vertexLine2[2]);
                        float z2 = float.Parse(vertexLine2[3]);

                        line = reader.ReadLine().Trim();
                        string[] vertexLine3 = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        float x3 = float.Parse(vertexLine3[1]);
                        float y3 = float.Parse(vertexLine3[2]);
                        float z3 = float.Parse(vertexLine3[3]);

                        while ((line = reader.ReadLine()) != null)
                        {
                            line = line.Trim();
                            if (line.StartsWith("endloop"))
                            {
                                break;
                            }
                        }

                        // 三角形を作成してリストに追加する
                        Triangle triangle = new Triangle(
                            numberOfTriangles,
                            new Node(x1, y1, z1),
                            new Node(x2, y2, z2),
                            new Node(x3, y3, z3)
                        );
                        triangles.Add(triangle);
                        numberOfTriangles++;

                        // "endfacet"まで読み込む
                        while ((line = reader.ReadLine()) != null)
                        {
                            line = line.Trim();
                            if (line.StartsWith("endfacet"))
                            {
                                break;
                            }
                        }
                    }
                }
            }
            return triangles;
        }
        /// <summary>
        /// 三角形の単位法線ベクトルを求める
        /// </summary>
        /// <param name="r0"></param>
        /// <param name="r1"></param>
        /// <param name="r2"></param>
        /// <returns></returns>
        private float[] CalculateTriangleNormal(float[] r0, float[] r1, float[] r2) 
        {
            float[] vectorA = new float[3] { r1[0] - r0[0], r1[1] - r0[1], r1[2] - r0[2] };
            float[] vectorB = new float[3] { r2[0] - r0[0], r2[1] - r0[1], r2[2] - r0[2] };
            float[] res = new float[3];                                 
            res[0] = vectorA[1] * vectorB[2] - vectorA[2] * vectorB[1];        
            res[1] = vectorA[2] * vectorB[0] - vectorA[0] * vectorB[2];         
            res[2] = vectorA[0] * vectorB[1] - vectorA[1] * vectorB[0];         
            float length = (float)Math.Sqrt(res[0] * res[0] + res[1] * res[1] + res[2] * res[2]);
            for (int i = 0; i < 3; i++)
            {
                res[i] = res[i] / length;
            }
            return res;
        }

        /// <summary>
        /// GMSH22を読み込んで、WALLのSTLを吐き出す (button2)
        /// </summary>
        /// <param name="mesh"></param>
        /// <param name="dirPath"></param>
        public void WriteSTLWALLSurface(Mesh mesh, string dirPath)  
        {
            string filePath = Path.Combine(dirPath, "gmsh22.stl"); 
            using (var sw = new StreamWriter(filePath)) 
            {
                sw.WriteLine($"solid surface from onoue");
                foreach (var cell in mesh.Cells) 
                {                                                                               
                    if (cell.CellType == CellType.Triangle && cell.PhysicalID == 10)            
                    {
                        Node node1 = mesh.Nodes[cell.NodesIndex[0] - 1];  
                        Node node2 = mesh.Nodes[cell.NodesIndex[1] - 1];  
                        Node node3 = mesh.Nodes[cell.NodesIndex[2] - 1];
                        var normal = CalculateTriangleNormal(new float[3] { node1.X, node1.Y, node1.Z }, new float[3] { node2.X, node2.Y, node2.Z }, new float[3] { node3.X, node3.Y, node3.Z });
                        sw.WriteLine($"facet normal {normal[0]} {normal[1]} {normal[2]}");
                        sw.WriteLine($"outer loop");
                        sw.WriteLine($"vertex {node1.X} {node1.Y} {node1.Z}");
                        sw.WriteLine($"vertex {node2.X} {node2.Y} {node2.Z}");
                        sw.WriteLine($"vertex {node3.X} {node3.Y} {node3.Z}");
                        sw.WriteLine($"endloop");
                        sw.WriteLine($"endfacet");
                    }
                }
                sw.WriteLine($"endsolid surface");
            }
        }







        public void WriteSTLInnerSurfaceFromCellsMostInnerPrism(Mesh mesh, string dirPath, string fileName) //966
        {
            Debug.WriteLine($"WriteSTLMostInnerSurface");
            string filePath = Path.Combine(dirPath, fileName);
            Debug.WriteLine($"{filePath}");
            using (var sw = new StreamWriter(filePath))
            {
                sw.WriteLine($"solid surface from onoue");
                foreach (var cell in mesh.CellsMostInnerPrismLayer)
                {
                    if (cell.CellType == CellType.Prism)
                    {
                        Node node1 = mesh.Nodes[cell.NodesIndex[4] - 1];
                        Node node2 = mesh.Nodes[cell.NodesIndex[5] - 1];
                        Node node3 = mesh.Nodes[cell.NodesIndex[3] - 1];
                        float[] test = Utility.CrossProductNormal(node1, node2, node3);
                        sw.WriteLine($"facet normal {test[0]} {test[1]} {test[2]}");
                        sw.WriteLine($"  outer loop");
                        sw.WriteLine($"    vertex {node1.X} {node1.Y} {node1.Z}");
                        sw.WriteLine($"    vertex {node2.X} {node2.Y} {node2.Z}");
                        sw.WriteLine($"    vertex {node3.X} {node3.Y} {node3.Z}");
                        sw.WriteLine($"  endloop");
                        sw.WriteLine($"endfacet");
                    }
                }
                sw.WriteLine($"endsolid surface");
            }
        }
        public void WriteVTKPoints(List<Node> nodes, string dirPath, string fileName)
        {
            Debug.WriteLine("WriteVTKPoints");
            string filePath = Path.Combine(dirPath, fileName);
            Debug.WriteLine($"{filePath}");
            using (StreamWriter sw = new StreamWriter(filePath))
            {
                string fixHeader = WriteVTKFixHeader();
                string datasetHeader = WriteVTKDatasetPolydata();
                string pointsHeader = WriteVTKPointsHeader(nodes.Count);
                string pointsList = WriteVTKPointsList(nodes);
                string test = WriteVTKPolygonsHeader(nodes);
                string testtest = WriteVTKPolygonsList(nodes);
                string testtesttest = WriteVTKPolygonsTest(nodes);
                sw.WriteLine(fixHeader);
                sw.WriteLine(datasetHeader);
                sw.WriteLine(pointsHeader);
                sw.WriteLine(pointsList);
                sw.WriteLine(test);
                sw.WriteLine(testtest);
                sw.WriteLine(testtesttest);
            }
        }
        public string WriteVTKPolygonsTest(List<Node> nodes)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append($"CELL_DATA {nodes.Count}").Append(Environment.NewLine);
            sb.Append($"FIELD FieldData {1}").Append(Environment.NewLine);
            sb.Append($"haus 1 {nodes.Count} float").Append(Environment.NewLine);
            foreach (var node in nodes)
            {
                sb.Append($"{node.NearestDistance}").Append(Environment.NewLine);
            }
            return sb.ToString();
        }
        private string WriteVTKDatasetPolydata()
        {
            return "DATASET POLYDATA" + Environment.NewLine;
        }
        private string WriteVTKPolygonsHeader(List<Node> nodes)
        {
            return $"POLYGONS {nodes.Count} {nodes.Count * 2}";
        }
        private string WriteVTKPolygonsList(List<Node> nodes)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var node in nodes)
            {
                sb.Append(1.ToString()).Append(" ").Append(node.Index).Append(Environment.NewLine);
            }
            return sb.ToString();
        }
        /// <summary>
        /// vtkを出力するための関数
        /// </summary>
        /// <param name="mesh"></param>
        /// <param name="dirPath"></param>
        /// <param name="fileName"></param>
        public void WriteVTKMesh(Mesh mesh, string dirPath, string fileName)     // 1134
        {
            bool flagCell = false;
            Debug.WriteLine($"WriteVTKMeshTest");
            string filePath = Path.Combine(dirPath, fileName);
            Debug.WriteLine($"{filePath}");
            using (StreamWriter sw = new StreamWriter(filePath))
            {
                string fixHeader = WriteVTKFixHeader();
                string datasetHeader = WriteVTKDatasetUnstructuredGrid();
                string pointsHeader = WriteVTKPointsHeader(mesh.Nodes.Count);
                string pointsList = WriteVTKPointsList(mesh.Nodes);
                string cellsHeader = WriteVTKCellsHeader(mesh.Cells);
                string cellsList = WriteVTKCellsList(mesh.Cells);
                string cellTypesHeader = WriteVTKCellTypesHeader(mesh.Cells);
                string cellTypesList = WriteVTKCellTypesList(mesh.Cells);
                string cellQualityAspectRatioHeader = WriteVTKCellQualityAspectRatioHeader(mesh.Cells, ref flagCell);
                string cellQualityAspectRatioList = WriteVTKCellQualityAspectRatioList(mesh);
                string cellQualityEdgeRatioHeader = WriteVTKCellQualityEdgeRatioHeader(mesh.Cells, ref flagCell);
                string cellQualityEdgeRatioList = WriteVTKCellQualityEdgeRatioList(mesh);
                string cellQualityRadiusRatioHeader = WriteVTKCellQualityRadiusRatioHeader(mesh.Cells, ref flagCell);
                string cellQualityRadiusRatioList = WriteVTKCellQualityRadiusRatioList(mesh);
                string cellAreaHeader = WriteVTKCellAreaHeader(mesh.Cells, ref flagCell);
                string cellAreaList = WriteVTKCellAreaList(mesh);
                string cellVolumeHeader = WriteVTKCellVolumeHeader(mesh.Cells, ref flagCell);
                string cellVolumeList = WriteVTKCellVolumeList(mesh);
                sw.Write(fixHeader);
                sw.Write(datasetHeader);
                sw.Write(pointsHeader);
                sw.Write(pointsList);
                sw.Write(cellsHeader);
                sw.Write(cellsList);
                sw.Write(cellTypesHeader);
                sw.Write(cellTypesList);
                sw.Write(cellQualityAspectRatioHeader);
                sw.Write(cellQualityAspectRatioList);
                sw.Write(cellQualityEdgeRatioHeader);
                sw.Write(cellQualityEdgeRatioList);
                sw.Write(cellQualityRadiusRatioHeader);
                sw.Write(cellQualityRadiusRatioList);
                sw.Write(cellAreaHeader);
                sw.Write(cellAreaList);
                sw.Write(cellVolumeHeader);
                sw.Write(cellVolumeList);
            }
        }
        /// <summary>
        /// unstrunctued gridを出力する際の決まったヘッダーを書き込む
        /// </summary>
        /// <returns></returns>
        private string WriteVTKFixHeader()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(WriteVTKDataFileHeader());
            sb.Append(WriteVTKHeader());
            sb.Append(WriteVTKASCII());
            return sb.ToString();
        }
        private string WriteVTKDataFileHeader()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("# vtk DataFile Version 2.0").Append(Environment.NewLine);
            return sb.ToString();
        }
        private string WriteVTKHeader()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("HEADER").Append(Environment.NewLine);
            return sb.ToString();
        }
        private string WriteVTKASCII()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("ASCII").Append(Environment.NewLine);
            return sb.ToString();
        }
        private string WriteVTKDatasetUnstructuredGrid()
        {
            return "DATASET UNSTRUCTURED_GRID" + Environment.NewLine;
        }

        private string WriteVTKPointsHeader(int numberOfPoints)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("POINTS").Append(" ").Append(numberOfPoints.ToString()).Append(" ").Append("float").Append(Environment.NewLine);
            return sb.ToString();
        }
        private string WriteVTKPointsList(List<Node> nodes)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var node in nodes)
            {
                sb.Append(node.X.ToString()).Append(" ").Append(node.Y.ToString()).Append(" ").Append(node.Z.ToString()).Append(Environment.NewLine);
            }
            return sb.ToString();
        }

        private string WriteVTKCellsHeader(List<Cell> cells)
        {
            int numberOfCells = cells.Count;
            int number = 0;
            foreach (var cell in cells)
            {
                if (cell.CellType == CellType.Triangle)
                {
                    number += 1 + 3;
                }
                if (cell.CellType == CellType.Quadrilateral)
                {
                    number += 1 + 4;
                }
                if (cell.CellType == CellType.Tetrahedron)
                {
                    number += 1 + 4;
                }
                if (cell.CellType == CellType.Prism)
                {
                    number += 1 + 6;
                }
            }
            StringBuilder sb = new StringBuilder();
            sb.Append($"CELLS").Append(" ").Append(numberOfCells.ToString()).Append(" ").Append(number.ToString()).Append(Environment.NewLine);
            return sb.ToString();
        }
        private string WriteVTKCellsList(List<Cell> cells)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var cell in cells)
            {
                if (cell.CellType == CellType.Triangle)
                {
                    sb.Append(3.ToString()).Append(" ").Append((cell.NodesIndex[0] - 1).ToString()).Append(" ").Append((cell.NodesIndex[1] - 1).ToString()).Append(" ").Append((cell.NodesIndex[2] - 1).ToString()).Append(Environment.NewLine);
                }
                if (cell.CellType == CellType.Quadrilateral)
                {
                    sb.Append(4.ToString()).Append(" ").Append((cell.NodesIndex[0] - 1).ToString()).Append(" ").Append((cell.NodesIndex[1] - 1).ToString()).Append(" ").Append((cell.NodesIndex[2] - 1).ToString()).Append(" ").Append((cell.NodesIndex[3] - 1).ToString()).Append(Environment.NewLine);
                }
                if (cell.CellType == CellType.Tetrahedron)
                {
                    sb.Append(4.ToString()).Append(" ").Append((cell.NodesIndex[0] - 1).ToString()).Append(" ").Append((cell.NodesIndex[1] - 1).ToString()).Append(" ").Append((cell.NodesIndex[2] - 1).ToString()).Append(" ").Append((cell.NodesIndex[3] - 1).ToString()).Append(Environment.NewLine);
                }
                if (cell.CellType == CellType.Prism)
                {
                    sb.Append(6.ToString()).Append(" ").Append((cell.NodesIndex[0] - 1).ToString()).Append(" ").Append((cell.NodesIndex[1] - 1).ToString()).Append(" ").Append((cell.NodesIndex[2] - 1).ToString()).Append(" ").Append((cell.NodesIndex[3] - 1).ToString()).Append(" ").Append((cell.NodesIndex[4] - 1).ToString()).Append(" ").Append((cell.NodesIndex[5] - 1).ToString()).Append(Environment.NewLine);
                }
            }
            return sb.ToString();
        }
        private string WriteVTKCellTypesHeader(List<Cell> cells)
        {
            int numberOfCells = cells.Count;
            StringBuilder sb = new StringBuilder();
            sb.Append($"CELL_TYPES").Append(" ").Append(numberOfCells.ToString()).Append(Environment.NewLine);
            return sb.ToString();
        }
        private string WriteVTKCellTypesList(List<Cell> cells)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var cell in cells)
            {
                if (cell.CellType == CellType.Triangle)
                {
                    sb.Append(VTK_TRIANGLE.ToString()).Append(Environment.NewLine);
                }
                if (cell.CellType == CellType.Quadrilateral)
                {
                    sb.Append(VTK_QUAD.ToString()).Append(Environment.NewLine);
                }
                if (cell.CellType == CellType.Tetrahedron)
                {
                    sb.Append(VTK_TETRA.ToString()).Append(Environment.NewLine);
                }
                if (cell.CellType == CellType.Prism)
                {
                    sb.Append(VTK_WEDGE.ToString()).Append(Environment.NewLine);
                }
            }
            return sb.ToString();
        }
        private string WriteVTKCellQualityAspectRatioHeader(List<Cell> cells, ref bool flag)
        {
            StringBuilder sb = new StringBuilder();
            int numberOfCells = cells.Count;
            if (!flag)
            {
                Debug.WriteLine($"flag is false");
                sb.Append("CELL_DATA").Append(" ").Append(numberOfCells.ToString()).Append(Environment.NewLine);
                flag = true;
            }
            sb.Append("SCALARS").Append(" ").Append("QualityAspectRatio").Append(" ").Append("double").Append(Environment.NewLine);
            sb.Append("LOOKUP_TABLE default").Append(Environment.NewLine);
            return sb.ToString();
        }
        private string WriteVTKCellQualityAspectRatioList(Mesh mesh)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var cell in mesh.Cells)
            {
                if (cell.CellType == CellType.Triangle)
                {
                    Node node0 = mesh.Nodes[cell.NodesIndex[0] - 1];
                    Node node1 = mesh.Nodes[cell.NodesIndex[1] - 1];
                    Node node2 = mesh.Nodes[cell.NodesIndex[2] - 1];
                    Triangle triangle = new Triangle(node0, node1, node2);
                    sb.Append(triangle.QualityAspectRatio.ToString()).Append(Environment.NewLine);
                }
                if (cell.CellType == CellType.Quadrilateral)
                {
                    sb.Append(0.ToString()).Append(Environment.NewLine);
                }
                if (cell.CellType == CellType.Tetrahedron)
                {
                    Node node0 = mesh.Nodes[cell.NodesIndex[0] - 1];
                    Node node1 = mesh.Nodes[cell.NodesIndex[1] - 1];
                    Node node2 = mesh.Nodes[cell.NodesIndex[2] - 1];
                    Node node3 = mesh.Nodes[cell.NodesIndex[3] - 1];
                    Tetrahedron tetrahedron = new Tetrahedron(node0, node1, node2, node3);
                    sb.Append(tetrahedron.QualityAspectRatio.ToString()).Append(Environment.NewLine);
                }
                if (cell.CellType == CellType.Prism)
                {
                    sb.Append(0.ToString()).Append(Environment.NewLine);
                }

            }
            return sb.ToString();
        }
        private string WriteVTKCellQualityEdgeRatioHeader(List<Cell> cells, ref bool flag)
        {
            StringBuilder sb = new StringBuilder();
            int numberOfCells = cells.Count;
            if (!flag)
            {
                Debug.WriteLine($"flag is false");
                sb.Append("CELL_DATA").Append(" ").Append(numberOfCells.ToString()).Append(Environment.NewLine);
                flag = true;
            }
            sb.Append("SCALARS").Append(" ").Append("QualityEdgeRatio").Append(" ").Append("double").Append(Environment.NewLine);
            sb.Append("LOOKUP_TABLE default").Append(Environment.NewLine);
            return sb.ToString();
        }
        private string WriteVTKCellQualityEdgeRatioList(Mesh mesh)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var cell in mesh.Cells)
            {
                if (cell.CellType == CellType.Triangle)
                {
                    Node node0 = mesh.Nodes[cell.NodesIndex[0] - 1];
                    Node node1 = mesh.Nodes[cell.NodesIndex[1] - 1];
                    Node node2 = mesh.Nodes[cell.NodesIndex[2] - 1];
                    Triangle triangle = new Triangle(node0, node1, node2);
                    sb.Append(triangle.QualityEdgeRatio.ToString()).Append(Environment.NewLine);
                }
                if (cell.CellType == CellType.Quadrilateral)
                {
                    sb.Append(0.ToString()).Append(Environment.NewLine);
                }
                if (cell.CellType == CellType.Tetrahedron)
                {
                    Node node0 = mesh.Nodes[cell.NodesIndex[0] - 1];
                    Node node1 = mesh.Nodes[cell.NodesIndex[1] - 1];
                    Node node2 = mesh.Nodes[cell.NodesIndex[2] - 1];
                    Node node3 = mesh.Nodes[cell.NodesIndex[3] - 1];
                    Tetrahedron tetrahedron = new Tetrahedron(node0, node1, node2, node3);
                    sb.Append(tetrahedron.QualityEdgeRatio.ToString()).Append(Environment.NewLine);
                }
                if (cell.CellType == CellType.Prism)
                {
                    sb.Append(0.ToString()).Append(Environment.NewLine);
                }

            }
            return sb.ToString();
        }
        private string WriteVTKCellQualityRadiusRatioHeader(List<Cell> cells, ref bool flag)
        {
            StringBuilder sb = new StringBuilder();
            int numberOfCells = cells.Count;
            if (!flag)
            {
                Debug.WriteLine($"flag is false");
                sb.Append("CELL_DATA").Append(" ").Append(numberOfCells.ToString()).Append(Environment.NewLine);
                flag = true;
            }
            sb.Append("SCALARS").Append(" ").Append("QualityRadiusRatio").Append(" ").Append("double").Append(Environment.NewLine);
            sb.Append("LOOKUP_TABLE default").Append(Environment.NewLine);
            return sb.ToString();
        }
        private string WriteVTKCellQualityRadiusRatioList(Mesh mesh)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var cell in mesh.Cells)
            {
                if (cell.CellType == CellType.Triangle)
                {
                    Node node0 = mesh.Nodes[cell.NodesIndex[0] - 1];
                    Node node1 = mesh.Nodes[cell.NodesIndex[1] - 1];
                    Node node2 = mesh.Nodes[cell.NodesIndex[2] - 1];
                    Triangle triangle = new Triangle(node0, node1, node2);
                    sb.Append(triangle.QualityRadiusRatio.ToString()).Append(Environment.NewLine);
                }
                if (cell.CellType == CellType.Quadrilateral)
                {
                    sb.Append(0.ToString()).Append(Environment.NewLine);
                }
                if (cell.CellType == CellType.Tetrahedron)
                {
                    Node node0 = mesh.Nodes[cell.NodesIndex[0] - 1];
                    Node node1 = mesh.Nodes[cell.NodesIndex[1] - 1];
                    Node node2 = mesh.Nodes[cell.NodesIndex[2] - 1];
                    Node node3 = mesh.Nodes[cell.NodesIndex[3] - 1];
                    Tetrahedron tetrahedron = new Tetrahedron(node0, node1, node2, node3);
                    sb.Append(tetrahedron.QualityRadiusRatio.ToString()).Append(Environment.NewLine);
                }
                if (cell.CellType == CellType.Prism)
                {
                    sb.Append(0.ToString()).Append(Environment.NewLine);
                }
            }
            return sb.ToString();
        }
        private string WriteVTKCellAreaHeader(List<Cell> cells, ref bool flag)
        {
            StringBuilder sb = new StringBuilder();
            int numberOfCells = cells.Count;
            if (!flag)
            {
                Debug.WriteLine($"flag is false");
                sb.Append("CELL_DATA").Append(" ").Append(numberOfCells.ToString()).Append(Environment.NewLine);
                flag = true;
            }
            sb.Append("SCALARS").Append(" ").Append("Area").Append(" ").Append("double").Append(Environment.NewLine);
            sb.Append("LOOKUP_TABLE default").Append(Environment.NewLine);
            return sb.ToString();
        }
        /// <summary>
        /// 二次元セルの場合は面積
        /// 三次元セルの場合は表面積
        /// </summary>
        /// <param name="cells"></param>
        /// <param name="flag"></param>
        /// <returns></returns>
        private string WriteVTKCellAreaList(Mesh mesh)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var cell in mesh.Cells)
            {
                if (cell.CellType == CellType.Triangle)
                {
                    Node node0 = mesh.Nodes[cell.NodesIndex[0] - 1];
                    Node node1 = mesh.Nodes[cell.NodesIndex[1] - 1];
                    Node node2 = mesh.Nodes[cell.NodesIndex[2] - 1];
                    Triangle triangle = new Triangle(node0, node1, node2);
                    sb.Append(triangle.Area.ToString()).Append(Environment.NewLine);
                }
                if (cell.CellType == CellType.Quadrilateral)
                {
                    sb.Append(0.ToString()).Append(Environment.NewLine);
                }
                if (cell.CellType == CellType.Tetrahedron)
                {
                    Node node0 = mesh.Nodes[cell.NodesIndex[0] - 1];
                    Node node1 = mesh.Nodes[cell.NodesIndex[1] - 1];
                    Node node2 = mesh.Nodes[cell.NodesIndex[2] - 1];
                    Node node3 = mesh.Nodes[cell.NodesIndex[3] - 1];
                    Tetrahedron tetrahedron = new Tetrahedron(node0, node1, node2, node3);
                    sb.Append(tetrahedron.Area.ToString()).Append(Environment.NewLine);
                }
                if (cell.CellType == CellType.Prism)
                {
                    sb.Append(0.ToString()).Append(Environment.NewLine);
                }
            }
            return sb.ToString();
        }
        private string WriteVTKCellVolumeHeader(List<Cell> cells, ref bool flag)
        {
            StringBuilder sb = new StringBuilder();
            if (!flag)
            {
                sb.Append("CELL_DATA").Append(" ").Append(cells.Count.ToString()).Append(Environment.NewLine);
                flag = true;
            }
            sb.Append("SCALARS").Append(" ").Append("Volume").Append(" ").Append("double").Append(Environment.NewLine);
            sb.Append("LOOKUP_TABLE default").Append(Environment.NewLine);
            return sb.ToString();
        }
        /// <summary>
        /// 二次元セルの場合は0
        /// 三次元セルのみ存在する
        /// </summary>
        /// <param name="mesh"></param>
        /// <returns></returns>
        private string WriteVTKCellVolumeList(Mesh mesh)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var cell in mesh.Cells)
            {
                if (cell.CellType == CellType.Triangle)
                {
                    sb.Append(0.ToString()).Append(Environment.NewLine);
                }
                if (cell.CellType == CellType.Quadrilateral)
                {
                    sb.Append(0.ToString()).Append(Environment.NewLine);
                }
                if (cell.CellType == CellType.Tetrahedron)
                {
                    Node node0 = mesh.Nodes[cell.NodesIndex[0] - 1];
                    Node node1 = mesh.Nodes[cell.NodesIndex[1] - 1];
                    Node node2 = mesh.Nodes[cell.NodesIndex[2] - 1];
                    Node node3 = mesh.Nodes[cell.NodesIndex[3] - 1];
                    Tetrahedron tetrahedron = new Tetrahedron(node0, node1, node2, node3);
                    sb.Append(tetrahedron.Volume.ToString()).Append(Environment.NewLine);
                }
                if (cell.CellType == CellType.Prism)
                {
                    sb.Append(0.ToString()).Append(Environment.NewLine);
                }
            }
            return sb.ToString();
        }


        public void WriteGMSH22(Mesh mesh, string dirPath, string fileName)   // 2131
        {
            Debug.WriteLine($"WriteGMSH22");
            Debug.WriteLine($"{dirPath}");
            string filePath = Path.Combine(dirPath, fileName);
            Debug.WriteLine($"{filePath}");
            mesh.PhysicalInfos.Sort();
            using (var sw = new StreamWriter(filePath))
            {
                sw.WriteLine($"$MeshFormat");
                sw.WriteLine($"2.2 0 8");
                sw.WriteLine($"$EndMeshFormat");
                sw.WriteLine($"$PhysicalNames");
                sw.WriteLine($"{mesh.PhysicalInfos.Count}");
                foreach (var physical in mesh.PhysicalInfos)
                {
                    sw.WriteLine($"{physical.Dimension} {physical.ID} \"{physical.Name}\"");
                }
                sw.WriteLine($"$EndPhysicalNames");
                sw.WriteLine($"$Nodes");
                sw.WriteLine($"{mesh.Nodes.Count}");
                foreach (var node in mesh.Nodes)
                {
                    sw.WriteLine($"{node.Index + 1} {node.X} {node.Y} {node.Z}");
                }
                sw.WriteLine($"$EndNodes");
                sw.WriteLine($"$Elements");
                sw.WriteLine($"{mesh.Cells.Count}");
                int wholeIndex = 1;
                foreach (var cell in mesh.Cells)
                {
                    string line = $"";
                    line += $"{wholeIndex} ";
                    line += $"{(int)cell.CellType} ";
                    line += $"{(int)cell.Dummy} ";
                    line += $"{cell.PhysicalID} ";
                    line += $"{cell.EntityID} ";
                    for (int j = 0; j < cell.NodesIndex.Length; j++)
                    {
                        if (j == cell.NodesIndex.Length - 1)
                        {
                            line += $"{cell.NodesIndex[j]}";
                        }
                        else
                        {
                            line += $"{cell.NodesIndex[j]} ";
                        }
                    }
                    wholeIndex++;
                    sw.WriteLine(line);
                }
                sw.WriteLine($"$EndElements");
            }
        }

    }
}
