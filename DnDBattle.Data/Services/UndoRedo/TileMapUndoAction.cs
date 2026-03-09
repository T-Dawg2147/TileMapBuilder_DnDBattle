using DnDBattle.Data.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DnDBattle.Data.Models.Tiles;
using DnDBattle.Data.Models.Tiles.Metadata;

namespace DnDBattle.Data.Services.UndoRedo
{
    public class TilePlaceAction : IUndoableAction
    {
        private readonly TileMap _map;
        private readonly Tile _tile;

        public string Description => $"Place Tile at ({_tile.GridX}, {_tile.GridY})";

        public TilePlaceAction(TileMap map, Tile tile)
        {
            _map = map;
            _tile = tile;
        }

        public void Do()
        {
            if (!_map.PlacedTiles.Contains(_tile))
                _map.PlacedTiles.Add(_tile);
        }

        public void Undo()
        {
            _map.PlacedTiles.Remove(_tile);
        }
    }

    public class TileRemoveAction : IUndoableAction
    {
        private readonly TileMap _map;
        private readonly Tile _tile;

        public string Description => $"Remove Tile at ({_tile.GridX}, {_tile.GridY})";

        public TileRemoveAction(TileMap map, Tile tile)
        {
            _map = map;
            _tile = tile;
        }

        public void Do()
        {
            _map.PlacedTiles.Remove(_tile);
        }

        public void Undo()
        {
            if (!_map.PlacedTiles.Contains(_tile))
                _map.PlacedTiles.Add(_tile);
        }
    }

    public class TileBatchAction : IUndoableAction
    {
        private readonly TileMap _map;
        private readonly List<Tile> _tilesAdded;
        private readonly List<Tile> _tilesRemoved;

        public string Description { get; }

        public TileBatchAction(TileMap map, List<Tile> tilesAdded, List<Tile> tilesRemoved, string description = "Batch Edit")
        {
            _map = map;
            _tilesAdded = tilesAdded ?? new List<Tile>();
            _tilesRemoved = tilesRemoved ?? new List<Tile>();
            Description = description;
        }

        public void Do()
        {
            foreach (var tile in _tilesRemoved)
            {
                _map.PlacedTiles.Remove(tile);
            }

            foreach (var tile in _tilesAdded)
            {
                if (!_map.PlacedTiles.Contains(tile))
                {
                    _map.PlacedTiles.Add(tile);
                }
            }
        }

        public void Undo()
        {
            foreach (var tile in _tilesAdded)
            {
                _map.PlacedTiles.Remove(tile);
            }

            foreach (var tile in _tilesRemoved)
            {
                if (!_map.PlacedTiles.Contains(tile))
                {
                    _map.PlacedTiles.Add(tile);
                }
            }
        }
    }

    /// <summary>
    /// Undo action for tile metadata changes
    /// </summary>
    public class TileMetadataAction : IUndoableAction
    {
        private readonly Tile _tile;
        private readonly TileMetadata _metadata;
        private readonly bool _isAdd;

        public string Description => _isAdd ? $"Add {_metadata.Type}" : $"Remove {_metadata.Type}";

        public TileMetadataAction(Tile tile, TileMetadata metadata, bool isAdd)
        {
            _tile = tile;
            _metadata = metadata;
            _isAdd = isAdd;
        }

        public void Do()
        {
            if (_isAdd)
            {
                if (!_tile.Metadata.Contains(_metadata))
                {
                    _tile.Metadata.Add(_metadata);
                }
            }
            else
            {
                _tile.Metadata.Remove(_metadata);
            }
        }

        public void Undo()
        {
            if (_isAdd)
            {
                _tile.Metadata.Remove(_metadata);
            }
            else
            {
                if (!_tile.Metadata.Contains(_metadata))
                {
                    _tile.Metadata.Add(_metadata);
                }
            }
        }
    }

    /// <summary>
    /// Undo action for map property changes (size, name, etc.)
    /// </summary>
    public class MapPropertyChangeAction : IUndoableAction
    {
        private readonly TileMap _map;
        private readonly string _propertyName;
        private readonly object _oldValue;
        private readonly object _newValue;

        public string Description => $"Change {_propertyName}";

        public MapPropertyChangeAction(TileMap map, string propertyName, object oldValue, object newValue)
        {
            _map = map;
            _propertyName = propertyName;
            _oldValue = oldValue;
            _newValue = newValue;
        }

        public void Do()
        {
            SetPropertyValue(_newValue);
        }

        public void Undo()
        {
            SetPropertyValue(_oldValue);
        }

        private void SetPropertyValue(object value)
        {
            var prop = _map.GetType().GetProperty(_propertyName);
            if (prop != null && prop.CanWrite)
            {
                prop.SetValue(_map, value);
            }
        }
    }

    public class TileMoveAction : IUndoableAction
    {
        private readonly List<(Tile Tile, int OldX, int OldY)> _movedTiles;
        private readonly int _deltaX;
        private readonly int _deltaY;

        public string Description => $"Move {_movedTiles.Count} tile(s) by ({_deltaX}, {_deltaY})";

        public TileMoveAction(List<(Tile Tile, int OldX, int OldY)> movedTiles, int deltaX, int deltaY)
        {
            _movedTiles = movedTiles;
            _deltaX = deltaX;
            _deltaY = deltaY;
        }

        public void Do()
        {
            foreach (var (tile, oldX, oldY) in _movedTiles)
            {
                tile.GridX = oldX + _deltaX;
                tile.GridY = oldY + _deltaY;
            }
        }

        public void Undo()
        {
            foreach (var (tile, oldX, oldY) in _movedTiles)
            {
                tile.GridX = oldX;
                tile.GridY = oldY;
            }
        }
    }

    public class MapResizeAction : IUndoableAction
    {
        private readonly TileMap _map;
        private readonly int _oldWidth;
        private readonly int _oldHeight;
        private readonly int _addTop;
        private readonly int _addBottom;
        private readonly int _addLeft;
        private readonly int _addRight;
        private readonly List<Tile> _removedTiles;

        public string Description => $"Resize Map to {_map.Width}x{_map.Height}";

        public MapResizeAction(
            TileMap map,
            int oldWidth, int oldHeight,
            int addTop, int addBottom, int addLeft, int addRight,
            List<Tile> removedTiles)
        {
            _map = map;
            _oldWidth = oldWidth;
            _oldHeight = oldHeight;
            _addTop = addTop;
            _addBottom = addBottom;
            _addLeft = addLeft;
            _addRight = addRight;
            _removedTiles = removedTiles;
        }

        public void Do()
        {
            if (_addTop != 0 || _addLeft != 0)
            {
                foreach (var tile in _map.PlacedTiles)
                {
                    tile.GridX += _addLeft;
                    tile.GridY += _addTop;
                }
            }

            foreach (var tile in _removedTiles)
                _map.PlacedTiles.Remove(tile);

            _map.Width = _oldWidth + _addLeft + _addRight;
            _map.Height = _oldHeight + _addTop + _addBottom;
        }

        public void Undo()
        {
            _map.Width = _oldWidth;
            _map.Height = _oldHeight;

            if (_addTop != 0 || _addLeft != 0)
            {
                foreach (var tile in _map.PlacedTiles)
                {
                    tile.GridX -= _addLeft;
                    tile.GridY -= _addTop;
                }
            }

            foreach (var tile in _removedTiles)
            {
                if (!_map.PlacedTiles.Contains(tile))
                {
                    tile.GridX -= _addLeft;
                    tile.GridY -= _addTop;
                    _map.PlacedTiles.Add(tile);
                }
            }
        }
    }
}
