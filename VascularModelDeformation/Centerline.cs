using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VascularModelDeformation
{
    public class Centerline
    {
        public List<NodeCenterline> Nodes { get; private set; }
        public Node InletLocation { get; private set; }
        public Node OutletLocation { get; private set; }

        /// <summary>
        /// centerline.txtを解釈するコード
        /// centerline.txtは一行目に、
        /// pt3d 点の数
        /// が書いてあるので実際の点数は「ファイルの行数 - 1」
        /// </summary>
        /// <param name="lines"></param>
        public void InterpretCenterline(string[] lines) 
        {
            int centerlineLength = lines.Length - 1;
            List<NodeCenterline> nodes = new List<NodeCenterline>();
            for (int currentline = 1; currentline < lines.Length; currentline++) 
            {
                string[] cols = lines[currentline].Split(' '); 
                var node = new NodeCenterline()
                {
                    Index = currentline - 1,
                    X = float.Parse(cols[0]), 
                    Y = float.Parse(cols[1]),
                    Z = float.Parse(cols[2]),
                };
                nodes.Add(node);
            }
            this.Nodes = nodes;
            DetectInletLocation(nodes);
            DetectOutletLocation(nodes);
            this.CalculateCenterlineTangentVectors();
        }
        /// <summary>
        /// inletの位置を検出する
        /// この値は,gmshでファイルのphysicalを特定するために用いる
        /// </summary>
        /// <param name="nodes"></param>
        private void DetectInletLocation(List<NodeCenterline> nodes)
        {
            int inletIndex = 0;
            this.InletLocation = new Node();
            this.InletLocation.X = nodes[inletIndex].X; 
            this.InletLocation.Y = nodes[inletIndex].Y;
            this.InletLocation.Z = nodes[inletIndex].Z;
        }
        /// <summary>
        /// outletの位置を検出する
        /// この値は,gmshでファイルのphysicalを特定するために用いる
        /// </summary>
        /// <param name="nodes"></param>
        private void DetectOutletLocation(List<NodeCenterline> nodes)
        {
            int outletIndex = nodes.Count - 1;
            this.OutletLocation = new Node();
            this.OutletLocation.X = nodes[outletIndex].X;
            this.OutletLocation.Y = nodes[outletIndex].Y;
            this.OutletLocation.Z = nodes[outletIndex].Z;
        }
        /// <summary>
        /// 回転行列の計算
        /// </summary>
        /// <param name="centerlines"></param>
        public void CalculateRotationMatrix()
        {
            foreach (var node in this.Nodes)
            {
                float[] vecA = new float[3] { node.XTangent, node.YTangent, node.ZTangent }; 
                float[] vecB = new float[3] { node.XMovedTangent, node.YMovedTangent, node.ZMovedTangent }; 
                node.RotationMatrix = Utility.RotationMatrix(vecA, vecB);
            }
        }
        /// <summary>
        /// centerlineのNodeの平行移動量を計算
        /// </summary>
        public void CalculateCenterlineDifference()
        {
            foreach (var node in this.Nodes)
            {
                float[] difference = new float[3] { node.XMoved - node.X, node.YMoved - node.Y, node.ZMoved - node.Z };
                node.Difference = difference;
            }
        }
        /// <summary>
        /// 中心線の各Nodeの接線方向ベクトルを求める
        /// </summary>
        /// <param name="centerline"></param>
        public void CalculateCenterlineTangentVectors()
        {
            int length = this.Nodes.Count;   
            for (int i = 0; i < length; i++)
            {
                if (i == 0)
                {
                    float[] a = new float[3] { this.Nodes[i + 1].X, this.Nodes[i + 1].Y, this.Nodes[i + 1].Z };
                    float[] b = new float[3] { this.Nodes[i].X, this.Nodes[i].Y, this.Nodes[i].Z };
                    var vec = ForwardDifference(a, b); 
                    var vecNormalized = Utility.NormalizeVector(vec);
                    this.Nodes[i].XTangent = vecNormalized[0];
                    this.Nodes[i].YTangent = vecNormalized[1];
                    this.Nodes[i].ZTangent = vecNormalized[2];
                }
                else if (i == length - 1)
                {
                    float[] a = new float[3] { this.Nodes[i].X, this.Nodes[i].Y, this.Nodes[i].Z };
                    float[] b = new float[3] { this.Nodes[i - 1].X, this.Nodes[i - 1].Y, this.Nodes[i - 1].Z };  
                    var vec = BackwardDifference(a, b);
                    var vecNormalized = Utility.NormalizeVector(vec);
                    this.Nodes[i].XTangent = vecNormalized[0];
                    this.Nodes[i].YTangent = vecNormalized[1];
                    this.Nodes[i].ZTangent = vecNormalized[2];
                }
                else
                {
                    float[] a = new float[3] { this.Nodes[i + 1].X, this.Nodes[i + 1].Y, this.Nodes[i + 1].Z };
                    float[] b = new float[3] { this.Nodes[i - 1].X, this.Nodes[i - 1].Y, this.Nodes[i - 1].Z }; 
                    var vec = CentralDifference(a, b);
                    var vecNormalized = Utility.NormalizeVector(vec);
                    this.Nodes[i].XTangent = vecNormalized[0];
                    this.Nodes[i].YTangent = vecNormalized[1];
                    this.Nodes[i].ZTangent = vecNormalized[2];
                }
            }
        }
        /// <summary>
        /// 中心線の移動後の各Nodeの接線方向ベクトルを求める
        /// </summary>
        /// <param name="centerline"></param>
        public void CalculateCenterlineMovedTangentVectors()
        {
            int length = this.Nodes.Count;
            for (int i = 0; i < length; i++)
            {
                if (i == 0)
                {
                    float[] a = new float[3] { this.Nodes[i + 1].XMoved, this.Nodes[i + 1].YMoved, this.Nodes[i + 1].ZMoved };
                    float[] b = new float[3] { this.Nodes[i].XMoved, this.Nodes[i].YMoved, this.Nodes[i].ZMoved };
                    var vec = ForwardDifference(a, b);
                    var vecNormalized = Utility.NormalizeVector(vec);
                    this.Nodes[i].XMovedTangent = vecNormalized[0];
                    this.Nodes[i].YMovedTangent = vecNormalized[1];
                    this.Nodes[i].ZMovedTangent = vecNormalized[2];
                }
                else if (i == length - 1)
                {
                    float[] a = new float[3] { this.Nodes[i].XMoved, this.Nodes[i].YMoved, this.Nodes[i].ZMoved };
                    float[] b = new float[3] { this.Nodes[i - 1].XMoved, this.Nodes[i - 1].YMoved, this.Nodes[i - 1].ZMoved };
                    var vec = BackwardDifference(a, b);
                    var vecNormalized = Utility.NormalizeVector(vec);
                    this.Nodes[i].XMovedTangent = vecNormalized[0];
                    this.Nodes[i].YMovedTangent = vecNormalized[1];
                    this.Nodes[i].ZMovedTangent = vecNormalized[2];
                }
                else
                {
                    float[] a = new float[3] { this.Nodes[i + 1].XMoved, this.Nodes[i + 1].YMoved, this.Nodes[i + 1].ZMoved };
                    float[] b = new float[3] { this.Nodes[i - 1].XMoved, this.Nodes[i - 1].YMoved, this.Nodes[i - 1].ZMoved };
                    var vec = CentralDifference(a, b);
                    var vecNormalized = Utility.NormalizeVector(vec);
                    this.Nodes[i].XMovedTangent = vecNormalized[0];
                    this.Nodes[i].YMovedTangent = vecNormalized[1];
                    this.Nodes[i].ZMovedTangent = vecNormalized[2];
                }
            }
        }
        /// <summary>
        /// 中心差分
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        private static float[] CentralDifference(float[] a, float[] b)
        {
            float dx = a[0] - b[0];
            float dy = a[1] - b[1];
            float dz = a[2] - b[2];
            return new float[] { dx, dy, dz };
        }
        /// <summary>
        /// 前進差分
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        private static float[] ForwardDifference(float[] a, float[] b)
        {
            float dx = a[0] - b[0];
            float dy = a[1] - b[1];
            float dz = a[2] - b[2];
            return new float[] { dx, dy, dz };
        }
        /// <summary>
        /// 後退差分
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        private static float[] BackwardDifference(float[] a, float[] b)
        {
            float dx = a[0] - b[0];
            float dy = a[1] - b[1];
            float dz = a[2] - b[2];
            return new float[] { dx, dy, dz };
        }
    }
}
