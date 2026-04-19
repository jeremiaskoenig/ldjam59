using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class GridManager : Node
{
    [Export] public Label DebugInfo { get; set; }
    [Export] public Button DebugButton { get; set; }

    private readonly Dictionary<(int x, int y), Hex> hexStore = new();

    public override void _Ready()
    {
        DebugButton.Pressed += () =>
        {
            Debug();
        };
    }


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

        GD.Print("");
        GD.Print("New Search");
        while (current != null)
        {
            GD.Print($"> Checking {current.Name}");

            if (current == target)
            {
                GD.Print(">> Found");
                return true;
            }

            if (visited.Contains(current))
            {
                GD.Print(">> Circle");
                return false;
            }

            visited.Add(current);

            var neighbours = GetValidNeighbours(current.Coordinates.X, current.Coordinates.Y).Where(node => !visited.Contains(node));

            if (!neighbours.Any())
            {
                GD.Print(">> Dead End");
                return false;
            }

            current = neighbours.FirstOrDefault();
        }

        GD.Print(">> No Result");
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

    public void DeactivateUnused(int y)
    {
        foreach (var hex in hexStore.Values.Where(h => h.Coordinates.Y <= y && h.State.CanInteract()))
        {
            hex.Deactivate();
        }
    }

    private Hex debugStartHex = null;
    private Hex debugEndHex = null;

    public void Debug()
    {
        debugStartHex ??= hexStore.Values.First(p => p.State.StateType == Hex.HexStateType.Start);
        debugEndHex ??= hexStore.Values.First(p => p.State.StateType == Hex.HexStateType.End);
        
        var result = CheckConnection(tt(debugStartHex.Coordinates), tt(debugEndHex.Coordinates));

        DebugInfo.Text = $"Success: {result.Success}";

        if (result.Success)
        {
            foreach (var hex in result.Path)
            {
                hex.SetWireColor(Colors.Cyan);
                hex.State.IsLocked = true;
            }
            
            DeactivateUnused(debugEndHex.Coordinates.Y);
        }

        (int x, int y) tt(Vector2I v) => (v.X, v.Y);   
    }

    public void Debug(Hex hex)
    {
        DebugInfo.Text = $"Node: {hex.Name}";
        DebugInfo.Text += "\nValid Neighbours:";
        foreach (var neighbour in GetValidNeighbours(hex.Coordinates.X, hex.Coordinates.Y))
        {
            DebugInfo.Text += $"\n- {neighbour.Name}";
        }
    }

}
