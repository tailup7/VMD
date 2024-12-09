using System.Collections.Generic;

namespace VascularModelDeformation
{
    public class EntityInfo
    {
        public int EntityID { get; set; }
        public float CenterLocationX { get; set; }
        public float CenterLocationY { get; set; }
        public float CenterLocationZ { get; set; }
        public float[] NormalVector { get; set; }
        public float Radius { get; set; }
        public int NumberOfElements { get; set; }
        public HashSet<int> ContainedNodes { get; set; }
        public HashSet<int> ContainedElements { get; set; }
        public CellType CellType { get; set; }
    }
}
