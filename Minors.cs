using Raylib_cs;
using System.Numerics;


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
    public int basePwr { get; private set; }
    private Color circleColor = Color.Purple;



    public MinerState CurrentState { get; private set; } = MinerState.MovingUp;

    public PickaxeStats pickaxeStats;
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

    public void CheckClick(Vector2 mousePosition)
    {
        if (Vector2.Distance(mousePosition, Position) <= Radius)
        {
            CurrentState = MinerState.Returning;
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
            Program.Earth += invCount;
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