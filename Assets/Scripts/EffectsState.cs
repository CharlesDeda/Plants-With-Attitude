using UnityEngine;

public class EffectsState : MonoBehaviour
{
    public bool enemySoaked = false;
    public bool enemySunLocked = false;
    public int enemyBurnTurns = 0;

    public bool playerSoaked = false;
    public bool playerSunLocked = false;
    public int playerBurnTurns = 0;

    public void ResetAll()
    {
        enemySoaked = false; enemySunLocked = false; enemyBurnTurns = 0;
        playerSoaked = false; playerSunLocked = false; playerBurnTurns = 0;
    }
}
