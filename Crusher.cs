using Raylib_cs;
using System;
using System.Numerics;

public class Crusher
{
    private Caravan caravan;
    public Vector2 offset;
    public int Hopper { get; private set; }
    public float ConversionRate { get; private set; }  // Conversions per second
    private float conversionTimer;
    public int EarthStored { get; private set; }
    public int Stone { get; private set; }
    public int boxWidth;
    public int boxHeight;

    private const int extraYOffset = 200;

    public Crusher(Caravan caravan, int hopper = 100, int boxWidth = 50, int boxHeight = 50)
    {
        this.caravan = caravan;
        this.Hopper = hopper;
        this.ConversionRate = 1; // Start at 1 conversion per second
        this.boxWidth = boxWidth;
        this.boxHeight = boxHeight;
        this.offset = new Vector2(10, 10);
    }

    // Base position anchored to the caravan (using the caravan's top)
    public Vector2 GetPosition()
    {
        float caravanTop = caravan.Y - caravan.height;
        return new Vector2(0, caravanTop) + offset;
    }

    // Effective drawing position (adds extraYOffset)
    public Vector2 GetEffectivePosition()
    {
        return GetPosition() + new Vector2(0, extraYOffset);
    }

    public void Draw()
    {
        Vector2 pos = GetEffectivePosition();
        Color boxColor = Color.Red;
        Raylib.DrawRectangle((int)pos.X, (int)pos.Y, boxWidth, boxHeight, boxColor);

        string text = $"{EarthStored}/{Hopper} Earth\n{Stone} Stone\n{ConversionRate}/s";
        Raylib.DrawText(text, (int)pos.X + 5, (int)pos.Y + 5, 10, Color.Black);
      //  Raylib.DrawText($"({(int)pos.X},{(int)pos.Y})", (int)pos.X, (int)pos.Y - 15, 10, Color.Yellow);
    }

    public bool CheckClick(Vector2 mousePosition)
    {
        Vector2 pos = GetEffectivePosition();
        return mousePosition.X >= pos.X &&
               mousePosition.X <= pos.X + boxWidth &&
               mousePosition.Y >= pos.Y &&
               mousePosition.Y <= pos.Y + boxHeight;
    }

    public void ReceiveEarth(int amount)
    {
        EarthStored = Math.Min(EarthStored + amount, Hopper);
    }

    public void Update(float dt)
    {
        if (EarthStored > 0)
        {
            conversionTimer += dt;
            float conversionPeriod = 1.0f / ConversionRate; // Time per conversion

            while (conversionTimer >= conversionPeriod && EarthStored > 0)
            {
                conversionTimer -= conversionPeriod;
                EarthStored--;
                Stone++;
            }
        }
        else
        {
            conversionTimer = 0;
        }
    }

    private float HopUp = 0.5f;
    private float HopDown = 0.5f;
    public int HopCost => (int)(10f * HopDown);
    public void UpgradeHopper()
    {
        if (Stone >= HopCost)
        {
            Stone -= HopCost;
            Hopper += (int)(20f * HopUp);
            HopUp *= 1.1f;
            HopDown *= 1.1f;
        }
    }

    public int RateCost => (int)(20 * ConversionRate); // Cost scales with current rate
    public void UpgradeRate()
    {
        if (Stone >= RateCost)
        {
            Stone -= RateCost;
            ConversionRate *= 2; // Double the conversion rate
        }
    }


}
