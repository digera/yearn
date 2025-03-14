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
    public Miner? AssignedMiner { get; set; }

    // Visuals
    public int boxWidth;
    public int boxHeight;
    public int extraYOffset;
    private const int buttonSize = 20;
    private const int buttonMargin = 5;
    private const int tierButtonSize = 25; // Slightly larger button for tier creation

    // upgrade multipliers
    private readonly float hopperUpgradeMultiplier = 1.25f;
    private readonly float conversionUpgradeMultiplier = 1.2f;

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

    // Fields for tracking mouse hover over buttons for tooltips
    private bool isHoveringHopperButton = false;
    private bool isHoveringConversionButton = false;
    private bool isHoveringNextTierButton = false;

    public void Draw()
    {
        Vector2 pos = GetEffectivePosition();
        Vector2 mousePos = Program.GetMouseWorld();

        // Get colors for input and output stone types
        Color inputColor = StoneColorGenerator.GetColor(InputType);
        Color outputColor = StoneColorGenerator.GetColor(OutputType);
        
        // Create a gradient effect from input to output color
        Color crusherColor = new Color(
            (byte)((inputColor.R + outputColor.R) / 2),
            (byte)((inputColor.G + outputColor.G) / 2),
            (byte)((inputColor.B + outputColor.B) / 2),
            (byte)255
        );
        
        // Darker border color
        Color borderColor = new Color(
            (byte)Math.Max(0, crusherColor.R - 40),
            (byte)Math.Max(0, crusherColor.G - 40),
            (byte)Math.Max(0, crusherColor.B - 40),
            (byte)255
        );

        // Draw Crusher main body with rounded corners
        Raylib.DrawRectangle((int)pos.X, (int)pos.Y, boxWidth, boxHeight, crusherColor);
        Raylib.DrawRectangleLines((int)pos.X, (int)pos.Y, boxWidth, boxHeight, borderColor);
        
        // Draw crusher mechanism (gears, etc.)
        int gearSize = 15;
        int gearX = (int)pos.X + boxWidth / 2 - gearSize / 2;
        int gearY = (int)pos.Y + boxHeight / 2 - gearSize / 2;
        
        // Draw main gear
        Raylib.DrawCircle(gearX + gearSize/2, gearY + gearSize/2, gearSize/2, borderColor);
        Raylib.DrawCircle(gearX + gearSize/2, gearY + gearSize/2, gearSize/3, crusherColor);
        
        // Draw gear teeth
        for (int i = 0; i < 8; i++)
        {
            float angle = i * MathF.PI / 4;
            float x1 = gearX + gearSize/2 + (gearSize/2 - 2) * MathF.Cos(angle);
            float y1 = gearY + gearSize/2 + (gearSize/2 - 2) * MathF.Sin(angle);
            float x2 = gearX + gearSize/2 + (gearSize/2 + 2) * MathF.Cos(angle);
            float y2 = gearY + gearSize/2 + (gearSize/2 + 2) * MathF.Sin(angle);
            Raylib.DrawLine((int)x1, (int)y1, (int)x2, (int)y2, borderColor);
        }
        
        // Draw input hopper (left side)
        Raylib.DrawTriangle(
            new Vector2(pos.X + 5, pos.Y + 5),
            new Vector2(pos.X + 20, pos.Y + 15),
            new Vector2(pos.X + 5, pos.Y + 25),
            inputColor
        );
        
        // Draw output chute (right side)
        Raylib.DrawTriangle(
            new Vector2(pos.X + boxWidth - 5, pos.Y + 5),
            new Vector2(pos.X + boxWidth - 20, pos.Y + 15),
            new Vector2(pos.X + boxWidth - 5, pos.Y + 25),
            outputColor
        );

        // Choose text color based on crusher color brightness for better readability
        Color textColor = (crusherColor.R + crusherColor.G + crusherColor.B > 380) ? 
            new Color((byte)20, (byte)20, (byte)20, (byte)255) : 
            new Color((byte)230, (byte)230, (byte)230, (byte)255);
            
        // Create a small background for the text to ensure readability
        Raylib.DrawRectangle(
            (int)pos.X + 5,
            (int)pos.Y + boxHeight - 30,
            boxWidth - 10,
            25,
            new Color((byte)0, (byte)0, (byte)0, (byte)150)
        );
        
        // Display current resource counts and conversion rate
        string text = $"{InputResource}/{Hopper} {InputResourceName}\n" +
                      $"{Program.stoneCounts[(int)OutputType]} {OutputResourceName}\n" +
                      $"{ConversionAmount}/s";
        Raylib.DrawText(text, (int)pos.X + 8, (int)pos.Y + boxHeight - 28, 10, textColor);

        // Draw the two upgrade buttons with improved visuals and symbols
        isHoveringHopperButton = DrawUpgradeButton(pos, 0, HopperUpgradeCost.ToString(), "⬆H", inputColor, mousePos);
        isHoveringConversionButton = DrawUpgradeButton(pos, 1, ConversionUpgradeCost.ToString(), "⬆S", outputColor, mousePos);

        // Draw the create next tier button if this crusher can create a next tier
        // AND if there isn't already a crusher for the next tier
        if (CanCreateNextTier && !CrusherExistsForType((StoneType)((int)OutputType + 1)))
        {
            isHoveringNextTierButton = DrawNextTierButton(pos, mousePos);
        }
        else
        {
            isHoveringNextTierButton = false;
        }
        
        // Draw tooltips if hovering over buttons
        DrawTooltips(pos, mousePos);
    }
    
    // Draw tooltips for buttons when hovered
    private void DrawTooltips(Vector2 pos, Vector2 mousePos)
    {
        if (isHoveringHopperButton || isHoveringConversionButton || isHoveringNextTierButton)
        {
            // Draw the tooltip directly on the right side of the caravan
            // Get caravan dimensions
            float caravanWidth = caravan.width;
            float caravanY = caravan.Y;
            
            // Position the tooltip in the lower right area of the caravan
            // Based on the yellow square in the screenshot
            float tooltipX = caravanWidth * 0.7f;
            float tooltipY = caravanY + caravan.height * 0.3f; // Move it down to the lower part
            
            // Make the tooltip larger
            int tooltipWidth = 300;
            int tooltipHeight = 150;
            
            // Draw tooltip background with semi-transparent dark background
            Raylib.DrawRectangle(
                (int)tooltipX, 
                (int)tooltipY, 
                tooltipWidth, 
                tooltipHeight, 
                new Color((byte)0, (byte)0, (byte)0, (byte)180)
            );
            
            // Draw tooltip border
            Raylib.DrawRectangleLines(
                (int)tooltipX, 
                (int)tooltipY, 
                tooltipWidth, 
                tooltipHeight, 
                Color.White
            );
            
            // Prepare tooltip text based on which button is being hovered
            string tooltipTitle = "";
            string tooltipDescription = "";
            string tooltipCost = "";
            string tooltipCurrent = "";
            Color tooltipColor = Color.White;
            
            if (isHoveringHopperButton)
            {
                tooltipTitle = "UPGRADE HOPPER";
                tooltipDescription = "Increases storage capacity for input resources";
                tooltipCost = $"Cost: {HopperUpgradeCost} {OutputResourceName}";
                tooltipCurrent = $"Current capacity: {Hopper}";
                tooltipColor = StoneColorGenerator.GetColor(InputType);
            }
            else if (isHoveringConversionButton)
            {
                tooltipTitle = "UPGRADE CONVERSION SPEED";
                tooltipDescription = "Increases the rate at which resources are converted";
                tooltipCost = $"Cost: {ConversionUpgradeCost} {OutputResourceName}";
                tooltipCurrent = $"Current rate: {ConversionAmount}/s";
                tooltipColor = StoneColorGenerator.GetColor(OutputType);
            }
            else if (isHoveringNextTierButton)
            {
                StoneType nextTierType = (StoneType)((int)OutputType + 1);
                tooltipTitle = "CREATE NEXT TIER CRUSHER";
                tooltipDescription = $"Creates a new crusher that converts {OutputType} to {nextTierType}";
                tooltipCost = $"Cost: {tierUpgradeCost} {OutputResourceName}";
                tooltipCurrent = "";
                tooltipColor = StoneColorGenerator.GetColor(nextTierType);
            }
            
            // Draw colored header bar
            Raylib.DrawRectangle(
                (int)tooltipX, 
                (int)tooltipY, 
                tooltipWidth, 
                30, 
                tooltipColor
            );
            
            // Choose text color based on header color brightness
            Color headerTextColor = (tooltipColor.R + tooltipColor.G + tooltipColor.B > 380) ? 
                new Color((byte)20, (byte)20, (byte)20, (byte)255) : 
                new Color((byte)230, (byte)230, (byte)230, (byte)255);
            
            // Draw tooltip text with larger font sizes
            Raylib.DrawText(tooltipTitle, (int)tooltipX + 10, (int)tooltipY + 8, 20, headerTextColor);
            Raylib.DrawText(tooltipDescription, (int)tooltipX + 10, (int)tooltipY + 40, 16, Color.White);
            Raylib.DrawText(tooltipCost, (int)tooltipX + 10, (int)tooltipY + 70, 18, Color.White);
            
            if (!string.IsNullOrEmpty(tooltipCurrent))
            {
                Raylib.DrawText(tooltipCurrent, (int)tooltipX + 10, (int)tooltipY + 100, 18, Color.White);
            }
            
            // Draw a small indicator line from the button to the tooltip
            if (isHoveringHopperButton || isHoveringConversionButton)
            {
                int buttonIndex = isHoveringHopperButton ? 0 : 1;
                int buttonX = (int)pos.X + buttonIndex * (buttonSize + buttonMargin) + buttonSize/2;
                int buttonY = (int)pos.Y + boxHeight + buttonSize/2;
                
                Raylib.DrawLine(
                    buttonX,
                    buttonY,
                    (int)tooltipX,
                    (int)tooltipY + tooltipHeight/2,
                    Color.White
                );
            }
            else if (isHoveringNextTierButton)
            {
                int buttonX = (int)pos.X + 2 * (buttonSize + buttonMargin) + tierButtonSize/2;
                int buttonY = (int)pos.Y + boxHeight + tierButtonSize/2;
                
                Raylib.DrawLine(
                    buttonX,
                    buttonY,
                    (int)tooltipX,
                    (int)tooltipY + tooltipHeight/2,
                    Color.White
                );
            }
        }
    }

    // Utility method for drawing an upgrade button with improved visuals and symbols
    private bool DrawUpgradeButton(Vector2 pos, int index, string cost, string symbol, Color baseColor, Vector2 mousePos)
    {
        int x = (int)pos.X + index * (buttonSize + buttonMargin);
        int y = (int)pos.Y + boxHeight;
        
        // Create a lighter version of the base color for the button
        Color buttonColor = new Color(
            (byte)Math.Min(255, baseColor.R + 40),
            (byte)Math.Min(255, baseColor.G + 40),
            (byte)Math.Min(255, baseColor.B + 40),
            (byte)255
        );
        
        // Create a darker version for the border
        Color borderColor = new Color(
            (byte)Math.Max(0, baseColor.R - 40),
            (byte)Math.Max(0, baseColor.G - 40),
            (byte)Math.Max(0, baseColor.B - 40),
            (byte)255
        );
        
        // Check if mouse is hovering over this button
        bool isHovering = mousePos.X >= x && mousePos.X <= x + buttonSize &&
                          mousePos.Y >= y && mousePos.Y <= y + buttonSize;
        
        // If hovering, make the button brighter
        if (isHovering)
        {
            buttonColor = new Color(
                (byte)Math.Min(255, buttonColor.R + 30),
                (byte)Math.Min(255, buttonColor.G + 30),
                (byte)Math.Min(255, buttonColor.B + 30),
                (byte)255
            );
        }
        
        // Draw button with border
        Raylib.DrawRectangle(x, y, buttonSize, buttonSize, buttonColor);
        Raylib.DrawRectangleLines(x, y, buttonSize, buttonSize, borderColor);
        
        // Add 3D effect with a highlight on top/left and shadow on bottom/right
        Raylib.DrawLine(x + 1, y + 1, x + buttonSize - 2, y + 1, Color.White);
        Raylib.DrawLine(x + 1, y + 1, x + 1, y + buttonSize - 2, Color.White);
        Raylib.DrawLine(x + buttonSize - 1, y + 1, x + buttonSize - 1, y + buttonSize - 1, borderColor);
        Raylib.DrawLine(x + 1, y + buttonSize - 1, x + buttonSize - 1, y + buttonSize - 1, borderColor);
        
        // Choose text color based on button color brightness
        Color textColor = (buttonColor.R + buttonColor.G + buttonColor.B > 380) ? 
            new Color((byte)20, (byte)20, (byte)20, (byte)255) : 
            new Color((byte)230, (byte)230, (byte)230, (byte)255);
            
        // Draw the symbol in the center of the button with larger font
        Raylib.DrawText(symbol, x + buttonSize/2 - 8, y + buttonSize/2 - 6, 12, textColor);
        
        return isHovering;
    }

    // Draw the button for creating the next tier crusher with improved visuals and symbols
    private bool DrawNextTierButton(Vector2 pos, Vector2 mousePos)
    {
        // Position the button at the bottom, after the upgrade buttons
        int x = (int)pos.X + 2 * (buttonSize + buttonMargin);
        int y = (int)pos.Y + boxHeight;
        
        // Get the color for the next tier stone type
        StoneType nextTierType = (StoneType)((int)OutputType + 1);
        Color nextTierColor = StoneColorGenerator.GetColor(nextTierType);
        
        // Create a lighter version of the color for the button
        Color buttonColor = new Color(
            (byte)Math.Min(255, nextTierColor.R + 40),
            (byte)Math.Min(255, nextTierColor.G + 40),
            (byte)Math.Min(255, nextTierColor.B + 40),
            (byte)255
        );
        
        // Create a darker version for the border
        Color borderColor = new Color(
            (byte)Math.Max(0, nextTierColor.R - 40),
            (byte)Math.Max(0, nextTierColor.G - 40),
            (byte)Math.Max(0, nextTierColor.B - 40),
            (byte)255
        );
        
        // Check if mouse is hovering over this button
        bool isHovering = mousePos.X >= x && mousePos.X <= x + tierButtonSize &&
                          mousePos.Y >= y && mousePos.Y <= y + tierButtonSize;
        
        // If hovering, make the button brighter
        if (isHovering)
        {
            buttonColor = new Color(
                (byte)Math.Min(255, buttonColor.R + 30),
                (byte)Math.Min(255, buttonColor.G + 30),
                (byte)Math.Min(255, buttonColor.B + 30),
                (byte)255
            );
        }
        
        // Draw button with border
        Raylib.DrawRectangle(x, y, tierButtonSize, tierButtonSize, buttonColor);
        Raylib.DrawRectangleLines(x, y, tierButtonSize, tierButtonSize, borderColor);
        
        // Add 3D effect with a highlight on top/left and shadow on bottom/right
        Raylib.DrawLine(x + 1, y + 1, x + tierButtonSize - 2, y + 1, Color.White);
        Raylib.DrawLine(x + 1, y + 1, x + 1, y + tierButtonSize - 2, Color.White);
        Raylib.DrawLine(x + tierButtonSize - 1, y + 1, x + tierButtonSize - 1, y + tierButtonSize - 1, borderColor);
        Raylib.DrawLine(x + 1, y + tierButtonSize - 1, x + tierButtonSize - 1, y + tierButtonSize - 1, borderColor);
        
        // Choose text color based on button color brightness
        Color textColor = (buttonColor.R + buttonColor.G + buttonColor.B > 380) ? 
            new Color((byte)20, (byte)20, (byte)20, (byte)255) : 
            new Color((byte)230, (byte)230, (byte)230, (byte)255);
            
        // Draw a clear "+" symbol in the center of the button
        Raylib.DrawText("+", x + tierButtonSize/2 - 6, y + tierButtonSize/2 - 8, 16, textColor);
        
        return isHovering;
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
    public int HopperUpgradeCost => (int)(10 * Math.Pow(hopperUpgradeMultiplier, Hopper / 200.0));

    public void UpgradeHopper()
    {
        if (Program.stoneCounts[(int)OutputType] >= HopperUpgradeCost)
        {
            Program.stoneCounts[(int)OutputType] -= HopperUpgradeCost;
            // Increase hopper capacity by more as the game progresses
            int increase = 20 + (Hopper / 100);  // Base increase of 20, plus 1 for every 100 current capacity
            Hopper += increase;
        }
    }
    // "(int)(20" is the starting cost
    public int ConversionUpgradeCost => (int)(20 * Math.Pow(conversionUpgradeMultiplier, ConversionAmount / 1.5));

    public void UpgradeConversion()
    {
        if (Program.stoneCounts[(int)OutputType] >= ConversionUpgradeCost)
        {
            Program.stoneCounts[(int)OutputType] -= ConversionUpgradeCost;
            // Increase conversion amount by more as it gets higher
            float increase = 1.0f;
            if (ConversionAmount >= 10) increase = 2.0f;
            if (ConversionAmount >= 20) increase = 3.0f;
            if (ConversionAmount >= 50) increase = 5.0f;
            ConversionAmount += increase;
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
        StoneType nextTierType = (StoneType)((int)OutputType + 1);
        if (CrusherExistsForType(nextTierType))
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
            nextTierType,
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
