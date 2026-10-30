using System.Globalization;
using UnityEngine;

namespace Inspection.Client
{
    public static class InspectionCoordinates
    {
        public static bool TryParse(string x, string y, string z, out Vector3 position)
        {
            position = default;
            if (!TryAxis(x, out var px) || !TryAxis(y, out var py) || !TryAxis(z, out var pz)) return false;
            position = new Vector3(px, py, pz);
            return true;
        }

        private static bool TryAxis(string text, out float value)
        {
            // 使用统一小数点格式，坐标校验与后端的范围限制一致。
            return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value)
                && !float.IsNaN(value) && !float.IsInfinity(value) && Mathf.Abs(value) <= 100000;
        }
    }
}
