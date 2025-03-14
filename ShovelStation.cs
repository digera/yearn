using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Numerics;

/// <summary>
/// ShovelStation class, handles the creation of shovels from stone resources
/// </summary>
public class ShovelStation
{
    private Caravan caravan;
    private Vector2 offset;
    public int Width { get; private set; }
    public int Height { get; private set; }
    private Color color = new Color(70, 130, 180, 255); // Steel blue color for the station
    private List<Shovel> activeShovels = new List<Shovel>();
    private float stationTimer = 0;
    private const float STATION_COOLDOWN = 5.0f; // Time between automatic shovel creation
    private bool canCreateShovel = true;
    private bool isHovered = false;
    
    public bool CanCreateShovel => canCreateShovel;
    public float StationTimer => stationTimer;
    public List<Shovel> ActiveShovels => activeShovels;
    
    public ShovelStation(Caravan caravan, int width = 70, int height = 70)
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
        if (!canCreateShovel)
        {
            stationTimer += dt;
            if (stationTimer >= STATION_COOLDOWN)
            {
                stationTimer = 0;
                canCreateShovel = true;
            }
        }
        
        // Update active shovels
        for (int i = activeShovels.Count - 1; i >= 0; i--)
        {
            var shovel = activeShovels[i];
            shovel.Update(dt);
            
            // Remove shovels that have been collected
            if (shovel.IsCollected)
            {
                activeShovels.RemoveAt(i);
            }
        }
        
        // Check if mouse is hovering over the station
        Vector2 mousePos = Program.GetMouseWorld();
        Vector2 stationPos = GetEffectivePosition();
        isHovered = Raylib.CheckCollisionPointRec(mousePos, new Rectangle(stationPos.X, stationPos.Y, Width, Height));
    }
    
    // This method is called from EarthPile when a stone is dropped on the station
    public void CreateShovelFromPile(StoneType stoneType)
    {
        if (canCreateShovel && Program.stoneCounts[(int)stoneType] > 0)
        {
            CreateShovel(stoneType);
            Program.stoneCounts[(int)stoneType] -= 50;
            canCreateShovel = false;
            stationTimer = 0;
        }
    }
    
    private void CreateShovel(StoneType stoneType)
    {
        // Create a shovel that appears in the center, similar to pickaxes
        Vector2 spawnPos = new Vector2(Program.refWidth / 2, caravan.Y - 300 - Raylib.GetRandomValue(0, 200));
        
        // Calculate shovel stats based on stone type
        ShovelStats stats = CalculateShovelStats(stoneType);
        
        // Create and add the shovel
        Shovel newShovel = new Shovel(spawnPos, stats);
        activeShovels.Add(newShovel);
    }
    
    private ShovelStats CalculateShovelStats(StoneType stoneType)
    {
        // Base stats
        float speed = 1.0f;
        float capacity = 10.0f;
        
        // Adjust stats based on stone type
        int stoneValue = (int)stoneType;
        
        // Speed increases with stone value
        speed += stoneValue * 0.1f;
        
        // Capacity increases with stone value
        capacity += stoneValue * 10.0f;
        
        // Determine color based on stone type
        Color color = StoneColorGenerator.GetColor(stoneType);
        
        return new ShovelStats(speed, capacity, color);
    }
    
    public void Draw()
    {
        Vector2 pos = GetEffectivePosition();
        
        // Draw the station with a nice design
        Raylib.DrawRectangle((int)pos.X, (int)pos.Y, Width, Height, color);
        
        // Add some details to the station
        Color darkBlue = new Color((byte)25, (byte)25, (byte)112, (byte)255);
        Raylib.DrawRectangle((int)pos.X + 5, (int)pos.Y + 5, Width - 10, Height - 10, darkBlue);
        
        // Draw a glowing effect if the station is active
        if (canCreateShovel)
        {
            Color blueGlow = new Color((byte)100, (byte)149, (byte)237, (byte)150);
            Raylib.DrawRectangle((int)pos.X + 15, (int)pos.Y + 15, Width - 30, Height - 30, blueGlow);
        }
        
        // Draw station label with a better font size and position
        Raylib.DrawText("Shovel", (int)pos.X + 5, (int)pos.Y + 10, 15, Color.White);
        Raylib.DrawText("Station", (int)pos.X + 10, (int)pos.Y + 30, 15, Color.White);
        
        // Draw a highlight when hovered
        if (isHovered)
        {
            Raylib.DrawRectangleLines((int)pos.X, (int)pos.Y, Width, Height, Color.Yellow);
            
            // Show tooltip when hovering
            string tooltip = "Drag resources here\nto create shovels";
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
        if (!canCreateShovel)
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
        
        // Draw active shovels
        foreach (var shovel in activeShovels)
        {
            shovel.Draw();
        }
    }
    
    public bool CheckClick(Vector2 mousePos)
    {
        Vector2 pos = GetEffectivePosition();
        return Raylib.CheckCollisionPointRec(mousePos, new Rectangle(pos.X, pos.Y, Width, Height));
    }
}

/// <summary>
/// Shovel class that can be collected by miners to increase their carrying capacity
/// </summary>
public class Shovel
{
    public Vector2 Position;
    public ShovelStats Stats;
    public bool IsCollected { get; private set; } = false;
    
    private float bounceTimer = 0;
    private const float BOUNCE_PERIOD = 1.0f;
    private float bounceHeight = 10.0f;
    private Vector2 basePosition;
    
    public Shovel(Vector2 position, ShovelStats stats)
    {
        Position = position;
        basePosition = position;
        Stats = stats;
    }
    
    public void Update(float dt)
    {
        // Make the shovel bounce
        bounceTimer += dt;
        if (bounceTimer > BOUNCE_PERIOD)
        {
            bounceTimer -= BOUNCE_PERIOD;
        }
        
        float bounceOffset = (float)Math.Sin(bounceTimer / BOUNCE_PERIOD * Math.PI * 2) * bounceHeight;
        Position = basePosition + new Vector2(0, bounceOffset);
        
        // Check if any miner is close enough to collect this shovel
        foreach (var miner in Program.miners)
        {
            if (Vector2.Distance(miner.Position, Position) < miner.Radius + 10)
            {
                // Only collect if this shovel is better than the miner's current one
                if (miner.IsShovelBetter(Stats))
                {
                    miner.UpgradeShovel(Stats);
                    IsCollected = true;
                    break;
                }
            }
        }
    }
    
    public void Draw()
    {
        // Draw the shovel with a shovel-like shape
        // Draw the handle
        Raylib.DrawRectangle((int)(Position.X - 3), (int)(Position.Y - 20), 6, 20, new Color(139, 69, 19, 255)); // Brown handle
        
        // Draw the blade
        Raylib.DrawRectangle((int)(Position.X - 10), (int)(Position.Y), 20, 10, Stats.Color);
        Raylib.DrawRectangleLines((int)(Position.X - 10), (int)(Position.Y), 20, 10, Color.Black);
        
        // Draw stats when mouse is over the shovel
        if (Vector2.Distance(Program.GetMouseWorld(), Position) < 20)
        {
            string statsText = $"Capacity: +{Stats.Capacity}\nSpeed: {Stats.Speed:F2}";
            Raylib.DrawText(statsText, (int)Position.X + 15, (int)Position.Y - 15, 15, Color.Black);
        }
    }
}
