using Godot;
using System;
using System.Collections.Generic;

public partial class GridManager : Node
{
    [Export] public Label DebugInfo { get; set; }

    private readonly Dictionary<(int x, int y), Hex> hexStore = new();

    public void RegisterHex(int x, int y, Hex hex)
    {
        hexStore.Add((x, y), hex);
    }

    public Hex GetAt(int x, int y)
    {
        return hexStore.GetValueOrDefault((x, y));
    }
}
