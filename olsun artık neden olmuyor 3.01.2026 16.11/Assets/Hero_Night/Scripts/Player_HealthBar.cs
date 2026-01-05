using UnityEngine;
using UnityEngine.UI;

public class Player_HealthBar : MonoBehaviour
{
    [Header("UI Görselleri")]
    public Image BackGroundImg; // Arkadaki yavaş azalan bar (Sarı/Beyaz)
    public Image HealthImg;     // Öndeki ana can barı (Kırmızı/Yeşil)

    [Header("Değerler")]
    public float MaxHealth = 100f;
    public float CurrentHealth;

    public bool isDead = false;

    void Start()
    {
        CurrentHealth = MaxHealth;
        isDead = false;

        // Başlangıçta barlar tam dolu olsun
        if (HealthImg != null) HealthImg.fillAmount = 1f;
        if (BackGroundImg != null) BackGroundImg.fillAmount = 1f;
    }

    void Update()
    {
        // Efekt: Arka plan barının yavaşça ana bara yaklaşması (Lerp)
        if (BackGroundImg != null && HealthImg != null)
        {
            BackGroundImg.fillAmount = Mathf.Lerp(
                BackGroundImg.fillAmount,
                HealthImg.fillAmount,
                Time.deltaTime * 5f
            );
        }
    }

    // Bu fonksiyonu Player_Script çağırır
    public void GetDamage(float damage)
    {
        if (isDead) return;

        CurrentHealth -= damage;

        // Can 0'ın altına düşerse
        if (CurrentHealth <= 0)
        {
            CurrentHealth = 0;
            isDead = true;
            Debug.Log("HealthBar: Oyuncu Öldü.");
        }

        // Sadece görseli güncelle, animasyonu Player_Script oynatır
        UpdateUI();
    }

    public void Heal(float amount)
    {
        if (isDead) return;

        CurrentHealth += amount;
        if (CurrentHealth > MaxHealth) CurrentHealth = MaxHealth;
        
        UpdateUI();
    }

    void UpdateUI()
    {
        if (HealthImg != null)
        {
            HealthImg.fillAmount = CurrentHealth / MaxHealth;
        }
    }
}