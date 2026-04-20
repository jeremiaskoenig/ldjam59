using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class RoundManager : Node
{
    [Export] public CameraInteraction CameraInteraction { get; set; }
    [Export] public DebugLevelGenerator LevelGenerator { get; set; }
    [Export] public GridManager GridManager { get; set; }
    [Export] public ProgressBar RoundProgress { get; set; }
    [Export] public int RoundDelay { get; set; }
    [Export] public Button RetryButton { get; set; }
    [Export] public Label3D ScoreLabel { get; set; }
    [Export] public Label3D HighscoreLabel { get; set; }
    [Export] public TransitionCamera TransitionCamera { get; set; }
    [Export] public Camera3D ScoreCamera { get; set; }

    private float currentDelay = 2;

    public int CurrentRound { get; set; } = 0;
    public bool IsRunning { get; set; }

    private Stack<Hex> startHexes = [];
    private Hex endHex = null;

    public event Action PlayerLost;

    public void ResetLevel()
    {
        CurrentRound = 0;
        startHexes.Clear();
        endHex = null;
        IsRunning = false;
        RoundProgress.Value = 0;
        LevelGenerator.ResetLevel();
    }

    public void StartGame()
    {
        CameraInteraction.IsInputLocked = false;
        GridManager.ResetLevel();
        endHex = LevelGenerator.GenerateLevel();
        startHexes.Push(LevelGenerator.StartHex);
        CameraInteraction.MoveToFocusHexRow(endHex.Coordinates.Y, true);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!IsRunning)
            return;

        var (success, path) = CheckForSuccess();

        if (success)
        {
            AdvanceRound(path);
        }
    }


    public override void _Process(double delta)
    {
        if (!IsRunning)
            return;

        if (currentDelay > 0)
        {
            currentDelay = Mathf.Max(0, currentDelay - (float)delta);
            return;
        }

        RoundProgress.Value += delta;

        if (RoundProgress.Value >= RoundProgress.MaxValue)
        {
            var (success, path) = CheckForSuccess();

            if (success)
            {
                AdvanceRound(path);
            }
            else
            {
                IsRunning = false;
                RoundProgress.Visible = false;
                PlayerLost?.Invoke();
            }

        }
    }

    private void AdvanceRound(IEnumerable<Hex> path)
    {
        RoundProgress.Value = 0;
        currentDelay = RoundDelay;

        foreach (var hex in path)
        {
            hex.SetSolved();
            hex.State.IsLocked = true;
        }
        
        GridManager.DeactivateUnused(endHex.Coordinates.Y);
        StartNextLevel();
    }

    private (bool Success, Hex[] Path) CheckForSuccess()
    {
        var startHex = startHexes.Peek();
        var start = (startHex.Coordinates.X, startHex.Coordinates.Y);
        var end = (endHex.Coordinates.X, endHex.Coordinates.Y);
        var (success, path) = GridManager.CheckConnection(start, end);
        success &= !path.Any(h => GridManager.IsHexDisrupted(h));
        return (success, path);
    }

    public void StartNextLevel()
    {
        CurrentRound++;
        GD.Print($"Starting round {CurrentRound}");

        var targetRow = endHex.Coordinates.Y;
        startHexes.Push(endHex);
        LevelGenerator.CurrentLevel = CurrentRound;
        endHex = LevelGenerator.GenerateLevel();
        targetRow += endHex.Coordinates.Y;
        targetRow /= 2;
        CameraInteraction.MoveToFocusHexRow(targetRow);
    }

    private uint highScore;
    private const string HighscoreFile = "user://highscore.dat";

    public override void _Ready()
    {
        if (FileAccess.FileExists(HighscoreFile))
        {
            using var access = FileAccess.Open(HighscoreFile, FileAccess.ModeFlags.Read);
            highScore = access.Get32();
            HighscoreLabel.Text = highScore.ToString();
        }
    }


    public void StartRecap()
    {
        CameraInteraction.IsInputLocked = true;

        var score = startHexes.Count - 1;
        ScoreLabel.Text = score.ToString();
        
        if (highScore < score)
        {
            highScore = (uint)score;
            using var access = FileAccess.Open(HighscoreFile, FileAccess.ModeFlags.Write);
            access.Store32(highScore);
            
        }
        HighscoreLabel.Text = highScore.ToString();

        float duration = endHex.Coordinates.Y * 0.1f;
        CameraInteraction.MoveToFocusHexRow(5, duration: duration);
        var timer = GetTree().CreateTimer(duration);
        timer.Timeout += () =>
        {
            TransitionCamera.Transition(CameraInteraction, ScoreCamera, 1);
            TransitionCamera.TransitionFinished += () =>
            {
                        RetryButton.Visible = true;
            };
            timer.Dispose();
        };
    }
}
