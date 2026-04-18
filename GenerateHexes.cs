using Godot;
using System;

[Tool]
public partial class GenerateHexes : Node3D
{
    [Export] public PackedScene Hex { get; set; }
    [Export] public float StepX { get; set; } = 0.43f;
    [Export] public float StepY { get; set; } = 0.75f;

    [Export] public int GenerateFromX { get; set; } = -25;
    [Export] public int GenerateFromY { get; set; } = -25;
    [Export] public int GenerateToX { get; set; } = 25;
    [Export] public int GenerateToY { get; set; } = 25;
    
    [Export] public float HeightOffsetFactor { get; set; } = 0.1f;

    [Export] bool Generate
    {
        get => false;
        set
        {
            if (value)
            {
                GenerateHexGrid();
            }
        }
    }

    [Export] bool Clear
    {
        get => false;
        set
        {
            if (value)
            {
                foreach (Node child in GetChildren())
                {
                    child.QueueFree();
                }
            }
        }
    }

    private void GenerateHexGrid()
    {
        for (int y = GenerateFromY; y < GenerateToY; y++)
        {
            for (int x = GenerateFromX; x < GenerateToX; x++)
            {
                PlaceHexAt(x, y);
            }        
        }
    }

    private void PlaceHexAt(int x, int y)
    {
        var newHex = Hex.Instantiate<Node3D>();
        newHex.Name = $"Hex_{x}_{y}";   
        this.AddChild(newHex);
        

        var xPos = StepX * 2 * x;
        var yPos = GD.Randf() * HeightOffsetFactor;
        var zPos = StepY * y;

        if (y % 2 == 0)
        {
            xPos += StepX;
        }

        newHex.GlobalPosition = new(xPos, yPos, zPos);
        newHex.Owner = GetTree().EditedSceneRoot;
        
    }
}