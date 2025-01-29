using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;

public class GameState
{
    public int Earth { get; set; }
    public float DurabilityMultiplier { get; set; }
    public int YieldBonus { get; set; }

    public List<MinerSaveData> Miners { get; set; }

    // Player is now a single object, not a list
    public PlayerSaveData Player { get; set; }

    public class MinerSaveData
    {
        public int InvCount { get; set; }
        public int InvMax { get; set; }
        public int BasePower { get; set; }
        public float Speed { get; set; } // Added Speed property
        public PickaxeStats PickaxeStats { get; set; }
    }

    public class PlayerSaveData
    {
        public int BasePower { get; set; }
        public float Speed { get; set; } // Added Speed property
        public PickaxeStats PickaxeStats { get; set; }
    }
}

public class SaveSystem
{
    private const string SAVE_FILE = "gamestate.json";
    private float saveTimer = 0f;
    private const float SAVE_INTERVAL = 10.0f;

    public void Update(float dt)
    {
        saveTimer += dt;
        if (saveTimer >= SAVE_INTERVAL)
        {
            SaveGame();
            saveTimer = 0f;
        }
    }

    public void SaveGame()
    {
        var gameState = new GameState
        {
            Earth = Program.Earth,
            DurabilityMultiplier = Block.currentDurabilityMultiplier,
            YieldBonus = Block.currentYieldBonus,

            // Save all miners
            Miners = Program.miners.Select(m => new GameState.MinerSaveData
            {
                InvCount = m.invCount,
                InvMax = m.invMax,
                BasePower = m.basePwr,
                Speed = m.Speed, // Save the miner's speed
                PickaxeStats = m.pickaxeStats
            }).ToList(),

            // Save the player
            Player = new GameState.PlayerSaveData
            {
                BasePower = Program.player != null ? Program.player.basePwr : 0,
                Speed = Program.player != null ? Program.player.Speed : 200f, // Save the player’s speed
                PickaxeStats = Program.player != null ? Program.player.GetPickaxeStats() : new PickaxeStats()
            }
        };

        // Serialize to JSON with pretty-printing
        string jsonString = JsonSerializer.Serialize(gameState, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText(SAVE_FILE, jsonString);
        Console.WriteLine("Game saved!");
    }

    public void LoadGame()
    {
        if (!File.Exists(SAVE_FILE)) return;

        string jsonString = File.ReadAllText(SAVE_FILE);
        var gameState = JsonSerializer.Deserialize<GameState>(jsonString);
        if (gameState == null) return;

        // Restore global stats
        Program.Earth = gameState.Earth;
        Block.currentDurabilityMultiplier = gameState.DurabilityMultiplier;
        Block.currentYieldBonus = gameState.YieldBonus;

        // Clear and restore miners
        Program.miners.Clear();
        if (gameState.Miners != null)
        {
            foreach (var minerState in gameState.Miners)
            {
                // Create each miner near the caravan (or however you like).
                // For example, alternating between x=30% and x=70% of screen:
                float xOffset = Program.refWidth * (Program.miners.Count % 2 == 0 ? 0.3f : 0.7f);

                var miner = new Miner(
                    new Vector2(xOffset, Program.refHeight * 0.9f),
                    Program.caravan,
                    minerState.BasePower
                );

                // Restore miner's inventory, speed, pickaxe
                miner.invCount = minerState.InvCount;
                miner.invMax = minerState.InvMax;
                miner.Speed = minerState.Speed; // Load the miner’s speed
                miner.pickaxeStats = minerState.PickaxeStats;

                Program.miners.Add(miner);
            }
        }

        // Restore player
        if (gameState.Player != null && Program.player != null)
        {
            Program.player.basePwr = gameState.Player.BasePower;
            Program.player.Speed = gameState.Player.Speed; // Load the player’s speed
            Program.player.SetPickaxeStats(gameState.Player.PickaxeStats);
        }

        Console.WriteLine("Game loaded!");
    }
}