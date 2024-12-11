using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VascularModelDeformation
{
    internal class Boundary
    {
        /// <summary>
        /// 境界の名前
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// 境界の中心座標
        /// </summary>
        public float LocationX { get; }
        public float LocationY { get; }
        public float LocationZ { get; }
        /// <summary>
        /// 境界にある三角形セルからなるEntityID
        /// 一つだけ
        /// </summary>
        public int CorrespondTriangleEntityID { get; private set; }
        /// <summary>
        /// 境界にある四角形セルからなるEntityID
        /// 複数ある可能性がある
        /// </summary>
        public List<int> CorrespondQuadrilateralEntityID { get; private set; }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="name"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        public Boundary(string name, float x, float y, float z)
        {
            this.Name = name;
            this.LocationX = x;
            this.LocationY = y;
            this.LocationZ = z;
        }
        /// <summary>
        /// 境界上のEntityIDをすべて検出する
        /// </summary>
        /// <param name="boundary"></param>
        /// <param name="mesh"></param>
        public void DetectAllcorrespondEntityID(Mesh mesh)
        {
            this.DetectCorrespondTriangleEntityID(mesh);
            this.DetectCorrespondQuadrilateralEntityID(mesh);
        }
        public void DetectAllcorrespondEntityIDInner(MeshInner mesh)
        {
            this.DetectCorrespondTriangleEntityID(mesh);
            this.DetectCorrespondQuadrilateralEntityID(mesh);
        }
        /// <summary>
        /// 境界上の三角形セルからなるEntityIDを検出する
        /// 境界のLocationX, LocationY, LocationZと一番近い三角形セルのEntityIDを返す
        /// </summary>
        /// <param name="boundary"></param>
        /// <param name="mesh"></param>
        public void DetectCorrespondTriangleEntityID(Mesh mesh)
        {
            // 負の数というありえない数字を入れておく
            int mostNearTriangleEntityID = int.MinValue;
            // 距離にありえない大きい数字を入れておく
            float shortestDitance = float.MaxValue;

            // すべてのEntityInfosをまわす
            foreach (var entity in mesh.EntityInfos)
            {
                float tmpDistance = 0f;
                if (entity.CellType != CellType.Triangle)
                    break;

                // entity.CenterLocation is 3
                tmpDistance += Math.Abs(entity.CenterLocationX - this.LocationX);
                tmpDistance += Math.Abs(entity.CenterLocationY - this.LocationY);
                tmpDistance += Math.Abs(entity.CenterLocationZ - this.LocationZ);
                if (tmpDistance < shortestDitance)
                {
                    shortestDitance = tmpDistance;
                    mostNearTriangleEntityID = entity.EntityID;
                }
            }
            this.CorrespondTriangleEntityID = mostNearTriangleEntityID;
        }
        /// <summary>
        /// 境界上の四角形セルからなるEntityIDを検出する
        /// DetectCorrespondTriangleEntityIDで検出したEntityIDと共有する点が3つ以上ある四角形セルのEntityIDを返す
        /// </summary>
        /// <param name="mesh"></param>
        public void DetectCorrespondQuadrilateralEntityID(Mesh mesh)
        {
            // 初期化、ここで実態を宣言
            this.CorrespondQuadrilateralEntityID = new List<int>();
            foreach (var ei1 in mesh.EntityInfos)
            {
                if (ei1.EntityID == this.CorrespondTriangleEntityID)
                {
                    foreach (var ei2 in mesh.EntityInfos)
                    {
                        if (ei2.CellType != CellType.Quadrilateral)
                            continue;

                        var matchdPoints = new HashSet<int>(ei1.ContainedNodes);
                        matchdPoints.IntersectWith(ei2.ContainedNodes);
                        // 1だとグルッと回ってくるEntityIDのものをはじけないので2にする
                        if (matchdPoints.Count > 2)
                        {
                            this.CorrespondQuadrilateralEntityID.Add(ei2.EntityID);
                        }
                    }
                }
            }
        }
    }
}
