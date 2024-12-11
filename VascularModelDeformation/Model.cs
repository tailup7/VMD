using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VascularModelDeformation
{
    [Serializable]
    public class Model : IDisposable
    {
        public int Index { get; set; }
        public string Name { get; set; }
        /// <summary>
        /// ディレクトリパス
        /// </summary>
        public string DirPath { get; set; }
        /// <summary>
        /// 中心線
        /// 元
        /// </summary>
        public Centerline Centerline { get; set; }
        /// <summary>
        /// 中心線
        /// 変形先
        /// </summary>
        public Centerline CenterlineFinalPosition { get; set; }
        public Mesh Mesh { get; set; }
        public MeshSurface MeshOuterSurface { get; set; }
        public MeshSurface MeshInnerSurface { get; set; }
        public MeshSurfaceAndPrismLayer MeshSurfaceAndPrismLayer { get; set; }
        public MeshInner MeshInner { get; set; }
        public Mesh MeshMerged { get; set; }
        public Dictionary<int, int> IndexPair { get; set; }
        private List<Boundary> Boundaries { get; set; }
        public NodeSurface[] Surface3DCoordinate { get; set; }
        public List<int> SurfaceCorrespondIndex { get; set; }
        /// <summary>
        /// track whether dispose has been called
        /// </summary>
        private bool Disposed = false;
        /// <summary>
        /// メモリの破棄を行う
        /// Disposeだけでは不十分であり、Destructorと同時に使うものらしい
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }
        /// <summary>
        /// メモリの破棄を行う
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!Disposed)
            {
                if (disposing)
                {

                }

                this.Centerline = null;
                this.CenterlineFinalPosition = null;
                this.Mesh = null;
                this.MeshOuterSurface = null;
                this.MeshInnerSurface = null;
                this.MeshSurfaceAndPrismLayer = null;
                this.MeshInner = null;
                this.MeshMerged = null;
                this.IndexPair = null;
                this.Boundaries = null;
                this.Surface3DCoordinate = null;
                this.SurfaceCorrespondIndex = null;

                Disposed = true;
            }
        }
        /// <summary>
        /// constructor
        /// </summary>
        public Model()
        {
            Debug.WriteLine($"Model() constructor");
        }
        /// <summary>
        /// destructor
        /// </summary>
        ~Model()
        {
            Debug.WriteLine($"Model() destructor");
            Dispose(false);
        }

        /// <summary>
        /// centerlineとcenterlineFinalPositionの二つを使って、移動させるための行列などを計算する
        /// </summary>
        public void CalculateCenterlineAndCenterlineFinalPositoin()
        {
            try
            {
                this.SetCenterlineMovedCoordinate(this.Centerline, this.CenterlineFinalPosition);
                //this.SstPositionAdjustment(this.Centerline, this.CenterlineFinalPosition);
                this.Centerline.CalculateCenterlineDifference();
                this.Centerline.CalculateCenterlineTangentVectors();
                this.Centerline.CalculateCenterlineMovedTangentVectors();
                //this.Centerline.Nodes[0].XTangent = 0.0f;
                //this.Centerline.Nodes[0].YTangent = 0.0f;
                //this.Centerline.Nodes[0].ZTangent = 1.0f;
                //this.Centerline.Nodes[this.Centerline.Nodes.Count - 1].XTangent = 0.0f;
                //this.Centerline.Nodes[this.Centerline.Nodes.Count - 1].YTangent = 0.0f;
                //this.Centerline.Nodes[this.Centerline.Nodes.Count - 1].ZTangent = -1.0f;
                this.Centerline.CalculateRotationMatrix();
            }
            catch (Exception e)
            {
                Debug.WriteLine($"{e.Message}");
                throw;
            }
        }
        /// <summary>
        /// Centerline.Nodesに対して、移動先のNodeの座標（XMoved, YMoved, ZMoved）をセットする
        /// </summary>
        /// <param name="centerline"></param>
        /// <param name="centerlineFinalPosition"></param>
        private void SetCenterlineMovedCoordinate(Centerline centerline, Centerline centerlineFinalPosition)
        {
            // 二つのCenterlineのNodeの数が一致していない場合は例外を投げる
            if (centerline.Nodes.Count != centerlineFinalPosition.Nodes.Count)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append($"二つのCenterlineのNodeの数が一致していません！！！").Append(Environment.NewLine).Append($"{centerline.Nodes.Count}と{centerlineFinalPosition.Nodes.Count}");
                throw new Exception(sb.ToString());
                // この時点で関数の処理は終了する
                // これ以降は実行されない
            }

            int number_of_centerline_nodes = centerline.Nodes.Count;
            for (int i = 0; i < number_of_centerline_nodes; i++)
            {
                centerline.Nodes[i].XMoved = centerlineFinalPosition.Nodes[i].X;
                centerline.Nodes[i].YMoved = centerlineFinalPosition.Nodes[i].Y;
                centerline.Nodes[i].ZMoved = centerlineFinalPosition.Nodes[i].Z;
            }
        }










        /// <summary>
        /// PhysicalInfoの中からSOMETHINGを削除する
        /// </summary>
        /// <param name="physicalInfos"></param>
        private void RemoveSOMETHINGPhysicalInfo(List<PhysicalInfo> physicalInfos)
        {
            // このsortを用いると以下のようになることを想定している
            // 10 WALL
            // 11 INLET
            // 12 OUTLET
            // 99 SOMETHING
            // 100 INTERNAL
            physicalInfos.Sort();
            // 下から2番目が99 SOMETHINGであることを想定している
            physicalInfos.RemoveAt(this.MeshMerged.PhysicalInfos.Count - 2);
        }
        /// <summary>
        /// MeshとInnerMeshを組み合わせてMergedMeshを作る
        /// </summary>
        public void MakeMergeMesh()
        {
            /* 二つのメッシュを組み合わせる前に、
             * SurfaceとPrismLayerで組み合わされたメッシュの最も内側のメッシュ
             * と
             * InnerMeshの表面
             * の二つのそれぞれのメッシュのNodeの対応を求める
             *
             * IndexPair<InnerMeshの表面のNode, SurfaceとPrismLayerで組み合わされたメッシュの最も内側のメッシュのNode>
             */
            try
            {
                this.IndexPair = Algorithm.KDTree(this.MeshSurfaceAndPrismLayer, this.MeshInner);
            }
            catch (Exception e)
            {
                Debug.WriteLine($"キャッチした例外");
                Debug.WriteLine($"{e}");
                Environment.Exit(0);
            }
            int numberOfIncluded = 0;
            int numberOfNotIncluded = 0;
            // SurfaceとPrismLayerで組み合わされたメッシュの最も内側のメッシュのNodeのハッシュセット
            HashSet<Node> test = new HashSet<Node>();
            foreach (var cell in this.MeshSurfaceAndPrismLayer.CellsMostInnerPrismLayer)
            {
                int a = cell.NodesIndex[0];
                int b = cell.NodesIndex[1];
                int c = cell.NodesIndex[2];
                int d = cell.NodesIndex[3];
                int e = cell.NodesIndex[4];
                int f = cell.NodesIndex[5];
                Node node1 = this.MeshSurfaceAndPrismLayer.Nodes[d - 1];
                Node node2 = this.MeshSurfaceAndPrismLayer.Nodes[e - 1];
                Node node3 = this.MeshSurfaceAndPrismLayer.Nodes[f - 1];
                test.Add(node1);
                test.Add(node2);
                test.Add(node3);
            }
            // SurfaceとPrismLayerで組み合わされたメッシュのNodeになかったNodeを追加した場合の
            // Dictionary<元々のIndex, 新たに追加した際に割り振る予定のIndex>を作成する
            Dictionary<int, int> addPointIndexPair = new Dictionary<int, int>();
            this.MeshMerged = new Mesh();
            this.MeshMerged.Nodes = new List<Node>();
            this.MeshMerged.PhysicalInfos = this.MeshSurfaceAndPrismLayer.PhysicalInfos;
            // まずsurfaceとprismLayerのメッシュのNodeを追加する
            // これは条件なしに追加していく
            foreach (var node in this.MeshSurfaceAndPrismLayer.Nodes)
            {
                this.MeshMerged.Nodes.Add(node);
            }
            // 次にInnerMeshのメッシュのNodeを追加する
            // こっちは条件あり
            foreach (var node in this.MeshInner.Nodes)
            {
                // + 1 の理由は？
                // nodeのindexは0-indexだが、nodesIndexは1-indexだから？
                int index = node.Index + 1;
                if (this.IndexPair.ContainsKey(index))
                {
                    numberOfIncluded++;
                }
                else
                {
                    bool add_or_not_add = test.Add(node);
                    if (add_or_not_add == true)
                    {
                        int ttt = this.MeshMerged.Nodes.Count + 1;
                        addPointIndexPair.Add(index, ttt);
                        node.Index = this.MeshMerged.Nodes.Count;
                        this.MeshMerged.Nodes.Add(node);
                        numberOfNotIncluded++;
                    }
                }
            }
            this.MeshMerged.Cells = new List<Cell>();
            foreach (var cell in this.MeshSurfaceAndPrismLayer.Cells)
            {
                if (cell.CellType == CellType.Prism)
                {
                    cell.EntityID = 1000;
                }
                this.MeshMerged.Cells.Add(cell);
            }
            int counter = 0;
            foreach (var cell in this.MeshInner.Cells)
            {
                if (cell.PhysicalID == 11)
                {
                    Cell cellTmp = new Cell()
                    {
                        Index = counter,
                        CellType = cell.CellType,
                        Dummy = 2,
                        PhysicalID = 11,
                        EntityID = 180,
                    };
                    int a = 0;
                    int b = 0;
                    int c = 0;
                    if (this.IndexPair.ContainsKey(cell.NodesIndex[0]))
                    {
                        a = this.IndexPair[cell.NodesIndex[0]];
                    }
                    else
                    {
                        a = addPointIndexPair[cell.NodesIndex[0]];
                    }
                    if (this.IndexPair.ContainsKey(cell.NodesIndex[1]))
                    {
                        b = this.IndexPair[cell.NodesIndex[1]];
                    }
                    else
                    {
                        b = addPointIndexPair[cell.NodesIndex[1]];
                    }
                    if (this.IndexPair.ContainsKey(cell.NodesIndex[2]))
                    {
                        c = this.IndexPair[cell.NodesIndex[2]];
                    }
                    else
                    {
                        c = addPointIndexPair[cell.NodesIndex[2]];
                    }
                    cellTmp.NodesIndex = new int[]
                    {
                        a,
                        b,
                        c,
                    };
                    counter++;
                    this.MeshMerged.Cells.Add(cellTmp);
                }
                if (cell.PhysicalID == 12)
                {
                    Cell cellTmp = new Cell()
                    {
                        Index = counter,
                        CellType = cell.CellType,
                        Dummy = 2,
                        PhysicalID = 12,
                        EntityID = 190,
                    };
                    int a = 0;
                    int b = 0;
                    int c = 0;
                    if (this.IndexPair.ContainsKey(cell.NodesIndex[0]))
                    {
                        a = this.IndexPair[cell.NodesIndex[0]];
                    }
                    else
                    {
                        a = addPointIndexPair[cell.NodesIndex[0]];
                    }
                    if (this.IndexPair.ContainsKey(cell.NodesIndex[1]))
                    {
                        b = this.IndexPair[cell.NodesIndex[1]];
                    }
                    else
                    {
                        b = addPointIndexPair[cell.NodesIndex[1]];
                    }
                    if (this.IndexPair.ContainsKey(cell.NodesIndex[2]))
                    {
                        c = this.IndexPair[cell.NodesIndex[2]];
                    }
                    else
                    {
                        c = addPointIndexPair[cell.NodesIndex[2]];
                    }
                    cellTmp.NodesIndex = new int[]
                    {
                        a,
                        b,
                        c,
                    };
                    counter++;
                    this.MeshMerged.Cells.Add(cellTmp);
                }
            }
            foreach (var cell in this.MeshInner.CellsTetra)
            {
                Cell cellTmp = new Cell()
                {
                    Index = counter,
                    CellType = cell.CellType,
                    Dummy = 2,
                    PhysicalID = 100,
                    EntityID = 190
                };
                int a = 0;
                int b = 0;
                int c = 0;
                int d = 0;
                if (this.IndexPair.ContainsKey(cell.NodesIndex[0]))
                {
                    a = this.IndexPair[cell.NodesIndex[0]];
                }
                else
                {
                    a = addPointIndexPair[cell.NodesIndex[0]];
                }
                if (this.IndexPair.ContainsKey(cell.NodesIndex[1]))
                {
                    b = this.IndexPair[cell.NodesIndex[1]];
                }
                else
                {
                    b = addPointIndexPair[cell.NodesIndex[1]];
                }
                if (this.IndexPair.ContainsKey(cell.NodesIndex[2]))
                {
                    c = this.IndexPair[cell.NodesIndex[2]];
                }
                else
                {
                    c = addPointIndexPair[cell.NodesIndex[2]];
                }
                if (this.IndexPair.ContainsKey(cell.NodesIndex[3]))
                {
                    d = this.IndexPair[cell.NodesIndex[3]];
                }
                else
                {
                    d = addPointIndexPair[cell.NodesIndex[3]];
                }

                cellTmp.NodesIndex = new int[]
                {
                        a,
                        b,
                        c,
                        d
                };
                counter++;
                this.MeshMerged.Cells.Add(cellTmp);
            }
            RemoveSOMETHINGPhysicalInfo(this.MeshMerged.PhysicalInfos);
        }
        /// <summary>
        ///
        /// </summary>
        public void SetBoundaries(Mesh mesh)    // 470
        {
            if (this.Centerline == null)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append($"centerlinegaが定義されていません。");
                throw new Exception(sb.ToString());
            }

            this.Boundaries = new List<Boundary>();
            Boundary inletBoundary = new Boundary("INLET", Centerline.InletLocation.X, Centerline.InletLocation.Y, Centerline.InletLocation.Z);
            Boundary outletBoundary = new Boundary("OUTLET", Centerline.OutletLocation.X, Centerline.OutletLocation.Y, Centerline.OutletLocation.Z);
            this.Boundaries.Add(inletBoundary);
            this.Boundaries.Add(outletBoundary);

            foreach (var boundary in this.Boundaries)
            {
                boundary.DetectAllcorrespondEntityID(mesh);
            }
        }
        public void SetBoundariesInner(MeshInner MeshInner)
        {
            if (this.CenterlineFinalPosition == null)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append($"centerlinegaが定義されていません。");
                throw new Exception(sb.ToString());
            }

            this.Boundaries = new List<Boundary>();
            Boundary inletBoundary = new Boundary("INLET", CenterlineFinalPosition.InletLocation.X, CenterlineFinalPosition.InletLocation.Y, CenterlineFinalPosition.InletLocation.Z);
            Boundary outletBoundary = new Boundary("OUTLET", CenterlineFinalPosition.OutletLocation.X, CenterlineFinalPosition.OutletLocation.Y, CenterlineFinalPosition.OutletLocation.Z);
            this.Boundaries.Add(inletBoundary);
            this.Boundaries.Add(outletBoundary);

            foreach (var boundary in this.Boundaries)
            {
                boundary.DetectAllcorrespondEntityIDInner(MeshInner);
            }
        }

        private void MakeDictionaryEntityIDCorrespond(Mesh mesh)
        {
            mesh.InletEntityIDCorrespond = new Dictionary<int, int>();
            mesh.OutletEntityIDCorrespond = new Dictionary<int, int>();
            foreach (var boundary in this.Boundaries)
            {
                if (boundary.Name == "INLET")
                {
                    mesh.InletEntityIDCorrespond[boundary.CorrespondTriangleEntityID] = 11;
                    foreach (var quadEntityID in boundary.CorrespondQuadrilateralEntityID)
                    {
                        mesh.InletEntityIDCorrespond[quadEntityID] = 11;
                    }
                }
                if (boundary.Name == "OUTLET")
                {
                    mesh.OutletEntityIDCorrespond[boundary.CorrespondTriangleEntityID] = 12;
                    foreach (var quadEntityID in boundary.CorrespondQuadrilateralEntityID)
                    {
                        mesh.OutletEntityIDCorrespond[quadEntityID] = 12;
                    }
                }
            }
        }
        /// <summary>
        ///
        /// </summary>
        public void RemoveUnneedPartMesh()     // 530
        {
            this.MeshSurfaceAndPrismLayer = this.Mesh.MakeNeedPart();
        }
        /// <summary>
        ///
        /// </summary>
        public void OrganizeMeshData()
        {
            try
            {
                this.Mesh.OrganizeEntityInfo();
                this.SetBoundaries(this.Mesh);
                this.MakeDictionaryEntityIDCorrespond(this.Mesh);
                this.Mesh.AddPhysicalID();
                this.Mesh.RewritePhysicalID();
            }
            catch (Exception e)
            {
                Debug.WriteLine($"{e}");
                Environment.Exit(0);
            }
        }
        public void OrganizeMeshDataInner()
        {
            if (this.CenterlineFinalPosition == null)
                return;

            this.MeshInner.OrganizeEntityInfo();
            this.SetBoundariesInner(this.MeshInner);
            this.MakeDictionaryEntityIDCorrespond(this.MeshInner);
            this.MeshInner.AddPhysicalID();
            this.MeshInner.RewritePhysicalID();
        }









        private void CalculateMeshDeformationParallel(Mesh mesh, Centerline centerline)
        {
            foreach (var node in mesh.Nodes)
            {
                /** ここの例外処理の説明
                 * node.CorrespondIndexListが
                 * nullでない、かつ、空でない
                 * の逆の条件を満たす場合に例外を投げる
                 * つまり、null または 空 の場合に例外を投げる
                 * https://qiita.com/takaya901/items/393a3afd2c7b256af6e7
                 * 上記の条件では、以下の2つのケースのいずれかに当てはまる場合にtrueとなります：
                 * node.CorrespondIndexList が null である。
                 * node.CorrespondIndexList の要素数が 0 である。
                 */
                if (!(node.CorrespondIndexList != null && node.CorrespondIndexList.Count > 0))
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append($"nodeのCorrespondIndexListがnullまたは空です").Append(Environment.NewLine);
                    throw new Exception(sb.ToString());
                }

                foreach (var correspondIndex in node.CorrespondIndexList)
                {
                    if (correspondIndex == -1)
                    {
                        Debug.WriteLine($"{node.Index} is - 1");
                        continue;
                    }

                    // 元の中心線のNode
                    // 移動先の中心線のNode
                    // のそれぞれの座標の差が足される
                    node.XMovedSum += centerline.Nodes[correspondIndex].Difference[0];
                    node.YMovedSum += centerline.Nodes[correspondIndex].Difference[1];
                    node.ZMovedSum += centerline.Nodes[correspondIndex].Difference[2];
                }
            }
        }
        private void CalculateMeshDeformationRotation(Mesh mesh, Centerline centerline)
        {
            foreach (var node in mesh.Nodes)
            {
                /** ここの例外処理の説明
                 * node.CorrespondIndexListが
                 * nullでない、かつ、空でない
                 * の逆の条件を満たす場合に例外を投げる
                 * つまり、null または 空 の場合に例外を投げる
                 * https://qiita.com/takaya901/items/393a3afd2c7b256af6e7
                 * 上記の条件では、以下の2つのケースのいずれかに当てはまる場合にtrueとなります：
                 * node.CorrespondIndexList が null である。
                 * node.CorrespondIndexList の要素数が 0 である。
                 */
                if (!(node.CorrespondIndexList != null && node.CorrespondIndexList.Count > 0))
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append($"nodeのCorrespondIndexListがnullまたは空です").Append(Environment.NewLine);
                    throw new Exception(sb.ToString());
                }

                foreach (var correspondIndex in node.CorrespondIndexList)
                {
                    if (correspondIndex == -1)
                    {
                        Debug.WriteLine($"{node.Index} is -1");
                        continue;
                    }

                    // 対応する中心線のnodeの座標を取得
                    // これがこれからする回転の原点となる
                    float[] coordCenterlineNodeGlobal = new float[3] {
                        centerline.Nodes[correspondIndex].X,
                        centerline.Nodes[correspondIndex].Y,
                        centerline.Nodes[correspondIndex].Z,
                    };

                    // 移動前のメッシュのノードの座標を取得
                    float[] coordMeshNodeGlobal = new float[3]
                    {
                        node.X,
                        node.Y,
                        node.Z,
                    };

                    // 中心線のnodeの座標をローカルの原点とする座標軸とする、メッシュのノードの座標を計算
                    float[] coordMeshNodeLocal = new float[3]
                    {
                        coordMeshNodeGlobal[0] - coordCenterlineNodeGlobal[0],
                        coordMeshNodeGlobal[1] - coordCenterlineNodeGlobal[1],
                        coordMeshNodeGlobal[2] - coordCenterlineNodeGlobal[2],
                    };

                    // 回転行列を取得
                    var rotationMatrix = centerline.Nodes[correspondIndex].RotationMatrix;

                    // ローカルの座標軸で、メッシュのノードの座標を回転させる
                    float[] coordMeshNodeLocalMoved = Utility.MatVec(rotationMatrix, coordMeshNodeLocal);

                    float[] coordMeshNodeGlobalMoved = new float[3]
                    {
                        coordMeshNodeLocalMoved[0] + coordCenterlineNodeGlobal[0],
                        coordMeshNodeLocalMoved[1] + coordCenterlineNodeGlobal[1],
                        coordMeshNodeLocalMoved[2] + coordCenterlineNodeGlobal[2],
                    };

                    // ここで、移動量の和に足しこみ
                    // 実際の移動量は、この後に平均をとる
                    node.XMovedSum += coordMeshNodeGlobalMoved[0];
                    node.YMovedSum += coordMeshNodeGlobalMoved[1];
                    node.ZMovedSum += coordMeshNodeGlobalMoved[2];
                }
            }
        }
        private void ResetMeshDeformation(Mesh mesh)
        {
            foreach (var node in mesh.Nodes)
            {
                node.XMovedSum = 0.0f;
                node.YMovedSum = 0.0f;
                node.ZMovedSum = 0.0f;
            }
        }

        public void CalculateMeshDeformation(Mesh mesh, Centerline centerline)
        {
            try
            {
                this.ResetMeshDeformation(mesh);
                this.CalculateMeshDeformationRotation(mesh, centerline);
                this.CalculateMeshDeformationParallel(mesh, centerline);
            }
            catch
            {
                throw;
            }
        }


        /// <summary>
        /// 計算した総和移動量をもとに、実際にメッシュを変形させる
        /// </summary>
        /// <param name="mesh"></param>
        public void ExecuteMeshDeformation(Mesh mesh)
        {
            foreach (var node in mesh.Nodes)
            {
                if (!(node.CorrespondIndexList != null && node.CorrespondIndexList.Count > 0))
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append($"nodeのCorrespondIndexListがnullまたは空です").Append(Environment.NewLine);
                    throw new Exception(sb.ToString());
                }

                // 今までの移動量の総和を、CorrespondIndexの数で割って平均をとる
                node.X = node.XMovedSum / node.CorrespondIndexList.Count;
                node.Y = node.YMovedSum / node.CorrespondIndexList.Count;
                node.Z = node.ZMovedSum / node.CorrespondIndexList.Count;
            }
        }

        /// <summary>
        /// MeshSurfaceAndPrismLayerを変形させる
        /// この際にそれぞれのNodeには複数のCorrespondIndexがあり、それぞれの移動の和の平均をとることで
        /// メッシュの変形による歪みを少なくする
        /// </summary>
        /// <param name="mesh"></param>
        /// <param name="centerline"></param>
        public void MeshDeformationMultiple(Mesh mesh, Centerline centerline)
        {
            try
            {
                this.CalculateMeshDeformation(mesh, centerline);
                this.ExecuteMeshDeformation(mesh);
            }
            catch (Exception e)
            {
                Debug.WriteLine($"{e}");
                Environment.Exit(0);
            }
        }





    }
}
