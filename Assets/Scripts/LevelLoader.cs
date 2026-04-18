using System;
using Newtonsoft.Json;
using UnityEngine;

public static class LevelLoader
{
    public static LevelData LoadFromResources(string resourcePath)
    {
        var asset = Resources.Load<TextAsset>(resourcePath);
        if (asset == null)
        {
            throw new ArgumentException($"Level asset not found: Resources/{resourcePath}");
        }

        return Parse(asset.text);
    }

    public static LevelData Parse(string json)
    {
        var dto = JsonConvert.DeserializeObject<LevelDto>(json);
        if (dto == null)
        {
            throw new ArgumentException("Failed to parse level JSON");
        }

        var level = new LevelData
        {
            Id = dto.id,
            Name = dto.name,
            Width = dto.width,
            Height = dto.height,
            Start = new Vector2Int(dto.start.x, dto.start.y),
            Exit = new Vector2Int(dto.exit.x, dto.exit.y),
            Tiles = ParseTiles(dto.tiles, dto.width, dto.height),
            Columns = ParseColumns(dto.nodes)
        };
        return level;
    }

    private static TileColor[,] ParseTiles(string[] rows, int width, int height)
    {
        if (rows == null || rows.Length != height)
        {
            throw new ArgumentException($"Expected {height} tile rows, got {rows?.Length ?? 0}");
        }

        var tiles = new TileColor[width, height];
        for (var row = 0; row < height; row++)
        {
            var y = height - 1 - row;
            var line = rows[row];
            if (line.Length != width)
            {
                throw new ArgumentException($"Row {row} has length {line.Length}, expected {width}");
            }

            for (var x = 0; x < width; x++)
            {
                tiles[x, y] = CharToColor(line[x]);
            }
        }

        return tiles;
    }

    private static TileColor CharToColor(char c)
    {
        return c switch
        {
            '.' => TileColor.None,
            '#' => TileColor.Wall,
            'R' => TileColor.Red,
            'G' => TileColor.Green,
            'B' => TileColor.Blue,
            _ => throw new ArgumentException($"Unknown tile char: '{c}'")
        };
    }

    private static NeuronNode[][] ParseColumns(string[][] dtoColumns)
    {
        if (dtoColumns == null)
        {
            return Array.Empty<NeuronNode[]>();
        }

        var columns = new NeuronNode[dtoColumns.Length][];
        for (var c = 0; c < dtoColumns.Length; c++)
        {
            var col = dtoColumns[c];
            columns[c] = new NeuronNode[col.Length];
            for (var i = 0; i < col.Length; i++)
            {
                columns[c][i] = NodeCatalog.FromId(col[i]);
            }
        }

        return columns;
    }

    [Serializable]
    private class LevelDto
    {
        public string id;
        public string name;
        public int width;
        public int height;
        public string[] tiles;
        public CoordDto start;
        public CoordDto exit;
        public string[][] nodes;
    }

    [Serializable]
    private class CoordDto
    {
        public int x;
        public int y;
    }
}