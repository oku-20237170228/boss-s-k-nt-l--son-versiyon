using System.Collections;
using UnityEngine;

public class Uçangöz : MonoBehaviour
{
    [Header("Hedef")]
    public Transform player;
    private Player_Script playerScript;
    private Rigidbody2D rb;
    private Animator anim;
    
    // YENİ: Mesafe ölçümü için gerekli colliderlar
    private Collider2D myCollider;
    private Collider2D playerCollider;

    [Header("Görsel Ayarlar")]
    public bool spriteSagaBakiyor = true; 

    [Header("Uçış Ayarları")]
    public float flySpeed = 3f;       
    public float chaseDist = 10f;     
    
    // DİKKAT: Artık "Kabuktan Kabuğa" ölçüyoruz, bu değerleri küçülttüm
    public float attackDist = 0.5f;   // Vücutlar arası 0.5 birim kalınca vursun
    public float stopDist = 0.2f;     // 0.2 birim kala dursun (İçine girmesin)

    [Header("Saldırı")]
    public int damage = 15;
    public float attackCooldown = 2f;
    private float nextAttackTime = 0f;

    [Header("Durum")]
    public int health = 3;
    private bool isDead = false;
    private bool isAttacking = false;

    private float initialScaleX;
    private float initialScaleY;

    void Awake()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        myCollider = GetComponent<Collider2D>(); // Kendi colliderını al
        
        initialScaleX = Mathf.Abs(transform.localScale.x);
        initialScaleY = transform.localScale.y;
    }

    void Start()
    {
        rb.gravityScale = 0; 
        rb.bodyType = RigidbodyType2D.Kinematic; 

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
            {
                player = p.transform;
                playerScript = p.GetComponent<Player_Script>();
                playerCollider = p.GetComponent<Collider2D>(); // Oyuncunun colliderını al
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

        // --- EN ÖNEMLİ DEĞİŞİKLİK BURADA ---
        // Artık Transform.position (Nokta) değil, Collider (Vücut) mesafesi ölçüyoruz.
        // Bu sayede Pivot noktası nerede olursa olsun, vücutlar birbirine yaklaşınca algılar.
        ColliderDistance2D colDist = myCollider.Distance(playerCollider);
        float distanceGap = colDist.distance; // Aradaki net boşluk

        // Yön ve Boyut Ayarı
        bool playerSagda = player.position.x > transform.position.x;
        int direction = playerSagda ? 1 : -1;
        if (!spriteSagaBakiyor) direction *= -1;
        transform.localScale = new Vector3(initialScaleX * direction, initialScaleY, 1);

        // 1. SALDIRI (Aradaki boşluk attackDist'ten azsa)
        if (distanceGap <= attackDist)
        {
            StopMovement();
            if (Time.time >= nextAttackTime)
            {
                StartCoroutine(AttackRoutine());
                nextAttackTime = Time.time + attackCooldown;
            }
        }
        // 2. KOVALAMA (Görüş alanında ve dibine girmediyse)
        else if (distanceGap <= chaseDist && distanceGap > stopDist)
        {
            // MoveTowards ile oyuncunun merkezine değil, kenarına gitmeye çalışıyoruz
            transform.position = Vector2.MoveTowards(transform.position, player.position, flySpeed * Time.deltaTime);
        }
        else
        {
            StopMovement();
        }
    }

    void StopMovement()
    {
        rb.linearVelocity = Vector2.zero;
    }

    IEnumerator AttackRoutine()
    {
        isAttacking = true;
        anim.SetTrigger("attack");

        yield return new WaitForSeconds(0.5f);

        if (player != null && !isDead)
        {
            // Vuruş anında tekrar mesafe kontrolü (Collider ile)
            ColliderDistance2D colDist = myCollider.Distance(playerCollider);
            
            // +0.5f Tolerans ekledik ki kaçarken ucu ucuna vurabilsin
            if (colDist.distance <= attackDist + 0.5f)
            {
                if (playerScript != null) playerScript.TriggerHurt(damage);
            }
        }

        yield return new WaitForSeconds(0.5f);
        isAttacking = false;
    }

    public void TakeDamage(int damageAmount)
    {
        if (isDead) return;

        health -= damageAmount;
        StopAllCoroutines();
        isAttacking = false;
        
        anim.SetTrigger("hit");

        if (health <= 0) Die();
    }

    void Die()
    {
        isDead = true;
        anim.SetTrigger("death");
        
        rb.linearVelocity = Vector2.zero;
        
        // Düşmesi için
        rb.bodyType = RigidbodyType2D.Dynamic; 
        rb.gravityScale = 1; 

        // İçinden geçilmesi için
        GetComponent<Collider2D>().enabled = false; 
        
        this.enabled = false;
        Destroy(gameObject, 2f);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackDist);
    }
}