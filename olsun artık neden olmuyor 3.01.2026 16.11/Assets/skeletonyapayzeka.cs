using System.Collections;
using UnityEngine;

public class skeletonyapayzeka : MonoBehaviour
{
    [Header("Hedef")]
    public Transform player;
    private Player_Script playerScript;
    private Rigidbody2D rb;
    private Animator anim;
    private AudioSource audioSource;

    private Collider2D myCollider;
    private Collider2D playerCollider;

    [Header("Mesafe Ayarları")]
    public float chaseDist = 8f;     
    public float attackDist = 1.0f;  
    public float stopDist = 0.7f;    

    [Header("Hareket")]
    public float moveSpeed = 1.5f;   
    public bool spriteYonuTers = false;

    [Header("Saldırı")]
    public float attackCooldown = 2.5f; 
    public int damageToPlayer = 25;
    private float nextAttackTime = 0f;

    [Header("Kalkan ve Sersemleme")]
    public bool hasShield = true;    
    [Range(0, 100)] public int blockChance = 40; 
    public AudioClip blockSound;     
    public float stunDuration = 0.75f; 

    [Header("Devriye")]
    public float patrolSpeed = 1f;
    public float patrolTime = 3f;
    public float waitTime = 2f;

    [Header("Durum")]
    public int health = 5;
    private bool isDead = false;
    private bool isAttacking = false;
    private bool isStunned = false; 
    private float defaultScaleX;
    
    private float patrolTimer;
    private float waitTimer;
    private bool isWalking = false;
    private int patrolDirection = 1;

    void Awake()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        myCollider = GetComponent<Collider2D>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        defaultScaleX = Mathf.Abs(transform.localScale.x);
    }

    void Start()
    {
        patrolTimer = patrolTime;
        waitTimer = waitTime;

        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
            {
                player = p.transform;
                playerScript = p.GetComponent<Player_Script>();
                playerCollider = p.GetComponent<Collider2D>();
            }
        }
    }

    void Update()
    {
        if (isDead || player == null || playerCollider == null || isStunned) return;

        if (isAttacking)
        {
            StopMovement();
            return;
        }

        ColliderDistance2D colDist = myCollider.Distance(playerCollider);
        float distanceGap = colDist.distance; 

        int direction = (player.position.x > transform.position.x) ? 1 : -1;

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
        else if (distanceGap <= chaseDist && distanceGap > stopDist)
        {
            anim.SetBool("isRunning", true);
            FlipSprite(direction);
            rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);
        }
        else if (distanceGap <= stopDist)
        {
            StopMovement();
            FlipSprite(direction);
        }
        else
        {
            PatrolLogic();
        }
    }

    IEnumerator StunRoutine()
    {
        isStunned = true;       
        isAttacking = false;    
        StopMovement();         
        
        anim.SetTrigger("block"); 
        if (blockSound != null) audioSource.PlayOneShot(blockSound);

        yield return new WaitForSeconds(stunDuration);

        // Uyanma vakti!
        anim.SetTrigger("idle"); 
        isStunned = false;       
    }

    // --- BURASI DÜZELTİLDİ (SONSUZ STUN FİXLENDİ) ---
    public void TakeDamage(int damage)
    {
        if (isDead) return;

        // 1. Blok Kontrolü (Sadece sersemlememişse blok yapabilir)
        if (hasShield && !isStunned) 
        {
            int sans = Random.Range(0, 100);
            if (sans < blockChance)
            {
                StopAllCoroutines(); // Saldırıyorsa durdur
                StartCoroutine(StunRoutine()); // Sersemlet
                return; // Hasar alma
            }
        }

        // 2. Hasar Alma
        health -= damage;

        // EĞER ZATEN SERSEMLEMİŞSE, SAKIN "StopAllCoroutines" YAPMA!
        // Yaparsan uyanma sayacını öldürürsün.
        if (!isStunned) 
        {
            StopAllCoroutines(); // Saldırıyı kes
            isAttacking = false;
        }

        anim.SetTrigger("hit");
        StopMovement();

        if (health <= 0) Die();
    }
    // ---------------------------------------------------

    IEnumerator AttackRoutine()
    {
        isAttacking = true;
        StopMovement();
        anim.SetTrigger("attack");

        yield return new WaitForSeconds(0.5f);

        if (player != null && !isDead && !isStunned) 
        {
            ColliderDistance2D colDist = myCollider.Distance(playerCollider);
            if (colDist.distance <= attackDist + 0.5f)
            {
                if (playerScript != null) playerScript.TriggerHurt(damageToPlayer);
            }
        }

        yield return new WaitForSeconds(0.8f); 
        isAttacking = false;
    }

    void PatrolLogic()
    {
        if (isWalking)
        {
            anim.SetBool("isRunning", true);
            rb.linearVelocity = new Vector2(patrolDirection * patrolSpeed, rb.linearVelocity.y);
            FlipSprite(patrolDirection);

            patrolTimer -= Time.deltaTime;
            if (patrolTimer <= 0)
            {
                isWalking = false;
                waitTimer = waitTime;
                StopMovement();
            }
        }
        else
        {
            StopMovement();
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0)
            {
                isWalking = true;
                patrolTimer = patrolTime;
                patrolDirection = Random.Range(0, 2) == 0 ? -1 : 1;
            }
        }
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

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackDist + 0.5f);
    }
}