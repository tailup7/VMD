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
                    if (line.StartsWith("facet"))
                    {
                        // 法線ベクトルを読み込む
                        string[] normalLine = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        float nx = float.Parse(normalLine[2]);
                        float ny = float.Parse(normalLine[3]);
                        float nz = float.Parse(normalLine[4]);

                        // 三角形の頂点を読み込む
                        line = reader.ReadLine();
                        string[] vertexLine1 = reader.ReadLine().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        float x1 = float.Parse(vertexLine1[1]);
                        float y1 = float.Parse(vertexLine1[2]);
                        float z1 = float.Parse(vertexLine1[3]);

                        string[] vertexLine2 = reader.ReadLine().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        float x2 = float.Parse(vertexLine2[1]);
                        float y2 = float.Parse(vertexLine2[2]);
                        float z2 = float.Parse(vertexLine2[3]);

                        string[] vertexLine3 = reader.ReadLine().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        float x3 = float.Parse(vertexLine3[1]);
                        float y3 = float.Parse(vertexLine3[2]);
                        float z3 = float.Parse(vertexLine3[3]);

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
            string filePath = Path.Combine(dirPath, "surface-from-gmsh22.stl"); 
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
