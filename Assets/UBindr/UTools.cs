using UnityEngine;

namespace Assets.UBindr
{
    public static class UTools
    {
        public static Color SetAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        public static Color LerpAlpha(Color color, float alpha, float step)
        {
            color.a = Mathf.Lerp(color.a, alpha, step);
            return color;
        }
    }
}