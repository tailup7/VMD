using System;
using System.Collections.Generic;
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
    }
}
