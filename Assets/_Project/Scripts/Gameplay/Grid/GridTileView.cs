using NanoFrame.Utility;
using Project.Gameplay.Visuals;
using UnityEngine;

namespace Project.Gameplay.Grid
{
    public sealed class GridTileView : MonoBehaviour
    {
        private Renderer _renderer;
        private Vector3 _baseScale;

        public int TileID { get; private set; }
        public Vector2Int GridPosition { get; private set; }

        public void Initialize(int tileID, Vector2Int gridPosition)
        {
            TileID = tileID;
            GridPosition = gridPosition;
            _renderer = GetComponent<Renderer>();
            _baseScale = transform.localScale;

            Material sharedMaterial = PrototypeMaterialFactory.GetSharedLitMaterial();
            if (_renderer != null && sharedMaterial != null)
            {
                _renderer.sharedMaterial = sharedMaterial;
            }
        }

        public void ApplyVisual(Color color, float progress)
        {
            if (_renderer != null)
            {
                VisualUtility.SetInstancedColor(_renderer, color);
            }

            float pulse = 1f + Mathf.Clamp01(progress) * 0.05f;
            transform.localScale = new Vector3(_baseScale.x, _baseScale.y * pulse, _baseScale.z);
        }
    }
}