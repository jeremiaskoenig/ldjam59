using System;
using System.Linq;
using Godot;

public partial class CameraInteraction : Camera3D
{
    [Export] public Godot.Collections.Dictionary<string, AudioStream> SoundEffects { get; set; }

    [Export] public AudioStreamPlayer AudioStreamPlayer { get; set; }
    [Export] public Node GridManagerNode { get; set; }
    private GridManager GridManager => (GridManager)GridManagerNode;

    private Hex GetHexOnCursor()
    {
        var mousePos = GetViewport().GetMousePosition();

        const int rayLength = 10000;
        var from = this.GlobalPosition;
        var direction = this.ProjectRayNormal(mousePos);
        var to = from + direction * rayLength;

        var spaceState = GetWorld3D().DirectSpaceState;
        var query = PhysicsRayQueryParameters3D.Create(from, to);

        query.HitFromInside = true;
        query.HitBackFaces = true;
        query.CollisionMask = uint.MaxValue;

        var result = spaceState.IntersectRay(query);

        if (result.Count > 0)
        {
            var hex = ((Node3D)result["collider"])?.GetParent<Hex>();
            if (hex != null)
            {
                return hex;
            }
        }
        return null;
    }

    private float zOffsetToTileRow;

    public override void _Ready()
    {
        zOffsetToTileRow = GlobalPosition.Z - 3.75f;
        //GD.Print($"Offset detected: {zOffsetToTileRow}");
    }

    private Tween currentMovement;

    public void MoveToFocusHexRow(int y, bool instant = false, float duration = 1)
    {
        if (currentMovement != null)
        {
            currentMovement.Stop();
            currentMovement.Dispose();
        }

        Vector3 target = new(6, 4.5f, GridManager.GetNominalPosition(0, y).Z + zOffsetToTileRow);
        if (instant)
        {
            //GD.Print($"Snapping to target: {target}");
            GlobalPosition = target;
        }
        else
        {
            //GD.Print($"Tweening to target: {target} over {duration} seconds");

            currentMovement = GetTree().CreateTween()
                                    .SetTrans(Tween.TransitionType.Cubic);
                                    //.SetEase(Tween.EaseType.);
            currentMovement.TweenProperty(this, "global_position", target, duration);
            
            currentMovement.Finished += () =>
            {
                currentMovement.Dispose();
                currentMovement = null;
            };
        }
    }

    private Vector3 startLocation = default;
    private Vector3 targetLocation = default;

    private Hex currentSwapHex = null;
    private Hex currentRotateHex = null;

    private void PlaySound(string key)
    {
        AudioStreamPlayer.Stream = SoundEffects[key];
        AudioStreamPlayer.Play();
    }
    private const string SOUND_SWAP = "swap";
    private const string SOUND_CLICK = "click";
    private const string SOUND_BUMP = "bump";

    public override void _Input(InputEvent @event)
    {
        if (IsInputLocked)
            return;

        if (@event is InputEventMouseButton mouseEvent)
        {
            var hex = GetHexOnCursor();
            if (hex?.State.CanInteract() ?? false)
            {
                if (mouseEvent.IsReleased() && mouseEvent.ButtonIndex == MouseButton.Left)
                {
                    if (currentRotateHex != null)
                    {
                        currentRotateHex.IsRotationMode = false;
                        currentRotateHex = null;
                    }

                    if (currentSwapHex == null)
                    {
                        PlaySound(SOUND_CLICK);
                        currentSwapHex = hex;
                        currentSwapHex.IsSwapMode = true;
                    }
                    else if (currentSwapHex == hex)
                    {
                        PlaySound(SOUND_CLICK);
                        currentSwapHex.IsSwapMode = false;
                        currentSwapHex = null;
                    }
                    else
                    {
                        PlaySound(SOUND_SWAP);

                        var stateCurrentHex = currentSwapHex.State;
                        var targetHex = hex.State;

                        currentSwapHex.UpdateState(targetHex);
                        hex.UpdateState(stateCurrentHex);

                        currentSwapHex.AnimateSwap();
                        hex.AnimateSwap();

                        currentSwapHex.IsSwapMode = false;
                        currentSwapHex = null;
                    }
                }
                else if (mouseEvent.IsPressed())
                {
                    if (mouseEvent.ButtonIndex == MouseButton.WheelUp)
                    {
                        PlaySound(SOUND_BUMP);
                        hex.UpdateState(hex.State.Rotated(-1));
                    }
                    else if (mouseEvent.ButtonIndex == MouseButton.WheelDown)
                    {
                        PlaySound(SOUND_BUMP);
                        hex.UpdateState(hex.State.Rotated(1));
                    }
                }
            }
        }
        else if (@event is InputEventKey keyEvent)
        {
            if (keyEvent.Keycode == Key.Alt)
            {
                DebugShowLabels =  keyEvent.IsPressed();
            }

            if (keyEvent.IsReleased() && currentRotateHex != null)
            {
                if (keyEvent.Keycode == Key.Q)
                {
                    PlaySound(SOUND_BUMP);
                    currentRotateHex.UpdateState(currentRotateHex.State.Rotated(1));
                }
                else if (keyEvent.Keycode == Key.E)
                {
                    PlaySound(SOUND_BUMP);
                    currentRotateHex.UpdateState(currentRotateHex.State.Rotated(-1));
                }
            }
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (IsInputLocked)
            return;

        var hex = GetHexOnCursor();
        if (hex != null && hex.State.CanInteract())
        {
            hex.Hover();
        }
        currentRotateHex = hex;

        if (currentSwapHex != null && !currentSwapHex.IsSwapMode)
        {
            currentSwapHex = null;
        }
        if (currentRotateHex != null && !currentRotateHex.IsRotationMode)
        {
            currentRotateHex = null;
        }
    }

    public void ResetLevel()
    {
        currentSwapHex = null;
        currentRotateHex = null;
    }

    public static bool DebugShowLabels { get; private set; }
    public bool IsInputLocked { get; internal set; }

}
