using Godot;
using System;

public partial class RoundManager : Node
{
    [Export] public GridManager GridManager { get; set; }

    [Export] public ProgressBar RoundProgress { get; set; }

    [Export] public int RoundDelay { get; set; }

    private float currentDelay = 2;

    public int CurrentRound { get; set; } = 0;

    public override void _Process(double delta)
    {
        if (currentDelay > 0)
        {
            currentDelay = Mathf.Max(0, currentDelay - (float)delta);
            return;
        }

        RoundProgress.Value += delta;

        if (RoundProgress.Value >= RoundProgress.MaxValue)
        {
            //TODO: Check and init new level

            RoundProgress.Value = 0;
            currentDelay = RoundDelay;
        }
    }

    public void StartNextLevel()
    {
        CurrentRound++;

        // var nextLevelEndHex = LevelGenerator.GenerateLevel();
    }

}
