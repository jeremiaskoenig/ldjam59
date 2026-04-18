using Godot;
using System;

public partial class MainScene : Node3D
{
	public static int CurrentLevel { get; private set; }

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		CurrentLevel = 0;

		//var hexGenerator = GetNode<GenerateHexes>("HexGenerator");
		//hexGenerator.generateNextLevel();		
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
