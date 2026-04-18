using System;
using System.Collections.Generic;
using UnityEngine;

public enum TileColor
{
    None,
    Wall,
    Red,
    Green,
    Blue
}

public enum UnitStatus
{
    Running,
    Won,
    Crashed,
    Stuck
}

public abstract class NeuronNode
{
    public string Id { get; set; }
}

public sealed class InputNode : NeuronNode
{
    public TileColor TriggerColor { get; set; }

    public InputNode(TileColor triggerColor)
    {
        TriggerColor = triggerColor;
        Id = $"In.{ColorChar(triggerColor)}";
    }

    static char ColorChar(TileColor c) => c switch
    {
        TileColor.Red => 'R',
        TileColor.Green => 'G',
        TileColor.Blue => 'B',
        _ => '?'
    };
}

public sealed class OutputNode : NeuronNode
{
    public char Code { get; set; }
    public Vector2Int Step { get; set; }

    public OutputNode(char code, Vector2Int step)
    {
        Code = code;
        Step = step;
        Id = $"Out.{code}";
    }
}

public static class NodeCatalog
{
    public static NeuronNode FromId(string id) => id switch
    {
        "In.R"  => new InputNode(TileColor.Red),
        "In.G"  => new InputNode(TileColor.Green),
        "In.B"  => new InputNode(TileColor.Blue),
        "Out.F" => new OutputNode('F', new Vector2Int(1,  0)),
        "Out.U" => new OutputNode('U', new Vector2Int(1,  1)),
        "Out.D" => new OutputNode('D', new Vector2Int(1, -1)),
        _ => throw new ArgumentException($"Unknown node id: {id}")
    };
}

public struct Wire
{
    public InputNode From { get; set; }
    public OutputNode To { get; set; }

    public Wire(InputNode from, OutputNode to)
    {
        From = from;
        To = to;
    }
}

public struct UnitState
{
    public Vector2Int Position { get; set; }
    public UnitStatus Status { get; set; }
}

public class BrainData
{
    public List<Wire> Wires { get; } = new();
}

public class LevelData
{
    public string Id { get; set; }
    public string Name { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public TileColor[,] Tiles { get; set; }
    public Vector2Int Start { get; set; }
    public Vector2Int Exit { get; set; }
    public NeuronNode[][] Columns { get; set; }
}
