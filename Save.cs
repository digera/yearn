using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;

public class GameState
{
    public int[] StoneCounts { get; set; } = Array.Empty<int>();
    public float DurabilityMultiplier { get; set; }
    public int YieldBonus { get; set; }

    public List<MinerSaveData> Miners { get; set; } = new List<MinerSaveData>();
    public PlayerSaveData Player { get; set; } = new PlayerSaveData();

    // Save data for crushers.
    public List<CrusherSaveData> Crushers { get; set; } = new List<CrusherSaveData>();

    // Save data for stone piles.
    public List<EarthPileSaveData> EarthPiles { get; set; } = new List<EarthPileSaveData>();

    public class MinerSaveData
    {
        public string MinerName { get; set; }
        public int InvCount { get; set; }
        public int InvMax { get; set; }
        public int BasePower { get; set; }
        public float Speed { get; set; }
        public PickaxeStats PickaxeStats { get; set; }
        public ShovelStats ShovelStats { get; set; }

        // New: Save EXP for each miner.
        public int Exp { get; set; }
        public int MaxExp { get; set; }
    }

    public class PlayerSaveData
    {
        public int BasePower { get; set; }
        public float Speed { get; set; }
        public PickaxeStats PickaxeStats { get; set; }

        // New: Save player's EXP.
        public int Exp { get; set; }
        public int MaxExp { get; set; }
    }

    public class CrusherSaveData
    {
        public int Hopper { get; set; }
        public float ConversionAmount { get; set; }
        public int InputResource { get; set; }
        public int OutputResource { get; set; }
        public StoneType InputType { get; set; }
        public StoneType OutputType { get; set; }
        public int ID { get; set; }
    }

    public class EarthPileSaveData
    {
        public StoneType StoneType { get; set; }
        public Vector2 Position { get; set; }
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
            StoneCounts = Program.stoneCounts,
            DurabilityMultiplier = Block.currentDurabilityMultiplier,
            YieldBonus = Block.currentYieldBonus,

            // Save all miners including their EXP.
            Miners = Program.miners.Select(m => new GameState.MinerSaveData
            {
                MinerName = m.MinerName,
                InvCount = m.invCount,
                InvMax = m.invMax,
                BasePower = m.basePwr,
                Speed = m.Speed,
                PickaxeStats = m.pickaxeStats,
                ShovelStats = m.shovelStats,
                Exp = m.exp,
                MaxExp = m.expToNextLevel
            }).ToList(),


            Player = new GameState.PlayerSaveData
            {
                BasePower = Program.player != null ? Program.player.basePwr : 0,
                Speed = Program.player != null ? Program.player.Speed : 200f,
                PickaxeStats = Program.player != null ? Program.player.GetPickaxeStats() : new PickaxeStats(),
                Exp = Program.player != null ? Program.player.exp : 0,
                MaxExp = Program.player != null ? Program.player.expToNextLevel : 0
            },

            // Save multiple crushers.
            Crushers = Program.crushers.Select(c => new GameState.CrusherSaveData
            {
                Hopper = c.Hopper,
                ConversionAmount = c.ConversionAmount,
                InputResource = c.InputResource,
                OutputResource = c.OutputResource,
                InputType = c.InputType,
                OutputType = c.OutputType,
                ID = c.ID
            }).ToList(),

            // Save multiple earth piles.
            EarthPiles = Program.earthPiles.Select(ep => new GameState.EarthPileSaveData
            {
                StoneType = ep.StoneType,
                Position = ep.Position
            }).ToList()
        };

        // Serialize to JSON.
        string jsonString = JsonSerializer.Serialize(gameState, new JsonSerializerOptions
        {
            WriteIndented = true,
            IncludeFields = true
        });

        File.WriteAllText(SAVE_FILE, jsonString);
        Console.WriteLine("Game saved!");
    }

    public void LoadGame()
    {
        if (!File.Exists(SAVE_FILE))
        {
            Console.WriteLine("Save file not found.");
            return;
        }

        try
        {
            string jsonString = File.ReadAllText(SAVE_FILE);
            
            // Check if the file is empty or contains only whitespace
            if (string.IsNullOrWhiteSpace(jsonString))
            {
                Console.WriteLine("Save file is empty or contains only whitespace.");
                return;
            }
            
            var options = new JsonSerializerOptions
            {
                IncludeFields = true
            };
            
            var gameState = JsonSerializer.Deserialize<GameState>(jsonString, options);
            if (gameState == null)
            {
                Console.WriteLine("Failed to deserialize game state.");
                return;
            }

            // Restore global stats.
            Program.stoneCounts = gameState.StoneCounts ?? new int[Enum.GetValues(typeof(StoneType)).Length];
            Block.currentDurabilityMultiplier = gameState.DurabilityMultiplier;
            Block.currentYieldBonus = gameState.YieldBonus;

            // Clear and restore miners.
            Program.miners.Clear();
            if (gameState.Miners != null)
            {
                foreach (var minerState in gameState.Miners)
                {
                    float xOffset = Program.refWidth * (Program.miners.Count % 2 == 0 ? 0.3f : 0.7f);
                    var miner = new Miner(
                        new Vector2(xOffset, Program.refHeight * 0.9f),
                        Program.caravan,
                        minerState.BasePower,
                        minerState.Speed,
                        minerState.MinerName
                    );
                    miner.MinerName = minerState.MinerName;
                    miner.invCount = minerState.InvCount;
                    miner.invMax = minerState.InvMax;
                    miner.Speed = minerState.Speed;
                    miner.pickaxeStats = minerState.PickaxeStats;
                    miner.shovelStats = minerState.ShovelStats;
                    miner.exp = minerState.Exp;
                    miner.expToNextLevel = minerState.MaxExp;
                    Program.miners.Add(miner);
                }
            }
            Program.minerThreshold = 10 * (Program.miners.Count + 1);

            if (gameState.Player != null && Program.player != null)
            {
                Program.player.basePwr = gameState.Player.BasePower;
                Program.player.Speed = gameState.Player.Speed;
                Program.player.SetPickaxeStats(gameState.Player.PickaxeStats);
                Program.player.exp = gameState.Player.Exp;
                Program.player.expToNextLevel = gameState.Player.MaxExp;
            }

            // Restore multiple crushers.
            Program.crushers.Clear();
            if (gameState.Crushers != null)
            {
                foreach (var crusherData in gameState.Crushers)
                {
                    var crusher = new Crusher(Program.caravan, crusherData.InputType, crusherData.OutputType, crusherData.Hopper, 50, 50, (crusherData.ID * 70) + 200, crusherData.ID);
                    crusher.RestoreState(crusherData);
                    Program.crushers.Add(crusher);
                }
            }

            // Restore multiple earth piles.
            Program.earthPiles.Clear();
            if (gameState.EarthPiles != null)
            {
                foreach (var earthPileData in gameState.EarthPiles)
                {
                    var earthPile = new EarthPile(Program.caravan, 50, 50, earthPileData.StoneType);
                    earthPile.Position = earthPileData.Position;
                    Program.earthPiles.Add(earthPile);
                }
            }

            Console.WriteLine("Game loaded!");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error loading game: " + ex.Message);
        }
    }
}
