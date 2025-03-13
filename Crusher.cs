using Raylib_cs;
using System;
using System.Numerics;

/// <summary>
/// Crusher class, handles the conversion of one resource to another
/// </summary>
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
    public Miner AssignedMiner { get; private set; }

    // Visuals
    public int boxWidth;
    public int boxHeight;
    public int extraYOffset;
    private const int buttonSize = 20;
    private const int buttonMargin = 5;
    private const int tierButtonSize = 25; // Slightly larger button for tier creation

    // upgrade multipliers
    private readonly float hopperUpgradeMultiplier = 1.05f;
    private readonly float conversionUpgradeMultiplier = 1.05f;

    // Tier upgrade cost - cost to create the next tier crusher
    private readonly int tierUpgradeCost = 50;
    public bool CanCreateNextTier => (int)OutputType < Enum.GetValues(typeof(StoneType)).Length - 1;

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

    // Get the base position from the caravan
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
                      $"{Program.stoneCounts[(int)OutputType]} {OutputResourceName}\n" +
                      $"{ConversionAmount}/s";
        Raylib.DrawText(text, (int)pos.X + 5, (int)pos.Y + 5, 10, Color.Black);

        // Draw the two upgrade buttons:
        // Button 0: Upgrade Hopper (capacity)
        // Button 1: Upgrade Conversion (increase conversion amount per tick)
        DrawUpgradeButton(pos, 0, HopperUpgradeCost.ToString());
        DrawUpgradeButton(pos, 1, ConversionUpgradeCost.ToString());

        // Draw the create next tier button if this crusher can create a next tier
        // AND if there isn't already a crusher for the next tier
        if (CanCreateNextTier && !CrusherExistsForType((StoneType)((int)OutputType + 1)))
        {
            DrawNextTierButton(pos);
        }
    }

    // Utility method for drawing an upgrade button should be in button.cs I think
    private void DrawUpgradeButton(Vector2 pos, int index, string cost)
    {
        int x = (int)pos.X + index * (buttonSize + buttonMargin);
        int y = (int)pos.Y + boxHeight;
        Raylib.DrawRectangle(x, y, buttonSize, buttonSize, Color.Gray);
        Raylib.DrawText(cost, x + 2, y + 2, 8, Color.White);
    }

    // Draw the button for creating the next tier crusher
    private void DrawNextTierButton(Vector2 pos)
    {
        // Position the button at the bottom, after the upgrade buttons
        int x = (int)pos.X + 2 * (buttonSize + buttonMargin);
        int y = (int)pos.Y + boxHeight;
        Raylib.DrawRectangle(x, y, tierButtonSize, tierButtonSize, Color.Green);
        Raylib.DrawText(tierUpgradeCost.ToString(), x + 2, y + 2, 8, Color.White);
        Raylib.DrawText("+", x + 10, y + 8, 12, Color.White);
    }

    // TODO: move CheckClick and many other methods to a utility class -- each class has its own and they're all slightly different and use the Raylib.CheckCollisionPointRec method 
    public bool CheckClick(Vector2 mousePos)
    {
        Vector2 pos = GetEffectivePosition();
        return Raylib.CheckCollisionPointRec(mousePos, new Rectangle(pos.X, pos.Y, boxWidth, boxHeight));
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

    // Check if the next tier button was clicked
    public bool CheckNextTierClick(Vector2 mousePos)
    {
        if (!CanCreateNextTier)
            return false;
            
        Vector2 pos = GetEffectivePosition();
        // Match the position calculation with the DrawNextTierButton method
        int x = (int)pos.X + 2 * (buttonSize + buttonMargin);
        int y = (int)pos.Y + boxHeight;
        return Raylib.CheckCollisionPointRec(mousePos, new Rectangle(x, y, tierButtonSize, tierButtonSize));
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
        if (InputResource > ConversionAmount)
        {
            conversionTimer += dt;
            while (conversionTimer >= 1.0f && InputResource >= 2) 
            {
                conversionTimer -= 1.0f;
                int converted = Math.Min(InputResource - (InputResource % 2), (int)ConversionAmount * 2); 
                InputResource -= converted;
                Program.stoneCounts[(int)OutputType] += converted / 2;
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
        if (Program.stoneCounts[(int)OutputType] >= HopperUpgradeCost)
        {
            Program.stoneCounts[(int)OutputType] -= HopperUpgradeCost;
            Hopper += 20;
        }
    }
    // "(int)(20" is the starting cost
    public int ConversionUpgradeCost => (int)(20 * Math.Pow(conversionUpgradeMultiplier, ConversionAmount - 1));

    public void UpgradeConversion()
    {
        if (Program.stoneCounts[(int)OutputType] >= ConversionUpgradeCost)
        {
            Program.stoneCounts[(int)OutputType] -= ConversionUpgradeCost;
            // ConversionAmount = ConversionAmount + ConversionAmount;
            ConversionAmount += 1;
        }
    }

    // Check if a crusher already exists for a specific stone type
    private bool CrusherExistsForType(StoneType type)
    {
        foreach (var crusher in Program.crushers)
        {
            if (crusher.OutputType == type)
                return true;
        }
        return false;
    }

    // Method to create the next tier crusher
    public bool CreateNextTierCrusher()
    {
        if (!CanCreateNextTier || Program.stoneCounts[(int)OutputType] < tierUpgradeCost)
            return false;
            
        // Check if a crusher for the next tier already exists
        StoneType nextOutputType = (StoneType)((int)OutputType + 1);
        if (CrusherExistsForType(nextOutputType))
            return false;
            
        // Consume resources
        Program.stoneCounts[(int)OutputType] -= tierUpgradeCost;
        
        // Calculate the next tier crusher's input and output types
        StoneType nextInputType = OutputType;
        
        // Create the new crusher
        int newCrusherID = Program.crushers.Count;
        Program.crushers.Add(new Crusher(
            caravan,
            nextInputType,
            nextOutputType,
            100,
            50,
            50,
            (newCrusherID * 70) + 200,
            newCrusherID
        ));
        
        return true;
    }

    public void RestoreState(GameState.CrusherSaveData data)
    {
        Hopper = data.Hopper;
        ConversionAmount = data.ConversionAmount;
        InputResource = data.InputResource;
        // extraYOffset = data.ExtraYOffset;
    }

    public void AssignMiner(Miner miner)
    {
        AssignedMiner = miner;
        miner.CurrentState = MinerState.Working;
        miner.TargetCrushers.Clear();
        miner.TargetCrushers.Add(this);
    }
}
