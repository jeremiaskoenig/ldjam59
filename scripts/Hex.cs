using System;
using System.ComponentModel;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using Godot;

public partial class Hex : MeshInstance3D
{
    private Vector3 lockedPosition;

    private int hoverState => IsSelected ? 2 : (IsHovered ? 1 : 0);
    public int HoverFrames { get; set; } = 0;
    public bool IsHovered => HoverFrames > 0;
    public bool IsSelected => IsRotationMode || IsSwapMode;

    public bool IsRotationMode { get; set; }
    public bool IsSwapMode { get; set; }

    public int SwapAnimationFrames { get; set; }

    [Export] public MeshInstance3D Outline { get; set; }

    [Export] public Node3D[] Connectors { get; set; }

    [Export] public Node3D StartMarker { get; set; }
    [Export] public Node3D EndMarker { get; set; }

    [Export] public HexStateType StartType { get; set; }

    public HexState State { get; set; }

    public enum HexStateType
    {
        Connection,
        Start,
        End,
    }

    public record HexState(int ConnectionType, int Rotation, HexStateType StateType)
    {
        public static HexState Random() => new HexState(GD.RandRange(0, 2), GD.RandRange(0, 5), HexStateType.Connection);
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
                switch (ConnectionType)
                {
                    case 0:
                        connectors[Rotate(0)].Visible = true;
                        connectors[Rotate(1)].Visible = true;
                        break;
                    case 1:
                        connectors[Rotate(0)].Visible = true;
                        connectors[Rotate(2)].Visible = true;
                        break;
                    case 2:
                        connectors[Rotate(0)].Visible = true;
                        connectors[Rotate(3)].Visible = true;
                        break;
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
    }


    public void UpdateState(HexState state)
    {
        State = state;
        State.UpdateView(Connectors, StartMarker, EndMarker);
    }

    private void RandomizeTile()
    {
        UpdateState(HexState.Random());
    }

    public Color OutlineColor
    {
        get
        {
            var material = Outline.MaterialOverride as ShaderMaterial;
            if (material != null)
            {
                return (Color)material.GetShaderParameter("outline_color");
            }
            return new Color(0, 0, 0, 0);
        }
        set
        {
            var material = Outline.MaterialOverride as ShaderMaterial;
            if (material != null)
            {
                material.SetShaderParameter("outline_color", value);
            }
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
        foreach (var connector in Connectors)
        {
            MakeMaterialUnique(connector.GetChild<MeshInstance3D>(0));
        }
        MakeMaterialUnique(StartMarker as MeshInstance3D);
        MakeMaterialUnique(EndMarker as MeshInstance3D);

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
    }

    public override void _Process(double delta)
    {
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

        this.Outline.Visible = this.hoverState > 0;
        
        switch (this.hoverState)
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
        var targetPos = this.hoverState switch
        {
            0 => lockedPosition,
            1 => lockedPosition + new Vector3(0, 0.1f, 0),
            2 => lockedPosition + new Vector3(0, 0.25f, 0),
            _ => this.Position
        };

        var totalDistance = startPos.DistanceTo(targetPos);
        if (Mathf.Abs(totalDistance) < 0.0001f)
            return;

        const float speed = 2.0f;

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

}
