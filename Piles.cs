using Raylib_cs;
using System;
using System.Numerics;

public class EarthPile
{
    // Reference to the caravan so we can attach to its center.
    private Caravan caravan;


    // Position in world coordinates.
    public Vector2 Position;
    public int Width;
    public int Height;

    // Dragging state.
    private bool isDragging;
    private Vector2 dragOffset;

    public EarthPile(Caravan caravan, int width, int height)
    {
        this.caravan = caravan;
        Width = width;
        Height = height;
        isDragging = false;
        dragOffset = Vector2.Zero;

        // Start attached at the caravan's center.
        Position = GetEffectivePosition();
    }

    /// <summary>
    /// Returns the position for the EarthPile so that it is centered on the caravan.
    /// </summary>
    public Vector2 GetEffectivePosition()
    {
        return caravan.Center - new Vector2(Width / 2, Height / 2);
    }

    /// <summary>
    /// Updates the EarthPile.
    /// - When not dragging, it stays attached to the caravan.
    /// - When dragging, it follows the mouse in world space.
    /// - On drop, if the mouse is over the crusher, up to 10 units of resource are transferred
    ///   from Program.Earth to the crusher via ReceiveResource.
    /// </summary>
    public void Update(Camera2D camera)
    {
        // Convert the mouse's screen position to world coordinates.
        Vector2 mouseScreenPos = Raylib.GetMousePosition();
        Vector2 worldMousePos = Raylib.GetScreenToWorld2D(mouseScreenPos, camera);

        // If the left mouse button is pressed and the mouse is over the EarthPile, start dragging.
        if (Raylib.IsMouseButtonPressed(MouseButton.Left) && IsMouseOver(worldMousePos))
        {
            isDragging = true;
            dragOffset = Position - worldMousePos;
        }

        // If dragging, update the pile's position to follow the mouse.
        if (isDragging)
        {
            // Position = worldMousePos + dragOffset;
        }
        else
        {
            // If not dragging, always ensure the pile is attached to the caravan.
            Position = GetEffectivePosition();
        }

        // When the mouse button is released, check if we dropped on the crusher.
        if (Raylib.IsMouseButtonReleased(MouseButton.Left))
        {
            if (isDragging)
            {
                // Get the crusher's rectangle in world coordinates.
                foreach (var crusher in Program.crushers)
                {
                    Vector2 crusherPos = crusher.GetEffectivePosition();
                    Rectangle crusherRect = new Rectangle(crusherPos.X, crusherPos.Y, crusher.boxWidth, crusher.boxHeight);

                    // If the drop point (world mouse position) is within the crusher's area...
                    if (Raylib.CheckCollisionPointRec(worldMousePos, crusherRect))
                    {
                        int transferAmount = Math.Min((crusher.Hopper - crusher.InputResource), Program.stoneCounts[(int)StoneType.Earth]);
                        if (transferAmount > 0)
                        {
                            Program.stoneCounts[(int)StoneType.Earth] -= transferAmount;
                            crusher.ReceiveResource(transferAmount);
                            Program.player.exp += transferAmount;
                        }
                    }
                }
                // Snap the EarthPile back to the caravan center.
                Position = GetEffectivePosition();
                isDragging = false;
            }
        }
    }

    /// <summary>
    /// Returns true if the given world coordinate is over the EarthPile.
    /// </summary>
    private bool IsMouseOver(Vector2 worldPos)
    {
        return worldPos.X >= Position.X && worldPos.X <= Position.X + Width &&
               worldPos.Y >= Position.Y && worldPos.Y <= Position.Y + Height;
    }

    /// <summary>
    /// Draws the EarthPile.
    /// Make sure to call this from within BeginMode2D(camera) so it’s drawn in world space.
    /// Also, while dragging, a small circle is drawn at the mouse cursor.
    /// </summary>
    public void Draw(Camera2D camera)
    {
        // Draw the EarthPile rectangle.
        Raylib.DrawRectangle((int)Position.X, (int)Position.Y, Width, Height, Color.Brown);
        // Display the global earth count.
        Raylib.DrawText($"Earth: {Program.stoneCounts[(int)StoneType.Earth]}", (int)Position.X + 5, (int)Position.Y + 5, 10, Color.White);

        // If dragging, draw a small circle at the mouse cursor.
        if (isDragging)
        {
            Vector2 mouseScreenPos = Raylib.GetMousePosition();
            Vector2 worldMousePos = Raylib.GetScreenToWorld2D(mouseScreenPos, camera);
            Raylib.DrawCircleV(worldMousePos, 20, Color.Brown);
        }
    }
}
