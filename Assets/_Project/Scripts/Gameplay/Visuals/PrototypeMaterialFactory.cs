using UnityEngine;

namespace Project.Gameplay.Visuals
{
    public static class PrototypeMaterialFactory
    {
        private static Material _sharedLitMaterial;

        public static Material GetSharedLitMaterial()
        {
            if (_sharedLitMaterial != null)
            {
                return _sharedLitMaterial;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            if (shader == null)
            {
                return null;
            }

            _sharedLitMaterial = new Material(shader)
            {
                name = "Prototype_Shared_Lit_Material"
            };

            return _sharedLitMaterial;
        }
    }
}