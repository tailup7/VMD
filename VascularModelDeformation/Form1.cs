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

        private void button1_Click(object sender, EventArgs e)
        {
            // makemesh.py のパス (適切に変更してください!)
            string pythonScriptPath = @"C:\git\VMD\makemesh.py";

            // Python.exe のパス (適切に変更してください!)
            string pythonExePath = @"C:\git\VMD\venv\Scripts\python.exe";

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = pythonExePath,
                Arguments = $"\"{pythonScriptPath}\"", 
                CreateNoWindow = true, 
                UseShellExecute = false, 
            };

            try
            {
                Process process = Process.Start(startInfo);
                process.WaitForExit(); 

                MessageBox.Show("Making mesh has been done successfully.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error executing Python script: {ex.Message}");
            }

        }

        private void button2_Click(object sender, EventArgs e)
        {
            Model model = new Model();
            (model.Mesh, this.DirPath) = this.IO.ReadGMSH22Ori();
            model.Mesh.AnalyzeMesh();
            this.IO.WriteSTLWALLSurface(model.Mesh, this.DirPath);
        }
    }
}
