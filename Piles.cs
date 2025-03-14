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
            Raylib.DrawRectangle((int)Position.X, (int)Position.Y, Width, Height, Color.Brown);
            Raylib.DrawText($"{StoneType}: {Program.stoneCounts[(int)StoneType]}", (int)Position.X + 5, (int)Position.Y + 5, 10, Color.White);

            if (isDragging)
            {
                // Use the virtual mouse position for drawing the dragged item
                Vector2 worldMousePos = Program.GetMouseWorld();
                Raylib.DrawCircleV(worldMousePos, 20, Color.Brown);
            }
        }
    }
}