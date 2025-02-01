using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Numerics;


public class Miner
{
    public string MinerName { get; set; }
    public Vector2 Position;
    public Vector2 TargetPosition;
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
    private int exp = 0;
    private int expToNextLevel = 10;

    public MinerState CurrentState { get; set; } = MinerState.Idle;

    public PickaxeStats pickaxeStats;
    private List<(Vector2 Position, Block Target)> activePickaxes = new List<(Vector2, Block)>();
    private float pickaxeTimer;

    private float tip = 0f;
    private const float tipSpan = 1f;

    private float workingFillTimer = 0f;

    public Miner(Vector2 startPos, Caravan caravan, int basePwr, float speed, string minerName)
    {
        MinerName = minerName;
        Position = startPos;
        TargetPosition = startPos; 
        this.caravan = caravan;
        this.basePwr = basePwr;
        Speed = speed;

        pickaxeStats = new PickaxeStats(
            speed: 0.75f,
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
        
        if (CurrentState == MinerState.Working)
        {
        
            Vector2 collectionPoint = new Vector2(Program.refWidth / 2, caravan.Y);
            if (invCount < invMax)
            {
        
                Vector2 dir = collectionPoint - Position;
                if (dir != Vector2.Zero) { dir = Vector2.Normalize(dir); }
                Position += dir * Speed * dt;

                if (Vector2.Distance(Position, collectionPoint) < 5f)
                {
                    workingFillTimer += dt;
                    if (workingFillTimer >= 0.25f)
                    {
                        workingFillTimer = 0;
                        if (Program.Earth > 0)
                        {
                            Program.Earth--;
                            invCount++;
                        }
                    }
                }
            }
            else 
            {
                
                Vector2 crusherDropOff = Program.crusher.GetPosition() + new Vector2(0, 200);
                Vector2 dir = crusherDropOff - Position;
                if (dir != Vector2.Zero) { dir = Vector2.Normalize(dir); }
                Position += dir * Speed * dt;

                if (Vector2.Distance(Position, crusherDropOff) < 5f)
                {
                
                    Program.crusher.ReceiveEarth(invCount);
                    invCount = 0;
                
                    if (Program.crusher.EarthStored >= Program.crusher.Hopper)
                    {
                        CurrentState = MinerState.MovingUp;
                    }
                
                }
            }
            return;
        }
        
        if (Raylib.CheckCollisionPointCircle(Program.GetMouseWorld(), Position, Radius) || Raylib.IsKeyDown(KeyboardKey.F1))
        {
            tip = tipSpan;
        }
        else if (tip > 0)
        {
            tip -= dt;
        }

        attn += dt;
        if (invCount >= invMax)
        {
            CurrentState = MinerState.Returning;
        }
        if (exp >= expToNextLevel)
        {
            exp = 0;
            expToNextLevel += 10 * basePwr;
            basePwr++;
        }

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

    // deprecated...
    private void MoveTowardsTarget(float dt)
    {
        Vector2 dir = TargetPosition - Position;
        if (dir != Vector2.Zero)
        {
            dir = Vector2.Normalize(dir);
            Position += dir * Speed * dt;
        }
        ClampToScreen();
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
            Vector2 newPos = pos + dir * 250f * dt;

            float dist = Vector2.Distance(newPos, targetCenter);
            if (dist <= 5f)
            {
                exp++;
                target.Dur -= pickaxeStats.MiningPower + basePwr;

                if (target.Yield > 1)
                {
                    target.Yield--;
                    invCount++;
                }
                if (target.Dur <= 0)
                {
                    invCount += target.Yield;
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
            if (pickaxeTimer >= pickaxeStats.Speed)
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

    public void Draw(float dt)
    {
        foreach (var (pos, _) in activePickaxes)
        {
            Raylib.DrawCircleV(pos, pickaxeStats.Size, pickaxeStats.Color);
        }

        Raylib.DrawCircle((int)Position.X, (int)Position.Y, Radius, circleColor);

        if (tip > 0)
        {
            Raylib.DrawCircleLines((int)Position.X, (int)Position.Y, MINING_RANGE, Color.Yellow);
            string st = $"{MinerName} {CurrentState} ({invCount}/{invMax})\nPwr:{basePwr}+{pickaxeStats.MiningPower}\nMov:{Speed}\nSpd:{pickaxeStats.Speed}";
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
}
