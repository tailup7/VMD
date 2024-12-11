using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VascularModelDeformation
{
    public class LocalPath
    {
        public string AppPath { get; private set; }
        public string LocalPathTextPath { get; private set; }
        public string PythonEnvironmentPath { get; private set; }
        public string MakeInnerMeshPath { get; private set; }
        /// <summary>
        /// constructor
        /// </summary>
        public LocalPath()
        {
            this.AppPath = Process.GetCurrentProcess().MainModule.FileName;
            string appPathParent = System.IO.Path.GetDirectoryName(this.AppPath);
            string appPathParentParent = System.IO.Path.GetDirectoryName(appPathParent);
            string appPathParentParentParent = System.IO.Path.GetDirectoryName(appPathParentParent);
            string appPathParentParentParentParent = System.IO.Path.GetDirectoryName(appPathParentParentParent);
            string localPathTextPath = Path.Combine(appPathParentParentParentParent, "LocalPath.txt");
            this.LocalPathTextPath = localPathTextPath;
            try
            {
                string[] lines = File.ReadAllLines(this.LocalPathTextPath);
                this.PythonEnvironmentPath = Path.GetFullPath(lines[0].Replace('/', '\\'));
                this.MakeInnerMeshPath = Path.GetFullPath(lines[1].Replace('/', '\\'));
            }
            catch (IOException e)
            {
                Debug.WriteLine($"An error occurred: {e.Message}");
            }
        }
        public void Print()
        {
            Debug.WriteLine($"This form application path is {this.AppPath}");
            Debug.WriteLine($"LocalPathTextPath is {this.LocalPathTextPath}");
            Debug.WriteLine($"PythonEnvironmentPath is {this.PythonEnvironmentPath}");
            Debug.WriteLine($"MakeInnerMeshPath is {this.MakeInnerMeshPath}");
        }


    }
}

