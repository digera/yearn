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
            // Only expand on X axis, keep Y fixed
            int resourceCount = Program.stoneCounts[(int)StoneType];
            // Apply logarithmic scaling to show difference between orders of magnitude (100, 1000, 10000)
            float sizeMultiplier = 1.0f + (float)(Math.Log10(resourceCount + 1) * 0.3f);
            // Cap maximum size at reasonable limit
            sizeMultiplier = Math.Min(sizeMultiplier, 2.5f);
            int effectiveWidth = (int)(Width * sizeMultiplier);
            int effectiveHeight = Height; // Keep Y size fixed
            
            // Center the pile regardless of size
            int xOffset = (Width - effectiveWidth) / 2;
            int yOffset = 0; // No vertical offset needed since height is fixed
            
            // Draw the main pile with the stone's color - using multiple circles for oval effect
            int numCircles = 5; // Number of circles to create oval effect
            int circleRadius = effectiveHeight / 2;
            int circleSpacing = (effectiveWidth - circleRadius * 2) / Math.Max(1, numCircles - 1);
            
            for (int i = 0; i < numCircles; i++)
            {
                int circleX = (int)Position.X + xOffset + circleRadius + (i * circleSpacing);
                int circleY = (int)Position.Y + (effectiveHeight / 2);
                
                // Draw filled circle
                Raylib.DrawCircle(circleX, circleY, circleRadius, stoneColor);
                
                // Draw outline
                Color borderColor = new Color(
                    (byte)Math.Max(0, stoneColor.R - 40),
                    (byte)Math.Max(0, stoneColor.G - 40),
                    (byte)Math.Max(0, stoneColor.B - 40),
                    (byte)255
                );
                Raylib.DrawCircleLines(circleX, circleY, circleRadius, borderColor);
            }
            
            // Add texture to make it look more like a pile of stones
            // Draw small stone shapes within the pile
            Random random = new Random(StoneType.GetHashCode()); // Consistent random pattern for each stone type
            int numStones = Math.Min(resourceCount / 5, 20); // Cap at 20 stones for performance
            
            // Create a more natural layering of stones
            for (int i = 0; i < numStones; i++)
            {
                // Vary the stone colors slightly
                Color stoneVariation = new Color(
                    (byte)Math.Min(255, stoneColor.R + random.Next(-20, 21)),
                    (byte)Math.Min(255, stoneColor.G + random.Next(-20, 21)),
                    (byte)Math.Min(255, stoneColor.B + random.Next(-20, 21)),
                    (byte)255
                );
                
                // Random position with logical layering effect - stones at the bottom have higher Y values
                float angle = random.Next(0, 360) * MathF.PI / 180;
                float radiusVariation = random.Next(70, 95) / 100f; // 70-95% of max radius
                
                // Create layering effect - higher stones tend to be in the middle (bell curve distribution)
                float heightFactor = 1.0f - (MathF.Abs(MathF.Cos(angle)) * 0.5f);
                
                int stoneX = (int)(Position.X + xOffset + (effectiveWidth / 2) + (effectiveWidth / 2) * radiusVariation * MathF.Cos(angle));
                int stoneY = (int)(Position.Y + (effectiveHeight / 2) + (effectiveHeight / 2) * radiusVariation * MathF.Sin(angle) * heightFactor);
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
                
                // Draw a more visually appealing dragged item - oval shape
                float dragWidth = 30;
                float dragHeight = 20;
                
                // Draw main oval using multiple circles
                int dragCircles = 3;
                float dragCircleRadius = dragHeight / 2;
                float dragCircleSpacing = (dragWidth - dragCircleRadius * 2) / Math.Max(1, dragCircles - 1);
                
                for (int i = 0; i < dragCircles; i++)
                {
                    float circleX = worldMousePos.X - (dragWidth / 2) + dragCircleRadius + (i * dragCircleSpacing);
                    float circleY = worldMousePos.Y;
                    
                    Raylib.DrawCircle((int)circleX, (int)circleY, dragCircleRadius, stoneColor);
                }
                
                // Add some small stones in the dragged oval to show it's a collection
                for (int i = 0; i < 5; i++)
                {
                    float angle = i * (2 * MathF.PI / 5);
                    float radiusX = dragWidth / 2 - 5;
                    float radiusY = dragHeight / 2 - 5;
                    Vector2 stonePos = new Vector2(
                        worldMousePos.X + radiusX * MathF.Cos(angle),
                        worldMousePos.Y + radiusY * MathF.Sin(angle)
                    );
                    
                    Color stoneVariation = new Color(
                        (byte)Math.Min(255, stoneColor.R + random.Next(-20, 21)),
                        (byte)Math.Min(255, stoneColor.G + random.Next(-20, 21)),
                        (byte)Math.Min(255, stoneColor.B + random.Next(-20, 21)),
                        (byte)255
                    );
                    
                    Raylib.DrawCircleV(stonePos, 5, stoneVariation);
                }
                
                // Create a darker border color for the outline
                Color dragBorderColor = new Color(
                    (byte)Math.Max(0, stoneColor.R - 40),
                    (byte)Math.Max(0, stoneColor.G - 40),
                    (byte)Math.Max(0, stoneColor.B - 40),
                    (byte)255
                );
                
                // Add a highlight effect to the dragged item - using circle lines instead of ellipse
                for (int i = 0; i < dragCircles; i++)
                {
                    float circleX = worldMousePos.X - (dragWidth / 2) + dragCircleRadius + (i * dragCircleSpacing);
                    float circleY = worldMousePos.Y;
                    
                    Raylib.DrawCircleLines((int)circleX, (int)circleY, dragCircleRadius + 1, dragBorderColor);
                }
            }
        }
    }
}