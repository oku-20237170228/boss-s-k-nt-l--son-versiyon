using UnityEngine;

public class AnimationRelay : MonoBehaviour
{
    private Player_Script parentScript;

    void Start()
    {
        // Üst objemizdeki ana scripti buluyoruz
        parentScript = GetComponentInParent<Player_Script>();
    }

    // --- BURAYI DÜZELTTİK ---
    // Artık PlayAttackSwing yok, yerine PlayComboSound var.
    
    public void PlayComboSound(int comboNo)
    {
        if(parentScript != null) parentScript.PlayComboSound(comboNo);
    }

    public void PlayFootstep()
    {
        if(parentScript != null) parentScript.PlayFootstep();
    }
}