using Raylib_cs;
using System;
using System.Numerics;

public class EarthPile
{
    private Caravan caravan;
    public Vector2 Position;
    public int Width;
    public int Height;
    public StoneType StoneType;
    private bool isDragging;
    private Vector2 dragOffset;

    public EarthPile(Caravan caravan, int width, int height, StoneType stoneType)
    {
        this.caravan = caravan;
        Width = width;
        Height = height;
        StoneType = stoneType;
        isDragging = false;
        dragOffset = Vector2.Zero;
        Position = GetEffectivePosition();
    }

    public Vector2 GetEffectivePosition()
    {
        int index = (int)StoneType;

        return caravan.Center - new Vector2(Width / 2, Height / 2) + new Vector2(0, index * 80);
    }

    public void Update(Camera2D camera)
    {
        // Use the virtual mouse position instead of the raw mouse position
        Vector2 worldMousePos = Program.GetMouseWorld();

        if (Raylib.IsMouseButtonPressed(MouseButton.Left) &&
            Raylib.CheckCollisionPointRec(worldMousePos, new Rectangle(Position.X, Position.Y, Width, Height)))
        {
            isDragging = true;
            dragOffset = Position - worldMousePos;
            Program.DraggedStoneType = StoneType;
        }
      //do not add this, this isn't needed.
      //  if (isDragging)
       // {
         //   Position = worldMousePos + dragOffset;
       // }
        else
        {
            Position = GetEffectivePosition();
        }

        if (Raylib.IsMouseButtonReleased(MouseButton.Left))
        {
            if (isDragging)
            {
                bool dropped = false;
                
                // Check if dropped on a crusher
                foreach (var crusher in Program.crushers)
                {
                    Vector2 crusherPos = crusher.GetEffectivePosition();
                    Rectangle crusherRect = new Rectangle(crusherPos.X, crusherPos.Y, crusher.boxWidth, crusher.boxHeight);

                    if (Raylib.CheckCollisionPointRec(worldMousePos, crusherRect))
                    {
                        dropped = true;
                        
                        if (crusher.InputType == StoneType)
                        {
                            int transferAmount = Math.Min((crusher.Hopper - crusher.InputResource), Program.stoneCounts[(int)StoneType]);
                            if (transferAmount > 0)
                            {
                                Program.stoneCounts[(int)StoneType] -= transferAmount;
                                crusher.ReceiveResource(transferAmount);
                                Program.player.exp += transferAmount;
                            }
                        }
                    }
                }
                
                // Check if dropped on forge - handle it the same way as crushers
                if (!dropped)
                {
                    Vector2 forgePos = Program.forge.GetEffectivePosition();
                    Rectangle forgeRect = new Rectangle(forgePos.X, forgePos.Y, Program.forge.Width, Program.forge.Height);
                    
                    if (Raylib.CheckCollisionPointRec(worldMousePos, forgeRect))
                    {
                        dropped = true;
                        
                        // Check if we can create a pickaxe
                        if (Program.forge.CanCreatePickaxe && Program.stoneCounts[(int)StoneType] > 0)
                        {
                            Program.forge.CreatePickaxeFromPile(StoneType);
                        }
                    }
                }
                
                // Check if dropped on shovel station
                if (!dropped)
                {
                    Vector2 stationPos = Program.shovelStation.GetEffectivePosition();
                    Rectangle stationRect = new Rectangle(stationPos.X, stationPos.Y, Program.shovelStation.Width, Program.shovelStation.Height);
                    
                    if (Raylib.CheckCollisionPointRec(worldMousePos, stationRect))
                    {
                        dropped = true;
                        
                        // Check if we can create a shovel
                        if (Program.shovelStation.CanCreateShovel && Program.stoneCounts[(int)StoneType] > 0)
                        {
                            Program.shovelStation.CreateShovelFromPile(StoneType);
                        }
                    }
                }
                
                Position = GetEffectivePosition();
                isDragging = false;
                Program.DraggedStoneType = null;
            }
        }
    }

    public void Draw(Camera2D camera)
    {
        if (Program.stoneCounts[(int)StoneType] > 0)
        {
            // Get the appropriate color for this stone type
            Color stoneColor = StoneColorGenerator.GetColor(StoneType);
            
            // Calculate pile size based on resource count (with a reasonable cap)
            int resourceCount = Program.stoneCounts[(int)StoneType];
            float sizeMultiplier = Math.Min(1.0f + (resourceCount / 200.0f), 2.0f);
            int effectiveWidth = (int)(Width * sizeMultiplier);
            int effectiveHeight = (int)(Height * sizeMultiplier);
            
            // Center the pile regardless of size
            int xOffset = (Width - effectiveWidth) / 2;
            int yOffset = (Height - effectiveHeight) / 2;
            
            // Draw the main pile with the stone's color
            Raylib.DrawRectangle(
                (int)Position.X + xOffset, 
                (int)Position.Y + yOffset, 
                effectiveWidth, 
                effectiveHeight, 
                stoneColor
            );
            
            // Add a darker border for definition
            Color borderColor = new Color(
                (byte)Math.Max(0, stoneColor.R - 40),
                (byte)Math.Max(0, stoneColor.G - 40),
                (byte)Math.Max(0, stoneColor.B - 40),
                (byte)255
            );
            Raylib.DrawRectangleLines(
                (int)Position.X + xOffset, 
                (int)Position.Y + yOffset, 
                effectiveWidth, 
                effectiveHeight, 
                borderColor
            );
            
            // Add texture to make it look more like a pile of stones
            // Draw small stone shapes within the pile
            Random random = new Random(StoneType.GetHashCode()); // Consistent random pattern for each stone type
            int numStones = Math.Min(resourceCount / 5, 20); // Cap at 20 stones for performance
            
            for (int i = 0; i < numStones; i++)
            {
                // Vary the stone colors slightly
                Color stoneVariation = new Color(
                    (byte)Math.Min(255, stoneColor.R + random.Next(-20, 21)),
                    (byte)Math.Min(255, stoneColor.G + random.Next(-20, 21)),
                    (byte)Math.Min(255, stoneColor.B + random.Next(-20, 21)),
                    (byte)255
                );
                
                // Random position within the pile
                int stoneX = (int)Position.X + xOffset + random.Next(10, effectiveWidth - 10);
                int stoneY = (int)Position.Y + yOffset + random.Next(10, effectiveHeight - 10);
                int stoneSize = random.Next(5, 12);
                
                // Draw the individual stone
                Raylib.DrawCircle(stoneX, stoneY, stoneSize, stoneVariation);
                
                // Add a highlight to give depth
                Raylib.DrawCircleSector(
                    new Vector2(stoneX, stoneY),
                    stoneSize - 1,
                    45, // startAngle
                    225, // endAngle
                    8, // segments
                    new Color(
                        (byte)Math.Min(255, stoneVariation.R + 30),
                        (byte)Math.Min(255, stoneVariation.G + 30),
                        (byte)Math.Min(255, stoneVariation.B + 30),
                        (byte)180
                    )
                );
            }
            
            // Choose text color based on stone color brightness for better readability
            Color textColor = (stoneColor.R + stoneColor.G + stoneColor.B > 380) ? 
                new Color((byte)20, (byte)20, (byte)20, (byte)255) : 
                new Color((byte)230, (byte)230, (byte)230, (byte)255);
                
            // Create a small background for the text to ensure readability
            Raylib.DrawRectangle(
                (int)Position.X + 2,
                (int)Position.Y + 2,
                Width - 4,
                20,
                new Color((byte)0, (byte)0, (byte)0, (byte)150)
            );
            
            // Display the stone type and count
            Raylib.DrawText(
                $"{StoneType}: {resourceCount}", 
                (int)Position.X + 5, 
                (int)Position.Y + 5, 
                10, 
                textColor
            );

            if (isDragging)
            {
                // Use the virtual mouse position for drawing the dragged item
                Vector2 worldMousePos = Program.GetMouseWorld();
                
                // Draw a more visually appealing dragged item
                Raylib.DrawCircleV(worldMousePos, 20, stoneColor);
                
                // Add some small stones in the dragged circle to show it's a collection
                for (int i = 0; i < 5; i++)
                {
                    float angle = i * (2 * MathF.PI / 5);
                    float radius = 10;
                    Vector2 stonePos = new Vector2(
                        worldMousePos.X + radius * MathF.Cos(angle),
                        worldMousePos.Y + radius * MathF.Sin(angle)
                    );
                    
                    Color stoneVariation = new Color(
                        (byte)Math.Min(255, stoneColor.R + random.Next(-20, 21)),
                        (byte)Math.Min(255, stoneColor.G + random.Next(-20, 21)),
                        (byte)Math.Min(255, stoneColor.B + random.Next(-20, 21)),
                        (byte)255
                    );
                    
                    Raylib.DrawCircleV(stonePos, 5, stoneVariation);
                }
                
                // Add a highlight effect to the dragged item
                Raylib.DrawCircleLines((int)worldMousePos.X, (int)worldMousePos.Y, 22, borderColor);
            }
        }
    }
}