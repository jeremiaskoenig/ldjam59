using System;
using System.Linq;
using Godot;
using Godot.Collections;

public partial class Hex : MeshInstance3D
{
    [Export] public Label3D NodeLabel { get; set; }
    
    [Export] public Dictionary<string, Material> Materials { get; set; }

    private Vector3 lockedPosition;

    private int positionState => IsDropped ? 3 : selectionState;
    private int selectionState => IsSelected ? 2 : hoverState;
    private int hoverState => IsHovered ? 1 : 0;
    
    public int HoverFrames { get; set; } = 0;
    public bool IsHovered => HoverFrames > 0;
    public bool IsSelected => IsRotationMode || IsSwapMode;

    public bool IsDropped { get; set; }
    public int DropDelay { get; set; } = 0;

    public bool IsRotationMode { get; set; }
    public bool IsSwapMode { get; set; }

    public int SwapAnimationFrames { get; set; }

    [Export] public MeshInstance3D Outline { get; set; }

    [Export] public Node3D[] Connectors { get; set; }

    [Export] public Node3D StartMarker { get; set; }
    [Export] public Node3D EndMarker { get; set; }
    [Export] public Node3D DisruptorMarker { get; set; }
    [Export] public Node3D DisruptionEffect { get; set; }

    [Export] public HexStateType StartType { get; set; }

    public Vector2I Coordinates { get; set; }

    public HexState State { get; set; }

    public enum HexStateType
    {
        Connection,
        Start,
        End,
        Disruptor,
    }

    public record HexState(int ConnectionType, int Rotation, HexStateType StateType)
    {
        public static HexState FromState(HexStateType type) => new(0, 0, type);
        public static HexState Random(bool allowEmpty = false) => new HexState(GD.RandRange(allowEmpty ? 0 : 1, 3), GD.RandRange(0, 5), HexStateType.Connection);
        public HexState Rotated(int direction) => new (ConnectionType, (Rotation + 6 + direction) % 6, StateType);

        public void UpdateView(Node3D[] connectors, Node3D startMarker, Node3D endMarker, Node3D disruptorMarker)
        {
            foreach (var connector in connectors)
            {
                connector.Visible = false;
            }
            startMarker.Visible = false;
            endMarker.Visible = false;
            disruptorMarker.Visible = false;

            if (StateType == HexStateType.Connection)
            {
                foreach (var index in GetConnectionIndices)
                {
                    connectors[index].Visible = true;
                }
            }
            else if (StateType == HexStateType.Start)
            {
                startMarker.Visible = true;
            }
            else if (StateType == HexStateType.End)
            {
                endMarker.Visible = true;
            }
            else if (StateType == HexStateType.Disruptor)
            {
                disruptorMarker.Visible = true;
            }
        }

        private int Rotate(int index) => (index + Rotation) % 6;

        private int[] GetConnectionIndices => ConnectionType switch
        {
            //1 => [Rotate(0), Rotate(1)],
            1 => [Rotate(0), Rotate(3)], // replace with straight line
            2 => [Rotate(0), Rotate(2)],
            3 => [Rotate(0), Rotate(3)],
            _ => [],
        };

        public bool IsValidSide(int direction)
        {
            if (StateType != HexStateType.Connection)
            {
                return true;
            }
            return GetConnectionIndices.Contains(direction);
        }

        public bool IsLocked { get; set; }

        public bool CanInteract()
        {
            return !IsLocked && StateType == HexStateType.Connection;
        }
    }

    public void UpdateDisruptedVisuals(bool disrupted)
    {
        DisruptionEffect.Visible = disrupted;
        ApplyForWireMeshes(mesh =>
        {
            if (mesh.MaterialOverride != Materials["wire_solved"])
            {
                mesh.MaterialOverride = disrupted ? Materials["wire_disrupted"] : Materials["wire"];
            }
        });
    }

    public void UpdateState(HexState state)
    {
        State = state;
        State.UpdateView(Connectors, StartMarker, EndMarker, DisruptorMarker);
        NodeLabel.Text = Name.ToString().Replace("Hex_", "").Replace("_", " ");
    }

    private void RandomizeTile()
    {
        UpdateState(HexState.Random());
    }

    public void SetSolved()
    {
        DisruptionEffect.Visible = false;
        ApplyForWireMeshes(mesh =>
        {
            mesh.MaterialOverride = Materials["wire_solved"];
        });
    }

    private void ApplyForWireMeshes(Action<MeshInstance3D> apply)
    {
        foreach (var connector in Connectors)
        {
            apply(connector.GetChild<MeshInstance3D>(0));
        }
    }

    /// <summary>
    /// Sets the outline material
    /// </summary>
    /// <param name="state">0 = hover, 1 = swap select</param>
    public void SetOutline(int state)
    {
        switch (state)
        {
            case 0:
                Outline.MaterialOverride = Materials["outline_hover"];
                break;
            case 1:
                Outline.MaterialOverride = Materials["outline_swap"];
                break;
        }
    }

    public void SetBasePosition(Vector3 pos)
    {
        this.lockedPosition = pos;
    }

    public override void _Ready()
    {
        this.lockedPosition = this.Position;

        switch (StartType)
        {
            case HexStateType.Connection:
                RandomizeTile();
                break;
            case HexStateType.Start:
                UpdateState(new HexState(0, 0, HexStateType.Start));
                break;
            case HexStateType.End:
                UpdateState(new HexState(0, 0, HexStateType.End));
                break;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        HoverFrames = Mathf.Max(0, HoverFrames - 1);
        SwapAnimationFrames = Mathf.Max(0, SwapAnimationFrames - 1);
        if (IsDropped)
        {
            DropDelay--;
            
            if (DropDelay < -300)
            {
                //naive "lets just destroy it when it was dropped for X amount of time..."
                //can lead to issues in the GridManager since we now have potentially destroyed references
                QueueFree();
            }
        }
    }

    public override void _Process(double delta)
    {
        NodeLabel.Visible = CameraInteraction.DebugShowLabels;

        if (SwapAnimationFrames > 0)
        {
            float factor;
            if (SwapAnimationFrames > (SwapAnimationDuration / 2f))
            {
                factor = (SwapAnimationFrames - (SwapAnimationDuration / 2f)) / (SwapAnimationDuration / 2f);
            }
            else
            {
                factor = 1 - (SwapAnimationFrames / (SwapAnimationDuration / 2f));
            }
            Scale = Vector3.One * Mathf.Max(factor, 0.0001f);
        }
        else
        {
            Scale = Vector3.One;
        }

        this.Outline.Visible = this.positionState > 0;
        
        switch (this.positionState)
        {
            case 1:
                this.Outline.Visible = true;
                SetOutline(0);
                break;
            case 2:
                this.Outline.Visible = true;
                if (IsSwapMode)
                {
                    SetOutline(1);
                }
                else if (IsRotationMode)
                {
                    //SetOutline(2);
                }
                break;
            default:
            this.Outline.Visible = false;
                break;
        }

        var startPos = this.Position;
        var targetPos = this.positionState switch
        {
            0 => lockedPosition,
            1 => lockedPosition + new Vector3(0, 0.1f, 0),
            2 => lockedPosition + new Vector3(0, 0.25f, 0),
            3 => DropDelay > 0 ? lockedPosition : lockedPosition + new Vector3(0, -200f, 200f),
            _ => this.Position
        };

        var totalDistance = startPos.DistanceTo(targetPos);
        if (Mathf.Abs(totalDistance) < 0.0001f)
            return;

        var speed = this.positionState == 3 ? (5.0f + Math.Abs(DropDelay) * 1.03f) : (this.positionState == 0 ? 25f : 2f);// 2.0f;
        var t = speed * (float)delta / totalDistance;
        t = Mathf.Clamp(t, 0, 1);

        Position = Position.Lerp(targetPos, t);       
    }

    public void Hover()
    {
        HoverFrames = 5;
    }

    private const int SwapAnimationDuration = 10;

    public void AnimateSwap()
    {
        SwapAnimationFrames = SwapAnimationDuration;
    }

    public void Deactivate()
    {
        ApplyForWireMeshes(mesh => mesh.MaterialOverride = Materials["wire_inactive"]);
        State.IsLocked = true;
        IsDropped = true;
        DropDelay = (30 - Math.Abs(Coordinates.X) + Coordinates.Y) * 3;
        IsSwapMode = false;
        IsRotationMode = false;
    }
}
