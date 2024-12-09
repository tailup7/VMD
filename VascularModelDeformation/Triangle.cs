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
    }
}
