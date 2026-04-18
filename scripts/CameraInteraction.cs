using System.Linq;
using Godot;

public partial class CameraInteraction : Camera3D
{
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

    private Hex currentSwapHex = null;
    private Hex currentRotateHex = null;

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseEvent)
        {
            var hex = GetHexOnCursor();
            if (mouseEvent.IsReleased())
            {
                if (mouseEvent.ButtonIndex == MouseButton.Left && hex != null)
                {
                    if (currentRotateHex != null)
                    {
                        currentRotateHex.IsRotationMode = false;
                        currentRotateHex = null;
                    }

                    if (currentSwapHex == null)
                    {
                        currentSwapHex = hex;
                        currentSwapHex.IsSwapMode = true;
                    }
                    else
                    {
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
                else if (mouseEvent.ButtonIndex == MouseButton.Right && hex != null)
                {
                    if (currentSwapHex != null)
                    {
                        currentSwapHex.IsSwapMode = false;
                        currentSwapHex = null;
                    }

                    if (currentRotateHex == null)
                    {
                        currentRotateHex = hex;
                        currentRotateHex.IsRotationMode = true;
                    }
                    else
                    {
                        currentRotateHex.IsRotationMode = false;

                        if (currentRotateHex == hex)
                        {
                            currentRotateHex = null;
                        }
                        else
                        {
                            currentRotateHex = hex;
                            currentRotateHex.IsRotationMode = true;
                        }
                    }
                }
            }
            else if (mouseEvent.IsPressed() && currentRotateHex != null)
            {
                if (mouseEvent.ButtonIndex == MouseButton.WheelUp)
                {
                    currentRotateHex.UpdateState(currentRotateHex.State.Rotated(-1));
                }
                else if (mouseEvent.ButtonIndex == MouseButton.WheelDown)
                {
                    currentRotateHex.UpdateState(currentRotateHex.State.Rotated(1));
                }
            }
        }
        else if (@event is InputEventKey keyEvent)
        {
            if (keyEvent.IsReleased() && currentRotateHex != null)
            {
                if (keyEvent.Keycode == Key.Q)
                {
                    currentRotateHex.UpdateState(currentRotateHex.State.Rotated(1));
                }
                else if (keyEvent.Keycode == Key.E)
                {
                    currentRotateHex.UpdateState(currentRotateHex.State.Rotated(-1));
                }
            }
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        GetHexOnCursor()?.Hover();
    }

}
