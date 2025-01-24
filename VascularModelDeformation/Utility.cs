using VascularModelDeformation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace VascularModelDeformation
{
    public static class Utility
    {
        /// <summary>
        /// オブジェクトがnullかどうかを判定する
        /// nullだとtrueを返す
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static bool IsNull(this object obj)
        {
            if (Object.ReferenceEquals(obj, null))
                return true;
            return false;
        }
        public static float[,] MatMat(float[,] matA, float[,] matB)
        {
            float[,] res = new float[matA.GetLength(0), matA.GetLength(1)];
            for (int i = 0; i < matA.GetLength(0); i++)
            {
                for (int j = 0; j < matA.GetLength(1); j++)
                {
                    for (int k = 0; k < matA.GetLength(0); k++)
                    {
                        res[i, j] += matA[i, k] * matB[k, j];
                    }
                }
            }
            return res;
        }
        /// <summary>
        /// matrixとvectorの掛け算
        /// 戻り値はvector
        /// </summary>
        /// <param name="mat"></param>
        /// <param name="vec"></param>
        /// <returns></returns>
        public static float[] MatVec(float[,] mat, float[] vec)
        {
            float[] res = new float[vec.Length];
            for (int i = 0; i < mat.GetLength(0); i++)
            {
                res[i] = 0.0f;
                for (int j = 0; j < mat.GetLength(1); j++)
                {
                    res[i] += mat[i, j] * vec[j];
                }
            }
            return res;
        }
        /// <summary>
        /// ベクトルを正規化する
        /// </summary>
        /// <param name="vec"></param>
        /// <returns></returns>
        public static float[] NormalizeVector(float[] vec)
        {
            float length = 0;
            for (int i = 0; i < vec.Length; i++)
            {
                length += vec[i] * vec[i];
            }
            length = (float)Math.Sqrt(length);
            float[] res = new float[3] { vec[0] / length, vec[1] / length, vec[2] / length };
            return res;
        }
        /// <summary>
        /// 二つのベクトルが比較
        /// まったく一緒だったら、trueを返す
        /// 一緒じゃなかったら、falseを返す
        /// </summary>
        /// <param name="vecA"></param>
        /// <param name="vecB"></param>
        /// <returns></returns>
        public static bool VectorComparison(float[] vecA, float[] vecB)
        {
            if (vecA.Length != vecB.Length)
                return false;

            int vecSize = vecA.Length;
            for (int i = 0; i < vecSize; i++)
            {
                if (vecA[i] != vecB[i])
                {
                    return false;
                }
            }
            return true;
        }
        /// <summary>
        /// 二つのベクトルvecAとvecBが与えられたとき単位外積ベクトルを求める
        /// </summary>
        /// <param name="vecA"></param>
        /// <param name="vecB"></param>
        /// <returns></returns>
        public static float[] NormalOuterProduct(float[] vecA, float[] vecB)
        {
            float[] res = new float[3];
            res[0] = vecA[1] * vecB[2] - vecA[2] * vecB[1];
            res[1] = vecA[2] * vecB[0] - vecA[0] * vecB[2];
            res[2] = vecA[0] * vecB[1] - vecA[1] * vecB[0];
            float length = GetVecLength(res);
            if (length == 0.0f)
                return res;

            for (int i = 0; i < 3; i++)
            {
                res[i] = res[i] / length;
            }
            return res;
        }
        /// <summary>
        /// 内積を計算
        /// 引数1、float型の配列
        /// 引数2、float型の配列
        /// 同じ長さかのチェックを入れたほうがいい
        /// </summary>
        /// <param name="vecA"></param>
        /// <param name="vecB"></param>
        /// <returns></returns>
        public static float DotProduct(float[] vecA, float[] vecB)
        {
            float res = 0.0f;
            for (int i = 0; i < vecA.Length; i++)
            {
                res += vecA[i] * vecB[i];
            }
            return res;
        }
        /// <summary>
        /// 二つのベクトルから内積を計算する
        /// </summary>
        /// <param name="v0"></param>
        /// <param name="v1"></param>
        /// <returns></returns>
        public static float DotProduct(Vector3 v0, Vector3 v1)
        {
            return Vector3.Dot(v0, v1);
        }
        /// <summary>
        /// 二つのベクトルから外積を計算する
        /// </summary>
        /// <param name="v0"></param>
        /// <param name="v1"></param>
        /// <returns></returns>
        public static Vector3 CrossProduct(Vector3 v0, Vector3 v1)
        {
            return Vector3.Cross(v0, v1);
        }
        public static float L2Norm(Vector3 v)
        {
            return v.Length();
        }
        /// <summary>
        /// vectorの長さを求める
        /// </summary>
        /// <param name="vecA"></param>
        /// <returns></returns>
        public static float GetVecLength(float[] vecA)
        {
            float res = 0.0f;
            for (int i = 0; i < vecA.Length; i++)
            {
                res += vecA[i] * vecA[i];
            }
            res = (float)Math.Sqrt(res);
            return res;
        }
        public static Vector3 CrossProductNormal(Vector3 v0, Vector3 v1)
        {
            Vector3 crossProduct = CrossProduct(v0, v1);
            float length = crossProduct.Length();
            return new Vector3(
                crossProduct.X / length,
                crossProduct.Y / length,
                crossProduct.Z / length
            );
        }
        public static float[] CrossProductNormal(Node node0, Node node1, Node node2)
        {
            // Calculate edge vector
            Vector3 v0 = new Vector3(
                node1.X - node0.X,
                node1.Y - node0.Y,
                node1.Z - node0.Z
            );
            Vector3 v1 = new Vector3(
                node2.X - node0.X,
                node2.Y - node0.Y,
                node2.Z - node0.Z
            );
            Vector3 crossProductNormal = CrossProductNormal(v0, v1);
            return new float[3]
            {
                crossProductNormal.X,
                crossProductNormal.Y,
                crossProductNormal.Z
            };
        }
        /// <summary>
        /// n0を始点、n1を終点とする辺のベクトルを計算する
        /// </summary>
        /// <param name="n0"></param>
        /// <param name="n1"></param>
        /// <returns></returns>
        public static Vector3 EdgeVector(Node n0, Node n1)
        {
            return new Vector3(
                n0.X - n1.X,
                n0.Y - n1.Y,
                n0.Z - n1.Z
            );
        }
        /// <summary>
        /// 単位行列を作る
        /// 引数1 int size 単位行列の列と行の長さ、一緒なので列の長さだけでいい
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        public static float[,] MakeIdentityMatrix(int size)
        {
            float[,] identity = new float[size, size];
            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    if (i == j)
                    {
                        identity[i, j] = 1.0f;
                    }
                }
            }
            return identity;
        }
        public static float[,] RotationMatrix(float[] vecA, float[] vecB)
        {
            float theta;
            float[,] rotationMatrix = new float[3, 3];
            if (Utility.VectorComparison(vecA, vecB))
                return (rotationMatrix = Utility.MakeIdentityMatrix(3));

            float[] normalOuterProduct = Utility.NormalOuterProduct(vecA, vecB);
            if (normalOuterProduct[0] == 0.0f & normalOuterProduct[1] == 0.0f & normalOuterProduct[2] == 0.0f)
            {
                rotationMatrix = Utility.MakeIdentityMatrix(3);
                rotationMatrix[0, 0] = -1.0f * rotationMatrix[0, 0];
                rotationMatrix[1, 1] = -1.0f * rotationMatrix[1, 1];
                rotationMatrix[2, 2] = -1.0f * rotationMatrix[2, 2];
                return rotationMatrix;
            }

            float testA = Utility.DotProduct(vecA, vecB);
            float testB = Utility.GetVecLength(vecA) * Utility.GetVecLength(vecB);
            // math.Acosの引数は-1<= x <= 1の範囲でしか計算できないので、それ以外の場合は例外を返す
            float test = Utility.DotProduct(vecA, vecB) / (Utility.GetVecLength(vecA) * Utility.GetVecLength(vecB));
            if (test > 1)
            {
                theta = 0.0f;
            }
            else
            {
                theta = (float)Math.Acos(Utility.DotProduct(vecA, vecB) / (Utility.GetVecLength(vecA) * Utility.GetVecLength(vecB)));
            }
            float[,] K = new float[3, 3];
            K[0, 0] = 0;
            K[0, 1] = -normalOuterProduct[2];
            K[0, 2] = normalOuterProduct[1];
            K[1, 0] = normalOuterProduct[2];
            K[1, 1] = 0;
            K[1, 2] = -normalOuterProduct[0];
            K[2, 0] = -normalOuterProduct[1];
            K[2, 1] = normalOuterProduct[0];
            K[2, 2] = 0;
            float[,] KK = new float[3, 3];
            KK[0, 0] = normalOuterProduct[0] * normalOuterProduct[0];
            KK[0, 1] = normalOuterProduct[0] * normalOuterProduct[1];
            KK[0, 2] = normalOuterProduct[0] * normalOuterProduct[2];
            KK[1, 0] = normalOuterProduct[1] * normalOuterProduct[0];
            KK[1, 1] = normalOuterProduct[1] * normalOuterProduct[1];
            KK[1, 2] = normalOuterProduct[1] * normalOuterProduct[2];
            KK[2, 0] = normalOuterProduct[2] * normalOuterProduct[0];
            KK[2, 1] = normalOuterProduct[2] * normalOuterProduct[1];
            KK[2, 2] = normalOuterProduct[2] * normalOuterProduct[2];
            //float[,] KK = Utility.MatMat(K, K);
            //Debug.WriteLine($"output the rotation Matrix");
            float[,] E = Utility.MakeIdentityMatrix(3);
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    rotationMatrix[i, j] = (float)Math.Cos(theta) * E[i, j] + (float)Math.Sin(theta) * K[i, j] + (float)(1 - Math.Cos(theta)) * KK[i, j];

                    //if (j == 2)
                    //{
                    //    Debug.Write($"{rotationMatrix[i, j]}" + Environment.NewLine);
                    //} else
                    //{
                    //    Debug.Write($"{rotationMatrix[i, j]} ");
                    //}
                }
            }

            return rotationMatrix;
        }
        /// <summary>
        /// 最も長い辺の長さを計算する
        /// </summary>
        /// <param name="e0"></param>
        /// <param name="e1"></param>
        /// <param name="e2"></param>
        /// <returns></returns>
        public static float CalLengthMax(Vector3 v0, Vector3 v1, Vector3 v2)
        {
            float[] lengthes = new float[3]
            {
                v0.Length(),
                v1.Length(),
                v2.Length()
            };
            return lengthes.Max();
        }
        /// <summary>
        /// 最も短い辺の長さを計算する
        /// </summary>
        /// <param name="e0"></param>
        /// <param name="e1"></param>
        /// <param name="e2"></param>
        /// <returns></returns>
        public static float CalLengthMin(Vector3 v0, Vector3 v1, Vector3 v2)
        {
            float[] lengthes = new float[3]
            {
                v0.Length(),
                v1.Length(),
                v2.Length()
            };
            return lengthes.Min();
        }
        public static float CalLengthMax(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, Vector3 v5)
        {
            float[] lengthes = new float[6]
            {
                v0.Length(),
                v1.Length(),
                v2.Length(),
                v3.Length(),
                v4.Length(),
                v5.Length()
            };
            return lengthes.Max();
        }
        public static float CalLengthMin(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, Vector3 v5)
        {
            float[] lengthes = new float[6]
            {
                v0.Length(),
                v1.Length(),
                v2.Length(),
                v3.Length(),
                v4.Length(),
                v5.Length()
            };
            return lengthes.Min();
        }
        public static void PrismParentGmsh(VTK vtk, int numberOfLayer)
        {
            // これはprismが整列されて入力されているときにのみ正しく動く
            int parent = -1;
            Vector3 realOuterCentroid = new Vector3();
            for (int i = 0; i < vtk.CellVTKs.Count; i++)
            {
                if (i % numberOfLayer == 0)
                {
                    vtk.CellVTKs[i].ParentPrismIndex = i;
                    parent = i;
                    // 0 1 2 3 4 5
                    // 3 4 5が外側の三角形
                    float x = (vtk.Nodes[vtk.CellVTKs[i].NodesIndex[3]].X + vtk.Nodes[vtk.CellVTKs[i].NodesIndex[4]].X + vtk.Nodes[vtk.CellVTKs[i].NodesIndex[5]].X) / 3.0f;
                    float y = (vtk.Nodes[vtk.CellVTKs[i].NodesIndex[3]].Y + vtk.Nodes[vtk.CellVTKs[i].NodesIndex[4]].Y + vtk.Nodes[vtk.CellVTKs[i].NodesIndex[5]].Y) / 3.0f;
                    float z = (vtk.Nodes[vtk.CellVTKs[i].NodesIndex[3]].Z + vtk.Nodes[vtk.CellVTKs[i].NodesIndex[4]].Z + vtk.Nodes[vtk.CellVTKs[i].NodesIndex[5]].Z) / 3.0f;
                    realOuterCentroid = new Vector3(x, y, z);
                    vtk.CellVTKs[i].RealOuterCentroid = realOuterCentroid;
                }
                else
                {
                    vtk.CellVTKs[i].ParentPrismIndex = parent;
                    vtk.CellVTKs[i].RealOuterCentroid = realOuterCentroid;
                }
            }
        }
        public static void PrismParentIcem(VTK vtk, int numberOfLayer)
        {
            int parent = -1;
            int oneLayerPrismNumber = vtk.CellVTKs.Count / numberOfLayer;
            Vector3 realOuterCentroid = new Vector3();
            int counter = 0;
            for (int i = vtk.CellVTKs.Count - 1; i >= 0; i--)
            {
                if (counter < oneLayerPrismNumber)
                {
                    vtk.CellVTKs[i].ParentPrismIndex = i;
                    parent = i;
                    float x = (vtk.Nodes[vtk.CellVTKs[i].NodesIndex[0]].X + vtk.Nodes[vtk.CellVTKs[i].NodesIndex[1]].X + vtk.Nodes[vtk.CellVTKs[i].NodesIndex[2]].X) / 3.0f;
                    float y = (vtk.Nodes[vtk.CellVTKs[i].NodesIndex[0]].Y + vtk.Nodes[vtk.CellVTKs[i].NodesIndex[1]].Y + vtk.Nodes[vtk.CellVTKs[i].NodesIndex[2]].Y) / 3.0f;
                    float z = (vtk.Nodes[vtk.CellVTKs[i].NodesIndex[0]].Z + vtk.Nodes[vtk.CellVTKs[i].NodesIndex[1]].Z + vtk.Nodes[vtk.CellVTKs[i].NodesIndex[2]].Z) / 3.0f;
                    realOuterCentroid = new Vector3(x, y, z);
                    vtk.CellVTKs[i].RealOuterCentroid = realOuterCentroid;
                }
                else
                {
                    vtk.CellVTKs[i].ParentPrismIndex = vtk.CellVTKs[i + oneLayerPrismNumber].ParentPrismIndex;
                    vtk.CellVTKs[i].RealOuterCentroid = vtk.CellVTKs[vtk.CellVTKs[i + oneLayerPrismNumber].ParentPrismIndex].RealOuterCentroid;
                }
                counter++;
            }
        }

        //
        // HagenPoiseuilleValidation
        //
    }
}
