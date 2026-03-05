using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using DnDBattle.Data.Models.Tiles;
using DnDBattle.Data.Services.Interfaces;

namespace DnDBattle.Data.Services.TileService
{
    public sealed class TileMapService : ITileMapService
    {
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly ITileLibraryService _tileLibraryService;

        public TileMapService(ITileLibraryService tileLibraryService)
        {
            _tileLibraryService = tileLibraryService;
            _jsonOptions = new JsonSerializerOptions()
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true
            };
        }

        public async Task<bool?> SaveMapAsync(TileMap map, string filePath)
        {
            try
            {
                var dto = MapToDto(map);
                var json = JsonSerializer.Serialize(dto, _jsonOptions);
                await File.WriteAllTextAsync(filePath, json);

                Debug.WriteLine($"[TileMapService] Saved map to {filePath}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[TileMapService] Save failed: {ex.Message}");
                return false;
            }
        }
        public async Task<TileMap> LoadMapAsync(string filePath)
        {
            try
            {
                // Check file exists
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"Tile map file not found: {filePath}");
                }

                // Read JSON
                System.Diagnostics.Debug.WriteLine($"[TileMapService] Reading file...");
                string json = await File.ReadAllTextAsync(filePath);
                System.Diagnostics.Debug.WriteLine($"[TileMapService] File size: {json.Length} characters");

                // Try to deserialize
                System.Diagnostics.Debug.WriteLine($"[TileMapService] Deserializing JSON...");

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true, // ← Ignore case differences
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };

                TileMapDto dto;
                try
                {
                    dto = JsonSerializer.Deserialize<TileMapDto>(json, options)!;
                }
                catch (JsonException jsonEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[TileMapService] JSON Deserialization failed!");
                    System.Diagnostics.Debug.WriteLine($"[TileMapService] Error: {jsonEx.Message}");
                    System.Diagnostics.Debug.WriteLine($"[TileMapService] Line: {jsonEx.LineNumber}, Position: {jsonEx.BytePositionInLine}");

                    // Show first 500 chars of JSON for debugging
                    System.Diagnostics.Debug.WriteLine($"[TileMapService] JSON preview: {json.Substring(0, Math.Min(500, json.Length))}");

                    throw new Exception($"Invalid tile map file format.\n\nJSON Error at line {jsonEx.LineNumber}: {jsonEx.Message}", jsonEx);
                }

                if (dto == null)
                {
                    throw new Exception("Deserialized tile map is null");
                }

                System.Diagnostics.Debug.WriteLine($"[TileMapService] Deserialization successful!");
                System.Diagnostics.Debug.WriteLine($"[TileMapService] Map: {dto.Name}");
                System.Diagnostics.Debug.WriteLine($"[TileMapService] Size: {dto.Width}×{dto.Height}");
                System.Diagnostics.Debug.WriteLine($"[TileMapService] Tiles: {dto.PlacedTiles?.Count ?? 0}");

                // Convert DTO to TileMap
                System.Diagnostics.Debug.WriteLine($"[TileMapService] Converting DTO to TileMap...");
                var tileMap = DtoToMap(dto);

                System.Diagnostics.Debug.WriteLine($"[TileMapService] Load complete!");
                return tileMap;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TileMapService] LOAD FAILED: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[TileMapService] Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        private TileMap DtoToMap(TileMapDto dto)
        {
            if (_tileLibraryService.AvailableTiles.Count == 0)
            {
                _tileLibraryService.LoadTileLibrary();
            }

            var map = new TileMap
            {
                Id = dto.Id,
                Name = dto.Name,
                Width = dto.Width,
                Height = dto.Height,
                CellSize = dto.CellSize,
                BackgroundColor = dto.BackgroundColor,
                ShowGrid = dto.ShowGrid,
                CreatedDate = dto.CreatedDate,
                ModifiedDate = dto.ModifiedDate,
                PlacedTiles = new ObservableCollection<Tile>(
                    dto.PlacedTiles.Select(t => new Tile
                    {
                        Id = t.Id,
                        TileDefinitionId = ResolveTileDefinitionId(t),
                        GridX = t.GridX,
                        GridY = t.GridY,
                        Rotation = t.Rotation,
                        FlipHorizontal = t.FlipHorizontal,
                        FlipVertical = t.FlipVertical,
                        ZIndex = t.ZIndex,
                        Notes = t.Notes
                    })
                )
            };

            return map;
        }

        private TileMapDto MapToDto(TileMap map)
        {
            return new TileMapDto
            {
                Id = map.Id,
                Name = map.Name,
                Width = map.Width,
                Height = map.Height,
                CellSize = map.CellSize,
                BackgroundColor = map.BackgroundColor,
                ShowGrid = map.ShowGrid,
                CreatedDate = map.CreatedDate,
                ModifiedDate = map.ModifiedDate,
                PlacedTiles = map.PlacedTiles.Select(t => new TileDto
                {
                    Id = t.Id,
                    TileDefinitionId = t.TileDefinitionId!,
                    ImagePath = _tileLibraryService.GetTileById(t.TileDefinitionId!)!.ImagePath,
                    GridX = t.GridX,
                    GridY = t.GridY,
                    Rotation = t.Rotation,
                    FlipHorizontal = t.FlipHorizontal,
                    FlipVertical = t.FlipVertical,
                    ZIndex = t.ZIndex,
                    Notes = t.Notes
                }).ToList()
            };
        }

        #region Helpers

        private string ResolveTileDefinitionId(TileDto dto)
        {
            var tileDef = _tileLibraryService.GetTileById(dto.TileDefinitionId);
            if (tileDef != null)
                return dto.TileDefinitionId;

            // FALLBACK - if for whatever reason it can load from the ID, try the imagePath
            if (!string.IsNullOrEmpty(dto.ImagePath))
            {
                var fallback = _tileLibraryService.GetTilesByImagePath(dto.ImagePath);
                if (fallback != null)
                {
                    Debug.WriteLine($"[TileMapService] Resolved tile by ImagePath fallback: {dto.ImagePath} -> {fallback.Id}");
                    return fallback.Id;
                }
            }
            return dto.TileDefinitionId;
        }

        #endregion
    }
}
