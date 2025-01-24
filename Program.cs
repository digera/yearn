using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

public enum PlayerState
{
    Idle,
    Walking,
    Crafting,
    Mining,
    Riding
}

public class Block
{
    public int X;
    public int Y;
    public int Size;
    public int Dur = 100;
    public int Yield = 50;
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
                Raylib.DrawRectangleLines(
                    X - i,
                    Y - i,
                    Size + (i * 2),
                    Size + (i * 2),
                    Color.Yellow
                );
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
    Miner owner;

    public Projectile(Vector2 startPos, Block block, Miner owner = null)
    {
        Position = startPos;
        targetBlock = block;
        this.owner = owner;
    }

    public void Update(float dt, int miningPwr, List<Block> blocks)
    {
        if (!IsActive || targetBlock == null) return;

        Vector2 center = new Vector2(
            targetBlock.X + targetBlock.Size * 0.5f,
            targetBlock.Y + targetBlock.Size * 0.5f
        );
        Vector2 dir = center - Position;
        float dist = dir.Length();

        if (dist <= 5f)
        {
            IsActive = false;
            targetBlock.Dur -= miningPwr;
            if (targetBlock.Dur <= 0)
            {
                if (owner != null)
                {
                    owner.UpdateInventory(targetBlock);
                }
                else
                {
                    Program.GlobalScore += targetBlock.Yield;
                }
                blocks.Remove(targetBlock);
            }
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
    public float height;
    private Color color;

    // Added Y property for position access
    public float Y => position.Y;

    public Caravan(int screenWidth, int screenHeight)
    {
        width = screenWidth;
        height = screenHeight * 0.25f;
        position = new Vector2(0, screenHeight - height * 0.05f);
        color = new Color((byte)139, (byte)69, (byte)19, (byte)255);
    }

    public void SetY(float y) => position.Y = y;
    public float GetY() => position.Y;

    public void Update(float dt) => width = Raylib.GetScreenWidth();

    public void Draw()
    {
        Raylib.DrawEllipse(
            (int)(position.X + width / 2),
            (int)position.Y,
            width / 2,
            height,
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

    public Player(Vector2 startPos, Caravan caravan)
    {
        Position = startPos;
        TargetPosition = startPos;
        this.caravan = caravan;
    }
    private Caravan caravan;

    public void Update(float dt, List<Block> blocks)
    {
        UpdateMovement(dt, blocks);
        UpdateState(dt, blocks);
        if (Position.Y >= caravan.Y && CurrentState != PlayerState.Riding)
        {
            CurrentState= PlayerState.Riding;
            Position = new Vector2(Position.X, caravan.Y);
        }
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
                Program.projectiles.Add(new Projectile(Position, closest, null));
            }
        }
        else
        {
            if (CurrentState != PlayerState.Crafting)
                CurrentState = PlayerState.Idle;
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
            case PlayerState.Mining:
                circleColor = Color.Orange;
                break;
        }

        Raylib.DrawCircle((int)Position.X, (int)Position.Y, Radius, circleColor);

        string st = CurrentState.ToString();
        Vector2 ts = Raylib.MeasureTextEx(Raylib.GetFontDefault(), st, 20, 1);
        Raylib.DrawText(
            st,
            (int)(Position.X - ts.X / 2),
            (int)(Position.Y - Radius - 20),
            20,
            Color.Black
        );
    }

    public void SetTarget(Vector2 pos)
    {
        TargetPosition = pos;
        IsMoving = true;
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
    }
}

public enum MinerState
{
    MovingUp,
    Mining,
    Returning
}

public class Miner
{
    public Vector2 Position;
    public float Speed = 150f;
    public float Radius = 16f;
    public const float MINING_RANGE = 64f;
    public int MiningPwr = 20;
    public int invCount = 0;
    public int invMax = 100;
    private float attn = 0;
    private const float attnSpan = 2.0f;
    private Random random = new Random();
    private Caravan caravan;

    public MinerState CurrentState { get; private set; } = MinerState.MovingUp;

    float shootTimer;
    const float SHOOT_INTERVAL = 0.25f;
    Color circleColor = Color.Purple;
    Vector2 direction;

    public Miner(Vector2 startPos, Caravan caravan)
    {
        Position = startPos;
        this.caravan = caravan;

        float angleRadians = (-90 + Raylib.GetRandomValue(-60, 60)) * MathF.PI / 180f;
        direction = new Vector2(MathF.Cos(angleRadians), MathF.Sin(angleRadians));
        direction = Vector2.Normalize(direction);
    }

    public void Update(float dt, List<Block> blocks)
    {
        attn += dt;

        if (attn >= attnSpan)
        {
            attn = 0;
            float angleRadians = (-90 + (float)(random.NextDouble() - 0.5) * 120) * MathF.PI / 180f;
            direction = new Vector2(MathF.Cos(angleRadians), MathF.Sin(angleRadians));
            direction = Vector2.Normalize(direction);
        }

        switch (CurrentState)
        {
            case MinerState.MovingUp:
                MoveUp(dt, blocks);
                break;
            case MinerState.Mining:
                Mine(dt, blocks);
                break;
            case MinerState.Returning:
                ReturnToCaravan(dt);
                break;
        }
    }

    void MoveUp(float dt, List<Block> blocks)
    {
        Block closest = GetClosestBlockInRange(blocks);

        if (closest != null)
        {
            Vector2 blockCenter = new Vector2(closest.X + closest.Size * 0.5f, closest.Y + closest.Size * 0.5f);
            direction = Vector2.Normalize(blockCenter - Position);
            CurrentState = MinerState.Mining;
            return;
        }

        Vector2 nextPos = Position + direction * Speed * dt;
        if (!IsOverlappingAnyBlock(nextPos, blocks))
        {
            Position = nextPos;
        }

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
            Program.projectiles.Add(new Projectile(Position, closest, this));
        }
        ClampToScreen();
    }

    void ReturnToCaravan(float dt)
    {
        if (IsOverlappingCaravan())
        {
            Program.GlobalScore += invCount;
            invCount = 0;
            CurrentState = MinerState.MovingUp;
            return;
        }

        Vector2 targetPos = new Vector2(Program.refWidth / 2, caravan.Y);
        Vector2 dir = targetPos - Position;
        dir = Vector2.Normalize(dir);
        Position += dir * Speed * dt;
    }

    bool IsOverlappingCaravan()
    {
        return Position.Y >= caravan.Y - Radius;
    }

    public void UpdateInventory(Block block)
    {
        invCount += block.Yield;
        if (invCount >= invMax)
        {
            CurrentState = MinerState.Returning;
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

    bool IsOverlappingAnyBlock(Vector2 pos, List<Block> blocks)
    {
        foreach (var b in blocks)
            if (b.OverlapsCircle(pos, Radius)) return true;
        return false;
    }

    void ClampToScreen()
    {
        Position.X = Math.Clamp(Position.X, Radius, Program.refWidth - Radius);
    }

    public void Draw()
    {
        Raylib.DrawCircleLines((int)Position.X, (int)Position.Y, MINING_RANGE, Color.Yellow);
        Raylib.DrawCircle((int)Position.X, (int)Position.Y, Radius, circleColor);

        string st = $"{CurrentState} ({invCount}/{invMax})";
        Vector2 ts = Raylib.MeasureTextEx(Raylib.GetFontDefault(), st, 20, 1);
        Raylib.DrawText(
            st,
            (int)(Position.X - ts.X / 2),
            (int)(Position.Y - Radius - 20),
            20,
            Color.Black
        );
    }
}

public class Program
{
    public static int refWidth = 600;
    public static int refHeight = 800;
    static int blockSize = 50;
    static float nextSetStartY;
    static byte lastBaseRed = 100;
    static byte lastBaseGreen = 100;
    static byte lastBaseBlue = 100;

    public static List<Block> blocks = new List<Block>();
    public static List<Projectile> projectiles = new List<Projectile>();
    public static List<Miner> miners = new List<Miner>();
    static Caravan caravan;
    static Player player;
    static Camera2D camera;
    public static int GlobalScore = 0;
    static float caravanY;
    static float caravanSpeed = 20f;
    static float distanceThreshold = 150f;

    public static void Main()
    {
        Raylib.InitWindow(refWidth, refHeight, "Yearn");
        Raylib.SetTargetFPS(60);
        

        Vector2 playerStartPos = new Vector2(refWidth * 0.5f, refHeight * 0.8f);
        caravan = new Caravan(refWidth, refHeight);
        player = new Player(playerStartPos, caravan);

        // Initialize camera
        camera = new Camera2D
        {
            Target = playerStartPos,
            Offset = new Vector2(refWidth / 2, refHeight / 2),
            Rotation = 0f,
            Zoom = 1f
        };

        
        caravanY = caravan.GetY();

        miners.Add(new Miner(new Vector2(refWidth * 0.3f, refHeight * 0.9f), caravan));
        miners.Add(new Miner(new Vector2(refWidth * 0.7f, refHeight * 0.9f), caravan));
        miners.Add(new Miner(new Vector2(refWidth * 0.3f, refHeight * 0.9f), caravan));
        miners.Add(new Miner(new Vector2(refWidth * 0.7f, refHeight * 0.9f), caravan));
        miners.Add(new Miner(new Vector2(refWidth * 0.3f, refHeight * 0.9f), caravan));
        miners.Add(new Miner(new Vector2(refWidth * 0.7f, refHeight * 0.9f), caravan));

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
        nextSetStartY = 400 - (rows - 1) * blockSize;
        lastBaseRed = 100;
        lastBaseGreen = 100;
        lastBaseBlue = 100;

        while (!Raylib.WindowShouldClose())
        {
            float dt = Raylib.GetFrameTime();

            // Handle input
            if (Raylib.IsKeyPressed(KeyboardKey.E))
            {
                Console.WriteLine("keyboard button E pressed");
                miners.Add(new Miner(GetMouseWorld(), caravan));
            }
            if (Raylib.IsMouseButtonPressed(MouseButton.Left))
            {
                Vector2 mouseWorld = GetMousePositionRef();
                bool blockClicked = false;
                foreach (var b in blocks)
                {
                    if (mouseWorld.X >= b.X && mouseWorld.X <= b.X + b.Size &&
                        mouseWorld.Y >= b.Y && mouseWorld.Y <= b.Y + b.Size)
                    {
                        Vector2 center = new Vector2(b.X + b.Size * 0.5f, b.Y + b.Size * 0.5f);
                        player.SetTarget(center);
                        blockClicked = true;
                        break;
                    }
                }



                if (!blockClicked) player.SetTarget(mouseWorld);
            }
            if (blocks.Count < 300)
            {
                GenerateNewBlockSet();
            }
            player.Update(dt, blocks);
            foreach (var m in miners) m.Update(dt, blocks);
            foreach (var p in projectiles) p.Update(dt, player.MiningPwr, blocks);
            projectiles.RemoveAll(p => !p.IsActive);

            MoveCaravanUpIfNeeded(dt);
            caravan.Update(dt);
            UpdateCamera(dt);

            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.RayWhite);
            Raylib.BeginMode2D(camera);

            Raylib.DrawCircleV(player.TargetPosition, 5, Color.Red);
            Raylib.DrawLineV(player.Position, player.TargetPosition, Color.Red);

            foreach (var b in blocks) b.Draw();
            foreach (var proj in projectiles) proj.Draw();

            caravan.Draw();
            player.Draw();
            foreach (var m in miners) m.Draw();

            Raylib.EndMode2D();

            // UI
            Raylib.DrawText($"Player Pos: {player.Position.X:F2}, {player.Position.Y:F2}",
                            10, 10, 20, Color.Black);
            Raylib.DrawText($"Player State: {player.CurrentState}",
                            10, 30, 20, Color.Black);
            Raylib.DrawText($"Blocks: {blocks.Count}", 10, 50, 20, Color.Black);


            string scoreText = $"Total Resources: {GlobalScore}";
            Vector2 scoreSize = Raylib.MeasureTextEx(Raylib.GetFontDefault(), scoreText, 30, 1);
            float screenCenterX = Raylib.GetScreenWidth() / 2f;
            Raylib.DrawText(
                scoreText,
                (int)(screenCenterX - scoreSize.X / 2),
                Raylib.GetScreenHeight() - 50,
                30,
                Color.DarkGray
            );

            Raylib.EndDrawing();
        }

        Raylib.CloseWindow();
    }
    private static void GenerateNewBlockSet()
    {
        const int newRows = 28;  
        const int cols = 12;     
        byte baseRed = lastBaseRed;
        byte baseGreen = lastBaseGreen;
        byte baseBlue = lastBaseBlue;

        float startY = nextSetStartY - blockSize; 

        for (int y = 0; y < newRows; y++)
        {
            for (int x = 0; x < cols; x++)
            {
                baseRed = (byte)Math.Clamp(baseRed + Raylib.GetRandomValue(-5, 5), 50, 150);
                baseGreen = (byte)Math.Clamp(baseGreen + Raylib.GetRandomValue(-5, 5), 50, 150);
                baseBlue = (byte)Math.Clamp(baseBlue + Raylib.GetRandomValue(-5, 5), 50, 150);

                int blockX = x * blockSize;
                int blockY = (int)(startY - y * blockSize);

                blocks.Add(new Block(
                    blockX,
                    blockY,
                    blockSize,
                    new Color((byte)baseRed, (byte)baseGreen, (byte)baseBlue, (byte)255)
                ));
            }
        }


        lastBaseRed = baseRed;
        lastBaseGreen = baseGreen;
        lastBaseBlue = baseBlue;
        nextSetStartY = startY - (newRows - 1) * blockSize;  
    }

    static Vector2 GetMouseWorld()
    {
        Vector2 screenMouse = Raylib.GetMousePosition();
        return Raylib.GetScreenToWorld2D(screenMouse, camera);
    }

    static void UpdateCamera(float dt)
    {
        float smoothSpeed = 5f * dt;
        Vector2 desiredPosition = player.Position;

        float minX = refWidth / 2;
        float maxX = refWidth / 2;
        float minY = refHeight / 2;
        float maxY = -blocks.Count * blockSize + refHeight / 2;
        desiredPosition.X = Math.Clamp(desiredPosition.X, minX, maxX);
        camera.Target = Vector2.Lerp(camera.Target, desiredPosition, smoothSpeed);
    }

    static void MoveCaravanUpIfNeeded(float dt)
    {
        float caravanTopEdge = caravanY - caravan.height;
        float minDist = float.MaxValue;
        Block closest = null;

        foreach (var b in blocks)
        {
            float blockBottomY = b.Y + b.Size;
            if (blockBottomY < caravanTopEdge)
            {
                float dist = caravanTopEdge - blockBottomY;
                if (dist < minDist)
                {
                    minDist = dist;
                    closest = b;
                }
            }
        }

        if (closest != null && minDist > distanceThreshold)
        {
            caravanY -= caravanSpeed * dt;
        }

        caravan.SetY(caravanY);
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
        return Raylib.GetScreenToWorld2D(screenPos, camera);
    }
}