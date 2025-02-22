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
        Vector2 mouseScreenPos = Raylib.GetMousePosition();
        Vector2 worldMousePos = Raylib.GetScreenToWorld2D(mouseScreenPos, camera);

        if (Raylib.IsMouseButtonPressed(MouseButton.Left) &&
            Raylib.CheckCollisionPointRec(worldMousePos, new Rectangle(Position.X, Position.Y, Width, Height)))
        {
            isDragging = true;
            dragOffset = Position - worldMousePos;
            Program.DraggedStoneType = StoneType;
        }

        else
        {
            Position = GetEffectivePosition();
        }

        if (Raylib.IsMouseButtonReleased(MouseButton.Left))
        {
            if (isDragging)
            {
                foreach (var crusher in Program.crushers)
                {
                    Vector2 crusherPos = crusher.GetEffectivePosition();
                    Rectangle crusherRect = new Rectangle(crusherPos.X, crusherPos.Y, crusher.boxWidth, crusher.boxHeight);

                    if (Raylib.CheckCollisionPointRec(worldMousePos, crusherRect))
                    {
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
                Vector2 mouseScreenPos = Raylib.GetMousePosition();
                Vector2 worldMousePos = Raylib.GetScreenToWorld2D(mouseScreenPos, camera);
                Raylib.DrawCircleV(worldMousePos, 20, Color.Brown);
            }
        }
    }
}