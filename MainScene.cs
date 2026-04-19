using Godot;
using System;

public partial class MainScene : Node3D
{
	public static int CurrentLevel { get; private set; }

	[Export] public Node LevelGeneratorNode { get; set; }
	private LevelGenerator LevelGenerator => (LevelGenerator)LevelGeneratorNode;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		GD.Print("Ready");

		var levelGenerator = GetNode<LevelGenerator>("HexGen");
		levelGenerator.CurrentLevel = 0;
		levelGenerator.generateNextLevel();

		var camera = GetNode<CameraInteraction>("Camera3D");
		camera.Position = new Vector3(6.2f, 6f, 5.5f);
		camera.RotationDegrees = new Vector3(-70, 0, 0);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
 