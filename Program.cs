﻿using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text.Json.Serialization;
using Raylib_cs;
using System.Linq;
using System.IO;

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
    Returning,
    Working,
    Idle
}



public class Block
{
    public static float currentDurabilityMultiplier = 1.0f;
    public static int currentYieldBonus = 20;

    public int X;
    public int Y;
    public int Size;
    public int Dur;
    public int Yield;
    public string Mat = "Earth";
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
    public Color Color { get; set; }

    public PickaxeStats(float speed, float size, int miningPower, Color? color = null)
    {
        Speed = speed;
        Size = size;
        MiningPower = miningPower;
        Color = color ?? Color.Black;
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
    public static Camera2D camera;
    public static int Earth = 0;
    static float caravanY;
    static float caravanSpeed = 40f;
    static float distanceThreshold = 250f;
    public static List<Miner> miners = new List<Miner>();
    public static Crusher crusher;

    public static void Main()
    {
        Raylib.SetConfigFlags(ConfigFlags.ResizableWindow);
        Raylib.InitWindow(refWidth, refHeight, "Yearn");
        Raylib.SetTargetFPS(60);

        Vector2 playerStartPos = new Vector2(refWidth * 0.5f, refHeight * 0.8f);
        caravan = new Caravan(refWidth, refHeight);
        crusher = new Crusher(caravan, StoneType.Earth, StoneType.Stone);
        player = new Player(playerStartPos, caravan);

        camera = new Camera2D
        {
            Target = playerStartPos,
            Offset = new Vector2(refWidth / 2, refHeight / 2),
            Rotation = 0f,
            Zoom = 1f
        };

        caravanY = caravan.GetY();
        saveSystem = new SaveSystem();

        if (File.Exists("gamestate.json"))
        {
            saveSystem.LoadGame();
        }
        else
        {
            miners.Add(new Miner(
                new Vector2(refWidth * 0.3f, refHeight * 0.9f),
                caravan,
                1,
                255f,
                Names.GetUniqueName()
            ));
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
            camera.Zoom = (float)Raylib.GetScreenHeight() / refHeight;
            camera.Offset = new Vector2(Raylib.GetScreenWidth() / 2f, Raylib.GetScreenHeight() / 2f);

            saveSystem.Update(dt);
            crusher.Update(dt);

            if (Raylib.IsKeyPressed(KeyboardKey.E))
            {
                miners.Add(new Miner(
                    GetMouseWorld(),
                    caravan,
                    Random.Shared.Next(1, 10),
                    Random.Shared.Next(75, 200),
                    Names.GetUniqueName()
                ));
            }

            if (Raylib.IsMouseButtonDown(MouseButton.Left))
            {
                Vector2 mouseWorld = GetMousePositionRef();
                player.SetTarget(mouseWorld);
            }
            // oh my god send help
            if (Raylib.IsMouseButtonPressed(MouseButton.Left))
            {
                Vector2 mouseWorld = GetMousePositionRef();
                if (miners.Count > 0)
                {
                    foreach (var miner in miners)
                    {
                        miner.CheckClick(mouseWorld);
                    }
                }
                foreach (var miner in miners)
                {
                    miner.CheckClick(mouseWorld);
                }


                if (crusher.CheckUpgradeClick(mouseWorld, out int upgradeIndex))
                {
                    if (upgradeIndex == 0)
                    {
                        crusher.UpgradeHopper();
                    }
                    else if (upgradeIndex == 1)
                    {
                        crusher.UpgradeConversion();
                    }
                }
                else if (crusher.CheckClick(mouseWorld))
                {
                    if (miners.Count > 0)
                    {
                        int index = Random.Shared.Next(miners.Count);
                        miners[index].CurrentState = MinerState.Working;
       
                    }
                }
               
                else if (caravan.CheckClick(mouseWorld))
                {
                    player.CurrentState = PlayerState.Crafting;
                    player.IsMoving = false;
                    player.SetTarget(caravan.Center);
                }
                else
                {
                    player.CurrentState = PlayerState.Walking;
                    player.SetTarget(mouseWorld);

                }
            }

            if (player.Position.Y >= caravan.Y - 100)
            {
                player.Position = new Vector2(player.Position.X, caravan.Y - 100);
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
            foreach (var m in miners)
            {
                m.Update(dt, blocks);
            }

            MoveCaravanUpIfNeeded(dt);
            UpdateCamera(dt);

            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.RayWhite);
            Raylib.BeginMode2D(camera);

            Raylib.DrawCircleV(player.TargetPosition, 5, Color.Red);
            Raylib.DrawLineV(player.Position, player.TargetPosition, Color.Red);

            foreach (var b in blocks)
            {
                b.Draw();
            }
            caravan.Draw();
            crusher.Draw();
            player.Draw(dt);
            foreach (var m in miners)
            {
                m.Draw(dt);
            }

            Raylib.EndMode2D();

            Raylib.DrawText($"Player Pos: {player.Position.X:F2}, {player.Position.Y:F2}", 10, 10, 20, Color.Black);
            Raylib.DrawText($"Player State: {player.CurrentState}", 10, 30, 20, Color.Black);
            Raylib.DrawText($"Blocks: {blocks.Count}", 10, 50, 20, Color.Black);
            Raylib.DrawText($"Miners: {miners.Count}", 10, 70, 20, Color.Black);

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
        Block.currentYieldBonus += (int)(Block.currentYieldBonus * 1.1f);

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

    public static Vector2 GetMouseWorld()
    {
        Vector2 screenMouse = Raylib.GetMousePosition();
        return Raylib.GetScreenToWorld2D(screenMouse, camera);
    }

    static void UpdateCamera(float dt)
    {
        float smoothSpeed = 5f * dt;
        Vector2 desiredPosition = player.CurrentState == PlayerState.Crafting
                                    ? caravan.Center
                                    : player.Position;

        if (player.CurrentState != PlayerState.Crafting)
        {
            float minX = refWidth / 2;
            float maxX = refWidth / 2;
            desiredPosition.X = Math.Clamp(desiredPosition.X, minX, maxX);

            float maxY = caravan.Y - caravan.height * 2;
            desiredPosition.Y = Math.Min(desiredPosition.Y, maxY);
        }

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