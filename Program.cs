using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

public enum PlayerState
{
    Idle,
    Walking,
    Crafting,
    Mining
}

public class Block
{
    public int X;
    public int Y;
    public int Size;
    public int Dur = 100;
    public int Yield = 10;
    public string Mat = "stone";
    public Color Color;

    public Block(int x, int y, int size, Color color)
    {
        X = x;
        Y = y;
        Size = size;
        Color = color;
    }

    public void Draw()
    {
        Raylib.DrawRectangle(X, Y, Size, Size, Color);
        if (Program.IsBlockInMiningRange(this))
        {
            for (int i = 0; i < 3; i++)
            {
                Raylib.DrawRectangleLines(X - i, Y - i,
                                          Size + (i * 2),
                                          Size + (i * 2),
                                          Color.Yellow);
            }
        }
    }

    public bool OverlapsCircle(Vector2 pos, float radius)
    {
        float left = X;
        float right = X + Size;
        float top = Y;
        float bottom = Y + Size;
        float closestX = Math.Clamp(pos.X, left, right);
        float closestY = Math.Clamp(pos.Y, top, bottom);
        float dx = pos.X - closestX;
        float dy = pos.Y - closestY;
        return (dx * dx + dy * dy) <= (radius * radius);
    }
}

public class Projectile
{
    public Vector2 Position;
    public bool IsActive = true;
    float Speed = 300f;
    Block targetBlock;

    public Projectile(Vector2 startPos, Block block)
    {
        Position = startPos;
        targetBlock = block;
    }

    public void Update(float dt, int miningPwr, List<Block> blocks)
    {
        if (!IsActive || targetBlock == null) return;
        Vector2 center = new Vector2(targetBlock.X + targetBlock.Size * 0.5f, targetBlock.Y + targetBlock.Size * 0.5f);
        Vector2 dir = center - Position;
        float dist = dir.Length();
        if (dist <= 5f)
        {
            IsActive = false;
            targetBlock.Dur -= miningPwr;
            if (targetBlock.Dur <= 0) blocks.Remove(targetBlock);
            return;
        }
        dir = Vector2.Normalize(dir);
        Position += dir * Speed * dt;
    }

    public void Draw()
    {
        if (IsActive) Raylib.DrawCircleV(Position, 4, Color.Black);
    }
}
public class Caravan
{
    private Vector2 position;
    private float width;
    private float height;
    private Color color;

    public Caravan(int screenWidth, int screenHeight)
    {
        // Make the caravan width match the screen width
        width = screenWidth;
        // Height should be substantial - let's say 25% of screen height
        height = screenHeight * 0.25f;
        // Position it at bottom of screen, half-hidden
        position = new Vector2(0, screenHeight + height * 0.5f);
        // Soft earthen brown color
        color = new Color(139, 69, 19, 255); // Saddle brown
    }

    public void Update(float dt)
    {
        // For now, just update position if screen is resized
        position.Y = Raylib.GetScreenHeight() + height * 0.5f;
        width = Raylib.GetScreenWidth();
    }

    public void Draw()
    {
        // Draw as a large oval
        Rectangle rect = new Rectangle(
            position.X,
            position.Y - height,
            width,
            height * 2 // Double height for proper ellipse
        );

        // Draw the ellipse
        Raylib.DrawEllipse(
            (int)(position.X + width / 2), // center X
            (int)position.Y,             // center Y
            width / 2,                     // radius X
            height,                      // radius Y
            color
        );
    }
}
public class Player
{
    public Vector2 Position;
    public Vector2 TargetPosition;
    public float Speed = 200f;
    public float Radius = 16f;
    public const float MINING_RANGE = 64f;
    public int MiningPwr = 10;

    public PlayerState CurrentState { get; private set; } = PlayerState.Idle;
    public bool IsMoving = false;

    float shootTimer;
    const float SHOOT_INTERVAL = 0.25f;
    Color circleColor = Color.Red;
    const float STOP_DISTANCE = 5f;

    public Player(Vector2 startPos)
    {
        Position = startPos;
        TargetPosition = startPos;
    }

    public void Update(float dt, List<Block> blocks)
    {
        UpdateMovement(dt, blocks);
        UpdateState(dt, blocks);
    }

    void UpdateMovement(float dt, List<Block> blocks)
    {
        if (!IsMoving) return;
        Vector2 dir = TargetPosition - Position;
        float dist = dir.Length();
        if (dist <= STOP_DISTANCE)
        {
            Position = TargetPosition;
            IsMoving = false;
            ClampToScreen();
            return;
        }
        dir = Vector2.Normalize(dir);
        Vector2 nextPos = Position + dir * Speed * dt;
        if (!IsOverlappingAnyBlock(nextPos, blocks))
            Position = nextPos;
        else
            IsMoving = false;

        // After movement, clamp
        ClampToScreen();
    }

    void UpdateState(float dt, List<Block> blocks)
    {
        if (IsMoving)
        {
            CurrentState = PlayerState.Walking;
            return;
        }
        Block closest = GetClosestBlockInRange(blocks);
        if (closest != null)
        {
            CurrentState = PlayerState.Mining;
            shootTimer += dt;
            if (shootTimer >= SHOOT_INTERVAL)
            {
                shootTimer = 0f;
                Program.projectiles.Add(new Projectile(Position, closest));
            }
        }
        else
        {
            if (CurrentState != PlayerState.Crafting) CurrentState = PlayerState.Idle;
        }
    }

    Block GetClosestBlockInRange(List<Block> blocks)
    {
        float minDist = float.MaxValue;
        Block closest = null;
        foreach (var b in blocks)
        {
            Vector2 center = new Vector2(b.X + b.Size * 0.5f, b.Y + b.Size * 0.5f);
            float dist = Vector2.Distance(Position, center);
            if (dist <= MINING_RANGE && dist < minDist)
            {
                minDist = dist;
                closest = b;
            }
        }
        return closest;
    }

    public void Draw()
    {
        Raylib.DrawCircleLines((int)Position.X, (int)Position.Y, MINING_RANGE, Color.Yellow);
        switch (CurrentState)
        {
            case PlayerState.Idle: circleColor = Color.Red; break;
            case PlayerState.Walking: circleColor = Color.Green; break;
            case PlayerState.Crafting: circleColor = Color.Blue; break;
            case PlayerState.Mining: circleColor = Color.White; break;
        }
        Raylib.DrawCircle((int)Position.X, (int)Position.Y, Radius, circleColor);

        string st = CurrentState.ToString();
        Vector2 ts = Raylib.MeasureTextEx(Raylib.GetFontDefault(), st, 20, 1);
        Raylib.DrawText(st, (int)(Position.X - ts.X / 2), (int)(Position.Y - Radius - 20), 20, Color.Black);
    }

    public void SetTarget(Vector2 pos)
    {
        TargetPosition = pos;
        IsMoving = true;
    }

    bool IsOverlappingAnyBlock(Vector2 pos, List<Block> blocks)
    {
        foreach (var b in blocks) if (b.OverlapsCircle(pos, Radius)) return true;
        return false;
    }

    void ClampToScreen()
    {
        Position.X = Math.Clamp(Position.X, Radius, Program.refWidth - Radius);
        Position.Y = Math.Clamp(Position.Y, Radius, Program.refHeight - Radius);
    }
}

public enum MinerState
{
    MovingUp,
    Mining
}

public class Miner
{
    public Vector2 Position;
    public float Speed = 150f;
    public float Radius = 16f;
    public const float MINING_RANGE = 64f;
    public int MiningPwr = 10;
    public int invMax = 5;
    public int invCount = 0;
    public MinerState CurrentState { get; private set; } = MinerState.MovingUp;
    


    float shootTimer;
    const float SHOOT_INTERVAL = 0.25f;
    Color circleColor = Color.Purple;
    Vector2 direction;

    public Miner(Vector2 startPos)
    {
        Position = startPos;
        float angleDegrees = Raylib.GetRandomValue(-45, 45);
        float rad = MathF.PI * angleDegrees / 180f;
        Vector2 baseUp = new Vector2(0, -1);
        float sin = MathF.Sin(rad);
        float cos = MathF.Cos(rad);
        direction = new Vector2(baseUp.X * cos - baseUp.Y * sin,
                                baseUp.X * sin + baseUp.Y * cos);
    }

    public void Update(float dt, List<Block> blocks)
    {
        switch (CurrentState)
        {
            case MinerState.MovingUp:
                MoveUp(dt, blocks);
                break;
            case MinerState.Mining:
                Mine(dt, blocks);
                break;
        }
    }

    void MoveUp(float dt, List<Block> blocks)
    {
        Block closest = GetClosestBlockInRange(blocks);
        if (closest != null)
        {
            CurrentState = MinerState.Mining;
            return;
        }
        Vector2 nextPos = Position + direction * Speed * dt;
        if (!IsOverlappingAnyBlock(nextPos, blocks))
            Position = nextPos;

        ClampToScreen();
    }

    void Mine(float dt, List<Block> blocks)
    {
        Block closest = GetClosestBlockInRange(blocks);
        if (closest == null)
        {
            CurrentState = MinerState.MovingUp;
            return;
        }
        shootTimer += dt;
        if (shootTimer >= SHOOT_INTERVAL)
        {
            shootTimer = 0f;
            Program.projectiles.Add(new Projectile(Position, closest));
        }

        // If you'd like, you can drift or remain stationary. 
        // For now, the miner stands still while mining, so clamp in case it was near edge
        ClampToScreen();
    }

    Block GetClosestBlockInRange(List<Block> blocks)
    {
        float minDist = float.MaxValue;
        Block closest = null;
        foreach (var b in blocks)
        {
            Vector2 center = new Vector2(b.X + b.Size * 0.5f, b.Y + b.Size * 0.5f);
            float dist = Vector2.Distance(Position, center);
            if (dist <= MINING_RANGE && dist < minDist)
            {
                minDist = dist;
                closest = b;
            }
        }
        return closest;
    }

    bool IsOverlappingAnyBlock(Vector2 pos, List<Block> blocks)
    {
        foreach (var b in blocks)
            if (b.OverlapsCircle(pos, Radius)) return true;
        return false;
    }

    void ClampToScreen()
    {
        Position.X = Math.Clamp(Position.X, Radius, Program.refWidth - Radius);
        Position.Y = Math.Clamp(Position.Y, Radius, Program.refHeight - Radius);
    }

    public void Draw()
    {
        Raylib.DrawCircleLines((int)Position.X, (int)Position.Y, MINING_RANGE, Color.Yellow);
        Raylib.DrawCircle((int)Position.X, (int)Position.Y, Radius, circleColor);
        string st = CurrentState.ToString();
        Vector2 ts = Raylib.MeasureTextEx(Raylib.GetFontDefault(), st, 20, 1);
        Raylib.DrawText(st, (int)(Position.X - ts.X / 2), (int)(Position.Y - Radius - 20), 20, Color.Black);
    }
}

public class Program
{
    public static int refWidth = 600;
    public static int refHeight = 800;
    static int blockSize = 50;
    public static List<Block> blocks = new List<Block>();
    public static List<Projectile> projectiles = new List<Projectile>();
    public static List<Miner> miners = new List<Miner>();
    static Caravan caravan;
    static Player player;

    public static void Main()
    {
        Raylib.SetConfigFlags(ConfigFlags.ResizableWindow);
        Raylib.InitWindow(refWidth, refHeight, "Yearn with Miners");
        Raylib.SetTargetFPS(60);

        Vector2 playerStartPos = new Vector2(refWidth * 0.5f, refHeight * 0.8f);
        player = new Player(playerStartPos);
        caravan = new Caravan(refWidth, refHeight);
        miners.Add(new Miner(new Vector2(refWidth * 0.3f, refHeight * 0.9f)));
        miners.Add(new Miner(new Vector2(refWidth * 0.7f, refHeight * 0.9f)));
        miners.Add(new Miner(new Vector2(refWidth * 0.3f, refHeight * 0.9f)));
        miners.Add(new Miner(new Vector2(refWidth * 0.7f, refHeight * 0.9f)));


        int cols = refWidth / blockSize;
        int rows = (refHeight / 2 + 20 * blockSize) / blockSize;

        byte baseRed = 100, baseGreen = 100, baseBlue = 100;
        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < cols; x++)
            {
                baseRed = (byte)Math.Clamp(baseRed + Raylib.GetRandomValue(-5, 5), 50, 150);
                baseGreen = (byte)Math.Clamp(baseGreen + Raylib.GetRandomValue(-5, 5), 50, 150);
                baseBlue = (byte)Math.Clamp(baseBlue + Raylib.GetRandomValue(-5, 5), 50, 150);

                Color c = new Color((byte)baseRed, (byte)baseGreen, (byte)baseBlue, (byte)255);
                int blockX = x * blockSize;
                int blockY = (refHeight / 2) - y * blockSize;
                blocks.Add(new Block(blockX, blockY, blockSize, c));
            }
        }

        while (!Raylib.WindowShouldClose())
        {
            float dt = Raylib.GetFrameTime();

            float scaleX = (float)Raylib.GetScreenWidth() / refWidth;
            float scaleY = (float)Raylib.GetScreenHeight() / refHeight;
            Camera2D cam = new Camera2D
            {
                Target = new Vector2(0, 0),
                Offset = new Vector2(0, 0),
                Rotation = 0f,
                Zoom = Math.Min(scaleX, scaleY)
            };

            if (Raylib.IsMouseButtonPressed(MouseButton.Left))
            {
                Vector2 clickPos = GetMousePositionRef();
                bool blockClicked = false;
                foreach (var b in blocks)
                {
                    if (clickPos.X >= b.X && clickPos.X <= b.X + b.Size &&
                        clickPos.Y >= b.Y && clickPos.Y <= b.Y + b.Size)
                    {
                        Vector2 center = new Vector2(b.X + b.Size * 0.5f, b.Y + b.Size * 0.5f);
                        player.SetTarget(center);
                        blockClicked = true;
                        break;
                    }
                }
                if (!blockClicked) player.SetTarget(clickPos);
            }

            player.Update(dt, blocks);
            foreach (var m in miners) m.Update(dt, blocks);
            foreach (var p in projectiles) p.Update(dt, player.MiningPwr, blocks);
            projectiles.RemoveAll(p => !p.IsActive);

            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.RayWhite);
            Raylib.BeginMode2D(cam);

            Raylib.DrawCircleV(player.TargetPosition, 5, Color.Red);
            Raylib.DrawLineV(player.Position, player.TargetPosition, Color.Red);

            foreach (var b in blocks) b.Draw();
            foreach (var p in projectiles) p.Draw();
            caravan.Update(dt);
            caravan.Draw();
            player.Draw();
            foreach (var m in miners) m.Draw();

            Raylib.EndMode2D();

            Raylib.DrawText($"Player Pos: {player.Position.X:F2}, {player.Position.Y:F2}", 10, 10, 20, Color.Black);
            Raylib.DrawText($"Player State: {player.CurrentState}", 10, 30, 20, Color.Black);

            Raylib.EndDrawing();
        }

        Raylib.CloseWindow();
    }

    public static bool IsBlockInMiningRange(Block block)
    {
        if (player == null) return false;
        Vector2 center = new Vector2(block.X + block.Size * 0.5f, block.Y + block.Size * 0.5f);
        float dist = Vector2.Distance(player.Position, center);
        return dist <= Player.MINING_RANGE;
    }

    static Vector2 GetMousePositionRef()
    {
        Vector2 screenPos = Raylib.GetMousePosition();
        float scaleX = (float)Raylib.GetScreenWidth() / refWidth;
        float scaleY = (float)Raylib.GetScreenHeight() / refHeight;
        float zoom = Math.Min(scaleX, scaleY);
        Camera2D cam = new Camera2D { Target = new Vector2(0, 0), Offset = new Vector2(0, 0), Rotation = 0f, Zoom = zoom };
        Vector2 worldPos = Raylib.GetScreenToWorld2D(screenPos, cam);
        if (Raylib.IsMouseButtonPressed(MouseButton.Left))
            Console.WriteLine($"Click at screen: {screenPos}, world: {worldPos}");
        return worldPos;
    }
}
