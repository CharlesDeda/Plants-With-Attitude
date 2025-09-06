using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public enum Move { Sun, Water, Soil }
public enum EnemyType { Normal, Boss }
public enum BossKind { Sun, Water, Soil, Final }
enum PostDefeatAction { SpawnNextEnemy, StartNewCycle }


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
    public int bossDamage = 1;

    [Header("Enemy Slide")]
    public float enemySlideDistance = 800f;
    public float enemySlideDuration = 0.35f;
    public TMP_Text enemyQuipText;

    [Header("Quips")]
    [Tooltip("1-in-N chance the enemy taunts after a round")]
    public int quipOneInN = 2;
    public float quipDuration = 1.5f;

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
    public int specialDamage = 5;
    public Button specialButton;
    public Slider specialMeterBar;
    private int specialMeter = 0;
    private int specialHitStreak = 0;

    private const float specialBaseChance = 0.30f;
    private const float specialIncreasePerHit = 0.10f;
    private const float specialMaxChance = 0.95f;

    [Header("Special Visuals (optional)")]
    public Image specialBarFill;
    public Color readyColor = Color.yellow;
    public Color buildingColor = new Color(0f, 1f, 0f); 

    [Header("Screen Shake")]
    public RectTransform uiRootToShake;
    public Transform cameraToShake;
    public float specialShakeDuration = 1.5f;
    public float specialShakeMagnitude = 30f;

    [Header("Boss Progression")]
    public int normalsPerCycle = 1;
    public float hpScalePerSet = 1.25f;
    public float finalBossHpMultiplier = 1.3f;
    private List<BossKind> pendingBosses = new List<BossKind>();
    private BossKind currentBossKind = BossKind.Sun;
    private bool bossThisCycleIsFinal = false;
    private int setNumber = 1;
    private float currentHpScale = 1f;

    private bool unlockBurn = false;
    private bool unlockSoak = false;
    private bool unlockRoot = false;

    private bool enemySoaked = false;
    private bool enemySunLocked = false;
    private int enemyBurnTurns = 0;

    private bool playerSoaked = false;
    private bool playerSunLocked = false;
    private int playerBurnTurns = 0;

    public TMP_Text enemyStatusText;
    public TMP_Text playerStatusText;

    public Animator animator;

    void Start()
    {
        sunButton.onClick.AddListener(OnSunPressed);
        waterButton.onClick.AddListener(OnWaterPressed);
        soilButton.onClick.AddListener(OnSoilPressed);
        if (specialButton) specialButton.onClick.AddListener(OnSpecialPressed);

        if (specialMeterBar)
        {
            specialMeterBar.wholeNumbers = true;
            specialMeterBar.minValue = 0;
            specialMeterBar.maxValue = specialMeterMax;
            specialMeterBar.interactable = false;
            specialMeterBar.handleRect = null;
            specialMeterBar.direction = Slider.Direction.LeftToRight;
        }

        StartNewSet();
        StartNewCycle();
        UpdateSpecialUI();
        if (!cameraToShake && Camera.main) cameraToShake = Camera.main.transform;

    }

    bool TryLockInput()
    {
        if (inputLocked) return false;
        inputLocked = true;
        SetButtonsInteractable(false);
        return true;
    }

    void StartNewSet()
    {
        pendingBosses.Clear();
        pendingBosses.Add(BossKind.Sun);
        pendingBosses.Add(BossKind.Water);
        pendingBosses.Add(BossKind.Soil);
        ShuffleList(pendingBosses);
        bossThisCycleIsFinal = false;
        cycleNumber = 1; // show “Wave 1”, etc., for the new set
    }

    void ShuffleList<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int j = Random.Range(i, list.Count);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    string RandomPlantTaunt()
    {
        string[] lines = {
            "You’re about to get MULCHED!",
            "Photosynthesize this, punk.",
            "I’m ROOTIN for your demise!",
            "I'm gonna SOIL your plans!",
            "You're all BARK and no bite!",
            "I'll chlory-fill you with holes!"
        };
        return lines[Random.Range(0, lines.Length)];
    }
    void ShowEnemyTaunt(float seconds = 1.5f)
    {
        var t = enemyQuipText ? enemyQuipText : enemyCadenceText;
        if (!t) return;
        t.text = RandomPlantTaunt();
        StartCoroutine(ClearTextAfterDelay(t, seconds));
    }

    System.Collections.IEnumerator ClearTextAfterDelay(TMP_Text t, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (t) t.text = "";
    }
    void MaybeQuip()
    {
        if (currentEnemy != null && currentEnemy.Health > 0 && playerHealth > 0)
        {
            if (Random.Range(1, 2) == 1)
                ShowEnemyTaunt(quipDuration);
        }
    }
    string BossLabel(BossKind k) =>
        k == BossKind.Sun ? "SUN" :
        k == BossKind.Water ? "WATER" :
        k == BossKind.Soil ? "SOIL" :
        "FINAL";

    bool MoveMatchesBoss(Move m, BossKind k) =>
        (k == BossKind.Sun && m == Move.Sun) ||
        (k == BossKind.Water && m == Move.Water) ||
        (k == BossKind.Soil && m == Move.Soil);

    public void OnSunPressed()
    {
        if (playerSunLocked)
        {
            if (playerCadenceText) playerCadenceText.text = "Rooted! Can't use Sun this turn.";
            return;
        }
        if (TryLockInput()) StartCoroutine(RoundWithCountdown(Move.Sun));
    }

    public void OnWaterPressed() { if (TryLockInput()) StartCoroutine(RoundWithCountdown(Move.Water)); }
    public void OnSoilPressed() { if (TryLockInput()) StartCoroutine(RoundWithCountdown(Move.Soil)); }

    void SetButtonsInteractable(bool interactable)
    {
        if (sunButton) sunButton.interactable = interactable && !playerSunLocked;
        if (waterButton) waterButton.interactable = interactable;
        if (soilButton) soilButton.interactable = interactable;
        if (specialButton) specialButton.interactable = interactable && (specialMeter >= specialMeterMax);
    }

    void StartNewCycle()
    {
        normalsRemainingInCycle = normalsPerCycle;
        playerHealth = playerMaxHealth;

        if (playerCadenceText) playerCadenceText.text = "";
        if (enemyCadenceText) enemyCadenceText.text = "";
        if (waveText) waveText.text = $"Wave {cycleNumber} (Set {setNumber})";

        inputLocked = false;
        SetButtonsInteractable(true);

        SpawnNextEnemy();
        RefreshUI();
        UpdateSpecialUI();
    }


    void SpawnNextEnemy()
    {
        enemySoaked = false; enemySunLocked = false; enemyBurnTurns = 0;
        playerSoaked = false; playerSunLocked = false; playerBurnTurns = 0;

        if (normalsRemainingInCycle > 0)
        {
            int hp = Mathf.RoundToInt(normalHealth * currentHpScale);
            currentEnemy = new Enemy(EnemyType.Normal, hp);
            if (waveText) waveText.text = $"Wave {cycleNumber} • Grunt ({normalsRemainingInCycle} left)";
        }
        else
        {
            if (!bossThisCycleIsFinal)
            {
                if (pendingBosses.Count > 0)
                {
                    currentBossKind = pendingBosses[0];
                }
                else
                {
                    bossThisCycleIsFinal = true;
                    currentBossKind = BossKind.Final;
                }
            }

            float mult = bossThisCycleIsFinal ? finalBossHpMultiplier : 1f;
            int hp = Mathf.RoundToInt(bossHealth * currentHpScale * mult);
            currentEnemy = new Enemy(EnemyType.Boss, hp);

            if (waveText)
            {
                waveText.text = bossThisCycleIsFinal
                    ? $"Wave {cycleNumber} • FINAL BOSS"
                    : $"Wave {cycleNumber} • BOSS: {BossLabel(currentBossKind)}";
            }
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
        if (specialMeterBar)
        {
            specialMeterBar.maxValue = specialMeterMax;
            specialMeterBar.value = specialMeter;
        }

        if (specialBarFill)
        {
            specialBarFill.color = (specialMeter >= specialMeterMax) ? readyColor : buildingColor;
        }

        if (specialButton && !inputLocked)
        {
            specialButton.interactable = specialMeter >= specialMeterMax;
        }
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

    string BuildEnemyStatusText()
    {
        List<string> l = new List<string>();
        if (enemyBurnTurns > 0) l.Add($"Burning({enemyBurnTurns})");
        if (enemySoaked) l.Add("Soaked");
        if (enemySunLocked) l.Add("Rooted");
        return l.Count > 0 ? string.Join(" • ", l) : "";
    }

    string BuildPlayerStatusText()
    {
        List<string> l = new List<string>();
        if (playerBurnTurns > 0) l.Add($"Burning({playerBurnTurns})");
        if (playerSoaked) l.Add("Soaked");
        if (playerSunLocked) l.Add("Rooted");
        return l.Count > 0 ? string.Join(" • ", l) : "";
    }

    void RefreshStatusUI()
    {
        if (enemyStatusText) enemyStatusText.text = BuildEnemyStatusText();
        if (playerStatusText) playerStatusText.text = BuildPlayerStatusText();
    }

    void OnPlayerMove(Move playerMove)
    {
        StartCoroutine(RoundWithCountdown(playerMove));
    }

    public void OnSpecialPressed()
    {
        if (specialMeter < specialMeterMax) return;
        if (!TryLockInput()) return;
        animator.SetBool("isSpecialing", true);
        StartCoroutine(RoundWithCountdownSpecial());
    }

    void ApplyOutcome(Move playerMove, Move enemyMove)
    {
        int outcome = Resolve(playerMove, enemyMove);

        if (outcome > 0)
        {
            int dmg = playerDamage;

            if (unlockSoak && playerMove == Move.Sun && enemySoaked)
            {
                dmg += 2;
                enemySoaked = false;
            }

            currentEnemy.Health = Mathf.Max(0, currentEnemy.Health - dmg);
            if (enemyHpText) StartCoroutine(ShakeRect(enemyHpText.rectTransform, shakeDuration, shakeMagnitude));

            if (unlockSoak && playerMove == Move.Water) enemySoaked = true;
            if (unlockRoot && playerMove == Move.Soil) enemySunLocked = true;
            if (unlockBurn && playerMove == Move.Sun) enemyBurnTurns = 2;
        }
        else if (outcome < 0)
        {
            int dmg = (currentEnemy.Type == EnemyType.Boss) ? bossDamage : normalDamage;

            if (playerSoaked && enemyMove == Move.Sun) { dmg += 2; playerSoaked = false; }

            if (currentEnemy.Type == EnemyType.Boss)
            {
                bool final = (currentBossKind == BossKind.Final);
                bool canInflictThisMove =
                    final || MoveMatchesBoss(enemyMove, currentBossKind);

                if (canInflictThisMove)
                {
                    if (enemyMove == Move.Water) playerSoaked = true;
                    if (enemyMove == Move.Soil) playerSunLocked = true;
                    if (enemyMove == Move.Sun) playerBurnTurns = 2;
                }
            }

            playerHealth = Mathf.Max(0, playerHealth - dmg);
            if (playerHpText) StartCoroutine(ShakeRect(playerHpText.rectTransform, shakeDuration, shakeMagnitude));
        }

        if (enemyBurnTurns > 0) { currentEnemy.Health = Mathf.Max(0, currentEnemy.Health - 1); enemyBurnTurns--; }
        if (playerBurnTurns > 0) { playerHealth = Mathf.Max(0, playerHealth - 1); playerBurnTurns--; }

        IncrementSpecialMeter(1);

        RefreshUI();
        RefreshStatusUI();
        bool enemyDeadNow  = (currentEnemy != null && currentEnemy.Health <= 0);
        bool playerDeadNow = (playerHealth <= 0);
        if (!enemyDeadNow && !playerDeadNow)
        {
            MaybeQuip();
        }
        CheckRoundOver();
    }

    string MoveToText(Move m)
    {
        switch (m)
        {
            case Move.Sun: return "Sun";
            case Move.Water: return "Water";
            case Move.Soil: return "Soil";
            default: return m.ToString();
        }
    }


    public void OnSun() => OnPlayerMove(Move.Sun);
    public void OnWater() => OnPlayerMove(Move.Water);
    public void OnSoil() => OnPlayerMove(Move.Soil);

    Move GetEnemyMove(EnemyType type)
    {
        if (enemySunLocked)
        {
            enemySunLocked = false;
            return (Random.Range(0, 2) == 0) ? Move.Water : Move.Soil;
        }
        return (Move)Random.Range(0, 3);
    }


    int Resolve(Move p, Move e)
    {
        if (p == e) return 0;

        bool playerWins =
            (p == Move.Soil && e == Move.Sun) ||
            (p == Move.Sun && e == Move.Water) ||
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

            setNumber = 1;
            currentHpScale = 1f;
            StartNewSet();

            StartNewCycle();
            return;
        }

        if (currentEnemy != null && currentEnemy.Health <= 0)
        {
            if (currentEnemy.Type == EnemyType.Normal)
            {
                playerHealth = playerMaxHealth;
                normalsRemainingInCycle--;

                StartCoroutine(EnemySlideTransition(PostDefeatAction.SpawnNextEnemy));
                return;
            }
            else
            {
                if (!bossThisCycleIsFinal)
                {
                    if (currentBossKind == BossKind.Sun)   unlockBurn = true;
                    if (currentBossKind == BossKind.Water) unlockSoak = true;
                    if (currentBossKind == BossKind.Soil)  unlockRoot = true;
                    pendingBosses.RemoveAt(0);
                }
                else
                {
                    setNumber++;
                    currentHpScale *= hpScalePerSet;
                    StartNewSet();
                }

                cycleNumber++;
                StartCoroutine(EnemySlideTransition(PostDefeatAction.StartNewCycle));
                return;
            }
        }
        RefreshUI();
        RefreshStatusUI();
    }

    void RefreshUI()
    {
        if (playerHpText) playerHpText.text = $"Player HP: {playerHealth}/{playerMaxHealth}";
        if (enemyHpText) enemyHpText.text = currentEnemy != null
            ? $"Enemy HP: {currentEnemy.Health}/{currentEnemy.MaxHealth}"
            : $"Enemy HP: -";
        animator.SetBool("isAttacking", false);
        animator.SetBool("isSpecialing", false);

        RefreshStatusUI();
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
        animator.SetBool("isAttacking", true);
        if (currentEnemy == null) yield break;
        if (playerSunLocked && playerMove != Move.Sun) playerSunLocked = false;

        Move enemyMove = GetEnemyMove(currentEnemy.Type);

        if (playerCadenceText) playerCadenceText.text = "";
        if (enemyCadenceText) enemyCadenceText.text = "";

        foreach (var word in cadenceWords)
        {
            if (playerCadenceText) playerCadenceText.text = word;
            if (enemyCadenceText) enemyCadenceText.text = word;
            yield return new WaitForSeconds(beatSeconds);
        }

        if (playerCadenceText) playerCadenceText.text = $"{MoveToText(playerMove)}!";
        if (enemyCadenceText) enemyCadenceText.text = $"{MoveToText(enemyMove)}!";
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
        if (enemyCadenceText) enemyCadenceText.text = "";

        foreach (var word in cadenceWords)
        {
            if (playerCadenceText) playerCadenceText.text = word;
            if (enemyCadenceText) enemyCadenceText.text = word;
            yield return new WaitForSeconds(beatSeconds);
        }

        if (playerCadenceText) playerCadenceText.text = "Special!";
        if (enemyCadenceText) enemyCadenceText.text = $"{MoveToText(enemyMove)}!";
        yield return new WaitForSeconds(beatSeconds);
        float chance = CurrentSpecialChance();
        bool hit = true;
        //  Random.value < chance;

        ConsumeSpecialMeter();

        if (hit)
        {
            specialHitStreak++;
            currentEnemy.Health = Mathf.Max(0, currentEnemy.Health - specialDamage);
            if (enemyHpText) StartCoroutine(ShakeRect(enemyHpText.rectTransform, shakeDuration, shakeMagnitude));
            StartCoroutine(ShakeScreen(specialShakeDuration, specialShakeMagnitude));
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

    System.Collections.IEnumerator ShakeScreen(float duration, float magnitude)
    {
        Vector3 uiOrig = Vector3.zero;
        Vector3 camOrig = Vector3.zero;

        if (uiRootToShake) uiOrig = uiRootToShake.localPosition;
        if (cameraToShake) camOrig = cameraToShake.localPosition;

        float t = 0f;
        while (t < duration)
        {
            Vector2 jitter = Random.insideUnitCircle * magnitude;

            if (uiRootToShake)
                uiRootToShake.localPosition = uiOrig + new Vector3(jitter.x, jitter.y, 0f);

            if (cameraToShake)
                cameraToShake.localPosition = camOrig + new Vector3(jitter.x, jitter.y, 0f) * 0.02f; // tweak

            t += Time.deltaTime;
            yield return null;
        }

        if (uiRootToShake) uiRootToShake.localPosition = uiOrig;
        if (cameraToShake) cameraToShake.localPosition = camOrig;
    }
    System.Collections.IEnumerator TweenAnchored(RectTransform rt, Vector2 from, Vector2 to, float dur)
    {
        float t = 0f;
        while (t < dur)
        {
            float u = Mathf.SmoothStep(0f, 1f, t / dur);
            rt.anchoredPosition = Vector2.LerpUnclamped(from, to, u);
            t += Time.deltaTime;
            yield return null;
        }
        rt.anchoredPosition = to;
    }

    System.Collections.IEnumerator EnemySlideTransition(PostDefeatAction action)
    {
        inputLocked = true;
        SetButtonsInteractable(false);

        RectTransform rt = enemyImage ? enemyImage.rectTransform : null;
        Vector2 home = rt ? rt.anchoredPosition : Vector2.zero;
        Vector2 offRight = home + new Vector2(enemySlideDistance, 0f);

        if (rt) yield return StartCoroutine(TweenAnchored(rt, home, offRight, enemySlideDuration));

        if (action == PostDefeatAction.StartNewCycle) StartNewCycle();
        else                                          SpawnNextEnemy();

        if (rt)
        {
            Vector2 offLeft = home - new Vector2(enemySlideDistance, 0f);
            rt.anchoredPosition = offLeft;

            yield return StartCoroutine(TweenAnchored(rt, offLeft, home, enemySlideDuration));
        }

        inputLocked = false;
        SetButtonsInteractable(true);
    }
}