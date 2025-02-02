using Raylib_cs;
using System.Numerics;
//this is deprecated but maybe useful?
public class Button
{
    private Caravan caravan;
    private Vector2 offset;
    public int Width { get; private set; }
    public int Height { get; private set; }
    public string Text { get; set; }
    public Color BackgroundColor { get; private set; }
    public Color TextColor { get; private set; }

    public Button(Caravan caravan, Vector2 offset, int width, int height, string text, Color bgColor, Color textColor)
    {
        this.caravan = caravan;
        this.offset = offset;
        this.Width = width;
        this.Height = height;
        this.Text = text;
        this.BackgroundColor = bgColor;
        this.TextColor = textColor;
    }

    public Vector2 GetPosition()
    {
        float caravanTop = caravan.Y - caravan.height;
        return new Vector2(0, caravanTop) + offset;
    }

    public void Draw()
    {
        Vector2 pos = GetPosition();
        Raylib.DrawRectangle((int)pos.X, (int)pos.Y, Width, Height, BackgroundColor);

        int fontSize = 20;
        Vector2 textSize = Raylib.MeasureTextEx(Raylib.GetFontDefault(), Text, fontSize, 1);
        float textX = pos.X + (Width - textSize.X) / 2;
        float textY = pos.Y + (Height - textSize.Y) / 2;
        Raylib.DrawText(Text, (int)textX, (int)textY, fontSize, TextColor);
    }

    public bool IsClicked(Vector2 mousePos)
    {
        Vector2 pos = GetPosition();
        return mousePos.X >= pos.X &&
               mousePos.X <= pos.X + Width &&
               mousePos.Y >= pos.Y &&
               mousePos.Y <= pos.Y + Height;
    }
}