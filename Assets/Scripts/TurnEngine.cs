using UnityEngine;

public struct TurnResult
{
    public int playerHpDelta;   // negative = took damage
    public int enemyHpDelta;    // negative = took damage
}

public class TurnEngine : MonoBehaviour
{
    [Header("Damage")]
    public int playerDamage = 1;
    public int normalDamage = 1;
    public int bossDamage = 1;

    [Header("Unlocks")]
    public bool unlockBurn = false;
    public bool unlockSoak = false;
    public bool unlockRoot = false;

    public int Resolve(Move p, Move e)
    {
        if (p == e) return 0;
        bool playerWins =
            (p == Move.Soil && e == Move.Sun) ||
            (p == Move.Sun  && e == Move.Water) ||
            (p == Move.Water && e == Move.Soil);
        return playerWins ? 1 : -1;
    }

    public TurnResult ApplyOutcome(
        Move playerMove,
        Move enemyMove,
        Enemy currentEnemy,
        EnemyType enemyType,
        BossKind currentBossKind,
        EffectsState fx)
    {
        int outcome = Resolve(playerMove, enemyMove);
        int pDelta = 0, eDelta = 0;

        if (outcome > 0)
        {
            int dmg = playerDamage;
            if (unlockSoak && playerMove == Move.Sun && fx.enemySoaked)
            {
                dmg += 2;
                fx.enemySoaked = false;
            }
            eDelta -= dmg;

            if (unlockSoak && playerMove == Move.Water) fx.enemySoaked = true;
            if (unlockRoot && playerMove == Move.Soil) fx.enemySunLocked = true;
            if (unlockBurn && playerMove == Move.Sun)  fx.enemyBurnTurns = 2;
        }
        else if (outcome < 0)
        {
            int dmg = (enemyType == EnemyType.Boss) ? bossDamage : normalDamage;
            if (fx.playerSoaked && enemyMove == Move.Sun) { dmg += 2; fx.playerSoaked = false; }

            if (enemyType == EnemyType.Boss)
            {
                bool final = (currentBossKind == BossKind.Final);
                bool canInflictThisMove =
                    final || MoveMatchesBoss(enemyMove, currentBossKind);

                if (canInflictThisMove)
                {
                    if (enemyMove == Move.Water) fx.playerSoaked = true;
                    if (enemyMove == Move.Soil)  fx.playerSunLocked = true;
                    if (enemyMove == Move.Sun)   fx.playerBurnTurns = 2;
                }
            }
            pDelta -= dmg;
        }

        // burns
        if (fx.enemyBurnTurns > 0) { eDelta -= 1; fx.enemyBurnTurns--; }
        if (fx.playerBurnTurns > 0) { pDelta -= 1; fx.playerBurnTurns--; }

        return new TurnResult { playerHpDelta = pDelta, enemyHpDelta = eDelta };
    }

    public static bool MoveMatchesBoss(Move m, BossKind k) =>
        (k == BossKind.Sun   && m == Move.Sun) ||
        (k == BossKind.Water && m == Move.Water) ||
        (k == BossKind.Soil  && m == Move.Soil);
}
