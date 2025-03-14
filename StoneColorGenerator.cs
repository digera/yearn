using Raylib_cs;
using System;

/// <summary>
/// Utility class for generating consistent colors for different stone types
/// </summary>
public static class StoneColorGenerator
{
    /// <summary>
    /// Generates a color for a stone type based on its index in the enum
    /// </summary>
    /// <param name="stoneType">The stone type to generate a color for</param>
    /// <returns>A consistent color for the given stone type</returns>
    public static Color GetColor(StoneType stoneType)
    {
        int index = (int)stoneType;
        
        // Special cases for certain stone types to maintain consistency with previous colors
        switch (stoneType)
        {
            case StoneType.Earth:
                return new Color((byte)139, (byte)69, (byte)19, (byte)255); // Brown
            case StoneType.Diamond:
                return new Color((byte)185, (byte)242, (byte)255, (byte)255); // Light blue
            case StoneType.Aetherstone:
                return new Color((byte)200, (byte)230, (byte)255, (byte)255); // Ethereal blue
        }
        
        // Group stones by their general category and generate colors accordingly
        if (index <= 10) // Early sedimentary stones
        {
            // Earthy tones: browns, tans, grays
            byte r = (byte)(130 + (index * 12) % 60);
            byte g = (byte)(100 + (index * 8) % 40);
            byte b = (byte)(70 + (index * 5) % 30);
            return new Color((byte)r, (byte)g, (byte)b, (byte)255);
        }
        else if (index <= 20) // Mid-tier metamorphic stones
        {
            // Grays, whites, light blues
            byte r = (byte)(150 + (index * 5) % 70);
            byte g = (byte)(150 + (index * 7) % 80);
            byte b = (byte)(150 + (index * 9) % 90);
            return new Color((byte)r, (byte)g, (byte)b, (byte)255);
        }
        else if (index <= 30) // Higher-tier stones
        {
            // More saturated colors
            byte r = (byte)(100 + (index * 11) % 155);
            byte g = (byte)(100 + (index * 13) % 155);
            byte b = (byte)(100 + (index * 17) % 155);
            return new Color((byte)r, (byte)g, (byte)b, (byte)255);
        }
        else // Gemstones and precious materials
        {
            // Vibrant, jewel-like colors
            // Use sine waves to create a smooth color transition through the spectrum
            double position = (index - 30) / 15.0;
            byte r = (byte)(Math.Sin(position * Math.PI) * 127 + 128);
            byte g = (byte)(Math.Sin((position + 0.33) * Math.PI) * 127 + 128);
            byte b = (byte)(Math.Sin((position + 0.67) * Math.PI) * 127 + 128);
            
            // Increase saturation for gemstones
            Color baseColor = new Color((byte)r, (byte)g, (byte)b, (byte)255);
            return IncreaseSaturation(baseColor, 0.3f);
        }
    }
    
    /// <summary>
    /// Increases the saturation of a color by the specified amount
    /// </summary>
    private static Color IncreaseSaturation(Color color, float amount)
    {
        // Convert RGB to HSV
        float r = color.R / 255.0f;
        float g = color.G / 255.0f;
        float b = color.B / 255.0f;
        
        float max = Math.Max(r, Math.Max(g, b));
        float min = Math.Min(r, Math.Min(g, b));
        float delta = max - min;
        
        // Calculate hue
        float hue = 0;
        if (delta != 0)
        {
            if (max == r)
                hue = ((g - b) / delta) % 6;
            else if (max == g)
                hue = (b - r) / delta + 2;
            else
                hue = (r - g) / delta + 4;
        }
        hue *= 60;
        if (hue < 0)
            hue += 360;
        
        // Calculate saturation
        float saturation = max == 0 ? 0 : delta / max;
        
        // Increase saturation
        saturation = Math.Min(1.0f, saturation + amount);
        
        // Convert back to RGB
        float c = max * saturation;
        float x = c * (1 - Math.Abs((hue / 60) % 2 - 1));
        float m = max - c;
        
        float r1, g1, b1;
        if (hue >= 0 && hue < 60)
        {
            r1 = c; g1 = x; b1 = 0;
        }
        else if (hue >= 60 && hue < 120)
        {
            r1 = x; g1 = c; b1 = 0;
        }
        else if (hue >= 120 && hue < 180)
        {
            r1 = 0; g1 = c; b1 = x;
        }
        else if (hue >= 180 && hue < 240)
        {
            r1 = 0; g1 = x; b1 = c;
        }
        else if (hue >= 240 && hue < 300)
        {
            r1 = x; g1 = 0; b1 = c;
        }
        else
        {
            r1 = c; g1 = 0; b1 = x;
        }
        
        return new Color(
            (byte)((r1 + m) * 255),
            (byte)((g1 + m) * 255),
            (byte)((b1 + m) * 255),
            color.A
        );
    }
}
