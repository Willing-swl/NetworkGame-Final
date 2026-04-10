using UnityEngine;

namespace Project.Gameplay.Grid
{
    public sealed class GridCellState
    {
        public int TileID;
        public Vector2Int GridPosition;
        public int OwnerPlayerID;
        public float CaptureProgress;
        public GridTileView View;
    }
}