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
                sw.WriteLine($"solid surface from imai");
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

    }
}
