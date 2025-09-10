using UnityEngine;

public class AnimationControllerBridge : MonoBehaviour
{
    public Animator animator;

    public void SetAttacking(bool on)
    {
        if (animator) animator.SetBool("isAttacking", on);
    }

    public void SetSpecialing(bool on)
    {
        if (animator) animator.SetBool("isSpecialing", on);
    }

    public void GoIdle(string idleStateName = "Idle")
    {
        if (animator) animator.Play(idleStateName, 0, 0f);
    }
}
