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

        // Highlight if in mining range
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

    public bool OverlapsCircle(Vector2 playerPos, float radius)
    {
        float left = X;
        float right = X + Size;
        float top = Y;
        float bottom = Y + Size;

        float closestX = Math.Clamp(playerPos.X, left, right);
        float closestY = Math.Clamp(playerPos.Y, top, bottom);

        float distanceX = playerPos.X - closestX;
        float distanceY = playerPos.Y - closestY;
        float distanceSquared = distanceX * distanceX + distanceY * distanceY;

        return distanceSquared <= (radius * radius);
    }
}

public class Player
{
    public Vector2 Position;
    public Vector2 TargetPosition;
    public float Speed = 200.0f;
    public float Radius = 16f;
    private Color circleColor = Color.Red;
    private const float STOP_DISTANCE = 5.0f;
    public const float MINING_RANGE = 64f;

    public PlayerState CurrentState { get; private set; } = PlayerState.Idle;
    public bool IsMoving = false;

    public Player(Vector2 startPosition)
    {
        Position = startPosition;
        TargetPosition = startPosition;
    }

    public void Update(float deltaTime, List<Block> blocks)
    {
        UpdateMovement(deltaTime, blocks);
        UpdateState(blocks);
    }

    private void UpdateMovement(float deltaTime, List<Block> blocks)
    {
        if (!IsMoving) return;

        Vector2 direction = TargetPosition - Position;
        float distance = direction.Length();

        if (distance <= STOP_DISTANCE)
        {
            Position = TargetPosition;
            IsMoving = false;
            return;
        }

        direction = Vector2.Normalize(direction);
        Vector2 newPosition = Position + direction * Speed * deltaTime;

        if (!IsOverlappingAnyBlock(newPosition, blocks))
        {
            Position = newPosition;
        }
        else
        {
            IsMoving = false;
        }
    }

    private void UpdateState(List<Block> blocks)
    {
        // If we're moving, we're walking
        if (IsMoving)
        {
            CurrentState = PlayerState.Walking;
            return;
        }

        // We stop walking here. Check if any block is close enough to mine
        foreach (var block in blocks)
        {
            Vector2 blockCenter = new Vector2(block.X + block.Size / 2, block.Y + block.Size / 2);
            float dist = Vector2.Distance(Position, blockCenter);
            if (dist <= MINING_RANGE)
            {
                CurrentState = PlayerState.Mining;
                return;
            }
        }

        // Otherwise, if we aren't crafting, we go idle
        if (CurrentState != PlayerState.Crafting)
        {
            CurrentState = PlayerState.Idle;
        }
    }

    public void Draw()
    {
        // Draw mining range circle
        Raylib.DrawCircleLines(
            (int)Position.X,
            (int)Position.Y,
            MINING_RANGE,
            Color.Yellow
        );

        switch (CurrentState)
        {
            case PlayerState.Idle:
                circleColor = Color.Red;
                break;
            case PlayerState.Walking:
                circleColor = Color.Green;
                break;
            case PlayerState.Crafting:
                circleColor = Color.Blue;
                break;
            case PlayerState.Mining:
                circleColor = Color.Orange;
                break;
        }

        Raylib.DrawCircle((int)Position.X, (int)Position.Y, Radius, circleColor);

        string stateText = CurrentState.ToString();
        Vector2 textSize = Raylib.MeasureTextEx(Raylib.GetFontDefault(), stateText, 20, 1);
        Raylib.DrawText(
            stateText,
            (int)(Position.X - textSize.X / 2),
            (int)(Position.Y - Radius - 20),
            20,
            Color.Black
        );
    }

    public void SetTarget(Vector2 newTarget)
    {
        TargetPosition = newTarget;
        IsMoving = true;
    }

    private bool IsOverlappingAnyBlock(Vector2 nextPos, List<Block> blocks)
    {
        foreach (var block in blocks)
        {
            if (block.OverlapsCircle(nextPos, Radius))
                return true;
        }
        return false;
    }
}

public class Program
{
    static int refWidth = 600;
    static int refHeight = 800;
    static int blockSize = 50;
    static List<Block> blocks = new List<Block>();
    static Player player;

    public static void Main()
    {
        Raylib.SetConfigFlags(ConfigFlags.ResizableWindow);
        Raylib.InitWindow(refWidth, refHeight, "Yearn");
        Raylib.SetTargetFPS(60);

        Vector2 playerStartPos = new Vector2(refWidth * 0.5f, refHeight * 0.8f);
        player = new Player(playerStartPos);

        int cols = refWidth / blockSize;
        int rows = (refHeight / 2 + 20 * blockSize) / blockSize;

        byte baseRed = 100;
        byte baseGreen = 100;
        byte baseBlue = 100;

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < cols; x++)
            {
                baseRed = (byte)Math.Clamp(baseRed + Raylib.GetRandomValue(-5, 5), 50, 150);
                baseGreen = (byte)Math.Clamp(baseGreen + Raylib.GetRandomValue(-5, 5), 50, 150);
                baseBlue = (byte)Math.Clamp(baseBlue + Raylib.GetRandomValue(-5, 5), 50, 150);

                Color blockColor = new Color((byte)baseRed, (byte)baseGreen, (byte)baseBlue, (byte)255);

                int blockX = x * blockSize;
                int blockY = (refHeight / 2) - y * blockSize;

                blocks.Add(new Block(blockX, blockY, blockSize, blockColor));
            }
        }

        while (!Raylib.WindowShouldClose())
        {
            float deltaTime = Raylib.GetFrameTime();

            float scaleX = (float)Raylib.GetScreenWidth() / refWidth;
            float scaleY = (float)Raylib.GetScreenHeight() / refHeight;
            Camera2D camera = new Camera2D
            {
                Target = new Vector2(0, 0),
                Offset = new Vector2(0, 0),
                Rotation = 0f,
                Zoom = Math.Min(scaleX, scaleY)
            };

            // Handle mouse clicks for movement
            if (Raylib.IsMouseButtonPressed(MouseButton.Left))
            {
                Vector2 clickPosition = GetMousePositionRef();

                bool blockClicked = false;
                foreach (var block in blocks)
                {
                    if (clickPosition.X >= block.X && clickPosition.X <= block.X + block.Size &&
                        clickPosition.Y >= block.Y && clickPosition.Y <= block.Y + block.Size)
                    {
                        // Move to the block center, but we no longer set any "targetBlock"
                        Vector2 blockCenter = new Vector2(block.X + block.Size / 2, block.Y + block.Size / 2);
                        player.SetTarget(blockCenter);
                        blockClicked = true;
                        break;
                    }
                }

                if (!blockClicked)
                {
                    // Move to the exact click position if not on a block
                    player.SetTarget(clickPosition);
                }
            }

            player.Update(deltaTime, blocks);

            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.RayWhite);

            Raylib.BeginMode2D(camera);

            // Debug lines to show target position
            Raylib.DrawCircleV(player.TargetPosition, 5, Color.Red);
            Raylib.DrawLineV(player.Position, player.TargetPosition, Color.Red);

            // Draw each block
            foreach (var block in blocks)
            {
                block.Draw();
            }

            // Draw the player
            player.Draw();

            Raylib.EndMode2D();

            // Some on-screen debug
            Raylib.DrawText($"Player Pos: {player.Position.X:F4}, {player.Position.Y:F4}", 10, 10, 20, Color.Black);
            Raylib.DrawText($"Target Pos: {player.TargetPosition.X:F4}, {player.TargetPosition.Y:F4}", 10, 30, 20, Color.Black);
            Raylib.DrawText($"State: {player.CurrentState}", 10, 50, 20, Color.Black);

            Vector2 direction = player.TargetPosition - player.Position;
            float distance = direction.Length();
            Raylib.DrawText($"Distance to target: {distance:F4}", 10, 70, 20, Color.Black);

            Raylib.EndDrawing();
        }

        Raylib.CloseWindow();
    }

    public static bool IsBlockInMiningRange(Block block)
    {
        if (player == null) return false;

        Vector2 blockCenter = new Vector2(
            block.X + block.Size / 2,
            block.Y + block.Size / 2
        );

        float dist = Vector2.Distance(player.Position, blockCenter);
        return dist <= Player.MINING_RANGE;
    }

    private static Vector2 GetMousePositionRef()
    {
        Vector2 mouseScreenPos = Raylib.GetMousePosition();

        float scaleX = (float)Raylib.GetScreenWidth() / refWidth;
        float scaleY = (float)Raylib.GetScreenHeight() / refHeight;
        float zoom = Math.Min(scaleX, scaleY);

        Camera2D camera = new Camera2D
        {
            Target = new Vector2(0, 0),
            Offset = new Vector2(0, 0),
            Rotation = 0f,
            Zoom = zoom
        };

        Vector2 worldPos = Raylib.GetScreenToWorld2D(mouseScreenPos, camera);

        if (Raylib.IsMouseButtonPressed(MouseButton.Left))
        {
            Console.WriteLine($"Click at screen: {mouseScreenPos:F4}, world: {worldPos:F4}");
        }

        return worldPos;
    }
}
