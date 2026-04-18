using Godot;
using System;

public partial class LevelGenerator : Node
{
    [Export] public Node3D LevelContainer { get; set; }
    [Export] public Node GridManagerNode { get; set; }
    private GridManager GridManager => (GridManager)GridManagerNode;

    [Export] public int CurrentLevel { get; set; } = 0;

    public void GenerateLevel(int level)
    {
        //do stuff to gen level
    }
}
