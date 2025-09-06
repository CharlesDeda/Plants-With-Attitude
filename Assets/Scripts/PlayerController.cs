using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    enum Move
    {
        sun,
        water,
        soil
    }

    public int playerHealth = 10;
    public int plantHealth = 5;
    public int plantBossHealth = 10;


    void GetResult(Move player, Move enemy)
    {
        //soil loses to water, sun loses to soil, water loses to sun
        bool result;
        switch (player)
        {
            case Move.soil:
                if (enemy == Move.soil)
                {
                    break;
                }
                else if (enemy == Move.sun)
                {
                    plantHealth -= 1;
                }
                else if (enemy == Move.water)
                {
                    playerHealth -= 1;
                }
            case Move.water:
                if (enemy == Move.soil)
                {
                    player -= 1;
                }
                else if (enemy == Move.sun)
                {
                    playerHealth -= 1;
                }
                else if (enemy == Move.water)
                {
                    break;
                }
            case Move.sun:
                if (enemy == Move.soil)
                {
                    playerHealth -= 1;
                }
                else if (enemy == Move.sun)
                {
                    break;
                }
                else if (enemy == Move.water)
                {
                    plantHealth -= 1;
                }
        }
        RoundOver();
    }

    void RoundOver()
    {
        if (playerHealth == 0)
        {
            ResetHealth();
            Debug.Log("Game over!: plant won!");
            return;
        }
        else if (plantHealth == 0)
        {
            ResetHealth();
            Debug.Log("Game over!: player won!");
        }
        return;
    }

    void ResetHealth()
    {
        plantHealth = 10;
        playerHealth = 10;
        plantBossHealth = 10;
    }
}
