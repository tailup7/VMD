using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using static Microsoft.WindowsAPICodePack.Shell.PropertySystem.SystemProperties.System;

namespace VascularModelDeformation
{
    /** 四面体の頂点の指標の説明
 *             N3
 *            / |  \
 *          ↗  |   ↖
 *      L3 E3   |    E5 L5
 *        /     |     \
 *       /   L2 |       \
 *      N0--←E2-|--------N2
 *       \      ↑       /
 *        \     E4  L4 ↗
 *     L0 E0    |     E1 L1
 *          ↘  |    /
 *            \    /
 *              N1
 *
 */

    [Serializable]
    public class Tetrahedron : IGeometricShape
    {
        public int Index { get; set; }
        public int[] NodeIndexes { get; set; } = new int[4] { -1, -1, -1, -1 };
        /// <summary>
        /// Nodeは基本的に3次元上の座標を表すときに用いる
        /// </summary>
        public Node N0 { get; set; }
        public Node N1 { get; set; }
        public Node N2 { get; set; }
        public Node N3 { get; set; }
        public List<Node> Nodes { get; set; } = new List<Node>();
        /// <summary>
        /// Vector3は基本的にベクトルを表すときに用いる
        /// </summary>
        private Vector3 E0;
        private Vector3 E1;
        private Vector3 E2;
        private Vector3 E3;
        private Vector3 E4;
        private Vector3 E5;
        private float E0L2Norm;
        private float E1L2Norm;
        private float E2L2Norm;
        private float E3L2Norm;
        private float E4L2Norm;
        private float E5L2Norm;
        private float EL2NormMin = float.MaxValue;
        private float EL2NormMax = float.MinValue;
        public Node Center { get; set; }
        public float Volume { get; set; }
        public float Area { get; set; }
        private float r { get; set; }
        private float R { get; set; }
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
        public float QualityAspectRatio { get; set; } = -1.0f;
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
        /// <summary>
        /// 中心線の何番目のノードに対応するかのインデックス（0-index）
        /// </summary>
        public int CorrespondCenterlineIndex { get; set; } = -1;

        /// <summary>
        /// constructor
        /// </summary>
        public Tetrahedron(int index, Node n0, Node n1, Node n2, Node n3)
        {
            this.Index = index;
            this.N0 = n0;
            this.N1 = n1;
            this.N2 = n2;
            this.N3 = n3;
            this.Nodes = new List<Node>() { n0, n1, n2, n3 };
            this.Calculate();
        }
        public Tetrahedron(Node n0, Node n1, Node n2, Node n3)
        {
            this.N0 = n0;
            this.N1 = n1;
            this.N2 = n2;
            this.N3 = n3;
            this.Nodes = new List<Node>() { n0, n1, n2, n3 };
            this.Calculate();
        }
        /// <summary>
        /// 必要な情報を計算する
        /// </summary>
        /// <param name="n0"></param>
        /// <param name="n1"></param>
        /// <param name="n2"></param>
        /// <param name="n3"></param>
        public void Calculate()
        {
            Node n0 = this.N0;
            Node n1 = this.N1;
            Node n2 = this.N2;
            Node n3 = this.N3;
            this.E0 = Utility.EdgeVector(n0, n1);
            this.E1 = Utility.EdgeVector(n1, n2);
            this.E2 = Utility.EdgeVector(n2, n0);
            this.E3 = Utility.EdgeVector(n0, n3);
            this.E4 = Utility.EdgeVector(n1, n3);
            this.E5 = Utility.EdgeVector(n2, n3);
            this.E0L2Norm = Utility.L2Norm(this.E0);
            this.E1L2Norm = Utility.L2Norm(this.E1);
            this.E2L2Norm = Utility.L2Norm(this.E2);
            this.E3L2Norm = Utility.L2Norm(this.E3);
            this.E4L2Norm = Utility.L2Norm(this.E4);
            this.E5L2Norm = Utility.L2Norm(this.E5);
            this.EL2NormMax = Utility.CalLengthMax(this.E0, this.E1, this.E2, this.E3, this.E4, this.E5);
            this.EL2NormMin = Utility.CalLengthMin(this.E0, this.E1, this.E2, this.E3, this.E4, this.E5);
            this.Center = CalculateCenter();
            this.Volume = CalculateVolume();
            this.Area = CalculateArea();
            this.r = Calculater();
            this.R = CalculateR();
            this.QualityAspectRatio = CalculateQualityAspectRatio();
            this.QualityEdgeRatio = CalculateQualityEdgeRatio();
            this.QualityRadiusRatio = CalculateQualityRadiusRatio();
        }
        public Node CalculateCenter()
        {
            return new Node(
                (this.N0.X + this.N1.X + this.N2.X + this.N3.X) / 4.0f,
                (this.N0.Y + this.N1.Y + this.N2.Y + this.N3.Y) / 4.0f,
                (this.N0.Z + this.N1.Z + this.N2.Z + this.N3.Z) / 4.0f
            );
        }
        /// <summary>
        /// 体積を計算する
        /// </summary>
        /// <param name="v0"></param>
        /// <param name="v2"></param>
        /// <param name="v3"></param>
        /// <returns></returns>
        public float CalculateVolume()
        {
            return Utility.DotProduct(Utility.CrossProduct((-1.0f) * this.E2, this.E0), this.E3) / 6.0f;
        }
        /// <summary>
        /// 表面積を計算する
        /// </summary>
        /// <param name="v0"></param>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <param name="v3"></param>
        /// <param name="v4"></param>
        /// <returns></returns>
        public float CalculateArea()
        {
            float firatTerm = Utility.L2Norm(Utility.CrossProduct(this.E2, this.E0));
            float secondTerm = Utility.L2Norm(Utility.CrossProduct(this.E3, this.E0));
            float thirdTerm = Utility.L2Norm(Utility.CrossProduct(this.E4, this.E1));
            float fourthTerm = Utility.L2Norm(Utility.CrossProduct(this.E3, this.E2));
            return 0.5f * (firatTerm + secondTerm + thirdTerm + fourthTerm);
        }
        /// <summary>
        /// rを計算する
        /// </summary>
        /// <param name="volume"></param>
        /// <param name="surfaceArea"></param>
        /// <returns></returns>
        public float Calculater()
        {
            return 3.0f * this.Volume / this.Area;
        }
        /// <summary>
        /// Rを計算する
        /// </summary>
        /// <param name="v0"></param>
        /// <param name="v2"></param>
        /// <param name="v3"></param>
        /// <param name="volume"></param>
        /// <returns></returns>
        public float CalculateR()
        {
            Vector3 firstTerm = (float)Math.Pow(Utility.L2Norm(this.E3), 2) * Utility.CrossProduct(this.E2, this.E0);
            Vector3 secondTerm = (float)Math.Pow(Utility.L2Norm(this.E2), 2) * Utility.CrossProduct(this.E3, this.E0);
            Vector3 thirdTerm = (float)Math.Pow(Utility.L2Norm(this.E0), 2) * Utility.CrossProduct(this.E3, this.E2);
            return Utility.L2Norm(firstTerm + secondTerm + thirdTerm) / (12.0f * this.Volume);
        }
        /// <summary>
        /// QualityAspectRatioを計算する
        /// </summary>
        /// <param name="eL2Max"></param>
        /// <param name="r"></param>
        /// <returns></returns>
        public float CalculateQualityAspectRatio()
        {
            return this.EL2NormMax / (2.0f * (float)Math.Sqrt(6) * this.r);
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
        /// <summary>
        /// QualityRadiusRatioを計算する
        /// </summary>
        /// <param name="R"></param>
        /// <param name="r"></param>
        /// <returns></returns>
        public float CalculateQualityRadiusRatio()
        {
            return this.R / (3.0f * this.r);
        }
    }
}
