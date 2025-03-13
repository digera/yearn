using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Numerics;

/// <summary>
/// Forge class, handles the creation of pickaxes from stone resources
/// </summary>
public class Forge
{
    private Caravan caravan;
    private Vector2 offset;
    public int Width { get; private set; }
    public int Height { get; private set; }
    private Color color = new Color(165, 42, 42, 255); // Richer brown color for the forge
    private List<Pickaxe> activePickaxes = new List<Pickaxe>();
    private float forgeTimer = 0;
    private const float FORGE_COOLDOWN = 5.0f; // Time between automatic pickaxe creation
    private bool canCreatePickaxe = true;
    private bool isHovered = false;
    
    public bool CanCreatePickaxe => canCreatePickaxe;
    
    public Forge(Caravan caravan, int width = 70, int height = 70)
    {
        this.caravan = caravan;
        this.Width = width;
        this.Height = height;
        // Position the forge on the right side of the caravan, but more centered
        offset = new Vector2(Program.refWidth - width - 50, 50);
    }
    
    public Vector2 GetEffectivePosition()
    {
        float caravanTop = caravan.Y - caravan.height;
        return new Vector2(0, caravanTop) + offset;
    }
    
    public void Update(float dt)
    {
        // Check if mouse is hovering over the forge
        Vector2 mousePos = Program.GetMouseWorld();
        Vector2 forgePos = GetEffectivePosition();
        isHovered = Raylib.CheckCollisionPointRec(mousePos, new Rectangle(forgePos.X, forgePos.Y, Width, Height));
        
        // Update forge cooldown timer
        if (!canCreatePickaxe)
        {
            forgeTimer += dt;
            if (forgeTimer >= FORGE_COOLDOWN)
            {
                forgeTimer = 0;
                canCreatePickaxe = true;
            }
        }
        
        // Update active pickaxes
        for (int i = activePickaxes.Count - 1; i >= 0; i--)
        {
            var pickaxe = activePickaxes[i];
            pickaxe.Update(dt);
            
            // Remove pickaxes that have been collected or expired
            if (pickaxe.IsCollected || pickaxe.IsExpired)
            {
                activePickaxes.RemoveAt(i);
            }
        }
    }
    
    // This method is called from EarthPile when a stone is dropped on the forge
    public void CreatePickaxeFromPile(StoneType stoneType)
    {
        if (canCreatePickaxe && Program.stoneCounts[(int)stoneType] > 0)
        {
            CreatePickaxe(stoneType);
            // Consume the resource
            Program.stoneCounts[(int)stoneType]--;
            canCreatePickaxe = false;
            forgeTimer = 0;
        }
    }
    
    private void CreatePickaxe(StoneType stoneType)
    {
        // Create a pickaxe that jumps ahead of the caravan
        Vector2 spawnPos = new Vector2(Program.refWidth / 2, caravan.Y - 300 - Raylib.GetRandomValue(0, 200));
        
        // Calculate pickaxe stats based on stone type
        PickaxeStats stats = CalculatePickaxeStats(stoneType);
        
        // Create and add the pickaxe
        Pickaxe newPickaxe = new Pickaxe(spawnPos, stats, stoneType);
        activePickaxes.Add(newPickaxe);
    }
    
    private PickaxeStats CalculatePickaxeStats(StoneType stoneType)
    {
        // Base stats
        float speed = 0.75f;
        float size = 4f;
        int miningPower = 2;
        
        // Improve stats based on stone type
        int stoneLevel = (int)stoneType;
        speed = Math.Max(0.1f, speed - (stoneLevel * 0.05f)); // Lower is faster
        size = size + (stoneLevel * 0.5f);
        miningPower = miningPower + (stoneLevel * 2);
        
        // Determine color based on stone type
        Color color = stoneType switch
        {
            StoneType.Earth => new Color(139, 69, 19, 255), // Brown
            StoneType.Stone => new Color(169, 169, 169, 255), // Dark Gray
            StoneType.Hardstone => new Color(105, 105, 105, 255), // Dim Gray
            StoneType.Rock => new Color(128, 128, 128, 255), // Gray
            StoneType.Marble => new Color(245, 245, 245, 255), // White Smoke
            StoneType.Quartz => new Color(240, 255, 255, 255), // Azure
            StoneType.Limestone => new Color(250, 240, 230, 255), // Linen
            StoneType.Granite => new Color(105, 105, 105, 255), // Dim Gray
            StoneType.Sandstone => new Color(244, 164, 96, 255), // Sandy Brown
            StoneType.Quartzite => new Color(176, 224, 230, 255), // Powder Blue
            StoneType.Obsidian => new Color(0, 0, 0, 255), // Black
            StoneType.Diamondstone => new Color(185, 242, 255, 255), // Light Blue
            StoneType.Amethyst => new Color(153, 102, 204, 255), // Medium Purple
            StoneType.Sapphire => new Color(15, 82, 186, 255), // Blue
            StoneType.Ruby => new Color(224, 17, 95, 255), // Ruby Red
            StoneType.Emerald => new Color(80, 200, 120, 255), // Emerald Green
            StoneType.Citrine => new Color(228, 208, 10, 255), // Yellow
            StoneType.Onyx => new Color(53, 56, 57, 255), // Dark Gray
            StoneType.Diamond => new Color(185, 242, 255, 255), // Light Blue
            _ => new Color(255, 255, 255, 255) // White (default)
        };
        
        return new PickaxeStats(speed, size, miningPower, color);
    }
    
    public void Draw()
    {
        Vector2 pos = GetEffectivePosition();
        
        // Draw the forge with a more interesting design
        Raylib.DrawRectangle((int)pos.X, (int)pos.Y, Width, Height, color);
        
        // Add some details to the forge
        Color darkBrown = new Color(101, 67, 33, 255);
        Raylib.DrawRectangle((int)pos.X + 5, (int)pos.Y + 5, Width - 10, Height - 10, darkBrown);
        
        // Draw a glowing effect if the forge is active
        if (canCreatePickaxe)
        {
            Color orangeGlow = new Color(255, 140, 0, 150);
            Raylib.DrawRectangle((int)pos.X + 15, (int)pos.Y + 15, Width - 30, Height - 30, orangeGlow);
        }
        
        // Draw forge label with a better font size and position
        Raylib.DrawText("Forge", (int)pos.X + 10, (int)pos.Y + 10, 18, Color.White);
        
        // Draw a highlight when hovered
        if (isHovered)
        {
            Raylib.DrawRectangleLines((int)pos.X, (int)pos.Y, Width, Height, Color.Yellow);
            
            // Show tooltip when hovering
            string tooltip = "Drag resources here\nto create pickaxes";
            Vector2 textSize = Raylib.MeasureTextEx(Raylib.GetFontDefault(), tooltip, 15, 1);
            Raylib.DrawRectangle(
                (int)(pos.X + Width + 5),
                (int)(pos.Y),
                (int)textSize.X + 10,
                (int)textSize.Y + 10,
                new Color(50, 50, 50, 200)
            );
            Raylib.DrawText(
                tooltip,
                (int)(pos.X + Width + 10),
                (int)(pos.Y + 5),
                15,
                Color.White
            );
        }
        
        // Draw cooldown indicator
        if (!canCreatePickaxe)
        {
            float cooldownPercentage = forgeTimer / FORGE_COOLDOWN;
            int indicatorHeight = (int)(Height * cooldownPercentage);
            Raylib.DrawRectangle(
                (int)pos.X, 
                (int)(pos.Y + Height - indicatorHeight), 
                5, 
                indicatorHeight, 
                Color.Red);
        }
        
        // Draw active pickaxes
        foreach (var pickaxe in activePickaxes)
        {
            pickaxe.Draw();
        }
    }
    
    public bool CheckClick(Vector2 mousePos)
    {
        Vector2 pos = GetEffectivePosition();
        return Raylib.CheckCollisionPointRec(mousePos, new Rectangle(pos.X, pos.Y, Width, Height));
    }
}

/// <summary>
/// Pickaxe class that can be collected by miners
/// </summary>
public class Pickaxe
{
    public Vector2 Position;
    public PickaxeStats Stats;
    public StoneType StoneType;
    public bool IsCollected { get; private set; } = false;
    public bool IsExpired { get; private set; } = false;
    
    private float lifetime = 30.0f; // Pickaxe disappears after this many seconds
    private float bounceTimer = 0;
    private const float BOUNCE_PERIOD = 1.0f;
    private float bounceHeight = 10.0f;
    private Vector2 basePosition;
    
    public Pickaxe(Vector2 position, PickaxeStats stats, StoneType stoneType)
    {
        Position = position;
        basePosition = position;
        Stats = stats;
        StoneType = stoneType;
    }
    
    public void Update(float dt)
    {
        // Update lifetime
        lifetime -= dt;
        if (lifetime <= 0)
        {
            IsExpired = true;
            return;
        }
        
        // Make the pickaxe bounce
        bounceTimer += dt;
        if (bounceTimer > BOUNCE_PERIOD)
        {
            bounceTimer -= BOUNCE_PERIOD;
        }
        
        float bounceOffset = (float)Math.Sin(bounceTimer / BOUNCE_PERIOD * Math.PI * 2) * bounceHeight;
        Position = basePosition + new Vector2(0, bounceOffset);
        
        // Check if any miner is close enough to collect this pickaxe
        foreach (var miner in Program.miners)
        {
            if (Vector2.Distance(miner.Position, Position) < miner.Radius + 10)
            {
                // Only collect if this pickaxe is better than the miner's current one
                if (miner.IsPickaxeBetter(Stats))
                {
                    miner.UpgradePickaxe(Stats);
                    IsCollected = true;
                    break;
                }
            }
        }
    }
    
    public void Draw()
    {
        // Draw the pickaxe as a circle with the appropriate color
        Raylib.DrawCircleV(Position, Stats.Size * 2, Stats.Color);
        
        // Draw a pickaxe shape
        Vector2 handle = Position + new Vector2(-Stats.Size, Stats.Size);
        Vector2 head = Position + new Vector2(Stats.Size, -Stats.Size);
        Raylib.DrawLineEx(handle, head, 2, Color.Black);
        
        // Draw stats when mouse is over the pickaxe
        if (Vector2.Distance(Program.GetMouseWorld(), Position) < Stats.Size * 2)
        {
            string statsText = $"Mining Power: {Stats.MiningPower}\nSpeed: {Stats.Speed:F2}";
            Raylib.DrawText(statsText, (int)Position.X + 15, (int)Position.Y - 15, 15, Color.Black);
        }
    }
}
