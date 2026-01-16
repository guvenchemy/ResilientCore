/*using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Modules.Utils;

namespace YilmazHostingCore;

public class ArenaConfig
{
    public string MapName { get; set; } = "";
    public List<ArenaDefinition> Arenas { get; set; } = new();
}

public class ArenaDefinition
{
    public int ArenaId { get; set; }
    public SpawnPoint? T_Spawn { get; set; }
    public SpawnPoint? CT_Spawn { get; set; }
}

public class SpawnPoint
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public float Angle { get; set; }
    //CSSharp transformer from vector
    public static SpawnPoint FromPlayer(Vector pos, QAngle ang)
    {
        return new SpawnPoint()
        {
            X = pos.X,
            Y = pos.Y,
            Z = pos.Z,
            Angle = ang.Y
        };
    }

}*/