using Godot;
using System;
using System.Collections.Generic;

public partial class LevelGenerator : Node
{
	[Export] public PackedScene Hex { get; set; }
	[Export] public Node GridManagerNode { get; set; }
	private GridManager GridManager => (GridManager)GridManagerNode;

	[Export] public int CurrentLevel { get; set; } = 0;

	[Export] public float StepX { get; set; } = 0.43f;
	[Export] public float StepY { get; set; } = 0.75f;
	[Export] public float HeightOffsetFactor { get; set; } = 0.1f;
	private Hex startHex;
	private Hex endHex;
	private int BaseLength = 10;
	private int BaseWidth = 15;
	
	public void generateNextLevel()
	{
		GD.Print($"Generating level {CurrentLevel}");

		if (CurrentLevel == 0)
		{
			GenerateStartGrid();
		}
		else
		{
			GeneratePlayGrid();
			determineStartAndEnd();
		}
	}

	private void GenerateStartGrid()
	{
		for (int y = 0; y < BaseLength; y++)
		{
			for (int x = 0; x < BaseWidth; x++)
			{
				PlaceHexAt(x, y);
			}        
		}

		startHex = GridManager.GetAt(5, 5);
		startHex.UpdateState(global::Hex.HexState.FromState(global::Hex.HexStateType.Start));

		endHex = GridManager.GetAt(10, 5);
		endHex.UpdateState(global::Hex.HexState.FromState(global::Hex.HexStateType.End));
	}

	private void GeneratePlayGrid()
	{
		for (int y = Mathf.RoundToInt(startHex.Position.X); y < Mathf.RoundToInt(startHex.Position.X + BaseLength); y++)
		{
			for (int x = 0; x < BaseWidth; x++)
			{
				PlaceHexAt(x, y);
			}        
		}

		var camera = GetParent().GetNode<CameraInteraction>("Camera3D");
		var CameraX = Mathf.RoundToInt(startHex.Position.X + BaseLength*0.5);
		GD.Print($"Moving camera to {CameraX}");
		GD.Print($"Moving camera to {camera.Position.X}");
		camera.Position = new Vector3(6.2f, 6f, CameraX);
	}

    private void PlaceHexAt(int x, int y)
    {
        var hex = Hex.Instantiate<Node3D>() as Hex;
		var hexName = $"Hex_{x}_{y}";
		hex.Name = hexName;
		if (HasNode(hexName))
        {
            return;
        }
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

        hex.SetBasePosition(hex.GlobalPosition);
        GridManager.RegisterHex(x, y, hex);
    }

	private void determineStartAndEnd()
	{
		int endZ = GD.RandRange(startHex.Coordinates.Y, startHex.Coordinates.Y + BaseLength);
		GD.Print($"starthex maxlength x: {startHex.Coordinates.Y + BaseLength}");
		GD.Print($"starthex x: {startHex.Coordinates.Y}");
		GD.Print($"endX: {endZ}");
		var endX = GD.RandRange(1, BaseWidth - 1);

		endHex.UpdateState(global::Hex.HexState.FromState(global::Hex.HexStateType.Start));
		startHex = endHex;

		endHex = GridManager.GetAt(endX, endZ);

		GD.Print($"New start: {endX}, New end: {endZ}");
		endHex.UpdateState(global::Hex.HexState.FromState(global::Hex.HexStateType.End));
	}
}
