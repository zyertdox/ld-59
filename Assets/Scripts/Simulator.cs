using System.Collections.Generic;
using UnityEngine;

public struct MoveCommand
{
    public Vector2Int From { get; set; }
    public Vector2Int To { get; set; }
    public UnitStatus Status { get; set; }
    public List<string> Highlights { get; set; }
}

public static class Simulator
{
    public static List<MoveCommand> Simulate(LevelData level, BrainData brain)
    {
        var commands = new List<MoveCommand>();
        var pos = level.Start;

        while (true)
        {
            var highlights = new List<string>();

            if (pos == level.Exit)
            {
                commands.Add(new MoveCommand
                {
                    From = pos,
                    To = pos,
                    Status = UnitStatus.Won,
                    Highlights = highlights
                });
                break;
            }

            var tile = GetTile(level, pos);
            if (tile == TileColor.None || tile == TileColor.Wall)
            {
                commands.Add(new MoveCommand
                {
                    From = pos,
                    To = pos,
                    Status = UnitStatus.Crashed,
                    Highlights = highlights
                });
                break;
            }

            var input = FindInputByColor(level, tile);
            if (input != null)
            {
                highlights.Add(input.Id);
            }

            var activeOutputs = new List<OutputNode>();
            if (input != null)
            {
                foreach (var wire in brain.Wires)
                {
                    if (wire.From != null && wire.From.Id == input.Id)
                    {
                        highlights.Add(wire.Id);
                        highlights.Add(wire.To.Id);
                        activeOutputs.Add(wire.To);
                    }
                }
            }

            if (activeOutputs.Count == 0)
            {
                commands.Add(new MoveCommand
                {
                    From = pos,
                    To = pos,
                    Status = UnitStatus.Stuck,
                    Highlights = highlights
                });
                break;
            }

            var output = activeOutputs[0];
            var newPos = pos + output.Step;

            commands.Add(new MoveCommand
            {
                From = pos,
                To = newPos,
                Status = UnitStatus.Running,
                Highlights = highlights
            });

            pos = newPos;
        }

        return commands;
    }

    private static TileColor GetTile(LevelData level, Vector2Int pos)
    {
        if (pos.x < 0 || pos.x >= level.Width || pos.y < 0 || pos.y >= level.Height)
        {
            return TileColor.None;
        }

        return level.Tiles[pos.x, pos.y];
    }

    private static InputNode FindInputByColor(LevelData level, TileColor color)
    {
        foreach (var column in level.Columns)
        foreach (var node in column)
        {
            if (node is InputNode input && input.TriggerColor == color)
            {
                return input;
            }
        }

        return null;
    }
}