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

public enum MinerState
{
    MovingUp,
    Mining,
    Returning
}

public class Block
{
    public static float currentDurabilityMultiplier = 1.0f;
    public static int currentYieldBonus = 0;

    public int X;
    public int Y;
    public int Size;
    public int Dur;
    public int Yield;
    public string Mat = "earth";
    public Color Color;

    public Block(int x, int y, int size, Color color, float durabilityMultiplier, int yieldBonus)
    {
        X = x;
        Y = y;
        Size = size;
        Color = color;
        Dur = (int)(100 * durabilityMultiplier);
        Yield = 1 + yieldBonus;
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

public struct PickaxeStats
{
    public float Speed;
    public float Size;
    public int MiningPower;
    public Color Color;

    public PickaxeStats(float speed = 300f, float size = 4f, int miningPower = 1, Color? color = null)
    {
        Speed = speed;
        Size = size;
        MiningPower = miningPower;
        Color = color ?? Color.Black;
    }
}

public class Caravan
{
    private Vector2 position;
    private float width;
    public float height;
    private Color color;

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
    public PlayerState CurrentState { get; private set; } = PlayerState.Idle;
    public bool IsMoving = false;

    private PickaxeStats pickaxeStats;
    private List<(Vector2 Position, Block Target)> activePickaxes = new List<(Vector2, Block)>();
    private float pickaxeTimer;
    private const float PICKAXE_INTERVAL = 0.25f;
    private Caravan caravan;
    private int basePwr = 20;

    public Player(Vector2 startPos, Caravan caravan)
    {
        Position = startPos;
        TargetPosition = startPos;
        this.caravan = caravan;
        this.basePwr = basePwr;
        pickaxeStats = new PickaxeStats(
            speed: 300f,
            size: 4f,
            miningPower: 2,
            color: Color.Red
        );
    }

    public void Update(float dt, List<Block> blocks)
    {
        UpdateMovement(dt, blocks);
        UpdateState(dt, blocks);
        UpdatePickaxes(dt, blocks);

        if (Position.Y >= caravan.Y && CurrentState != PlayerState.Riding)
        {
            CurrentState = PlayerState.Riding;
            Position = new Vector2(Position.X, caravan.Y);
        }
    }

    private void UpdatePickaxes(float dt, List<Block> blocks)
    {
        for (int i = activePickaxes.Count - 1; i >= 0; i--)
        {
            var (pos, target) = activePickaxes[i];

            if (!blocks.Contains(target))
            {
                activePickaxes.RemoveAt(i);
                continue;
            }

            Vector2 targetCenter = new Vector2(
                target.X + target.Size * 0.5f,
                target.Y + target.Size * 0.5f
            );

            Vector2 dir = Vector2.Normalize(targetCenter - pos);
            Vector2 newPos = pos + dir * pickaxeStats.Speed * dt;

            float dist = Vector2.Distance(newPos, targetCenter);
            if (dist <= 5f)
            {
                target.Dur -= pickaxeStats.MiningPower + basePwr;
                if (target.Dur <= 0)
                {
                    Program.GlobalScore += target.Yield;
                    blocks.Remove(target);
                }
                activePickaxes.RemoveAt(i);
            }
            else
            {
                activePickaxes[i] = (newPos, target);
            }
        }

        if (CurrentState == PlayerState.Mining)
        {
            pickaxeTimer += dt;
            if (pickaxeTimer >= PICKAXE_INTERVAL)
            {
                pickaxeTimer = 0f;
                Block target = GetClosestBlockInRange(blocks);
                if (target != null)
                {
                    activePickaxes.Add((Position, target));
                }
            }
        }
    }

    public void Draw()
    {
        foreach (var (pos, _) in activePickaxes)
        {
            Raylib.DrawCircleV(pos, pickaxeStats.Size, pickaxeStats.Color);
        }

        Raylib.DrawCircleLines((int)Position.X, (int)Position.Y, MINING_RANGE, Color.Yellow);

        Color circleColor = CurrentState switch
        {
            PlayerState.Idle => Color.Red,
            PlayerState.Walking => Color.Green,
            PlayerState.Crafting => Color.Blue,
            PlayerState.Mining => Color.Orange,
            _ => Color.Red
        };

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

    private void UpdateMovement(float dt, List<Block> blocks)
    {
        if (!IsMoving) return;
        Vector2 dir = TargetPosition - Position;
        float dist = dir.Length();
        if (dist <= 5f)
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

    private void UpdateState(float dt, List<Block> blocks)
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
        }
        else
        {
            if (CurrentState != PlayerState.Crafting)
                CurrentState = PlayerState.Idle;
        }
    }

    private Block GetClosestBlockInRange(List<Block> blocks)
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

    private bool IsOverlappingAnyBlock(Vector2 pos, List<Block> blocks)
    {
        foreach (var b in blocks)
            if (b.OverlapsCircle(pos, Radius)) return true;
        return false;
    }

    private void ClampToScreen()
    {
        Position.X = Math.Clamp(Position.X, Radius, Program.refWidth - Radius);
    }
}

public class Miner
{
    public Vector2 Position;
    public float Speed = 150f;
    public float Radius = 16f;
    public const float MINING_RANGE = 64f;
    public int invCount = 0;
    public int invMax = 10;
    private float attn = 0;
    private const float attnSpan = 2.0f;
    private Random random = new Random();
    private Caravan caravan;
    private Vector2 direction;
    private int basePwr = 1;
    private Color circleColor = Color.Purple;

    public MinerState CurrentState { get; private set; } = MinerState.MovingUp;

    private PickaxeStats pickaxeStats;
    private List<(Vector2 Position, Block Target)> activePickaxes = new List<(Vector2, Block)>();
    private float pickaxeTimer;
    private const float PICKAXE_INTERVAL = 0.25f;

    public Miner(Vector2 startPos, Caravan caravan, int basePwr)
    {
        Position = startPos;
        this.caravan = caravan;
        this.basePwr = basePwr;

        pickaxeStats = new PickaxeStats(
            speed: 250f,
            size: 3f,
            miningPower: 1,
            color: Color.Purple
        );

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

        UpdatePickaxes(dt, blocks);
    }

    private void UpdatePickaxes(float dt, List<Block> blocks)
    {
        for (int i = activePickaxes.Count - 1; i >= 0; i--)
        {
            var (pos, target) = activePickaxes[i];

            if (!blocks.Contains(target))
            {
                activePickaxes.RemoveAt(i);
                continue;
            }

            Vector2 targetCenter = new Vector2(
                target.X + target.Size * 0.5f,
                target.Y + target.Size * 0.5f
            );

            Vector2 dir = Vector2.Normalize(targetCenter - pos);
            Vector2 newPos = pos + dir * pickaxeStats.Speed * dt;

            float dist = Vector2.Distance(newPos, targetCenter);
            if (dist <= 5f)
            {
                target.Dur -= pickaxeStats.MiningPower + basePwr;
                if (target.Dur <= 0)
                {
                    UpdateInventory(target);
                    blocks.Remove(target);
                }
                activePickaxes.RemoveAt(i);
            }
            else
            {
                activePickaxes[i] = (newPos, target);
            }
        }

        if (CurrentState == MinerState.Mining)
        {
            pickaxeTimer += dt;
            if (pickaxeTimer >= PICKAXE_INTERVAL)
            {
                pickaxeTimer = 0f;
                Block target = GetClosestBlockInRange(blocks);
                if (target != null)
                {
                    activePickaxes.Add((Position, target));
                }
            }
        }
    }

    private void MoveUp(float dt, List<Block> blocks)
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

    private void Mine(float dt, List<Block> blocks)
    {
        Block closest = GetClosestBlockInRange(blocks);
        if (closest == null)
        {
            CurrentState = MinerState.MovingUp;
            return;
        }
    }

    private void ReturnToCaravan(float dt)
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

    private bool IsOverlappingCaravan()
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

    private Block GetClosestBlockInRange(List<Block> blocks)
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

    private bool IsOverlappingAnyBlock(Vector2 pos, List<Block> blocks)
    {
        foreach (var b in blocks)
            if (b.OverlapsCircle(pos, Radius)) return true;
        return false;
    }

    private void ClampToScreen()
    {
        Position.X = Math.Clamp(Position.X, Radius, Program.refWidth - Radius);
    }

    public void Draw()
    {
        foreach (var (pos, _) in activePickaxes)
        {
            Raylib.DrawCircleV(pos, pickaxeStats.Size, pickaxeStats.Color);
        }

        Raylib.DrawCircleLines((int)Position.X, (int)Position.Y, MINING_RANGE, Color.Yellow);
        Raylib.DrawCircle((int)Position.X, (int)Position.Y, Radius, circleColor);

        string st = $"{CurrentState} ({invCount}/{invMax}) Pwr:{basePwr + pickaxeStats.MiningPower}";
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
    static Caravan caravan;
    static Player player;
    static Camera2D camera;
    public static int GlobalScore = 0;
    static float caravanY;
    static float caravanSpeed = 40f;
    static float distanceThreshold = 150f;
    public static List<Miner> miners = new List<Miner>();

    public static void Main()
    {
        Raylib.InitWindow(refWidth, refHeight, "Yearn");
        Raylib.SetTargetFPS(60);

        Vector2 playerStartPos = new Vector2(refWidth * 0.5f, refHeight * 0.8f);
        caravan = new Caravan(refWidth, refHeight);
        player = new Player(playerStartPos, caravan);

        camera = new Camera2D
        {
            Target = playerStartPos,
            Offset = new Vector2(refWidth / 2, refHeight / 2),
            Rotation = 0f,
            Zoom = 1f
        };

        caravanY = caravan.GetY();

        miners.Add(new Miner(new Vector2(refWidth * 0.3f, refHeight * 0.9f), caravan, (int)1));
        miners.Add(new Miner(new Vector2(refWidth * 0.7f, refHeight * 0.9f), caravan, (int)5));
        miners.Add(new Miner(new Vector2(refWidth * 0.3f, refHeight * 0.9f), caravan, (int)10));
        miners.Add(new Miner(new Vector2(refWidth * 0.7f, refHeight * 0.9f), caravan, (int)15));
        miners.Add(new Miner(new Vector2(refWidth * 0.3f, refHeight * 0.9f), caravan, (int)20));
        miners.Add(new Miner(new Vector2(refWidth * 0.7f, refHeight * 0.9f), caravan, (int)25));

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
                blocks.Add(new Block(
                                blockX,
                                blockY,
                                blockSize,
                                c,
                                Block.currentDurabilityMultiplier,
                                Block.currentYieldBonus));
            }
        }
        nextSetStartY = 400 - (rows - 1) * blockSize;
        lastBaseRed = 100;
        lastBaseGreen = 100;
        lastBaseBlue = 100;

        while (!Raylib.WindowShouldClose())
        {
            float dt = Raylib.GetFrameTime();

            if (Raylib.IsKeyPressed(KeyboardKey.E))
            {
                //Console.WriteLine("keyboard button E pressed");
                miners.Add(new Miner(GetMouseWorld(), caravan, Random.Shared.Next(1, 10)));
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

            MoveCaravanUpIfNeeded(dt);
            caravan.Update(dt);
            UpdateCamera(dt);

            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.RayWhite);
            Raylib.BeginMode2D(camera);

            Raylib.DrawCircleV(player.TargetPosition, 5, Color.Red);
            Raylib.DrawLineV(player.Position, player.TargetPosition, Color.Red);

            foreach (var b in blocks) b.Draw();
            caravan.Draw();
            player.Draw();
            foreach (var m in miners) m.Draw();

            Raylib.EndMode2D();

            Raylib.DrawText($"Player Pos: {player.Position.X:F2}, {player.Position.Y:F2}",
                            10, 10, 20, Color.Black);
            Raylib.DrawText($"Player State: {player.CurrentState}",
                            10, 30, 20, Color.Black);
            Raylib.DrawText($"Blocks: {blocks.Count}", 10, 50, 20, Color.Black);
            Raylib.DrawText($"Minors: {miners.Count}", 10, 70, 20, Color.Black);

            string scoreText = $"Earth: {GlobalScore}";
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
        Block.currentDurabilityMultiplier *= 1.1f;
        Block.currentYieldBonus += 1;

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
                    new Color((byte)baseRed, (byte)baseGreen, (byte)baseBlue, (byte)255),
                    Block.currentDurabilityMultiplier,
                    Block.currentYieldBonus
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
        desiredPosition.X = Math.Clamp(desiredPosition.X, minX, maxX);
        float caravanHalfHeight = caravan.height;
        float maxY = caravan.Y - caravanHalfHeight * 2; 
        float minY = float.MinValue; 
        desiredPosition.Y = Math.Min(desiredPosition.Y, maxY);
        camera.Target = Vector2.Lerp(camera.Target, desiredPosition, smoothSpeed);
        camera.Target.Y = Math.Min(camera.Target.Y, maxY);
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