using Raylib_cs;
using System.Numerics;

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
    public int basePwr = 20;

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

        if (Position.Y >= caravan.Y - 100 && CurrentState != PlayerState.Riding)
        {
            CurrentState = PlayerState.Riding;
            Position = new Vector2(Position.X, caravan.Y - 100);
        }
    }

    private void UpdatePickaxes(float dt, List<Block> blocks)
    {
        for (int i = activePickaxes.Count - 1; i >= 0; i--)
        {
            var (pos, target) = activePickaxes[i];

            // If target is gone, remove pickaxe
            if (!blocks.Contains(target))
            {
                activePickaxes.RemoveAt(i);
                continue;
            }

            Vector2 targetCenter = new Vector2(
                target.X + target.Size * 0.5f,
                target.Y + target.Size * 0.5f
            );

            // Move pickaxe toward block center
            Vector2 dir = Vector2.Normalize(targetCenter - pos);
            Vector2 newPos = pos + dir * pickaxeStats.Speed * dt;

            float dist = Vector2.Distance(newPos, targetCenter);
            if (dist <= 5f)
            {
                target.Dur -= pickaxeStats.MiningPower + basePwr;
                if (target.Yield > 0) 
                { 
                target.Yield--;
                Program.Earth++;
                }
                if (target.Dur <= 0)
                   
                {
                    Program.Earth += target.Yield;
                    blocks.Remove(target);
                }
                activePickaxes.RemoveAt(i);
            }
            else
            {
                // Update pickaxe position
                activePickaxes[i] = (newPos, target);
            }
        }

        // Auto-fire pickaxe while mining
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
        // Draw pickaxes
        foreach (var (pos, _) in activePickaxes)
        {
            Raylib.DrawCircleV(pos, pickaxeStats.Size, pickaxeStats.Color);
        }

        // Draw mining range
       Raylib.DrawCircleLines((int)Position.X, (int)Position.Y, MINING_RANGE, Color.Yellow);

        // Draw player circle
        Color circleColor = CurrentState switch
        {
            PlayerState.Idle => Color.Red,
            PlayerState.Walking => Color.Green,
            PlayerState.Crafting => Color.Blue,
            PlayerState.Mining => Color.Orange,
            PlayerState.Riding => Color.Gold,
            _ => Color.Red
        };

        Raylib.DrawCircle((int)Position.X, (int)Position.Y, Radius, circleColor);

        // Label with current state
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

        // Stop if overlapping a block; else move
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
        {
            if (b.OverlapsCircle(pos, Radius))
                return true;
        }
        return false;
    }

    private void ClampToScreen()
    {
        Position.X = Math.Clamp(Position.X, Radius, Program.refWidth - Radius);
    }

    // These methods let the save system read/write pickaxe stats for the player
    public PickaxeStats GetPickaxeStats()
    {
        return pickaxeStats;
    }

    public void SetPickaxeStats(PickaxeStats stats)
    {
        pickaxeStats = stats;
    }
}