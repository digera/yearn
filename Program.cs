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
    public float Speed { get; set; }
    public float Size { get; set; }
    public int MiningPower { get; set; }
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






public class Program
{
    public static int refWidth = 600;
    public static int refHeight = 800;
    static int blockSize = 50;
    static float nextSetStartY;
    static byte lastBaseRed = 100;
    static byte lastBaseGreen = 100;
    static byte lastBaseBlue = 100;
    private static SaveSystem saveSystem = new SaveSystem();
    public static List<Block> blocks = new List<Block>();
    public static Caravan caravan;
    public static Player player;
    static Camera2D camera;
    public static int Earth = 0;
    static float caravanY;
    static float caravanSpeed = 40f;
    static float distanceThreshold = 250f;
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
        SaveSystem saveSystem = new SaveSystem();


        if (File.Exists("gamestate.json"))
        {
            saveSystem.LoadGame();
        }
        else
        {
            // Default game setup
            miners.Add(new Miner(new Vector2(refWidth * 0.3f, refHeight * 0.9f), caravan, (int)1));
            miners.Add(new Miner(new Vector2(refWidth * 0.7f, refHeight * 0.9f), caravan, (int)5));
            miners.Add(new Miner(new Vector2(refWidth * 0.3f, refHeight * 0.9f), caravan, (int)10));
            miners.Add(new Miner(new Vector2(refWidth * 0.7f, refHeight * 0.9f), caravan, (int)15));
            miners.Add(new Miner(new Vector2(refWidth * 0.3f, refHeight * 0.9f), caravan, (int)20));
            miners.Add(new Miner(new Vector2(refWidth * 0.7f, refHeight * 0.9f), caravan, (int)25));
        }

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
            saveSystem.Update(dt);
            if (Raylib.IsKeyPressed(KeyboardKey.E))
            {
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

                if (!blockClicked)
                {
                    player.SetTarget(mouseWorld);
                }

                // Check if any miner was clicked
                foreach (var miner in miners)
                {
                    miner.CheckClick(mouseWorld);
                }
            }
            if (blocks.Count < 300)
            {
                GenerateNewBlockSet();
            }
            if (player.Position.Y < nextSetStartY + 800 || miners.Any(miner => miner.Position.Y < nextSetStartY + 800))
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

            string scoreText = $"Earth: {Earth}";
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

    public static void GenerateNewBlockSet()
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