using UnityEngine;

namespace NanoFrame.Utility
{
    public static class VisualUtility
    {
        private static MaterialPropertyBlock _mpb;
        private static readonly int ColorPropertyId = Shader.PropertyToID("_BaseColor"); // 对应你URP材质的颜色属性名

        public static void SetInstancedColor(Renderer renderer, Color newColor)
        {
            if (_mpb == null) _mpb = new MaterialPropertyBlock();

            renderer.GetPropertyBlock(_mpb);
            _mpb.SetColor(ColorPropertyId, newColor);
            renderer.SetPropertyBlock(_mpb);
        }
    }
}