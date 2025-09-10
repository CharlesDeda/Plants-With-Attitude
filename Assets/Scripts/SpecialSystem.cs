using UnityEngine;
using UnityEngine.UI;

public class SpecialSystem : MonoBehaviour
{
    [Header("Special")]
    public int specialMeterMax = 7;
    public int specialDamage = 5;
    public Button specialButton;
    public Slider specialMeterBar;

    [Header("Special Visuals (optional)")]
    public Image specialBarFill;
    public Color readyColor = Color.yellow;
    public Color buildingColor = Color.green;

    [Header("Screen Shake (special)")]
    public float specialShakeDuration = 1.5f;
    public float specialShakeMagnitude = 30f;

    public int specialBonusVsSoaked = 2;
    public int specialBonusVsRooted = 1;
    public int specialBonusVsBurning = 1;
    public bool specialConsumesSoaked = true;

    public int SpecialMeter { get; private set; } = 0;
    public int HitStreak { get; private set; } = 0;
    public bool IsReady => SpecialMeter >= specialMeterMax;

    public System.Action OnBecameReady;

    bool wasReadyLast;

    void Awake()
    {
        if (specialMeterBar)
        {
            specialMeterBar.wholeNumbers = true;
            specialMeterBar.minValue = 0;
            specialMeterBar.maxValue = specialMeterMax;
            specialMeterBar.interactable = false;
            specialMeterBar.handleRect = null;
            specialMeterBar.direction = Slider.Direction.LeftToRight;
        }
        RefreshUI();
    }

    public void Increment(int amt = 1)
    {
        SpecialMeter = Mathf.Clamp(SpecialMeter + amt, 0, specialMeterMax);
        RefreshUI();
    }

    public void Consume()
    {
        SpecialMeter = 0;
        HitStreak = 0;
        RefreshUI();
    }

    public void AddHitStreak() { HitStreak++; }

    public int CalculateDamage(bool enemySoaked, bool enemySunLocked, int enemyBurnTurns)
    {
        int dmg = specialDamage;
        if (enemySoaked) dmg += specialBonusVsSoaked;
        if (enemySunLocked) dmg += specialBonusVsRooted;
        if (enemyBurnTurns > 0) dmg += specialBonusVsBurning;
        return dmg;
    }

    public void RefreshUI()
    {
        if (specialMeterBar)
        {
            specialMeterBar.maxValue = specialMeterMax;
            specialMeterBar.value = SpecialMeter;
        }
        if (specialBarFill) specialBarFill.color = IsReady ? readyColor : buildingColor;

        if (!wasReadyLast && IsReady) OnBecameReady?.Invoke();
        wasReadyLast = IsReady;

        if (specialButton) specialButton.interactable = IsReady;
    }
}
