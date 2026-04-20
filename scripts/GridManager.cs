using Godot;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class GridManager : Node
{
    private readonly Dictionary<(int x, int y), Hex> hexStore = new();

    public int MaxY => hexStore.Any() ? hexStore.Keys.Max(p => p.y) : 0;

    public void RegisterHex(int x, int y, Hex hex)
    {
        hexStore.Add((x, y), hex);
        hex.Coordinates = new(x, y);
    }

    public Hex GetAt(int x, int y)
    {
        return hexStore.GetValueOrDefault((x, y));
    }

    private static Dictionary<(int x, int y), int> GetDirectionTable(int y) => y % 2 == 0 ? directionLookupTableEven : directionLookupTableOdd;
    private static readonly Dictionary<(int x, int y), int> directionLookupTableOdd = new()
    {
        { (- 1, - 1), 0 },
        { (  0, - 1), 1 },
        { (+ 1,   0), 2 },
        { (  0, + 1), 3 },
        { (- 1, + 1), 4 },
        { (- 1,   0), 5 },
    };
    private static readonly Dictionary<(int x, int y), int> directionLookupTableEven = new()
    {
        { (  0, - 1), 0 },
        { (+ 1, - 1), 1 },
        { (+ 1,   0), 2 },
        { (+ 1, + 1), 3 },
        { (  0, + 1), 4 },
        { (- 1,   0), 5 },
    };

    public Hex[] GetNeighbours(int x, int y) => GetDirectionTable(y).Keys.Select(p => GetAt(x + p.x, y + p.y)).ToArray();

    public IEnumerable<Hex> GetValidNeighbours(int x, int y)
    {
        List<Hex> result = new();
        var hex = GetAt(x, y);
        var neighbours = GetNeighbours(x, y);
        foreach (var neighbour in neighbours)
        {
            if (neighbour == null)
            {
                continue;
            }

            var dX = neighbour.Coordinates.X - x;
            var dY = neighbour.Coordinates.Y - y;

            var direction = GetDirectionTable(y)[(dX, dY)];
            var opposite = (direction + 3) % 6;

            if (hex.State.IsValidSide(direction) && neighbour.State.IsValidSide(opposite) && neighbour.State.StateType != Hex.HexStateType.Start)
            {
                result.Add(neighbour);
            }
        }
        return result;
    }

    private bool Search(List<Hex> visited, (int x, int y) start, (int x, int y) end)
    {
        var target = GetAt(end.x, end.y);
        var current = GetAt(start.x, start.y);

        while (current != null)
        {
            if (current == target)
            {
                return true;
            }

            if (visited.Contains(current))
            {
                return false;
            }

            visited.Add(current);

            var neighbours = GetValidNeighbours(current.Coordinates.X, current.Coordinates.Y).Where(node => !visited.Contains(node));

            if (!neighbours.Any())
            {
                return false;
            }

            current = neighbours.FirstOrDefault();
        }
        return false;
    }

    public (bool Success, Hex[] Path) CheckConnection((int x, int y) start, (int x, int y) end)
    {
        var neighbours = GetValidNeighbours(start.x, start.y);

        foreach (var neighbour in neighbours)
        {
            var list = new List<Hex>();
            if (Search(list, (neighbour.Coordinates.X, neighbour.Coordinates.Y), end))
            {
                return (true, Enumerable.Concat([neighbour], list).ToArray());
            }
        }

        return (false, []);
    }

    public IEnumerable<Hex> GetCurrentLevelHexes()
    {
        var startHex = hexStore.Values.Where(h => h.State.StateType == Hex.HexStateType.Start).OrderBy(h => h.Coordinates.Y).LastOrDefault();
        if (startHex == null)
        {
            return [];
        }
        return hexStore.Where(d => d.Key.y > startHex.Coordinates.Y).Select(d => d.Value);
    }

    public void UpdateDisruptedHexes()
    {
        foreach (var hex in GetCurrentLevelHexes())
        {
            var disrupted = IsHexDisrupted(hex);
            hex.UpdateDisruptedVisuals(disrupted);
        }
    }

    private double disruptorUpdate = 0;

    public override void _PhysicsProcess(double delta)
    {
        disruptorUpdate += delta;

        if (disruptorUpdate >= 0.25)
        {
            disruptorUpdate = 0;
            UpdateDisruptedHexes();
        }
    }


    public bool IsHexDisrupted(Hex hex)
    {
        if (hex == null)
            return false;

        foreach (var disruptorCoors in GetCurrentLevelHexes().Where(h => h.State.StateType == Hex.HexStateType.Disruptor).Select(h => h.Coordinates))
        {
            var neighbours = GetNeighbours(hex.Coordinates.X, hex.Coordinates.Y).Where(h => h != null);

            if (neighbours.Any(h => h.State.StateType == Hex.HexStateType.Disruptor))
            {
                return true;
            }

            foreach (var coords in neighbours.Select(n => n.Coordinates))
            {
                var result = CheckConnection((coords.X, coords.Y), 
                                            (disruptorCoors.X, disruptorCoors.Y));

                if (result.Success)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public void DeactivateUnused(int y)
    {
        List<Vector2I> indicesToRemove = [];

        foreach (var hex in hexStore.Values.Where(h => h.Coordinates.Y <= y && h.State.CanInteract()))
        {
            hex.Deactivate();
            indicesToRemove.Add(hex.Coordinates);
        }

        foreach (var index in indicesToRemove)
        {
            hexStore.Remove((index.X, index.Y));
        }
    }

    private Hex debugStartHex = null;
    private Hex debugEndHex = null;

    private float stepX = 0.43f;
    private float stepY = 0.75f;
    public Vector3 GetNominalPosition(int x, int y)
    {
        var xPos = stepX * 2 * x;
        var yPos = 0;
        var zPos = stepY * y;

        if (y % 2 == 0)
        {
            xPos += stepX;
        }

        return new(xPos, yPos, zPos);
    }

    public void ResetLevel()
    {
        foreach (var hex in hexStore.Values)
        {
            hex.QueueFree();
        }
        hexStore.Clear();
    }
}
