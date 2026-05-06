using NanoFrame.Core;
using NanoFrame.Event;
using Project.Gameplay.Config;
using Project.Gameplay.Match;
using UnityEngine;

namespace Project.Gameplay.Grid
{
    public class PrototypeGridManager : Singleton<PrototypeGridManager>, IManager
    {
        private PrototypeBalanceConfig _settings;
        private GridCellState[,] _cells;
        private GameObject _gridRoot;
        private float _step;
        private Vector3 _bottomLeftCenter;
        private int _player1TerritoryCount;
        private int _player2TerritoryCount;

        public PrototypeBalanceConfig Settings => _settings;
        public int Player1TerritoryCount => _player1TerritoryCount;
        public int Player2TerritoryCount => _player2TerritoryCount;

        public void OnInit()
        {
            _settings = PrototypeMatchManager.Instance.Settings;
            BuildGrid();
            ResetGrid();
        }

        public void OnUpdate()
        {
        }

        public void OnDestroyManager()
        {
            if (_gridRoot != null)
            {
                Destroy(_gridRoot);
            }

            _gridRoot = null;
            _cells = null;
            _player1TerritoryCount = 0;
            _player2TerritoryCount = 0;
        }

        public void ResetGrid()
        {
            if (_cells == null || _settings == null)
            {
                return;
            }

            _player1TerritoryCount = 0;
            _player2TerritoryCount = 0;

            for (int x = 0; x < _settings.GridWidth; x++)
            {
                for (int y = 0; y < _settings.GridHeight; y++)
                {
                    GridCellState cell = _cells[x, y];
                    cell.OwnerPlayerID = 0;
                    cell.CaptureProgress = 0f;
                    cell.View?.ApplyVisual(_settings.NeutralTileColor, 0f);
                }
            }
            BroadcastTerritoryCount();
        }

        public void RefreshAllCellsVisuals()
        {
            if (_cells == null || _settings == null)
            {
                return;
            }

            for (int x = 0; x < _settings.GridWidth; x++)
            {
                for (int y = 0; y < _settings.GridHeight; y++)
                {
                    GridCellState cell = _cells[x, y];
                    cell.View?.ApplyVisual(GetColorForOwner(cell.OwnerPlayerID), cell.CaptureProgress);
                }
            }
        }

        public int GetTerritoryCount(int playerId)
        {
            if (playerId == 1)
            {
                return _player1TerritoryCount;
            }

            if (playerId == 2)
            {
                return _player2TerritoryCount;
            }

            return 0;
        }

        public float GetTerritoryRatio(int playerId)
        {
            int totalTiles = Mathf.Max(1, _settings.GridWidth * _settings.GridHeight);
            return GetTerritoryCount(playerId) / (float)totalTiles;
        }

        public Vector3 GetSpawnPosition(int playerId)
        {
            float halfWidth = (_settings.GridWidth - 1) * _step * 0.25f;
            float y = _settings.PlayerSpawnHeight;
            float z = 0f;
            return new Vector3(playerId == 1 ? -halfWidth : halfWidth, y, z);
        }

        public Vector3 GetSpawnFacing(int playerId)
        {
            return playerId == 1 ? Vector3.right : Vector3.left;
        }

        public bool IsInsideArena(Vector3 worldPosition)
        {
            float halfWidth = (_settings.GridWidth - 1) * _step * 0.5f + _settings.ArenaEliminationMargin;
            float halfHeight = (_settings.GridHeight - 1) * _step * 0.5f + _settings.ArenaEliminationMargin;
            return Mathf.Abs(worldPosition.x) <= halfWidth && Mathf.Abs(worldPosition.z) <= halfHeight;
        }

        public bool TryApplySpray(int playerId, Vector3 worldPosition, Vector3 facingDirection, float deltaTime)
        {
            if (_cells == null || _settings == null)
            {
                return false;
            }

            Vector2Int currentCell = WorldToCell(worldPosition);
            if (!IsValidCell(currentCell))
            {
                return false;
            }

            Vector2Int offset = DirectionToOffset(facingDirection);
            if (offset == Vector2Int.zero)
            {
                return false;
            }

            int reach = Mathf.Clamp(Mathf.RoundToInt(_settings.SprayRange), 1, Mathf.Max(_settings.GridWidth, _settings.GridHeight));
            float stepDeltaTime = deltaTime / reach;
            bool appliedAny = false;

            for (int step = 1; step <= reach; step++)
            {
                Vector2Int targetCell = new Vector2Int(currentCell.x + offset.x * step, currentCell.y + offset.y * step);
                if (!IsValidCell(targetCell))
                {
                    break;
                }

                appliedAny |= ApplyCapture(targetCell, playerId, stepDeltaTime);
            }

            return appliedAny;
        }

        public int AbsorbTilesForShockwave(int playerId, Vector3 worldPosition, int maxTiles)
        {
            if (_cells == null || _settings == null)
            {
                return 0;
            }

            Vector2Int centerCell = WorldToCell(worldPosition);
            if (!IsValidCell(centerCell))
            {
                return 0;
            }

            if (_cells[centerCell.x, centerCell.y].OwnerPlayerID != playerId)
            {
                return 0;
            }

            int tileLimit = Mathf.Clamp(maxTiles, 1, _settings.GridWidth * _settings.GridHeight);
            int absorbedCount = 0;

            System.Collections.Generic.Queue<Vector2Int> queue = new System.Collections.Generic.Queue<Vector2Int>();
            System.Collections.Generic.HashSet<Vector2Int> visited = new System.Collections.Generic.HashSet<Vector2Int>();

            queue.Enqueue(centerCell);
            visited.Add(centerCell);

            while (queue.Count > 0 && absorbedCount < tileLimit)
            {
                Vector2Int current = queue.Dequeue();
                GridCellState cell = _cells[current.x, current.y];

                if (cell.OwnerPlayerID == playerId)
                {
                    AbsorbCell(cell, playerId);
                    absorbedCount++;

                    Vector2Int[] neighbors = 
                    {
                        new Vector2Int(current.x + 1, current.y),
                        new Vector2Int(current.x - 1, current.y),
                        new Vector2Int(current.x, current.y + 1),
                        new Vector2Int(current.x, current.y - 1)
                    };

                    foreach (Vector2Int neighbor in neighbors)
                    {
                        if (IsValidCell(neighbor) && !visited.Contains(neighbor))
                        {
                            visited.Add(neighbor);
                            queue.Enqueue(neighbor);
                        }
                    }
                }
            }

            return absorbedCount;
        }

        private void BuildGrid()
        {
            if (_gridRoot != null)
            {
                Destroy(_gridRoot);
            }

            _gridRoot = new GameObject("PrototypeGrid");
            DontDestroyOnLoad(_gridRoot);

            _step = Mathf.Max(0.1f, _settings.TileSize + _settings.TileGap);

            float widthSpan = (_settings.GridWidth - 1) * _step;
            float heightSpan = (_settings.GridHeight - 1) * _step;
            _bottomLeftCenter = new Vector3(-widthSpan * 0.5f, 0f, -heightSpan * 0.5f);

            _cells = new GridCellState[_settings.GridWidth, _settings.GridHeight];

            int tileId = 0;
            for (int x = 0; x < _settings.GridWidth; x++)
            {
                for (int y = 0; y < _settings.GridHeight; y++)
                {
                    Vector3 localPosition = _bottomLeftCenter + new Vector3(x * _step, 0f, y * _step);

                    GameObject tileObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    tileObject.name = $"Tile_{x}_{y}";
                    tileObject.transform.SetParent(_gridRoot.transform, false);
                    tileObject.transform.localPosition = localPosition + new Vector3(0f, _settings.TileHeight * 0.5f, 0f);
                    tileObject.transform.localScale = new Vector3(_settings.TileSize, _settings.TileHeight, _settings.TileSize);

                    GridTileView tileView = tileObject.AddComponent<GridTileView>();
                    tileView.Initialize(tileId, new Vector2Int(x, y));

                    _cells[x, y] = new GridCellState
                    {
                        TileID = tileId,
                        GridPosition = new Vector2Int(x, y),
                        OwnerPlayerID = 0,
                        CaptureProgress = 0f,
                        View = tileView
                    };

                    tileView.ApplyVisual(_settings.NeutralTileColor, 0f);
                    tileId++;
                }
            }
        }

        private bool ApplyCapture(Vector2Int cellPosition, int playerId, float deltaTime)
        {
            GridCellState cell = _cells[cellPosition.x, cellPosition.y];
            int previousOwner = cell.OwnerPlayerID;

            if (previousOwner == playerId)
            {
                cell.CaptureProgress = 0f;
                cell.View?.ApplyVisual(GetColorForOwner(playerId), 0f);
                return true;
            }

            float captureSeconds = _settings.GetCaptureSeconds(previousOwner);
            float progressDelta = deltaTime / Mathf.Max(0.01f, captureSeconds);
            cell.CaptureProgress = Mathf.Clamp01(cell.CaptureProgress + progressDelta);

            Color displayColor = previousOwner == 0 ? GetColorForOwner(playerId) : GetColorForOwner(previousOwner);
            cell.View?.ApplyVisual(displayColor, cell.CaptureProgress);

            EventManager.Instance.Fire(new OnTileCaptureProgressChangedEvent
            {
                TileID = cell.TileID,
                OwnerPlayerID = playerId,
                Progress = cell.CaptureProgress,
                Threshold = 1f
            });

            if (cell.CaptureProgress < 1f)
            {
                return true;
            }

            if (previousOwner == 1)
            {
                _player1TerritoryCount = Mathf.Max(0, _player1TerritoryCount - 1);
            }
            else if (previousOwner == 2)
            {
                _player2TerritoryCount = Mathf.Max(0, _player2TerritoryCount - 1);
            }

            cell.OwnerPlayerID = playerId;
            cell.CaptureProgress = 0f;

            if (playerId == 1)
            {
                _player1TerritoryCount++;
            }
            else if (playerId == 2)
            {
                _player2TerritoryCount++;
            }

            cell.View?.ApplyVisual(GetColorForOwner(playerId), 0f);

            EventManager.Instance.Fire(new OnTileCapturedEvent
            {
                PlayerID = playerId,
                TileID = cell.TileID
            });

            BroadcastTerritoryCount();

            return true;
        }

        private void AbsorbCell(GridCellState cell, int playerId)
        {
            if (cell.OwnerPlayerID != playerId)
            {
                return;
            }

            cell.OwnerPlayerID = 0;
            cell.CaptureProgress = 0f;

            if (playerId == 1)
            {
                _player1TerritoryCount = Mathf.Max(0, _player1TerritoryCount - 1);
            }
            else if (playerId == 2)
            {
                _player2TerritoryCount = Mathf.Max(0, _player2TerritoryCount - 1);
            }

            cell.View?.ApplyVisual(_settings.NeutralTileColor, 0f);

            EventManager.Instance.Fire(new OnTileAbsorbedEvent
            {
                PlayerID = playerId,
                TileID = cell.TileID,
                GridPosition = cell.GridPosition
            });

            BroadcastTerritoryCount();
        }

        private void BroadcastTerritoryCount()
        {
            int totalTiles = _settings.GridWidth * _settings.GridHeight;

            EventManager.Instance.Fire(new TerritoryCountChangedEvent
            {
                PlayerId = 1,
                MyTileCount = _player1TerritoryCount,
                TotalTileCount = totalTiles
            });

            EventManager.Instance.Fire(new TerritoryCountChangedEvent
            {
                PlayerId = 2,
                MyTileCount = _player2TerritoryCount,
                TotalTileCount = totalTiles
            });
        }

        private Vector2Int WorldToCell(Vector3 worldPosition)
        {
            float localX = worldPosition.x - _bottomLeftCenter.x;
            float localZ = worldPosition.z - _bottomLeftCenter.z;

            int x = Mathf.RoundToInt(localX / _step);
            int y = Mathf.RoundToInt(localZ / _step);
            return new Vector2Int(x, y);
        }

        private bool IsValidCell(Vector2Int cellPosition)
        {
            return cellPosition.x >= 0 && cellPosition.x < _settings.GridWidth && cellPosition.y >= 0 && cellPosition.y < _settings.GridHeight;
        }

        private Vector2Int DirectionToOffset(Vector3 direction)
        {
            Vector3 flattened = Vector3.ProjectOnPlane(direction, Vector3.up);
            if (flattened.sqrMagnitude < 0.0001f)
            {
                return Vector2Int.zero;
            }

            if (Mathf.Abs(flattened.x) >= Mathf.Abs(flattened.z))
            {
                return new Vector2Int(flattened.x >= 0f ? 1 : -1, 0);
            }

            return new Vector2Int(0, flattened.z >= 0f ? 1 : -1);
        }

        private Color GetColorForOwner(int playerId)
        {
            return _settings.GetTileColor(playerId);
        }
    }
}