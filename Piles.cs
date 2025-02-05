﻿using Raylib_cs;
using System;
using System.Numerics;
using System.Collections.Generic;

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
                Vector2 crusherPos = Program.crusher.GetEffectivePosition();
                Rectangle crusherRect = new Rectangle(crusherPos.X, crusherPos.Y, Program.crusher.boxWidth, Program.crusher.boxHeight);

                // If the drop point (world mouse position) is within the crusher's area...
                if (Raylib.CheckCollisionPointRec(worldMousePos, crusherRect))
                {
                    int transferAmount = Math.Min( (Program.crusher.Hopper - Program.crusher.InputResource), Program.Earth);
                    if (transferAmount > 0)
                    {
                        Program.Earth -= transferAmount;
                        Program.crusher.ReceiveResource(transferAmount);
                        Program.player.exp += transferAmount;
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
        Raylib.DrawText($"Earth: {Program.Earth}", (int)Position.X + 5, (int)Position.Y + 5, 10, Color.White);

        // If dragging, draw a small circle at the mouse cursor.
        if (isDragging)
        {
            Vector2 mouseScreenPos = Raylib.GetMousePosition();
            Vector2 worldMousePos = Raylib.GetScreenToWorld2D(mouseScreenPos, camera);
            Raylib.DrawCircleV(worldMousePos, 20, Color.Brown);
        }
    }
}

public class Pile
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

    // Visuals
    public int boxWidth;
    public int boxHeight;
    private const int extraYOffset = 200;
    private const int buttonSize = 20;
    private const int buttonMargin = 5;

    // upgrade multipliers
    private readonly float hopperUpgradeMultiplier = 1.5f;
    private readonly float conversionUpgradeMultiplier = 1.5f;

    // each pile has unique inputType and outputType, iterations from the enum in names
    public Pile(Caravan caravan, StoneType inputType, StoneType outputType,
                   int hopper = 100, int boxWidth = 50, int boxHeight = 50)
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
    }

    // Get the base position from the caravan, then adjust it by an offset and then an extra offset lol shut up math is hard
    public Vector2 GetEffectivePosition()
    {
        float caravanTop = caravan.Y - caravan.height;
        return new Vector2(0, caravanTop) + offset + new Vector2(0, extraYOffset);
    }

    public void Draw()
    {
        Vector2 pos = GetEffectivePosition();

        // Draw Pile
        Raylib.DrawRectangle((int)pos.X, (int)pos.Y, boxWidth, boxHeight, Color.Red);

        // Display current resource counts and conversion rate
        string text = $"{InputResource}/{Hopper} {InputResourceName}\n" +
                      $"{OutputResource} {OutputResourceName}\n" +
                      $"{ConversionAmount}/s";
        Raylib.DrawText(text, (int)pos.X + 5, (int)pos.Y + 5, 10, Color.Black);

        // Draw the two upgrade buttons:
        // Button 0: Upgrade Hopper (capacity)
        // Button 1: Upgrade Conversion (increase conversion amount per tick)
        DrawUpgradeButton(pos, 0, HopperUpgradeCost.ToString());
        DrawUpgradeButton(pos, 1, ConversionUpgradeCost.ToString());
    }

    // Utility method for drawing an upgrade button
    private void DrawUpgradeButton(Vector2 pos, int index, string cost)
    {
        int x = (int)pos.X + index * (buttonSize + buttonMargin);
        int y = (int)pos.Y + boxHeight;
        Raylib.DrawRectangle(x, y, buttonSize, buttonSize, Color.Gray);
        Raylib.DrawText(cost, x + 2, y + 2, 8, Color.White);
    }

    // TODO: move CheckClick and many other methods to a utility class -- each class has its own and they're all slightly different
    public bool CheckClick(Vector2 mousePos)
    {
        Vector2 pos = GetEffectivePosition();
        return mousePos.X >= pos.X &&
               mousePos.X <= pos.X + boxWidth &&
               mousePos.Y >= pos.Y &&
               mousePos.Y <= pos.Y + boxHeight;
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

    public void ReceiveResource(int amount)
    {
        InputResource = Math.Min(InputResource + amount, Hopper);
    }


    public void Update(float dt)
    {
        if (InputResource > 0)
        {
            conversionTimer += dt;
            while (conversionTimer >= 1.0f && InputResource > 0)
            {
                conversionTimer -= 1.0f;
                int converted = Math.Min(InputResource, (int)ConversionAmount);
                InputResource -= converted;
                OutputResource += converted;
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
        if (OutputResource >= HopperUpgradeCost)
        {
            OutputResource -= HopperUpgradeCost;
            Hopper += 20;
        }
    }
    // "(int)(20" is the starting cost
    public int ConversionUpgradeCost => (int)(20 * Math.Pow(conversionUpgradeMultiplier, ConversionAmount - 1));

    public void UpgradeConversion()
    {
        if (OutputResource >= ConversionUpgradeCost)
        {
            OutputResource -= ConversionUpgradeCost;
            ConversionAmount += 1;
        }
    }
    public void RestoreState(GameState.CrusherSaveData data)
    {
        Hopper = data.Hopper;
        ConversionAmount = data.ConversionAmount;
        InputResource = data.InputResource;
        OutputResource = data.OutputResource;
    }
    // basically an alias... for stuff that's hopefully gone
    public int RateCost => (int)(20 * ConversionAmount);
    public void UpgradeRate()
    {
        if (OutputResource >= RateCost)
        {
            OutputResource -= RateCost;

        }
    }
}

public static class PileManager
{
    public static List<Pile> Piles = new List<Pile>();

    public static void UpdateAll(float dt)
    {
        foreach (var pile in Piles)
        {
            pile.Update(dt);
        }
    }

    public static void DrawAll()
    {
        foreach (var pile in Piles)
        {
            pile.Draw();
        }
    }
}
