using UnityEngine;

public class BossSpell : MonoBehaviour
{
    [Header("Ayarlar")]
    public float speed = 7f;      // Aşağı düşme hızı
    public int damage = 20;       // Vereceği hasar
    public float lifeTime = 3f;   // Kaç saniye sonra yok olsun

    private bool hitPlayer = false; // Aynı anda 50 kere vurmasın diye kontrol

    void Start()
    {
        // 3 saniye sonra otomatik silinsin (ıskalarsa diye)
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        // Aşağı doğru hareket et
        transform.Translate(Vector2.down * speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hitPlayer) return; // Zaten vurduysak işlem yapma

        // Oyuncuya çarptı mı?
        if (other.CompareTag("Player"))
        {
            Player_Script player = other.GetComponent<Player_Script>();
            
            if (player != null)
            {
                // --- KRİTİK KISIM: BLOK KONTROLÜ ---
                if (player.isBlocking)
                {
                    Debug.Log("🛡️ OYUNCU BÜYÜYÜ KALKANLA DURDURDU!");
                    // İstersen burada "Cling!" diye metal sesi çaldırabilirsin
                    player.TriggerBlockHit(); // Oyuncunun blok animasyonunu oynat
                }
                else
                {
                    Debug.Log("🔥 OYUNCU BÜYÜYÜ YEDİ!");
                    player.TriggerHurt(damage);
                }

                hitPlayer = true;
                
                // Büyü yok olsun (Çarpma efekti varsa Instantiate edebilirsin)
                Destroy(gameObject); 
            }
        }
        // Zemine çarparsa da yok olsun
        else if (other.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            Destroy(gameObject);
        }
    }
}