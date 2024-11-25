using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
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

        public Form1()
        {
            AllocConsole();
            Log.ConsoleWriteLine("start output log");

            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // makemesh.py のパス (適切に変更してください!)
            string pythonScriptPath = @"C:\git\VMD\makemesh.py";

            // Python.exe のパス (適切に変更してください!)
            string pythonExePath = @"C:\git\VMD\venv\Scripts\python.exe";

            // Pythonスクリプトを実行するためのプロセス設定
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = pythonExePath,
                Arguments = $"\"{pythonScriptPath}\"", // スクリプトの引数 (必要に応じて追加)
                CreateNoWindow = true, // コマンドプロンプトのウィンドウを表示しない
                UseShellExecute = false, // 標準出力を利用するため
            };

            try
            {
                // プロセスを開始してPythonスクリプトを実行
                Process process = Process.Start(startInfo);
                process.WaitForExit(); // スクリプトの実行が完了するのを待つ

                MessageBox.Show("Making mesh has been done successfully.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error executing Python script: {ex.Message}");
            }

        }
    }
}
