using Raylib_cs;
using System;
using System.Numerics;



public class Crusher
{
    private Caravan caravan;
    public Vector2 offset;

    public int Hopper { get; private set; }
    public float ConversionAmount { get; private set; }
    private float conversionTimer;
    public int InputResource { get; private set; }
    public int OutputResource { get; private set; }
    public StoneType InputType { get; private set; }
    public StoneType OutputType { get; private set; }
    public string InputResourceName { get; private set; }
    public string OutputResourceName { get; private set; }

    // Visuals
    public int boxWidth;
    public int boxHeight;
    public int extraYOffset;
    private const int buttonSize = 20;
    private const int buttonMargin = 5;

    // upgrade multipliers
    private readonly float hopperUpgradeMultiplier = 1.05f;
    private readonly float conversionUpgradeMultiplier = 1.05f;

    public int ID { get; set; }

    // each crusher has unique inputType and outputType, iterations from the enum in names
    public Crusher(Caravan caravan, StoneType inputType, StoneType outputType,
                   int hopper = 100, int boxWidth = 50, int boxHeight = 50, int extraYOffset = 0, int iD = 0)
    {
        this.caravan = caravan;
        Hopper = hopper;
        ConversionAmount = 1;

        this.boxWidth = boxWidth;
        this.boxHeight = boxHeight;
        offset = new Vector2(10, 10);
        InputType = inputType;
        OutputType = outputType;

        // enum in names
        InputResourceName = inputType.ToString();
        OutputResourceName = outputType.ToString();
        this.extraYOffset = extraYOffset;
        ID = iD;
    }

    // Get the base position from the caravan, then adjust it by an offset and then an extra offset lol shut up math is hard
    public Vector2 GetEffectivePosition()
    {
        float caravanTop = caravan.Y - caravan.height;
        return new Vector2(0, caravanTop) + offset + new Vector2(0, extraYOffset);
    }

    public void Draw()
    {
        Vector2 pos = GetEffectivePosition();

        // Draw Crusher
        Raylib.DrawRectangle((int)pos.X, (int)pos.Y, boxWidth, boxHeight, Color.Red);

        // Display current resource counts and conversion rate
        string text = $"{InputResource}/{Hopper} {InputResourceName}\n" +
                      $"{OutputResource} {OutputResourceName}\n" +
                      $"{ConversionAmount}/s";
        Raylib.DrawText(text, (int)pos.X + 5, (int)pos.Y + 5, 10, Color.Black);

        // Draw the two upgrade buttons:
        // Button 0: Upgrade Hopper (capacity)
        // Button 1: Upgrade Conversion (increase conversion amount per tick)
        DrawUpgradeButton(pos, 0, HopperUpgradeCost.ToString());
        DrawUpgradeButton(pos, 1, ConversionUpgradeCost.ToString());
    }

    // Utility method for drawing an upgrade button
    private void DrawUpgradeButton(Vector2 pos, int index, string cost)
    {
        int x = (int)pos.X + index * (buttonSize + buttonMargin);
        int y = (int)pos.Y + boxHeight;
        Raylib.DrawRectangle(x, y, buttonSize, buttonSize, Color.Gray);
        Raylib.DrawText(cost, x + 2, y + 2, 8, Color.White);
    }

    // TODO: move CheckClick and many other methods to a utility class -- each class has its own and they're all slightly different and use the Raylib.CheckCollisionPointRec method 
    public bool CheckClick(Vector2 mousePos)
    {
        Vector2 pos = GetEffectivePosition();
        return mousePos.X >= pos.X &&
               mousePos.X <= pos.X + boxWidth &&
               mousePos.Y >= pos.Y &&
               mousePos.Y <= pos.Y + boxHeight;
        //     Raylib.CheckCollisionPointRec(mousePos, new Rectangle(pos.X, pos.Y, boxWidth, boxHeight));
    }

    // Out parameter upgradeIndex tells which upgrade was selected (0 = Hopper, 1 = Conversion).
    public bool CheckUpgradeClick(Vector2 mousePos, out int upgradeIndex)
    {
        Vector2 pos = GetEffectivePosition();
        int buttonY = (int)pos.Y + boxHeight;
        for (int i = 0; i < 2; i++)
        {
            int buttonX = (int)pos.X + i * (buttonSize + buttonMargin);
            if (mousePos.X >= buttonX && mousePos.X <= buttonX + buttonSize &&
                mousePos.Y >= buttonY && mousePos.Y <= buttonY + buttonSize)
            {
                upgradeIndex = i;
                return true;
            }
        }
        upgradeIndex = -1;
        return false;
    }

    public void ReceiveResource(int amount)
    {
        if (InputType == (StoneType)ID)
        {
            InputResource = Math.Min(InputResource + amount, Hopper);
            //Program.stoneCounts[(int)StoneType.Stone] = Math.Min(Program.stoneCounts[(int)StoneType.Stone] + amount, Hopper);
        }
    }


    public void Update(float dt)
    {
        if (InputResource > 0)
        {
            conversionTimer += dt;
            while (conversionTimer >= 1.0f && InputResource > 0)
            {
                conversionTimer -= 1.0f;
                int converted = Math.Min(InputResource, (int)ConversionAmount);
                InputResource -= converted;
                //Program.stoneCounts[(int)StoneType.Stone] += converted;
                OutputResource += converted;

            }
        }
        else
        {
            conversionTimer = 0;
        }
    }

    //TODO: learn math
    public int HopperUpgradeCost => (int)(10 * Math.Pow(hopperUpgradeMultiplier, Hopper / 100.0));

    public void UpgradeHopper()
    {
        if (OutputResource >= HopperUpgradeCost)
        {
            OutputResource -= HopperUpgradeCost;
            Hopper += 20;
        }
    }
    // "(int)(20" is the starting cost
    public int ConversionUpgradeCost => (int)(20 * Math.Pow(conversionUpgradeMultiplier, ConversionAmount - 1));

    public void UpgradeConversion()
    {
        if (OutputResource >= ConversionUpgradeCost)
        {
            OutputResource -= ConversionUpgradeCost;
            // ConversionAmount = ConversionAmount + ConversionAmount;
            ConversionAmount += 1;
        }
    }
    public void RestoreState(GameState.CrusherSaveData data)
    {
        Hopper = data.Hopper;
        ConversionAmount = data.ConversionAmount;
        InputResource = data.InputResource;
        OutputResource = data.OutputResource;
       // extraYOffset = data.ExtraYOffset;
    }

}

