using Godot;
using System;

public partial class GameState : Node
{
    public enum State
    {
        MainMenu,
        HowToPlayMenu,
        CreditsMenu,
        GameRunning,
        GameLost,
    }

    [Export] public State CurrentState { get; set; }
    [Export] public Control MainMenu { get; set; }
    [Export] public Control IngameUI { get; set; }
    [Export] public TransitionCamera TransitionCamera { get; set; }
    [Export] public Camera3D MainMenuCamera { get; set; }
    [Export] public Camera3D ScoreboardCamera { get; set; }
    [Export] public CameraInteraction IngameCamera { get; set; }
    [Export] public RoundManager RoundManager { get; set; }
    [Export] public Node3D Scoreboard { get; set; }
    [Export] public Node3D HighscoreBoard { get; set; }
    [Export] public Node3D HowToPlayInfo { get; set; }
    [Export] public Node3D CreditsInfo { get; set; }
    [Export] public Button ButtonBack { get; set; }

    private Button btnNewGame;
    private Button btnHowToPlay;
    private Button btnCredits;
    private Button btnQuit;

    public override void _Ready()
    {
        btnNewGame = MainMenu.FindChild("StartGame") as Button;
        btnHowToPlay = MainMenu.FindChild("HowToPlay") as Button;
        btnCredits = MainMenu.FindChild("Credits") as Button;
        btnQuit = MainMenu.FindChild("Quit") as Button;

        btnNewGame.Pressed += StartGame;
        btnHowToPlay.Pressed += ChangeToHowToPlay;
        btnCredits.Pressed += ChangeToCredits;
        btnQuit.Pressed += () => { GetTree().Quit(); };

        ButtonBack.Pressed += ResetToMainMenu;

        RoundManager.PlayerLost += FinishGame;
    }

    private void ChangeToHowToPlay()
    {
        CurrentState = State.HowToPlayMenu;
        MainMenu.Visible = false; 
        HighscoreBoard.Visible = false;
        
        TransitionCamera.Transition(MainMenuCamera, ScoreboardCamera, 1);
        TransitionCamera.TransitionFinished += () =>
        {
            HowToPlayInfo.Visible = true;
            ButtonBack.Visible = true;
        };
    }

    private void ChangeToCredits()
    {
        CurrentState = State.HowToPlayMenu;
        MainMenu.Visible = false; 
        HighscoreBoard.Visible = false;
        
        TransitionCamera.Transition(MainMenuCamera, ScoreboardCamera, 1);
        TransitionCamera.TransitionFinished += () =>
        {
            CreditsInfo.Visible = true;
            ButtonBack.Visible = true;
        };
    }

    private void FinishGame()
    {
        CurrentState = State.GameLost;
        RoundManager.StartRecap();
        Scoreboard.Visible = true;
    }

    private void ResetToMainMenu()
    {
        CurrentState = State.MainMenu;
        ButtonBack.Visible = false;
        IngameUI.Visible = false;
        Scoreboard.Visible = false;
        HowToPlayInfo.Visible = false;
        CreditsInfo.Visible = false;
        RoundManager.ResetLevel();
        var currentCamera = GetTree().Root.GetCamera3D();
        TransitionCamera.Transition(currentCamera, MainMenuCamera, 1);
        TransitionCamera.TransitionFinished += () =>
        {
            MainMenu.Visible = true;
            HighscoreBoard.Visible = true;
        };
    }

    private void StartGame()
    {
        CurrentState = State.GameRunning;
        MainMenu.Visible = false; 
        HighscoreBoard.Visible = false;
        IngameCamera.MoveToFocusHexRow(5, true);
        TransitionCamera.Transition(MainMenuCamera, IngameCamera, 1);
        RoundManager.StartGame();
        TransitionCamera.TransitionFinished += () =>
        {
            IngameUI.Visible = true;
            RoundManager.IsRunning = true;
        };
    }
}
