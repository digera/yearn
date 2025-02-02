if you're reading this you should probably just download the bin.zip


req: https://dotnet.microsoft.com/en-us/download/dotnet/8.0


https://github.com/digera/yearn/blob/master/bin.zip
It's unsigned, so Windows Defender might yell. I've heard that if you make defender scan it, it will eventually become trusted

currently there are two buttons.

F1 will display all the tooltips (some are broken, wip)

E will spawn a new miner.

All miners must be clicked out of idle atm (working as intended but incomplete)

I've gotten over a thousand miners spawned without many performance issues but please be careful and let me know where you find limits please
public class Crusher
{
    private Caravan caravan;
    public Vector2 offset;
    public int Hopper { get; private set; }
    public float ConversionAmount { get; private set; }  // Amount converted each second (upgradable)
    private float conversionTimer;
    public int InputResource { get; private set; }
    public int OutputResource { get; private set; }
    public StoneType InputType { get; private set; }
    public StoneType OutputType { get; private set; }
    public string InputResourceName { get; private set; }
    public string OutputResourceName { get; private set; }
    public int boxWidth;
    public int boxHeight;
    private const int extraYOffset = 200;
    private const int buttonSize = 20;
    private const int buttonMargin = 5;
    private readonly float hopperUpgradeMultiplier = 1.1f;
    private readonly float conversionUpgradeMultiplier = 1.5f;

    public Crusher(Caravan caravan, StoneType inputType, StoneType outputType, int hopper = 100, int boxWidth = 50, int boxHeight = 50)
    {
        this.caravan = caravan;
        Hopper = hopper;
        ConversionAmount = 1;
        this.boxWidth = boxWidth;
        this.boxHeight = boxHeight;
        offset = new Vector2(10, 10);
        InputType = inputType;
        OutputType = outputType;
        InputResourceName = inputType.ToString();
        OutputResourceName = outputType.ToString();
    }

    // The effective position already factors in the additional Y offset.
    public Vector2 GetEffectivePosition()
    {
        float caravanTop = caravan.Y - caravan.height;
        return new Vector2(0, caravanTop) + offset + new Vector2(0, extraYOffset);
    }

    public void Draw()
    {
        Vector2 pos = GetEffectivePosition();
        Raylib.DrawRectangle((int)pos.X, (int)pos.Y, boxWidth, boxHeight, Color.Red);
        string text = $"{InputResource}/{Hopper} {InputResourceName}\n" +
                      $"{OutputResource} {OutputResourceName}\n" +
                      $"{ConversionAmount}/s";
        Raylib.DrawText(text, (int)pos.X + 5, (int)pos.Y + 5, 10, Color.Black);

        // Draw integrated upgrade buttons underneath the crusher.
        DrawUpgradeButton(pos, 0, HopperUpgradeCost.ToString());
        DrawUpgradeButton(pos, 1, ConversionUpgradeCost.ToString());
    }

    private void DrawUpgradeButton(Vector2 pos, int index, string cost)
    {
        int x = (int)pos.X + index * (buttonSize + buttonMargin);
        int y = (int)pos.Y + boxHeight;
        Raylib.DrawRectangle(x, y, buttonSize, buttonSize, Color.Gray);
        Raylib.DrawText(cost, x + 2, y + 2, 8, Color.White);
    }

    public bool CheckClick(Vector2 mousePos)
    {
        Vector2 pos = GetEffectivePosition();
        return mousePos.X >= pos.X &&
               mousePos.X <= pos.X + boxWidth &&
               mousePos.Y >= pos.Y &&
               mousePos.Y <= pos.Y + boxHeight;
    }

    // Check for clicks on either of the two upgrade buttons.
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

    // Accept resource (e.g. Earth) up to the current hopper capacity.
    public void ReceiveResource(int amount)
    {
        InputResource = Math.Min(InputResource + amount, Hopper);
    }

    // Conversion occurs once per second, converting a fixed amount (upgradable) of resources.
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
                OutputResource += converted;
            }
        }
        else
        {
            conversionTimer = 0;
        }
    }

    // Upgrade cost scales exponentially as the hopper increases.
    public int HopperUpgradeCost => (int)(10 * Math.Pow(hopperUpgradeMultiplier, Hopper / 100.0));

    public void UpgradeHopper()
    {
        if (OutputResource >= HopperUpgradeCost)
        {
            OutputResource -= HopperUpgradeCost;
            Hopper += 20;
        }
    }

    // Upgrade cost based on current conversion amount.
    public int ConversionUpgradeCost => (int)(20 * Math.Pow(conversionUpgradeMultiplier, ConversionAmount - 1));

    public void UpgradeConversion()
    {
        if (OutputResource >= ConversionUpgradeCost)
        {
            OutputResource -= ConversionUpgradeCost;
            ConversionAmount += 1;
        }
    }

    // Retained for possible future use.
    public int RateCost => (int)(20 * ConversionAmount);
    public void UpgradeRate()
    {
        if (OutputResource >= RateCost)
        {
            OutputResource -= RateCost;
            // Reserved functionality.
        }
    }
}
