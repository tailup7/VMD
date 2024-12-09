using System;

namespace VascularModelDeformation
{
    [Serializable]
    public class PhysicalInfo : IComparable<PhysicalInfo>
    {
        /// <summary>
        /// 境界条件の次元
        /// 表面や入口出口の場合は2次元
        /// 流体が流れる部分は部分は3次元
        /// </summary>
        public int Dimension { get; set; }
        /// <summary>
        ///　境界条件のID
        ///　2次元の場合は10~99
        ///　3次元の場合は100~
        ///　WALL 10
        ///　INLET 11
        ///　OUTLET 12
        ///　SOMETHING 99
        ///　INTERNAL 100
        /// </summary>
        public int ID { get; set; }
        /// <summary>
        /// 境界条件の名前
        /// INLET
        /// OUTLET
        /// WALL
        /// INTERNAL
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="dimension"></param>
        /// <param name="id"></param>
        /// <param name="name"></param>
        public PhysicalInfo(int dimension, int id, string name)
        {
            this.Dimension = dimension;
            this.Name = name;
            this.ID = id;
        }
        /// <summary>
        /// constructor
        /// </summary>
        public PhysicalInfo() { }
        private int SetDimension(int key)
        {
            int digits = this.CheckNumberOfDigits(key);
            return digits;
        }
        private int CheckNumberOfDigits(int key)
        {
            return (key == 0) ? 1 : ((int)Math.Log10(key) + 1);
        }
        public int CompareTo(PhysicalInfo other)
        {
            int dimensionComparison = Dimension.CompareTo(other.Dimension);
            if (dimensionComparison != 0)
            {
                return dimensionComparison;
            }
            return ID.CompareTo(other.ID);
        }
    }
}
