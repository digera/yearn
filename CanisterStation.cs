using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Numerics;

/// <summary>
/// CanisterStation class, handles the creation of canisters from stone resources
/// </summary>
public class CanisterStation
{
    private Caravan caravan;
    private Vector2 offset;
    public int Width { get; private set; }
    public int Height { get; private set; }
    private Color color = new Color(70, 130, 180, 255); // Steel blue color for the station
    private List<Canister> activeCanisters = new List<Canister>();
    private float stationTimer = 0;
    private const float STATION_COOLDOWN = 5.0f; // Time between automatic canister creation
    private bool canCreateCanister = true;
    private bool isHovered = false;
    
    public bool CanCreateCanister => canCreateCanister;
    public float StationTimer => stationTimer;
    public List<Canister> ActiveCanisters => activeCanisters;
    
    public CanisterStation(Caravan caravan, int width = 70, int height = 70)
    {
        this.caravan = caravan;
        this.Width = width;
        this.Height = height;
        // Position the station on the left side of the caravan
        offset = new Vector2(50, 50);
    }
    
    public Vector2 GetEffectivePosition()
    {
        float caravanTop = caravan.Y - caravan.height;
        return new Vector2(0, caravanTop) + offset;
    }
    
    public void Update(float dt)
    {
        // Update station cooldown timer
        if (!canCreateCanister)
        {
            stationTimer += dt;
            if (stationTimer >= STATION_COOLDOWN)
            {
                stationTimer = 0;
                canCreateCanister = true;
            }
        }
        
        // Update active canisters
        for (int i = activeCanisters.Count - 1; i >= 0; i--)
        {
            var canister = activeCanisters[i];
            canister.Update(dt);
            
            // Remove canisters that have been collected
            if (canister.IsCollected)
            {
                activeCanisters.RemoveAt(i);
            }
        }
        
        // Check if mouse is hovering over the station
        Vector2 mousePos = Program.GetMouseWorld();
        Vector2 stationPos = GetEffectivePosition();
        isHovered = Raylib.CheckCollisionPointRec(mousePos, new Rectangle(stationPos.X, stationPos.Y, Width, Height));
    }
    
    // This method is called from EarthPile when a stone is dropped on the station
    public void CreateCanisterFromPile(StoneType stoneType)
    {
        if (canCreateCanister && Program.stoneCounts[(int)stoneType] > 0)
        {
            CreateCanister(stoneType);
            // Consume the resource
            Program.stoneCounts[(int)stoneType] -= 50;
            canCreateCanister = false;
            stationTimer = 0;
        }
    }
    
    private void CreateCanister(StoneType stoneType)
    {
        // Create a canister that appears in the center, similar to pickaxes
        Vector2 spawnPos = new Vector2(Program.refWidth / 2, caravan.Y - 300 - Raylib.GetRandomValue(0, 200));
        
        // Calculate canister stats based on stone type
        CanisterStats stats = CalculateCanisterStats(stoneType);
        
        // Create and add the canister
        Canister newCanister = new Canister(spawnPos, stats);
        activeCanisters.Add(newCanister);
    }
    
    private CanisterStats CalculateCanisterStats(StoneType stoneType)
    {
        // Base stats
        int capacity = 10;
        float speed = 10f;
        
        // Improve stats based on stone type
        int stoneLevel = (int)stoneType;
        capacity = capacity + (stoneLevel * 2);
        speed = speed + (stoneLevel * 0.5f);
        
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
        
        return new CanisterStats(speed, capacity, color);
    }
    
    public void Draw()
    {
        Vector2 pos = GetEffectivePosition();
        
        // Draw the station with a nice design
        Raylib.DrawRectangle((int)pos.X, (int)pos.Y, Width, Height, color);
        
        // Add some details to the station
        Color darkBlue = new Color(25, 25, 112, 255);
        Raylib.DrawRectangle((int)pos.X + 5, (int)pos.Y + 5, Width - 10, Height - 10, darkBlue);
        
        // Draw a glowing effect if the station is active
        if (canCreateCanister)
        {
            Color blueGlow = new Color(100, 149, 237, 150);
            Raylib.DrawRectangle((int)pos.X + 15, (int)pos.Y + 15, Width - 30, Height - 30, blueGlow);
        }
        
        // Draw station label with a better font size and position
        Raylib.DrawText("Canister", (int)pos.X + 5, (int)pos.Y + 10, 15, Color.White);
        Raylib.DrawText("Station", (int)pos.X + 10, (int)pos.Y + 30, 15, Color.White);
        
        // Draw a highlight when hovered
        if (isHovered)
        {
            Raylib.DrawRectangleLines((int)pos.X, (int)pos.Y, Width, Height, Color.Yellow);
            
            // Show tooltip when hovering
            string tooltip = "Drag resources here\nto create canisters";
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
        if (!canCreateCanister)
        {
            float cooldownPercentage = stationTimer / STATION_COOLDOWN;
            int indicatorHeight = (int)(Height * cooldownPercentage);
            Raylib.DrawRectangle(
                (int)pos.X, 
                (int)(pos.Y + Height - indicatorHeight), 
                5, 
                indicatorHeight, 
                Color.Red);
        }
        
        // Draw active canisters
        foreach (var canister in activeCanisters)
        {
            canister.Draw();
        }
    }
    
    public bool CheckClick(Vector2 mousePos)
    {
        Vector2 pos = GetEffectivePosition();
        return Raylib.CheckCollisionPointRec(mousePos, new Rectangle(pos.X, pos.Y, Width, Height));
    }
}

/// <summary>
/// Canister class that can be collected by miners to increase their carrying capacity
/// </summary>
public class Canister
{
    public Vector2 Position;
    public CanisterStats Stats;
    public bool IsCollected { get; private set; } = false;
    
    private float bounceTimer = 0;
    private const float BOUNCE_PERIOD = 1.0f;
    private float bounceHeight = 10.0f;
    private Vector2 basePosition;
    
    public Canister(Vector2 position, CanisterStats stats)
    {
        Position = position;
        basePosition = position;
        Stats = stats;
    }
    
    public void Update(float dt)
    {
        // Make the canister bounce
        bounceTimer += dt;
        if (bounceTimer > BOUNCE_PERIOD)
        {
            bounceTimer -= BOUNCE_PERIOD;
        }
        
        float bounceOffset = (float)Math.Sin(bounceTimer / BOUNCE_PERIOD * Math.PI * 2) * bounceHeight;
        Position = basePosition + new Vector2(0, bounceOffset);
        
        // Check if any miner is close enough to collect this canister
        foreach (var miner in Program.miners)
        {
            if (Vector2.Distance(miner.Position, Position) < miner.Radius + 10)
            {
                // Only collect if this canister is better than the miner's current one
                if (miner.IsCanisterBetter(Stats))
                {
                    miner.UpgradeCanister(Stats);
                    IsCollected = true;
                    break;
                }
            }
        }
    }
    
    public void Draw()
    {
        // Draw the canister as a rectangle with the appropriate color
        Raylib.DrawRectangle((int)(Position.X - 8), (int)(Position.Y - 12), 16, 24, Stats.Color);
        
        // Draw a canister shape
        Raylib.DrawRectangleLines((int)(Position.X - 8), (int)(Position.Y - 12), 16, 24, Color.Black);
        Raylib.DrawRectangle((int)(Position.X - 4), (int)(Position.Y - 16), 8, 4, Color.DarkGray);
        
        // Draw stats when mouse is over the canister
        if (Vector2.Distance(Program.GetMouseWorld(), Position) < 20)
        {
            string statsText = $"Capacity: +{Stats.Capacity}\nSpeed: {Stats.Speed:F2}";
            Raylib.DrawText(statsText, (int)Position.X + 15, (int)Position.Y - 15, 15, Color.Black);
        }
    }
}
