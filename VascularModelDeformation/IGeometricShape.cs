using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VascularModelDeformation
{
    public interface IGeometricShape
    {
        void Calculate();
        Node CalculateCenter();
        float CalculateArea();
        float CalculateVolume();
        float Calculater();
        float CalculateR();
        float CalculateQualityAspectRatio();
        float CalculateQualityEdgeRatio();
        float CalculateQualityRadiusRatio();
    }
}