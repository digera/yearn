using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Raylib_cs;

public struct PickaxeStats
{
    public float Speed { get; set; }
    public float Size { get; set; }
    public int MiningPower { get; set; }
    public Color Color { get; set; }

    public PickaxeStats(float speed, float size, int miningPower, Color? color = null)
    {
        Speed = speed;
        Size = size;
        MiningPower = miningPower;
        Color = color ?? Color.Black;
    }
}
public struct CanisterStats
{
    public float Speed { get; set; }
    public float Capacity { get; set; }
    public Color Color { get; set; }

    public CanisterStats(float speed, float capacity, Color? color = null)
    {
        Speed = speed;
        Capacity = capacity;
        Color = color ?? Color.Black;
    }
}