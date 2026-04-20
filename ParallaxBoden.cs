using Godot;
using System;

public partial class ParallaxBoden : MeshInstance3D
{
	[Export] public float Speed = 5.0f;
	[Export] public Node3D Camera;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
        if (Camera != null)
        {
            var camPos = Camera.GlobalPosition;
            GlobalPosition = new Vector3(camPos.X, GlobalPosition.Y, camPos.Z);
        }

		var material = GetActiveMaterial(0) as StandardMaterial3D;

        if (material != null)
        {
            Vector3 offset = material.Uv1Offset;
            offset.Y += (float)(Speed * delta * 0.001);
            material.Uv1Offset = offset;
        }
	}
}
