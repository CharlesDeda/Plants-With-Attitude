using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class UIController : MonoBehaviour
{
    [Header("UI")]
    public Button sunButton;
    public Button waterButton;
    public Button soilButton;
    public Button specialButton;

    public TMP_Text playerHpText;
    public TMP_Text enemyHpText;
    public TMP_Text playerCadenceText;
    public TMP_Text enemyCadenceText;
    public TMP_Text enemyStatusText;
    public TMP_Text playerStatusText;
    public TMP_Text enemyQuipText;

    [Header("Shake")]
    public float shakeDuration = 1.2f;
    public float shakeMagnitude = 12f;
    public RectTransform uiRootToShake;
    public Transform cameraToShake;

    [Header("Slide")]
    public Image enemyImage;
    public float enemySlideDistance = 800f;
    public float enemySlideDuration = 0.35f;

    [Header("Audio")]
    public AudioController audioController;

    public void SetButtonsInteractable(bool on, bool playerSunLocked, bool specialReady)
    {
        if (sunButton)   sunButton.interactable = on && !playerSunLocked;
        if (waterButton) waterButton.interactable = on;
        if (soilButton)  soilButton.interactable = on;
        if (specialButton) specialButton.interactable = on && specialReady;
    }

    public void MakeNoDim(Button b)
    {
        if (!b) return;
        var cb = b.colors;
        cb.disabledColor = cb.normalColor;
        cb.fadeDuration = 0f;
        b.colors = cb;

        var ss = b.spriteState;
        ss.disabledSprite = null;
        b.spriteState = ss;
    }

    public void RefreshStatus(TMP_Text tEnemy, TMP_Text tPlayer, string enemyTxt, string playerTxt)
    {
        if (tEnemy)  tEnemy.text  = enemyTxt;
        if (tPlayer) tPlayer.text = playerTxt;
    }

    public void RefreshHP(TMP_Text pTxt, TMP_Text eTxt, int pHp, int pMax, Enemy currentEnemy)
    {
        if (pTxt) pTxt.text = $"Player HP: {pHp}/{pMax}";
        if (eTxt) eTxt.text = currentEnemy != null
            ? $"Enemy HP: {currentEnemy.Health}/{currentEnemy.MaxHealth}"
            : "Enemy HP: -";
    }

    public IEnumerator ShakeRect(RectTransform target, float dur, float mag, AudioClip clip = null, float vol = 1f, float basePitch = 1f, float jitter = 0f)
    {
        if (!target) yield break;
        if (clip && audioController) audioController.PlaySfx(clip, vol, basePitch, jitter);

        Vector2 original = target.anchoredPosition;
        float elapsed = 0f;
        while (elapsed < dur)
        {
            float x = Random.Range(-1f, 1f) * mag;
            float y = Random.Range(-1f, 1f) * mag * 0.5f;
            target.anchoredPosition = original + new Vector2(x, y);
            elapsed += Time.deltaTime;
            yield return null;
        }
        target.anchoredPosition = original;
    }

    public IEnumerator ShakeScreen(float duration, float magnitude, AudioClip clip = null, float vol = 1f, float basePitch = 1f, float jitter = 0f)
    {
        if (clip && audioController) audioController.PlaySfx(clip, vol, basePitch, jitter);

        Vector3 uiOrig = uiRootToShake ? uiRootToShake.localPosition : Vector3.zero;
        Vector3 camOrig = cameraToShake ? cameraToShake.localPosition : Vector3.zero;

        float t = 0f;
        while (t < duration)
        {
            Vector2 j = Random.insideUnitCircle * magnitude;
            if (uiRootToShake) uiRootToShake.localPosition = uiOrig + new Vector3(j.x, j.y, 0);
            if (cameraToShake) cameraToShake.localPosition = camOrig + new Vector3(j.x, j.y, 0) * 0.02f;
            t += Time.deltaTime;
            yield return null;
        }

        if (uiRootToShake) uiRootToShake.localPosition = uiOrig;
        if (cameraToShake) cameraToShake.localPosition = camOrig;
    }

    public IEnumerator EnemySlideTransition(System.Func<IEnumerator> swapAction)
    {
        RectTransform rt = enemyImage ? enemyImage.rectTransform : null;
        Vector2 home = rt ? rt.anchoredPosition : Vector2.zero;
        Vector2 offRight = home + new Vector2(enemySlideDistance, 0f);

        if (rt) yield return StartCoroutine(TweenAnchored(rt, home, offRight, enemySlideDuration));
        if (swapAction != null) yield return StartCoroutine(swapAction());
        if (rt)
        {
            Vector2 offLeft = home - new Vector2(enemySlideDistance, 0f);
            rt.anchoredPosition = offLeft;
            yield return StartCoroutine(TweenAnchored(rt, offLeft, home, enemySlideDuration));
        }
    }

    IEnumerator TweenAnchored(RectTransform rt, Vector2 from, Vector2 to, float dur)
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

    public void SetCadence(string player, string enemy)
    {
        if (playerCadenceText) playerCadenceText.text = player;
        if (enemyCadenceText)  enemyCadenceText.text  = enemy;
    }

    public void ClearCadence()
    {
        if (playerCadenceText) playerCadenceText.text = "";
        if (enemyCadenceText)  enemyCadenceText.text  = "";
    }

    public void MaybeQuip(System.Func<string> getLine, float seconds)
    {
        if (enemyQuipText == null) return;
        enemyQuipText.text = getLine();
        StartCoroutine(ClearTextAfterDelay(enemyQuipText, seconds));
    }

    IEnumerator ClearTextAfterDelay(TMP_Text t, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (t) t.text = "";
    }
}
