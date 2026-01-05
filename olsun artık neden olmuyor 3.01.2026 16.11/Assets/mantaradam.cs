using System.Collections;
using UnityEngine;

public class MushroomYapayZeka : MonoBehaviour
{
    [Header("Hedef")]
    public Transform player;
    private Player_Script playerScript;
    private Rigidbody2D rb;
    private Animator anim;
    
    [Header("ÇOK ÖNEMLİ: KUTUYU BURAYA SÜRÜKLE")]
    public Collider2D vucutCollider; 
    private Collider2D playerCollider;

    [Header("Hareket Ayarları")]
    public float moveSpeed = 2f;      
    public bool spriteYonuTers = false; 

    [Header("Mesafe Ayarları")]
    public float chaseDist = 7f;      
    public float attackDist = 0.8f;   
    public float stopDist = 0.6f;     

    [Header("Saldırı Ayarları")]
    public int damage = 10;
    public float attackCooldown = 2f; // İki saldırı arası bekleme
    private float nextAttackTime = 0f;

    [Header("SALDIRI ZAMANLAMASI (AYARLA)")]
    [Tooltip("Animasyon başladıktan kaç saniye sonra hasar yesin?")]
    public float hasarGecikmesi = 0.5f; // <-- İŞTE BU SENİN AYARLAYACAĞIN KISIM

    [Header("Durum")]
    public int health = 4; 
    private bool isDead = false;
    private bool isAttacking = false;
    private float defaultScaleX;

    void Awake()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        
        if (vucutCollider == null) 
            vucutCollider = GetComponent<Collider2D>();
        
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
                playerCollider = p.GetComponent<Collider2D>();
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

        ColliderDistance2D colDist = vucutCollider.Distance(playerCollider);
        float distanceGap = colDist.distance; 

        int direction = (player.position.x > transform.position.x) ? 1 : -1;

        // 1. SALDIRI
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
        // 2. KOVALAMA
        else if (distanceGap <= chaseDist && distanceGap > stopDist)
        {
            MoveToPlayer(direction);
        }
        // 3. BEKLEME
        else
        {
            StopMovement();
            if (distanceGap <= stopDist) FlipSprite(direction);
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

        // --- BURAYI DEĞİŞTİRDİK ---
        // Senin belirlediğin süre kadar bekliyor (Vuruş anına denk getirmek için)
        yield return new WaitForSeconds(hasarGecikmesi);

        // Tam o saniyede hala menzilde miyiz?
        if (player != null && !isDead)
        {
            ColliderDistance2D colDist = vucutCollider.Distance(playerCollider);
            if (colDist.distance <= attackDist + 0.5f)
            {
                if (playerScript != null) playerScript.TriggerHurt(damage);
            }
        }

        // Animasyonun geri kalanı ve cooldown için biraz daha bekle
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
        StopMovement();
        
        rb.gravityScale = 1; 
        if (vucutCollider != null) vucutCollider.enabled = false;
        
        this.enabled = false;
        Destroy(gameObject, 3f); 
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        if (vucutCollider != null)
            Gizmos.DrawWireSphere(vucutCollider.bounds.center, attackDist);
    }
}