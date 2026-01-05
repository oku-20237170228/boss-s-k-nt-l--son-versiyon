using System.Collections;
using UnityEngine;

public class goblinyapayzeka : MonoBehaviour
{
    [Header("Hedef")]
    public Transform player;
    private Player_Script playerScript;
    private Rigidbody2D rb;
    private Animator anim;
    
    // YENİ: İki collider arasındaki mesafeyi ölçmek için
    private Collider2D myCollider;
    private Collider2D playerCollider;

    [Header("Mesafe Ayarları (Kabuktan Kabuğa)")]
    public float chaseDist = 8f;      // Takip mesafesi
    public float attackDist = 0.5f;   // DİKKAT: Bu artık "Aradaki Boşluk". Yani kılıç boyu. (Küçük olmalı)
    public float stopDist = 0.2f;     // Neredeyse dibine kadar gelip dursun (0.2 birim kala)

    [Header("Saldırı")]
    public float attackCooldown = 1.5f;
    public int damageToPlayer = 20;

    [Header("Hareket")]
    public float moveSpeed = 3f;
    public bool spriteYonuTers = false;

    [Header("Durum")]
    public int health = 3;
    private bool isDead = false;
    private bool isAttacking = false;
    private float nextAttackTime = 0f;
    private float defaultScaleX;

    void Awake()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        myCollider = GetComponent<Collider2D>(); // Kendi collider'ımızı al
        defaultScaleX = Mathf.Abs(transform.localScale.x);
    }

    void Start()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
            {
                player = p.transform;
                playerScript = p.GetComponent<Player_Script>();
                playerCollider = p.GetComponent<Collider2D>(); // Oyuncunun collider'ını al
            }
        }
    }

    void Update()
    {
        if (isDead || player == null || playerCollider == null) return;

        if (isAttacking)
        {
            StopMovement();
            return;
        }

        // --- EN KRİTİK DEĞİŞİKLİK BURADA ---
        // Artık merkezden merkeze değil, "Et Ete" (Collider yüzeyleri arasındaki) mesafeyi ölçüyoruz.
        // Bu yöntem sağ/sol dönme hatalarını YOK EDER.
        ColliderDistance2D colliderDistance = myCollider.Distance(playerCollider);
        float distanceGap = colliderDistance.distance; // Aradaki net boşluk (Metre cinsinden)

        // Yön Hesabı (Klasik)
        int direction = (player.position.x > transform.position.x) ? 1 : -1;

        // --- MANTIK ---

        // 1. SALDIRI (Aradaki boşluk attackDist'ten azsa)
        if (distanceGap <= attackDist)
        {
            StopMovement(); 
            FlipSprite(direction);

            if (Time.time >= nextAttackTime)
            {
                StartCoroutine(AttackRoutine());
                nextAttackTime = Time.time + attackCooldown;
            }
        }
        // 2. TAKİP (Görüş alanındaysa VE durma mesafesinden uzaksa)
        else if (distanceGap <= chaseDist && distanceGap > stopDist)
        {
            MoveToPlayer(direction);
        }
        // 3. DUR (Çok yaklaştıysa)
        else if (distanceGap <= stopDist)
        {
            StopMovement();
            FlipSprite(direction);
        }
        else
        {
            StopMovement();
        }
    }

    void MoveToPlayer(int dir)
    {
        anim.SetBool("isRunning", true);
        FlipSprite(dir);
        rb.linearVelocity = new Vector2(dir * moveSpeed, rb.linearVelocity.y);
    }

    void StopMovement()
    {
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        anim.SetBool("isRunning", false);
    }

    void FlipSprite(int direction)
    {
        float currentScaleX = transform.localScale.x;
        int targetSign = direction * (spriteYonuTers ? -1 : 1);
        
        if (Mathf.Sign(currentScaleX) != Mathf.Sign(targetSign))
        {
            Vector3 newScale = transform.localScale;
            newScale.x = defaultScaleX * targetSign;
            transform.localScale = newScale;
        }
    }

    IEnumerator AttackRoutine()
    {
        isAttacking = true;
        StopMovement();
        anim.SetTrigger("attack");

        yield return new WaitForSeconds(0.4f); 

        if (player != null && !isDead)
        {
            // Vuruş anında tekrar aradaki boşluğu ölç
            ColliderDistance2D colDist = myCollider.Distance(playerCollider);
            
            // Toleranslı vuruş (+0.5f ekledik ki kaçarken ucu ucuna vurabilsin)
            if (colDist.distance <= attackDist + 0.5f)
            {
                if (playerScript != null) playerScript.TriggerHurt(damageToPlayer);
            }
        }

        yield return new WaitForSeconds(0.5f);
        isAttacking = false;
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;
        health -= damage;
        anim.SetTrigger("hit");
        StopMovement();
        isAttacking = false;
        if (health <= 0) Die();
    }

    void Die()
    {
        isDead = true;
        anim.SetTrigger("death");
        StopMovement();
        GetComponent<Collider2D>().enabled = false;
        rb.gravityScale = 0;
        this.enabled = false;
    }

    void OnDrawGizmosSelected()
    {
        // Bu modda gizmos çizmek zor çünkü dinamik hesap yapıyoruz
        // Ama kabaca merkezden göstermek gerekirse:
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackDist + 1f); // Tahmini
    }
}