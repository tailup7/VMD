using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VascularModelDeformation
{
    public partial class Form1 : Form
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AllocConsole();
        //#error version
        private string DirPath;

        public IO IO { get; set; } 
        private LocalPath LP { get; set; }

        public Form1()
        {
            AllocConsole();
            Log.ConsoleWriteLine("start output log");

            InitializeComponent();

            var os = Environment.OSVersion;
            Console.WriteLine("Current OS Information:\n");
            string thisOs = os.Platform.ToString();
            Console.WriteLine("Platform: {0:G}", os.Platform);
            Console.WriteLine("Version String: {0}", os.VersionString);
            Console.WriteLine("Version Information:");
            Console.WriteLine("   Major: {0}", os.Version.Major);
            Console.WriteLine("   Minor: {0}", os.Version.Minor);
            Console.WriteLine("Service Pack: '{0}'", os.ServicePack);
            Console.WriteLine(RuntimeInformation.FrameworkDescription);

            this.LP = new LocalPath();
            this.IO = new IO();
        }

        /// <summary>
        /// Gmshで作成したメッシュファイルの表面メッシュをSTLファイルとして出力
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button2_Click(object sender, EventArgs e)
        {
            Model model = new Model();
            (model.Mesh, this.DirPath) = this.IO.ReadGMSH22Ori();
            model.Mesh.AnalyzeMesh();
            this.IO.WriteSTLWALLSurface(model.Mesh, this.DirPath);
        }

        /// <summary>
        /// Button2で得たSTLとその中心線から、表面三角形パッチと中心線Nodeの関係を出力
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button3_Click(object sender, EventArgs e)
        {
            List<Triangle> triangles = this.IO.ReadSTLASCII();
            STL stl = new STL(triangles);
            Centerline centerline = new Centerline();
            (centerline, this.DirPath) = this.IO.ReadCenterline();

            Algorithm.CorrespondenceBetweenCenterlineNodeAndLumenalSurfaceTriangle(centerline.Nodes, triangles);

            // this.IO.WriteVTKSTL(stl, this.DirPath, @"test.vtk");
            this.IO.WritePLY(stl, this.DirPath, @"test.ply");
            // this.IO.WriteVTKPolydataCenterline(centerline, this.DirPath, 0);
        }

        /// <summary>
        /// STLと中心線から、中心線Node周りの平均半径を計算する
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button5_Click(object sender, EventArgs e)
        {
            List<Triangle> triangles = this.IO.ReadSTLASCII();
            STL stl = new STL(triangles);
            Centerline centerline = new Centerline();
            (centerline, this.DirPath) = this.IO.ReadCenterline();
            List<float> radius = Algorithm.CorrespondenceBetweenCenterlineNodeAndLumenalSurfaceNode_and_calculateRadius(centerline.Nodes, stl);
            radius = Utility.MovingAverage7(radius);
            this.IO.WriteRadius(radius, this.DirPath, "radius.txt");
        }

        /// <summary>
        /// Gmshで作成した解析モデルを、基準中心線と目標中心線から計算される移動変形量と、Button5で得る目標半径に従って移動させ、目標解析モデルを得る
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button4_Click(object sender, EventArgs e)
        {
            Console.WriteLine($"test");
            LocalPath lp = new LocalPath();
            // usingを使うことで、usingを抜けるときにDisposeが動いて、メモリの破棄を行う
            using (var model = new Model())
            {
                (model.Centerline, this.DirPath) = this.IO.ReadCenterline();
                if (model.Centerline == null)
                    return;
                (model.CenterlineFinalPosition, this.DirPath) = this.IO.ReadCenterline();
                string test = this.DirPath;
                model.CalculateCenterlineAndCenterlineFinalPositoin(); // ここでCenterlineとCenterlineFinalPositionのデータを用いて回転行列などを求める
                // this.IO.WriteVTKPolydataCenterline(model.Centerline, test, 0);
                // this.IO.WriteVTKPolydataCenterline(model.CenterlineFinalPosition, test, 1);
                model.SurfaceCorrespondIndex = this.IO.ReadPLY();
                model.CenterlineFinalPosition.Radius = this.IO.ReadRadius(); // 目標中心線をセットする
                (model.Mesh, this.DirPath) = this.IO.ReadGMSH22Ori();
                model.Mesh.AnalyzeMesh();

                //model.Mesh.FetchPrismLayerData();
                model.Mesh.SetCellCorrespondCenterlineIndex(model.SurfaceCorrespondIndex);
                //model.Mesh.AssignFaceCorrespondIndexToNodeCorrespondIndex();
                model.Mesh.AssignFaceCorrespondIndexToNodeCorrespondIndexList();

                model.OrganizeMeshData();
                model.RemoveUnneedPartMesh();
                model.Mesh.AnalyzeMesh();
                model.MeshSurfaceAndPrismLayer.AnalyzeMesh();
                model.MeshOuterSurface = model.Mesh.MakeOuterSurface(model.Mesh); // これだと変形前だよね
                model.MeshInnerSurface = model.Mesh.MakeInnerSurfaceMesh(model.Mesh);
                // this.IO.WriteVTKSurfaceWithCorrespondIndex(model.MeshOuterSurface, test, "MostOuterSurface-before.vtk");
                //model.MeshDeformation(model.MeshSurfaceAndPrismLayer, model.Centerline);
                model.MeshDeformationMultiple(model.MeshSurfaceAndPrismLayer, model.Centerline, model.CenterlineFinalPosition);
                // this.IO.WriteVTKMesh(model.MeshSurfaceAndPrismLayer, test, "eeeeeeeeeeeeeeeee.vtk");
                model.MeshSurfaceAndPrismLayer.AllEdgeSwap();
                model.MeshOuterSurface = model.Mesh.MakeOuterSurface(model.MeshSurfaceAndPrismLayer); // これだと変形後のものが吐き出せる
                // this.IO.WriteSTL(model.MeshSurfaceAndPrismLayer, test, "MostOuterSurface.stl");
                // this.IO.WriteVTKSurfaceWithCorrespondIndex(model.MeshOuterSurface, test, "MostOuterSurface.vtk");
                this.IO.WriteSTLInnerSurfaceFromCellsMostInnerPrism(model.MeshSurfaceAndPrismLayer, test, "MostInnerSurface.stl");
                this.IO.WriteGMSH22(model.MeshSurfaceAndPrismLayer, test, "MeshNeed.msh");

                ProcessStartInfo start = new ProcessStartInfo();
                start.FileName = lp.PythonEnvironmentPath;
                string pythonPath = lp.MakeInnerMeshPath;

                string firstArgument = pythonPath;
                string secondArgument = Path.Combine(test, "MostInnerSurface.stl");
                string thirdArgument = Path.Combine(test, "MeshInner.msh");
                string fourthArgument = Path.Combine(test, "MeshInner.vtk");
                start.Arguments = firstArgument + " " + secondArgument + " " + thirdArgument + " " + fourthArgument;
                start.UseShellExecute = false;
                start.RedirectStandardOutput = true;
                using (Process process = Process.Start(start))
                {
                    using (StreamReader reader = process.StandardOutput)
                    {
                        string result = reader.ReadToEnd();
                        Debug.WriteLine(result);
                    }
                }

                (model.MeshInner, this.DirPath) = this.IO.ReadGMSH22Inner();
                model.OrganizeMeshDataInner();
                model.MeshInner.AnalyzeMesh();
                //this.IO.WriteGMSH22(model.MeshInner, this.DirPath, "MeshInnerTest.msh");
                model.MakeMergeMesh();
                model.MeshMerged.Cells.Sort();
                for (int j = 0; j < model.MeshMerged.Cells.Count; j++)
                {
                    model.MeshMerged.Cells[j].Index = j + 1;
                }

                this.IO.WriteGMSH22(model.MeshMerged, this.DirPath, "MeshMerged.msh");
                this.IO.WriteVTKMesh(model.MeshMerged, this.DirPath, "MeshMerged.vtk");
                // this.IO.WriteCSVCellQuality(model.MeshMerged, this.DirPath);
            }
            GC.Collect();
        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void label6_Click(object sender, EventArgs e)
        {

        }


    }
}
