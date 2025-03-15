using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Numerics;

/// <summary>
/// InfoPanel class that handles displaying information about game elements
/// This serves as a centralized system for tooltips and will eventually become the main menu
/// </summary>
public class InfoPanel
{
    // Singleton instance
    private static InfoPanel? instance;
    
    // Panel properties
    private bool isVisible = false;
    private Vector2 position;
    private float width = 140;
    private float height = 150;
    private Color backgroundColor = new Color(40, 40, 40, 230);
    private Color borderColor = new Color(150, 150, 150, 255);
    public Color headerColor = new Color(60, 60, 60, 255);
    private Color textColor = Color.White;
    private string title = "Information";
    private List<string> contentLines = new List<string>();
    
    // Line properties
    private bool showConnectingLine = false;
    private Vector2 lineStartPosition;
    private Color lineColor = Color.White;
    
    // Private constructor to enforce singleton pattern
    private InfoPanel()
    {
        // fixed position
        position = new Vector2(410, 420);
        width = 170; // Fixed width
        height = 350; // Fixed height
    }
    
    // Get singleton instance
    public static InfoPanel GetInstance()
    {
        if (instance == null)
        {
            instance = new InfoPanel();
        }
        return instance;
    }
    
    // Set panel title
    public void SetTitle(string newTitle)
    {
        title = newTitle;
    }
    
    // Set panel content
    public void SetContent(List<string> lines)
    {
        contentLines = lines;
    }
    
    // Add a line to the content
    public void AddContentLine(string line)
    {
        contentLines.Add(line);
    }
    
    // Clear all content
    public void ClearContent()
    {
        contentLines.Clear();
    }
    
    // Set custom content for a stone type
    public void SetStoneTypeContent(StoneType stoneType, int amount)
    {
        title = $"{stoneType} Info";
        headerColor = StoneColorGenerator.GetColor(stoneType);
        ClearContent();
        AddContentLine($"Type: {stoneType}");
        AddContentLine($"Amount: {amount}");
    }
    
    // Set custom content for a crusher
    public void SetCrusherContent(StoneType inputType, StoneType outputType, int inputAmount, int outputAmount)
    {
        title = "Crusher Info";
        headerColor = StoneColorGenerator.GetColor(inputType);
        ClearContent();
        AddContentLine($"Input: {inputType} ({inputAmount})");
        AddContentLine($"Output: {outputType} ({outputAmount})");
    }
    
    // Show the panel
    public void Show()
    {
        isVisible = true;
    }
    
    // Hide the panel
    public void Hide()
    {
        isVisible = false;
        showConnectingLine = false;
    }
    
    // Show the panel with a connecting line from a button
    public void ShowWithConnectingLine(Vector2 buttonPosition)
    {
        isVisible = true;
        showConnectingLine = true;
        lineStartPosition = buttonPosition;
    }
    
    // Update the panel
    public void Update()
    {
        // Nothing to update for now
    }
    
    // Draw the panel
    public void Draw()
    {
        if (!isVisible)
        {
            return;
        }
        
        // Draw connecting line if needed
        if (showConnectingLine)
        {
            Vector2 lineEndPosition = new Vector2(position.X, position.Y + height / 2);
            Raylib.DrawLine((int)lineStartPosition.X, (int)lineStartPosition.Y, (int)lineEndPosition.X, (int)lineEndPosition.Y, lineColor);
        }
        
        // Create panel rectangle
        Rectangle panelRect = new Rectangle(position.X, position.Y, width, height);
        
        // Draw panel background
        Raylib.DrawRectangleRounded(panelRect, 0.1f, 8, backgroundColor);
        
        // Draw panel border - using DrawRectangleRoundedLinesEx with correct parameters
        Raylib.DrawRectangleRoundedLinesEx(panelRect, 0.1f, 8, 2, borderColor);
        
        // Draw colored header bar
        Rectangle headerRect = new Rectangle(position.X, position.Y, width, 40);
        Raylib.DrawRectangleRec(headerRect, headerColor);
        
        // Draw title
        Vector2 titlePos = new Vector2(position.X + 10, position.Y + 10);
        Raylib.DrawText(title, (int)titlePos.X, (int)titlePos.Y, 20, textColor);
        
        // Draw content lines
        float contentY = position.Y + 50;
        foreach (string line in contentLines)
        {
            Raylib.DrawText(line, (int)(position.X + 10), (int)contentY, 16, textColor);
            contentY += 20;
        }
    }
}
