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

    [Export] public Node GridManagerNode { get; set; }
    private GridManager GridManager => (GridManager)GridManagerNode;

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

    public override void _Ready()
    {
        GenerateHexGrid();

        var startX = GD.RandRange(GenerateFromX + 1, GenerateToX - 1);
        var startY = GD.RandRange(GenerateFromY + 1, GenerateFromY + 5);

        var startHex = GridManager.GetAt(startX, startY);
        startHex.UpdateState(global::Hex.HexState.FromState(global::Hex.HexStateType.Start));

        var endX = GD.RandRange(GenerateFromX + 1, GenerateToX - 1);
        var endY = GD.RandRange(GenerateToY - 5, GenerateToY - 1);

        var endHex = GridManager.GetAt(endX, endY);
        endHex.UpdateState(global::Hex.HexState.FromState(global::Hex.HexStateType.End));
    }


    private void GenerateHexGrid()
    {
        Clear = true;
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
        var hex = Hex.Instantiate<Node3D>();
        hex.Name = $"Hex_{x}_{y}";
        this.AddChild(hex);

        var xPos = StepX * 2 * x;
        var yPos = GD.Randf() * HeightOffsetFactor;
        var zPos = StepY * y;

        if (y % 2 == 0)
        {
            xPos += StepX;
        }

        hex.GlobalPosition = new(xPos, yPos, zPos);
        hex.Owner = GetTree().EditedSceneRoot;

        if (hex is Hex hexData)
        {
            hexData.SetBasePosition(hex.GlobalPosition);
            GridManager.RegisterHex(x, y, hexData);

            var randomOffset = GD.RandRange(5, 15);

            var spawnZ = 15f + randomOffset;
            var spawnY = -(spawnZ / 2.5f);

            hex.GlobalPosition += new Vector3(0, spawnY, spawnZ);
        }
    }
}