using Godot;
using System;

public partial class SpawnTrash : Node3D
{
    [Export] public PackedScene Stuff { get; set; }

    public override void _Ready()
    {
        for (int i = 0; i < 1800; i++)
        {
            var hex = Stuff.Instantiate<Node3D>();
            this.AddChild(hex);

            var xPos = 80;
            var yPos = 0;
            var zPos = -80;

            hex.GlobalPosition = new(xPos, yPos, zPos);
            hex.Owner = GetTree().EditedSceneRoot;

            if (hex is Hex hexData)
            {
                hexData.SetBasePosition(hex.GlobalPosition);
            }
        }
    }
}
