using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace VascularModelDeformation
{
    /** 三角形の頂点の指標の説明
*          N2
*          /\
*         /  \
*        /    ↖
*   L1 E1      E0 L0
*      ↙       \
*     /          \
*    /            \
*    ------E2->----
*   N0     L2     N1
*/
    [Serializable]
    public class Triangle
    {
        public int Index { get; set; }
        public int[] NodeIndexes { get; set; } = new int[3] { -1, -1, -1 };
        public Node N0 { get; set; }
        public Node N1 { get; set; }
        public Node N2 { get; set; }
        public List<Node> Nodes { get; set; }
        public Vector3 Normal { get; set; }
        private Vector3 E0 { get; set; }
        private Vector3 E1 { get; set; }
        private Vector3 E2 { get; set; }
        private float E0L2Norm { get; set; }
        private float E1L2Norm { get; set; }
        private float E2L2Norm { get; set; }
        private float EL2NormMin { get; set; }
        private float EL2NormMax { get; set; }
        /// <summary>
        /// 三角形の重心
        /// </summary>
        public Node Center { get; set; }
        /// <summary>
        /// 三角形の内心
        /// </summary>
        public Node InnerCenter { get; set; } 
        /// <summary>
        /// 三角形の外心
        /// </summary>
        public Node CircumCenter { get; set; }
        /// <summary>
        /// 三角形の指標のドキュメント
        /// https://vtk.org/Wiki/images/6/6b/VerdictManual-revA.pdf
        /// </summary>
        public float Area { get; set; } = -1.0f;
        public float Volume { get; set; } = 0.0f;
        private float r { get; set; }
        private float R { get; set; }
        /// <summary>
        /// 以下のような値をとる
        /// もし値を参照するときに負の値であればありえないので
        /// 計算がまだ済んでいないこと
        /// or
        /// 計算が間違っている
        /// ことを探知できるはず
        /// Acceptable Range [1, 1.3]
        /// Full Range [1, DBL_MAx]
        /// Normal Range [1, DBL_MAX]
        /// </summary>
        public float QualityAspectRatio { get; set; } = -1.0f;
        /// <summary>
        /// 以下のような値をとる
        /// もし値を参照するときに負の値であればありえないので
        /// 計算がまだ済んでいないこと
        /// or
        /// 計算が間違っている
        /// ことを探知できるはず
        /// Acceptable Range [1, 1.3]
        /// Full Range [1, DBL_MAx]
        /// Normal Range [1, DBL_MAX]
        /// </summary>
        public float QualityEdgeRatio { get; set; } = -1.0f;
        /// <summary>
        /// 以下のような値をとる
        /// もし値を参照するときに負の値であればありえないので
        /// 計算がまだ済んでいないこと
        /// or
        /// 計算が間違っている
        /// ことを探知できるはず
        /// Acceptable Range [1, 3]
        /// Full Range [1, DBL_MAx]
        /// Normal Range [1, DBL_MAX]
        /// </summary>
        public float QualityRadiusRatio { get; set; } = -1.0f;
        public List<int> AdjacencyIndexes { get; set; } = new List<int>();
        /// <summary>
        /// 中心線の何番目のノードに対応するかのインデックス（0-index）
        /// </summary>
        public int CorrespondCenterlineIndex { get; set; } = -1;
        public Vector3 CorrespondTriangleVector { get; set; } = new Vector3(0.0f, 0.0f, 0.0f);
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="index"></param>
        /// <param name="n0"></param>
        /// <param name="n1"></param>
        /// <param name="n2"></param>
        /// <param name="correspondCenterlineIndex"></param>
        public Triangle(int index, Node n0, Node n1, Node n2, int correspondCenterlineIndex)
        {
            this.Index = index;
            this.N0 = n0;
            this.N1 = n1;
            this.N2 = n2;
            this.Nodes = new List<Node>() { n0, n1, n2 };
            this.CorrespondCenterlineIndex = correspondCenterlineIndex;
            this.Calculate();
        }
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="index"></param>
        /// <param name="n0"></param>
        /// <param name="n1"></param>
        /// <param name="n2"></param>
        public Triangle(int index, Node n0, Node n1, Node n2)
        {
            this.Index = index;
            this.N0 = n0;
            this.N1 = n1;
            this.N2 = n2;
            this.Nodes = new List<Node>() { n0, n1, n2 };
            this.Calculate();
        }
        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="n0"></param>
        /// <param name="n1"></param>
        /// <param name="n2"></param>
        public Triangle(Node n0, Node n1, Node n2)
        {
            this.N0 = n0;
            this.N1 = n1;
            this.N2 = n2;
            this.Nodes = new List<Node>() { n0, n1, n2 };
            this.Calculate();
        }
        public void Calculate()
        {
            Node n0 = this.N0;
            Node n1 = this.N1;
            Node n2 = this.N2;
            this.E0 = Utility.EdgeVector(n1, n2);
            this.E1 = Utility.EdgeVector(n2, n0);
            this.E2 = Utility.EdgeVector(n0, n1);
            this.E0L2Norm = Utility.L2Norm(this.E0);
            this.E1L2Norm = Utility.L2Norm(this.E1);
            this.E2L2Norm = Utility.L2Norm(this.E2);
            this.EL2NormMax = Utility.CalLengthMax(this.E0, this.E1, this.E2);
            this.EL2NormMin = Utility.CalLengthMin(this.E0, this.E1, this.E2);
            this.Normal = Utility.CrossProductNormal(this.E1, (-1.0f) * this.E0);
            this.Center = CalculateCenter();
            this.Area = CalculateArea();
            this.Volume = CalculateVolume();
            this.r = Calculater();
            this.R = CalculateR();
            this.QualityAspectRatio = CalculateQualityAspectRatio();
            this.QualityEdgeRatio = CalculateQualityEdgeRatio();
            this.QualityRadiusRatio = CalculateQualityRadiusRatio();
        }
        /// <summary>
        /// 三角形の重心を計算する
        /// </summary>
        /// <param name="n0"></param>
        /// <param name="n1"></param>
        /// <param name="n2"></param>
        /// <returns></returns>
        public Node CalculateCenter()
        {
            return new Node(
                (this.N0.X + this.N1.X + this.N2.X) / 3.0f,
                (this.N0.Y + this.N1.Y + this.N2.Y) / 3.0f,
                (this.N0.Z + this.N1.Z + this.N2.Z) / 3.0f
            );
        }
        /// <summary>
        /// 三角形の体積を計算する
        /// 2次元なので0.0fを返す
        /// </summary>
        /// <returns></returns>
        public float CalculateVolume()
        {
            return 0.0f;
        }
        /// <summary>
        /// 三角形の面積を計算する
        /// </summary>
        /// <param name="e0"></param>
        /// <param name="e1"></param>
        /// <returns></returns>
        public float CalculateArea()
        {
            return 0.5f * Utility.CrossProductNormal(this.E1, (-1.0f) * this.E0).Length();
        }
        /// <summary>
        /// rを計算する
        /// </summary>
        /// <param name="area"></param>
        /// <param name="e0L2Norm"></param>
        /// <param name="e1L2Norm"></param>
        /// <param name="e2L2Norm"></param>
        /// <returns></returns>
        public float Calculater() 
        {
            return (2.0f * this.Area) / (this.E0L2Norm + this.E1L2Norm + this.E2L2Norm);
        }
        /// <summary>
        /// Rを計算する
        /// </summary>
        /// <param name="r"></param>
        /// <param name="e0L2Norm"></param>
        /// <param name="e1L2Norm"></param>
        /// <param name="e2L2Norm"></param>
        /// <returns></returns>
        public float CalculateR() 
        {
            return (this.E0L2Norm * this.E1L2Norm * this.E2L2Norm) / (2.0f * r * (this.E0L2Norm + this.E1L2Norm + this.E2L2Norm));
        }
        /// <summary>
        /// QualityAspectRatioを計算する
        /// </summary>
        /// <param name="vL2Max"></param>
        /// <param name="r"></param>
        /// <returns></returns>
        public float CalculateQualityAspectRatio()
        {
            //return this.EL2NormMax / (2.0f * (float)Math.Sqrt(3) * this.r);
            return (this.EL2NormMax * (this.E0L2Norm + this.E1L2Norm + this.E2L2Norm)) / (4.0f * (float)Math.Sqrt(3) * this.Area);
        }
        /// <summary>
        /// QualityEdgeRatioを計算する
        /// </summary>
        /// <param name="eL2Max"></param>
        /// <param name="eL2Min"></param>
        /// <returns></returns>
        public float CalculateQualityEdgeRatio()
        {
            return this.EL2NormMax / this.EL2NormMin;
        }
        public float CalculateQualityRadiusRatio()
        {
            return this.R / (2.0f * this.r);
        }
    }
}
