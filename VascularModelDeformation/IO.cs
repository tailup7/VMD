using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        public (Mesh, string) ReadGMSH22Ori() // ファイルを読み込んでメッシュデータ (mesh) とディレクトリパス (dirPath) を返す
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


    }
}
