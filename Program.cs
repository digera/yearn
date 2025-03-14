using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text.Json.Serialization;
using Raylib_cs;
using System.Linq;
using System.IO;
using System.Runtime.CompilerServices;

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
    public Vector2 Position => new Vector2(X, Y);
    public Vector2 Center => new Vector2(X + Size / 2, Y + Size / 2);


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
                Raylib.DrawText(
                    Dur.ToString(),
                    X + Size / 2 - 10,
                    Y + Size / 2 - 15,
                    20,
                    Color.Black
                );
                Raylib.DrawText(
                    Mat.ToString(),
                    X + Size / 2 - 25,
                    Y + Size / 2 + 5,
                    15,
                    Color.Black
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
    //public static int Earth = 0;
    static float caravanY;
    static float caravanSpeed = 40f;
    static float distanceThreshold = 250f;
    public static List<Miner> miners = new List<Miner>();
    //public static Crusher crusher;
    public static List<Crusher> crushers = new List<Crusher>();
    //public static EarthPile earthPile;
    public static List<EarthPile> earthPiles = new List<EarthPile>();
    public static float minerProgress = 0;
    public static float minerThreshold = 10;
    public static int[] stoneCounts = new int[Enum.GetValues(typeof(StoneType)).Length];
    public static StoneType? DraggedStoneType = null;  // Track which stone type is being dragged
    public static Forge forge;  // Add forge instance
    public static ShovelStation shovelStation; // Add shovel station instance
    
    // Flag to track if window size is being adjusted
    private static bool isAdjustingWindowSize = false;
    // Previous window size for comparison
    private static int prevWindowWidth = 0;
    private static int prevWindowHeight = 0;

    // Method to maintain aspect ratio by adjusting window size
    public static void MaintainAspectRatio()
    {
        // Don't process if we're already adjusting the window
        if (isAdjustingWindowSize)
            return;

        int currentWidth = Raylib.GetScreenWidth();
        int currentHeight = Raylib.GetScreenHeight();

        // Only adjust if the window size has actually changed
        if (currentWidth != prevWindowWidth || currentHeight != prevWindowHeight)
        {
            isAdjustingWindowSize = true;

            float targetAspectRatio = (float)refHeight / refWidth; // 800/600 = 4/3
            float currentAspectRatio = (float)currentHeight / currentWidth;

            // Adjust window size to match the target aspect ratio
            if (Math.Abs(currentAspectRatio - targetAspectRatio) > 0.01f) // Small threshold for floating point comparison
            {
                if (currentAspectRatio > targetAspectRatio)
                {
                    // Window is too tall, adjust height
                    int newHeight = (int)(currentWidth * targetAspectRatio);
                    Raylib.SetWindowSize(currentWidth, newHeight);
                }
                else
                {
                    // Window is too wide, adjust width
                    int newWidth = (int)(currentHeight / targetAspectRatio);
                    Raylib.SetWindowSize(newWidth, currentHeight);
                }
            }

            prevWindowWidth = Raylib.GetScreenWidth();
            prevWindowHeight = Raylib.GetScreenHeight();
            isAdjustingWindowSize = false;
        }
    }

    // This method is no longer needed as we'll use direct window size adjustment
    /*
    public void LockAspectRatio()
    {
        float targetAspectRatio = (float)3 / 4;

        int currentWidth = Raylib.GetScreenWidth();
        int currentHeight = Raylib.GetScreenHeight();
        float currentAspectRatio = (float)currentWidth / currentHeight;
        if (currentAspectRatio > targetAspectRatio)
        {
            int newWidth = (int)(currentHeight * targetAspectRatio);
            int sideBarWidth = (currentWidth - newWidth) / 2;
            Raylib.SetWindowSize(newWidth, currentHeight);
            Raylib.SetWindowPosition(sideBarWidth, 0);
        }
        else
        {
            int newHeight = (int)(currentWidth / targetAspectRatio);
            int topBarHeight = (currentHeight - newHeight) / 2;
            Raylib.SetWindowSize(currentWidth, newHeight);
            Raylib.SetWindowPosition(0, topBarHeight);
        }
    }
    */

    // New method to calculate virtual mouse position
    public static Vector2 GetVirtualMousePosition()
    {
        // Since we're maintaining aspect ratio with window size,
        // we can just scale the mouse position directly
        Vector2 mouse = Raylib.GetMousePosition();
        Vector2 virtualMouse = new Vector2(
            mouse.X * refWidth / Raylib.GetScreenWidth(),
            mouse.Y * refHeight / Raylib.GetScreenHeight()
        );
        
        return virtualMouse;
    }

    public static void CheckAndRemoveDestroyedBlocks()
    {

        List<Block> blocksToRemove = new List<Block>();

        foreach (var block in blocks)
        {
            if (block.Dur <= 0)
            {
                blocksToRemove.Add(block);
            }
        }


        foreach (var block in blocksToRemove)
        {
            blocks.Remove(block);

            OnBlockDestroyed();

        }
    }

    public static void OnBlockDestroyed()
    {
        minerProgress += Random.Shared.Next(1, 10);

    }

    public static void Main()
    {
        Raylib.SetConfigFlags(ConfigFlags.ResizableWindow | ConfigFlags.VSyncHint);

        Raylib.InitWindow(refWidth, refHeight, "Yearn");
        Raylib.SetTargetFPS(60);
        Raylib.SetWindowMinSize(300, 400); // Set minimum window size
        
        // Initialize previous window size
        prevWindowWidth = refWidth;
        prevWindowHeight = refHeight;

        stoneCounts = new int[Enum.GetValues(typeof(StoneType)).Length];
        Vector2 playerStartPos = new Vector2(refWidth * 0.5f, refHeight * 0.8f);
        caravan = new Caravan(refWidth, refHeight);
        crushers.Add(new Crusher(caravan, StoneType.Earth, StoneType.Stone, 100, 50, 50, 200, 0));

        player = new Player(playerStartPos, caravan);
        
        // Initialize the forge
        forge = new Forge(caravan);
        
        // Initialize the shovel station
        shovelStation = new ShovelStation(caravan);

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
            minerThreshold = 10 * (miners.Count) * 10;
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
            // Maintain aspect ratio by adjusting window size
            MaintainAspectRatio();

            float dt = Raylib.GetFrameTime();
            
            // Scale camera to match window size
            camera.Zoom = (float)Raylib.GetScreenWidth() / refWidth;
            camera.Offset = new Vector2(Raylib.GetScreenWidth() / 2f, Raylib.GetScreenHeight() / 2f);
            
            saveSystem.Update(dt);

            // Create piles for stone types that don't have one yet
            for (int i = 0; i < stoneCounts.Length; i++)
            {
                if (stoneCounts[i] > 0 && !earthPiles.Any(p => p.StoneType == (StoneType)i))
                {
                    earthPiles.Add(new EarthPile(caravan, 50, 50, (StoneType)i));
                }
            }

            // Remove piles for stone types that are empty
            earthPiles.RemoveAll(p => Program.stoneCounts[(int)p.StoneType] <= 0);

            // Update each earth pile
            foreach (var pile in earthPiles)
            {
                pile.Update(camera);
            }
            
            // Update the forge
            forge.Update(dt);
            
            // Update the shovel station
            shovelStation.Update(dt);

            foreach (var crusher in crushers)
            {
                crusher.Update(dt);
                
                // Handle crusher clicks to assign miners
                Vector2 mouseWorld = GetMouseWorld();
                if (Raylib.IsMouseButtonPressed(MouseButton.Left) && crusher.CheckClick(mouseWorld))
                {
                    // Find an available miner (not currently working)
                    var availableMiner = miners.FirstOrDefault(m => m.CurrentState != MinerState.Working);
                    if (availableMiner != null)
                    {
                        crusher.AssignMiner(availableMiner);
                    }
                }
            }

            if (minerProgress >= minerThreshold)
            {
                Vector2 SpawnPos = new Vector2(Random.Shared.Next(0, refWidth), caravan.Y + Random.Shared.Next(-1000, -500));
                minerProgress = 0;
                minerThreshold = 10 * (miners.Count) * 10;
                miners.Add(new Miner(
                                    SpawnPos,
                                    caravan,
                                    Random.Shared.Next(1, 10),
                                    Random.Shared.Next(75, 200),
                                    Names.GetUniqueName()
                                    ));
            }
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
            if (Raylib.IsKeyPressed(KeyboardKey.C))
            {
                int newCrusherID = crushers.Count;
                int numStoneTypes = Enum.GetValues(typeof(StoneType)).Length;
                crushers.Add(new Crusher(
                       caravan,
                       (StoneType)(crushers.Count % numStoneTypes),
                       (StoneType)((crushers.Count + 1) % numStoneTypes),
                       100,
                       50,
                       50,
                       (newCrusherID * 70) +200,
                       newCrusherID
                   ));
            }

            if (Raylib.IsMouseButtonDown(MouseButton.Left))
            {
                // Use virtual mouse position instead
                Vector2 mouseWorld = GetMousePositionRef();
                player.SetTarget(mouseWorld);
            }
            // oh my god send help -- update, jippity pulled through

            if (Raylib.IsMouseButtonPressed(MouseButton.Left))
            {
                // Use virtual mouse position instead
                Vector2 mouseWorld = GetMousePositionRef();

                // Process miner clicks (each miner is processed twice in separate passes)
                foreach (var miner in miners)
                {
                    miner.CheckClick(mouseWorld);
                }
                foreach (var miner in miners)
                {
                    miner.CheckClick(mouseWorld);
                }

                Crusher crusherToCreateNextTier = null;
                foreach (var crusher in crushers)
                {
                    if (crusher.CheckUpgradeClick(mouseWorld, out int upgradeIndex))
                    {
                        switch (upgradeIndex)
                        {
                            case 0:
                                crusher.UpgradeHopper();
                                break;
                            case 1:
                                crusher.UpgradeConversion();
                                break;
                        }
                    }
                    else if (crusher.CheckNextTierClick(mouseWorld))
                    {
                        // Store the crusher that needs to create a next tier
                        // We'll handle this after the loop to avoid collection modification during enumeration
                        crusherToCreateNextTier = crusher;
                    }
                    else if (crusher.CheckClick(mouseWorld))
                    {
                        if (miners.Count > 0)
                        {
                            int index = Random.Shared.Next(miners.Count);
                            miners[index].CurrentState = MinerState.Working;
                        }
                    }
                }

                // Create the next tier crusher if needed (outside the loop to avoid collection modification during enumeration)
                if (crusherToCreateNextTier != null)
                {
                    crusherToCreateNextTier.CreateNextTierCrusher();
                }

                // Process caravan click: if the caravan was clicked, set the player to Crafting and target the caravan center;
                // otherwise, set the player to Walking and target the mouse position.
                if (caravan.CheckClick(mouseWorld))
                {
                    player.CurrentState = PlayerState.Crafting;
                    player.IsMoving = false;
                    player.SetTarget(caravan.Center);
                }
                else if (forge.CheckClick(mouseWorld))
                {
                    // Handle forge click if needed
                }
                else if (shovelStation.CheckClick(mouseWorld))
                {
                    // Handle shovel station click if needed
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
            foreach (var crusher in crushers)
            {
                crusher.Update(dt);
            }
            MoveCaravanUpIfNeeded(dt);
            UpdateCamera(dt);

            // Begin drawing directly to the screen (no render texture needed)
            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.RayWhite);
            
            // Begin 2D mode with camera
            Raylib.BeginMode2D(camera);

            Raylib.DrawCircleV(player.TargetPosition, 5, Color.Red);
            Raylib.DrawLineV(player.Position, player.TargetPosition, Color.Red);

            foreach (var b in blocks)
            {
                b.Draw();
            }
            caravan.Draw();
            foreach (var crusher in crushers)
            {
                crusher.Draw();
            }

            // Draw the forge
            forge.Draw();
            
            // Draw the shovel station
            shovelStation.Draw();

            // Draw each earth pile
            foreach (var pile in earthPiles)
            {
                pile.Draw(camera);
            }

            player.Draw(dt);
            foreach (var m in miners)
            {
                m.Draw(dt);
            }

            Raylib.EndMode2D();

            // Draw UI elements directly to the screen
            Raylib.DrawText($"Player Pos: {player.Position.X:F2}, {player.Position.Y:F2}", 10, 10, 20, Color.Black);
            Raylib.DrawText($"Player State: {player.CurrentState}", 10, 30, 20, Color.Black);
            Raylib.DrawText($"Blocks: {blocks.Count}", 10, 50, 20, Color.Black);
            Raylib.DrawText($"Miners: {miners.Count}", 10, 70, 20, Color.Black);
            Raylib.DrawText($"Caravan Y: {caravan.Y}", 10, 90, 20, Color.Black);
            Raylib.DrawText($"Miner Progress: {minerProgress}", 10, 130, 20, Color.Black);
            Raylib.DrawText($"Miner Threshold: {minerThreshold}", 10, 150, 20, Color.Black);
            Raylib.DrawText($"CurrentYieldBonus: {Block.currentYieldBonus}", 10, 170, 20, Color.Black);
            Raylib.DrawText($"CurrentDurabilityMultiplier: {Block.currentDurabilityMultiplier}", 10, 190, 20, Color.Black);

            string scoreText = $"Earth: {stoneCounts[(int)StoneType.Earth]}";
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
        Block.currentYieldBonus += (Block.currentYieldBonus / 10) + 1;

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
        // Use virtual mouse position for letterboxing
        Vector2 virtualMouse = GetVirtualMousePosition();
        return Raylib.GetScreenToWorld2D(virtualMouse, camera);
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
        // Use virtual mouse position for letterboxing
        Vector2 virtualMouse = GetVirtualMousePosition();
        return Raylib.GetScreenToWorld2D(virtualMouse, camera);
    }
}