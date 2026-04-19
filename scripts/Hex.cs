using System;
using System.Linq;
using Godot;

public partial class Hex : MeshInstance3D
{
    [Export] public Label3D NodeLabel { get; set; }

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

    [Export] public HexStateType StartType { get; set; }

    public Vector2I Coordinates { get; set; }

    public HexState State { get; set; }

    public enum HexStateType
    {
        Connection,
        Start,
        End,
    }

    public record HexState(int ConnectionType, int Rotation, HexStateType StateType)
    {
        public static HexState FromState(HexStateType type) => new(0, 0, type);
        public static HexState Random(bool allowEmpty = false) => new HexState(GD.RandRange(allowEmpty ? 0 : 1, 3), GD.RandRange(0, 5), HexStateType.Connection);
        public HexState Rotated(int direction) => new (ConnectionType, (Rotation + 6 + direction) % 6, StateType);

        private static readonly Color wireColor = new(255, 204, 0);
        private static readonly Color endColor = new(255, 0, 0);
        private static readonly Color startColor = new(0, 255, 0);

        private void UpdateColor(Node3D[] connectors, Node3D startMarker, Node3D endMarker)
        {
            Color color = StateType switch
            {
                HexStateType.Connection => wireColor,
                HexStateType.Start => startColor,
                HexStateType.End => endColor,
                _ => Colors.Magenta,
            };

            foreach (var connector in connectors)
            {
                if (connector.GetChild<MeshInstance3D>(0).MaterialOverride is StandardMaterial3D material)
                {
                    material.AlbedoColor = color;
                }
            }
            if ((startMarker as MeshInstance3D).MaterialOverride is StandardMaterial3D materialStart)
            {
                materialStart.AlbedoColor = color;
            }
            if ((endMarker as MeshInstance3D).MaterialOverride is StandardMaterial3D materialEnd)
            {
                materialEnd.AlbedoColor = color;
            }
        }

        public void UpdateView(Node3D[] connectors, Node3D startMarker, Node3D endMarker)
        {
            UpdateColor(connectors, startMarker, endMarker);

            foreach (var connector in connectors)
            {
                connector.Visible = false;
            }
            startMarker.Visible = false;
            endMarker.Visible = false;

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


    public void UpdateState(HexState state)
    {
        State = state;
        State.UpdateView(Connectors, StartMarker, EndMarker);
        NodeLabel.Text = Name.ToString().Replace("Hex_", "").Replace("_", " ");
    }

    private void RandomizeTile()
    {
        UpdateState(HexState.Random());
    }

    public void SetWireColor (Color color)
    {
        ApplyForWireMeshes(mesh =>
        {
            (mesh.MaterialOverride as StandardMaterial3D).AlbedoColor = color;
        });
    }

    private void ApplyForWireMeshes(Action<MeshInstance3D> apply)
    {
        foreach (var connector in Connectors)
        {
            apply(connector.GetChild<MeshInstance3D>(0));
        }
        apply(StartMarker as MeshInstance3D);
        apply(EndMarker as MeshInstance3D);
    }

    public Color OutlineColor
    {
        get
        {
            var material = Outline.MaterialOverride as StandardMaterial3D;
            return material.AlbedoColor;
        }
        set
        {
            var material = Outline.MaterialOverride as StandardMaterial3D;
            material.AlbedoColor = value;
        }
    }

    public void SetBasePosition(Vector3 pos)
    {
        this.lockedPosition = pos;
    }

    private static void MakeMaterialUnique(MeshInstance3D node)
    {
        node.MaterialOverride = node.MaterialOverride.Duplicate() as Material;
    }

    public override void _Ready()
    {
        this.lockedPosition = this.Position;
        MakeMaterialUnique(this);
        MakeMaterialUnique(Outline);
        ApplyForWireMeshes(MakeMaterialUnique);

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
        DropDelay--;
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
            Scale = Vector3.One * factor;
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
                OutlineColor = Colors.Yellow;
                break;
            case 2:
                this.Outline.Visible = true;
                if (IsSwapMode)
                {
                    OutlineColor = Colors.Green;
                }
                else if (IsRotationMode)
                {
                    OutlineColor = Colors.Blue;
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
            3 => DropDelay > 0 ? lockedPosition : lockedPosition + new Vector3(0, -20000f, 20000f),
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
        SetWireColor(Colors.DarkOliveGreen);
        State.IsLocked = true;
        IsDropped = true;
        DropDelay = (30 - Math.Abs(Coordinates.X) + Coordinates.Y) * 3;
    }
}
