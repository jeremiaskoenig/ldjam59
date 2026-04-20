using Godot;
using System.Linq;

public partial class DebugLevelGenerator : Node
{
    [Export] public PackedScene Hex { get; set; }
    [Export] public GridManager GridManager { get; set; }

    [Export] public int LevelEmptyTiles { get; set; } = 5;
    [Export] public int LevelDisruptors { get; set; } = 10;


    public Hex StartHex => startHex;
    private Hex startHex;
    private Hex endHex;

    public int CurrentLevel { get; set; }

    private int baseLength = 10;
    private int baseWidth = 15;
    private float stepX = 0.43f;
    private float stepY = 0.75f;
    private float heightOffsetFactor = 0.05f;

    public void ResetLevel()
    {
        startHex = null;
        endHex = null;
        CurrentLevel = 0;
    }    

    public Hex GenerateLevel()
    {
        var fromY = GridManager.MaxY + 1;
        var toY = (endHex != null ? endHex.Coordinates.Y + 1 : 0) + baseLength;

        bool instant = endHex == null;

        for (int y = fromY; y < toY; y++)
        {
            for (int x = 0; x < baseWidth; x++)
            {
                var hex = PlaceHexAt(x, y, instant);
                hex?.UpdateState(global::Hex.HexState.Random(CurrentLevel >= LevelEmptyTiles));
            }        
        }

        if (endHex != null)
        {
            startHex = endHex;
            startHex.UpdateState(global::Hex.HexState.FromState(global::Hex.HexStateType.Start));
            
            endHex = null;
            var currentDisruptors = GridManager.GetCurrentLevelHexes().Where(h => h.State.StateType == global::Hex.HexStateType.Disruptor);
            while (endHex == null)
            {
                var endX = GD.RandRange(3, baseWidth - 4);
                var endY = GD.RandRange(startHex.Coordinates.Y + 2, startHex.Coordinates.Y + baseLength - 2);
                var newEnd = new Vector2I(endX, endY);
                if (currentDisruptors.All(d => d.Coordinates.DistanceTo(newEnd) > 2))
                {
                    endHex = GridManager.GetAt(endX, endY);
                }
            }
            endHex.UpdateState(global::Hex.HexState.FromState(global::Hex.HexStateType.End));
        }
        else
        {
            startHex = GridManager.GetAt(5, 5);
            startHex.UpdateState(global::Hex.HexState.FromState(global::Hex.HexStateType.Start));

            endHex = GridManager.GetAt(10, 5);
            endHex.UpdateState(global::Hex.HexState.FromState(global::Hex.HexStateType.End));
        }

        if (CurrentLevel >= LevelDisruptors)
        {
            var disruptionX = GD.RandRange(0, baseWidth - 1);
            var disruptionY = GD.RandRange(startHex.Coordinates.Y + 2, startHex.Coordinates.Y + baseLength - 2);

            var disruptionHex = GridManager.GetAt(disruptionX, disruptionY);
            
            bool farEnoughFromStart = disruptionHex.Coordinates.DistanceTo(startHex.Coordinates) > 2;
            bool farEnoughFromEnd = disruptionHex.Coordinates.DistanceTo(endHex.Coordinates) > 2;

            // maybe 3-5 tries?
            if (farEnoughFromEnd && farEnoughFromStart)
            {
                disruptionHex.UpdateState(global::Hex.HexState.FromState(global::Hex.HexStateType.Disruptor));
            }
        }
        
        return endHex;
    }

    private Hex PlaceHexAt(int x, int y, bool instant = false)
    {
        var hex = Hex.Instantiate<Node3D>();
        hex.Name = $"Hex_{x}_{y}";
        this.AddChild(hex);

        hex.GlobalPosition = GridManager.GetNominalPosition(x, y) + new Vector3(0, GD.Randf() * heightOffsetFactor, 0);
        hex.Owner = GetTree().EditedSceneRoot;

        if (hex is Hex hexData)
        {
            hexData.SetBasePosition(hex.GlobalPosition);
            GridManager.RegisterHex(x, y, hexData);

            if (!instant)
            {
                var randomOffset = GD.RandRange(5, 15);

                var spawnZ = 15f + randomOffset;
                var spawnY = -(spawnZ / 2.5f);

                hex.GlobalPosition += new Vector3(0, spawnY, spawnZ);
            }

            return hexData;
        }
        return null;
    }
}
