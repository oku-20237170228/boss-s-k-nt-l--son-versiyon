using System.Collections;
using TMPro;
using UnityEngine;

public class BossEnemyAI : MonoBehaviour
{
    [Header("Hedef ve Referanslar")]
    public Transform player;

    // B�LE�ENLER
    private Rigidbody2D rb;          // Parent'ta
    private Collider2D myCollider;   // Parent'ta
    private Animator anim;           // Child'da (Boss_Sprite)

    [Header("Boss �statistikleri")]
    public float maxHealth = 500f;
    public float currentHealth;
    public float moveSpeed = 2.5f;

    [Header("Mesafe Ayarlar�")]
    public float chaseDist = 5f;    // Takip Mesafesi (B�y�tt�m)
    public float spellDist = 3f;     // B�y� Mesafesi
    public float attackDist = 1f;  // Yak�n Vuru� (Collider b�y�d��� i�in bunu da art�rd�k)
    public float stopDist = 0.8f;    // Durma mesafesi

    [Header("Bekleme S�releri")]
    public float attackCooldown = 2f;
    public float spellCooldown = 4f;
    public float healCooldown = 15f;
    public int meleeDamage = 15;
    public int spellDamage = 10;
    public int healAmount = 30;

    [Header("G�rsel Ayarlar")]
    public Transform visualChild; // Boss_Sprite objesi (Inspector'dan atayaca��z)
    public bool spriteYonuTers = false;

    // Durumlar
    private bool isDead = false;
    private bool isBusy = false;
    private float nextAttackTime = 0f;
    private float nextSpellTime = 0f;
    private float nextHealTime = 0f;

    public TMP_Text deathTex;

    private int lastDirection = 1;
    private float flipDeadzone = 0.2f; // küçük mesafelerde flip olmasın


    public Player_Script playerScript;
    public GameObject attackHitbox;

    private AudioSource audioSource;
    public AudioClip YouDied_Sound;

    void Awake()
    {
        // Parent �zerindekiler
        rb = GetComponent<Rigidbody2D>();
        myCollider = GetComponent<Collider2D>();

        // Child �zerindeki Animator'� bul (�NEML� D�ZELTME)
        anim = GetComponentInChildren<Animator>();

        currentHealth = maxHealth;

        // G�rsel child objeyi otomatik bulmaya �al��al�m (Flip i�in laz�m)
        if (visualChild == null && transform.childCount > 0)
        {
            visualChild = transform.GetChild(0);
        }

        // Hata Kontrol�
        if (anim == null) Debug.LogError("HATA: Boss_Sprite �zerinde Animator bulunamad�!");
        if (myCollider == null) Debug.LogError("HATA: BossEnemy �zerinde Collider yok!");

        audioSource = GetComponent<AudioSource>();
    }

    void Start()
    {
        playerScript = FindObjectOfType<Player_Script>();
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
            {
                player = p.transform;
                // player script genelde root'ta olur, bunu kullan:
                playerScript = p.GetComponent<Player_Script>();
            }
        }

        // Hitbox başlangıçta kapalı olsun
        if (attackHitbox != null) attackHitbox.SetActive(false);

        // lastDirection'ı görsel scale'den türet (negatifse -1 olsun)
        if (visualChild != null)
            lastDirection = visualChild.localScale.x >= 0 ? 1 : -1;
    }


    void Update()
    {
        if (isDead || player == null) return;
        if (isBusy) { StopMovement(); return; }

        // MESAFE �L��M� (Collider merkezinden)
        float dist = Vector2.Distance(myCollider.bounds.center, player.position);

        // Y�n Hesab�
        int direction = (player.position.x > transform.position.x) ? 1 : -1;

        // --- YAPAY ZEKA ---

        // 1. Can Basma
        if (!isBusy && currentHealth < (maxHealth * 0.4f) && Time.time >= nextHealTime)
        {
            StartCoroutine(ActionRoutine("cast", 1.5f, () =>
            {
                currentHealth += healAmount;
                if (currentHealth > maxHealth) currentHealth = maxHealth;
                nextHealTime = Time.time + healCooldown;
            }));
        }

        // 2. Yak�n Sald�r�
        else if (dist <= attackDist)
        {
            StopMovement();
            FacePlayer(direction);
            if (Time.time >= nextAttackTime)
            {
                StartCoroutine(ActionRoutine("attack", 0.6f, () => {
                    // Hasar ver
                    if (playerScript != null)
                    {
                        Debug.Log("BOSS PLAYER'A HASAR VERDİ: " + meleeDamage);
                        playerScript.TriggerHurt(meleeDamage);
                    }
                    else
                    {
                        Debug.LogError("playerScript NULL! Boss hasar veremiyor");
                    }

                }));
                nextAttackTime = Time.time + attackCooldown;
            }
        }
        // 3. B�y� Atma (Spell)
        else if (dist <= spellDist && dist > attackDist)
        {
            StopMovement();
            FacePlayer(direction);
            if (Time.time >= nextSpellTime)
            {
                StartCoroutine(ActionRoutine("spell", 0.8f, () => {
                    // Uzaktan hasar
                    if (playerScript != null) playerScript.TriggerHurt(spellDamage);
                }));
                nextSpellTime = Time.time + spellCooldown;
            }
            else if (dist > stopDist) // Cooldown'daysa bo� durma y�r�
            {
                MoveToPlayer(direction);
            }
        }
        // 4. Takip
        else if (dist <= chaseDist && dist > stopDist)
        {
            MoveToPlayer(direction);
        }
        else
        {
            StopMovement();
        }

    }

    void MoveToPlayer(int dir)
    {
        audioSource.PlayOneShot(YouDied_Sound);
        anim.SetBool("walk", true);
        FacePlayer(dir);
        rb.linearVelocity = new Vector2(dir * moveSpeed, rb.linearVelocity.y);
    }

    void StopMovement()
    {
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        anim.SetBool("walk", false);
    }

    void FacePlayer(int direction)
    {
        // Sadece Child objeyi (G�rseli) �eviriyoruz
        if (visualChild != null)
        {
            Vector3 scale = visualChild.localScale;
            // ��areti kontrol et
            if (direction > 0) scale.x = Mathf.Abs(scale.x) * (spriteYonuTers ? -1 : 1);
            else scale.x = -Mathf.Abs(scale.x) * (spriteYonuTers ? -1 : 1);

            visualChild.localScale = scale;
        }
    }

    // Genel Aksiyon Y�neticisi (Attack, Spell, Heal i�in tek fonksiyon)
    IEnumerator ActionRoutine(string triggerName, float delay, System.Action onActionExecute)
    {
        isBusy = true;
        anim.SetTrigger(triggerName);
        StopMovement();

        // 🔴 Animasyonun vurma anına kadar bekle
        yield return new WaitForSeconds(delay);

        // 🔥 TAM BURASI
        if (triggerName == "attack")
        {
            Debug.Log("BOSS ATTACK ANIM - HITBOX AÇILDI");
            attackHitbox.SetActive(true);
        }

        // VURUŞ ANI
        yield return new WaitForSeconds(0.1f);

        if (triggerName == "attack")
            attackHitbox.SetActive(false);

        // Spell / heal vs için
        onActionExecute?.Invoke();

        yield return new WaitForSeconds(0.4f);
        isBusy = false;
    }



    public void TakeDamage(int damage)
    {
        Debug.Log("BOSS DAMAGE: " + damage + " | CAN ÖNCE: " + currentHealth);

        if (isDead) return;

        currentHealth -= damage;

        Debug.Log("BOSS CAN SONRA: " + currentHealth);

        if (!isBusy)
        {
            anim.SetTrigger("hurt");
            StopMovement();
        }

        if (currentHealth <= 0)
        {
            Debug.Log("BOSS DIE ÇAĞRILDI");
            Die();
        }
    }


    void Die()
    {
        if (isDead) return; // Zaten ölüyse tekrar öldürme

        isDead = true;
        isBusy = true;
        GetComponent<Collider2D>().enabled = false;
        rb.gravityScale = 0;
        rb.linearVelocity = Vector2.zero;
        deathTex.gameObject.SetActive(true);
        Debug.Log("BOSS ÖLDÜ - Animasyon Başlıyor");
        anim.SetTrigger("death");

        Destroy(gameObject, 1f);

    }

    void OnDrawGizmos()
    {
        // Gizmos art�k Collider merkezinden �iziliyor, g�rselle uyumlu olacak
        if (GetComponent<Collider2D>() != null)
        {
            Vector3 center = GetComponent<Collider2D>().bounds.center;
            Gizmos.color = Color.green; Gizmos.DrawWireSphere(center, chaseDist);
            Gizmos.color = Color.blue; Gizmos.DrawWireSphere(center, spellDist);
            Gizmos.color = Color.red; Gizmos.DrawWireSphere(center, attackDist);
        }
    }
}