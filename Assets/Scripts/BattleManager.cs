using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.UI;

public enum Move { Sun, Water, Soil }
public enum EnemyType { Normal, Boss }

[System.Serializable]
public class Enemy
{
    public EnemyType Type;
    public int MaxHealth;
    public int Health;

    public Enemy(EnemyType type, int hp)
    {
        Type = type;
        MaxHealth = hp;
        Health = hp;
    }
}

public class BattleManager : MonoBehaviour
{
    [Header("Player")]
    public int playerMaxHealth = 10;
    public int playerHealth = 10;
    public int playerDamage = 1;

    [Header("Enemy Settings")]
    public int normalMinPerCycle = 2;
    public int normalMaxPerCycle = 3;
    public int normalHealth = 5;
    public int normalDamage = 1;
    public int bossHealth = 10;
    public int bossDamage = 2;

    [Header("FX")]
    public float shakeDuration = 1.2f;
    public float shakeMagnitude = 12f;

    [Header("UI")]
    public Button sunButton;
    public Button waterButton;
    public Button soilButton;

    public TMP_Text playerHpText;
    public TMP_Text enemyHpText;
    public TMP_Text waveText;
    public TMP_Text playerCadenceText;
    public TMP_Text enemyCadenceText;

    public Image enemyImage; 
    public Sprite normalEnemySprite;
    public Sprite bossEnemySprite;

    private Enemy currentEnemy;
    private int normalsRemainingInCycle;
    private int cycleNumber = 1; 

    [Header("Timing")]
    public float beatSeconds = 0.5f;
    private readonly string[] cadenceWords = { "Sun...", "Soil...", "Water..." };
    private bool inputLocked = false;

    [Header("Special")]
    public int specialMeterMax = 7;
    public int specialDamage = 3;
    public Button specialButton;
    public TMP_Text specialMeterText;

    private int specialMeter = 0;
    private int specialHitStreak = 0;

    private const float specialBaseChance = 0.30f;
    private const float specialIncreasePerHit = 0.10f;
    private const float specialMaxChance = 0.95f;


    void Start()
    {
        sunButton.onClick.AddListener(OnSunPressed);
        waterButton.onClick.AddListener(OnWaterPressed);
        soilButton.onClick.AddListener(OnSoilPressed);
        if (specialButton) specialButton.onClick.AddListener(OnSpecialPressed);

        StartNewCycle();
        UpdateSpecialUI();
    }

   bool TryLockInput()
    {
        if (inputLocked) return false;
        inputLocked = true;
        SetButtonsInteractable(false);
        return true;
    }

    public void OnSunPressed()   { if (TryLockInput()) StartCoroutine(RoundWithCountdown(Move.Sun)); }
    public void OnWaterPressed() { if (TryLockInput()) StartCoroutine(RoundWithCountdown(Move.Water)); }
    public void OnSoilPressed()  { if (TryLockInput()) StartCoroutine(RoundWithCountdown(Move.Soil)); }

    void SetButtonsInteractable(bool interactable)
    {
        if (sunButton)   sunButton.interactable = interactable;
        if (waterButton) waterButton.interactable = interactable;
        if (soilButton)  soilButton.interactable = interactable;
        if (specialButton)  specialButton.interactable = interactable && (specialMeter >= specialMeterMax);
    }

    void StartNewCycle()
    {
        normalsRemainingInCycle = Random.Range(normalMinPerCycle, normalMaxPerCycle + 1);
        playerHealth = playerMaxHealth;

        if (playerCadenceText) playerCadenceText.text = "";
        if (enemyCadenceText)  enemyCadenceText.text  = "";
        if (waveText) waveText.text = $"Wave {cycleNumber}";

        inputLocked = false;
        SetButtonsInteractable(true);

        SpawnNextEnemy();
        RefreshUI();
        UpdateSpecialUI();
    }


    void SpawnNextEnemy()
    {
        if (normalsRemainingInCycle > 0)
        {
            currentEnemy = new Enemy(EnemyType.Normal, normalHealth);
            waveText.text = $"Wave: {cycleNumber}";
        }
        else
        {
            currentEnemy = new Enemy(EnemyType.Boss, Mathf.RoundToInt(bossHealth * BossHpScale()));
            waveText.text = $"Cycle {cycleNumber} • BOSS";
        }
        UpdateEnemyVisual();
        RefreshUI();
    }

    float CurrentSpecialChance()
{
    float c = specialBaseChance + specialIncreasePerHit * specialHitStreak;
    return Mathf.Min(c, specialMaxChance);
}

    void UpdateSpecialUI()
    {
        if (specialMeterText)
        {
            int pct = Mathf.RoundToInt(CurrentSpecialChance() * 100f);
            string state = specialMeter >= specialMeterMax ? "Ready" : "Building";
            specialMeterText.text = $"Special: {specialMeter}/{specialMeterMax} • {state} • {pct}%";
        }

        if (specialButton && !inputLocked)
            specialButton.interactable = specialMeter >= specialMeterMax;
    }

    void IncrementSpecialMeter(int amt = 1)
    {
        specialMeter = Mathf.Clamp(specialMeter + amt, 0, specialMeterMax);
        UpdateSpecialUI();
    }

    void ConsumeSpecialMeter()
    {
        specialMeter = 0;
        UpdateSpecialUI();
    }

    float BossHpScale()
    {
        return 1f + 0.1f * (cycleNumber - 1);
    }

  void OnPlayerMove(Move playerMove)
    {
        StartCoroutine(RoundWithCountdown(playerMove));
    }

        public void OnSpecialPressed()
    {
        if (specialMeter < specialMeterMax) return;
        if (!TryLockInput()) return;
        StartCoroutine(RoundWithCountdownSpecial());
    }

    void ApplyOutcome(Move playerMove, Move enemyMove)
    {
        int outcome = Resolve(playerMove, enemyMove);

        if (outcome > 0)
        {
            int dmg = currentEnemy.Type == EnemyType.Boss ? playerDamage : playerDamage;
            currentEnemy.Health = Mathf.Max(0, currentEnemy.Health - dmg);
            if (enemyHpText) StartCoroutine(ShakeRect(enemyHpText.rectTransform, shakeDuration, shakeMagnitude));
        }
        else if (outcome < 0)
        {
            int dmg = currentEnemy.Type == EnemyType.Boss ? bossDamage : normalDamage;
            playerHealth = Mathf.Max(0, playerHealth - dmg);
            if (playerHpText) StartCoroutine(ShakeRect(playerHpText.rectTransform, shakeDuration, shakeMagnitude));
        }

        IncrementSpecialMeter(1);
        RefreshUI();
        CheckRoundOver();
    }

    string MoveToText(Move m)
    {
        switch (m)
        {
            case Move.Sun:   return "Sun";
            case Move.Water: return "Water";
            case Move.Soil:  return "Soil";
            default:         return m.ToString();
        }
    }


    public void OnSun()   => OnPlayerMove(Move.Sun);
    public void OnWater() => OnPlayerMove(Move.Water);
    public void OnSoil()  => OnPlayerMove(Move.Soil);  

    Move GetEnemyMove(EnemyType type)
    {
        int r = Random.Range(0, 3);
        return (Move)r;
    }

    int Resolve(Move p, Move e)
    {
        if (p == e) return 0;

        bool playerWins =
            (p == Move.Soil && e == Move.Sun) ||
            (p == Move.Sun  && e == Move.Water) ||
            (p == Move.Water && e == Move.Soil);

        return playerWins ? 1 : -1;
    }

    void CheckRoundOver()
    {
        if (playerHealth <= 0)
        {
            specialMeter = 0;
            specialHitStreak = 0;
            UpdateSpecialUI();

            cycleNumber = 1;
            StartNewCycle();
            return;
        }

        if (currentEnemy.Health <= 0)
        {
            if (currentEnemy.Type == EnemyType.Normal)
            {
                normalsRemainingInCycle--;
                if (normalsRemainingInCycle > 0)
                {
                    SpawnNextEnemy();
                }
                else
                {
                    SpawnNextEnemy(); 
                }
            }
            else 
            {
                cycleNumber++;
                StartNewCycle();
            }
        }

        RefreshUI();
    }

    void RefreshUI()
    {
        if (playerHpText) playerHpText.text = $"Player HP: {playerHealth}/{playerMaxHealth}";
        if (enemyHpText) enemyHpText.text = currentEnemy != null
            ? $"Enemy HP: {currentEnemy.Health}/{currentEnemy.MaxHealth}"
            : $"Enemy HP: -";
    }

    void UpdateEnemyVisual()
    {
        if (!enemyImage) return;
        if (currentEnemy == null)
        {
            enemyImage.enabled = false;
            return;
        }

        enemyImage.enabled = true;
        enemyImage.sprite = currentEnemy.Type == EnemyType.Boss ? bossEnemySprite : normalEnemySprite;
    }

    System.Collections.IEnumerator RoundWithCountdown(Move playerMove)
    {
        if (currentEnemy == null) yield break;

        Move enemyMove = GetEnemyMove(currentEnemy.Type);

        if (playerCadenceText) playerCadenceText.text = "";
        if (enemyCadenceText)  enemyCadenceText.text  = "";

        foreach (var word in cadenceWords)
        {
            if (playerCadenceText) playerCadenceText.text = word;
            if (enemyCadenceText)  enemyCadenceText.text  = word;
            yield return new WaitForSeconds(beatSeconds);
        }

        if (playerCadenceText) playerCadenceText.text = $"{MoveToText(playerMove)}!";
        if (enemyCadenceText)  enemyCadenceText.text  = $"{MoveToText(enemyMove)}!";
        yield return new WaitForSeconds(beatSeconds);

        ApplyOutcome(playerMove, enemyMove);

        inputLocked = false;
        SetButtonsInteractable(true);
    }

    System.Collections.IEnumerator RoundWithCountdownSpecial()
    {
        if (currentEnemy == null) yield break;

        Move enemyMove = GetEnemyMove(currentEnemy.Type);

        if (playerCadenceText) playerCadenceText.text = "";
        if (enemyCadenceText)  enemyCadenceText.text  = "";

        foreach (var word in cadenceWords)
        {
            if (playerCadenceText) playerCadenceText.text = word;
            if (enemyCadenceText)  enemyCadenceText.text  = word;
            yield return new WaitForSeconds(beatSeconds);
        }

        if (playerCadenceText) playerCadenceText.text = "Special!";
        if (enemyCadenceText)  enemyCadenceText.text  = $"{MoveToText(enemyMove)}!";
        yield return new WaitForSeconds(beatSeconds);

        float chance = CurrentSpecialChance();
        bool hit = Random.value < chance;

        ConsumeSpecialMeter();

        if (hit)
        {
            specialHitStreak++;
            currentEnemy.Health = Mathf.Max(0, currentEnemy.Health - specialDamage);
            if (enemyHpText) StartCoroutine(ShakeRect(enemyHpText.rectTransform, shakeDuration, shakeMagnitude));
        }
        else
        {
            specialHitStreak = 0;
        }

        UpdateSpecialUI();
        RefreshUI();
        CheckRoundOver();

        inputLocked = false;
        SetButtonsInteractable(true);
    }



    System.Collections.IEnumerator ShakeRect(RectTransform target, float duration, float magnitude)
    {
        if (target == null) yield break;

        Vector2 original = target.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float offsetX = Random.Range(-1f, 1f) * magnitude;
            float offsetY = Random.Range(-1f, 1f) * magnitude * 0.5f; 
            target.anchoredPosition = original + new Vector2(offsetX, offsetY);

            elapsed += Time.deltaTime;
            yield return null;
        }

        target.anchoredPosition = original;
    }

}