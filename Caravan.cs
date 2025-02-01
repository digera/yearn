using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;



public class Caravan
{
    private Vector2 position;
    public float width;
    public float height;
    private Color color;

    public Vector2 Center => new Vector2(width / 2, position.Y);
    public float Y => position.Y;

    public Caravan(int refWidth, int refHeight)
    {
        width = Program.refWidth;
        height = Program.refHeight * 0.25f;
        position = new Vector2(0, Program.refHeight - height * 0.05f);
        color = new Color((byte)139, (byte)69, (byte)19, (byte)255);
    }

    public void SetY(float y) => position.Y = y;
    public float GetY() => position.Y;



    public void Draw()
    {
        Raylib.DrawEllipse(
            (int)(position.X + width / 2),  
            (int)position.Y,                
            (int)(width / 2),               
            (int)height,                    
            color
        );
        int rectHeight = (int)(height * 3f); 

        Raylib.DrawRectangle(
            (int)position.X,               
            (int)(position.Y + height - 200), 
            (int)width,                    
            rectHeight,                    
            color
        );
    }


    public bool CheckClick(Vector2 mousePosition)
    {
        float clickableLeft = position.X;
        float clickableTop = position.Y -100; 
        float clickableWidth = width;
        float clickableHeight = Program.refHeight - position.Y + 500;

        return mousePosition.X >= clickableLeft &&
               mousePosition.X <= clickableLeft + clickableWidth &&
               mousePosition.Y >= clickableTop &&
               mousePosition.Y <= clickableTop + clickableHeight;
    }

}