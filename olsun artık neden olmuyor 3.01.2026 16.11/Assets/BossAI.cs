using System.Collections;
using TMPro; 
using UnityEngine;

public class BossAI : MonoBehaviour
{
    [Header("---- HEDEF VE REFERANSLAR ----")]
    public Transform player;          
    public Transform visualChild;     
    private Rigidbody2D rb;          
    private Collider2D myCollider;   
    private Animator anim;           

    [Header("---- İSTATİSTİKLER ----")]
    public float maxHealth = 500f;
    [SerializeField] private float currentHealth; 
    public float moveSpeed = 2.5f;

    [Header("---- MESAFE AYARLARI ----")]
    public float chaseDist = 10f;    
    public float castDist = 7f;      
    public float attackTriggerDist = 1.5f;  
    
    [Header("---- VURUŞ ALANI (HITBOX) ----")]
    public float attackRadius = 2.5f; // Menzili biraz arttırdım ki ıska geçmesin
    public LayerMask playerLayer;     

    [Header("---- HASAR VE ZAMANLAMA ----")]
    public int meleeDamage = 20;     
    public float attackCooldown = 2f; 
    public float attackAnimDelay = 0.4f; 

    [Header("---- BÜYÜ SİSTEMİ ----")]
    public GameObject spellPrefab;   
    public float castCooldown = 5f;  
    public float castAnimDelay = 1.0f; 

    [Header("---- DİĞER ----")]
    public bool spriteYonuTers = false; 
    public TMP_Text deathText; 
    public AudioClip YouDied_Sound; 

    private bool isDead = false;
    private bool isBusy = false;     
    private float nextAttackTime = 0f;
    private float nextCastTime = 0f;
    private AudioSource audioSource;
    private Player_Script playerScript; 

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        myCollider = GetComponent<Collider2D>();
        audioSource = GetComponent<AudioSource>();
        if(audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        anim = GetComponentInChildren<Animator>(); 
        
        // Eğer visualChild boşsa otomatik bul
        if (visualChild == null && transform.childCount > 0)
            visualChild = transform.GetChild(0);
    }

    void Start()
    {
        currentHealth = maxHealth; 
        
        // Player'ı bulma
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) 
            { 
                player = p.transform; 
                playerScript = p.GetComponent<Player_Script>(); 
            }
        }
        else 
        { 
            playerScript = player.GetComponent<Player_Script>(); 
        }

        if (deathText != null) deathText.gameObject.SetActive(false);
    }

    void Update()
    {
        if (isDead || player == null) return;
        if (isBusy) { StopMovement(); return; }

        // MESAİFE ÖLÇÜMÜ (Merkezden)
        float dist = Vector2.Distance(transform.position, player.position);
        int direction = (player.position.x > transform.position.x) ? 1 : -1;

        // 1. DIBINDEYSE -> SALDIR
        if (dist <= attackTriggerDist)
        {
            StopMovement();
            FacePlayer(direction);
            
            if (Time.time >= nextAttackTime)
            {
                StartCoroutine(AttackRoutine());
                nextAttackTime = Time.time + attackCooldown;
            }
        }
        // 2. ORTA MESAFE -> BÜYÜ
        else if (dist <= castDist && dist > attackTriggerDist)
        {
            StopMovement();
            FacePlayer(direction);
            if (Time.time >= nextCastTime) 
            { 
                StartCoroutine(CastRoutine()); 
                nextCastTime = Time.time + castCooldown; 
            }
            else 
            { 
                MoveToPlayer(direction); 
            }
        }
        // 3. UZAK -> KOŞ
        else if (dist <= chaseDist)
        {
            MoveToPlayer(direction);
        }
        else 
        { 
            StopMovement(); 
        }
    }

    IEnumerator AttackRoutine()
    {
        isBusy = true; 
        StopMovement();
        anim.SetTrigger("attack"); 

        yield return new WaitForSeconds(attackAnimDelay);

        // --- VURUŞ NOKTASI (Goblin Fix) ---
        // Boss'un tam merkezinden biraz yukarısı (Göğüs hizası)
        Vector2 centerPoint = (Vector2)transform.position + new Vector2(0, 0.5f); 
        
        Collider2D hitPlayer = Physics2D.OverlapCircle(centerPoint, attackRadius, playerLayer);

        if (hitPlayer != null)
        {
            Player_Script p = hitPlayer.GetComponent<Player_Script>();
            if (p != null) 
            {
                p.TriggerHurt(meleeDamage); 
                Debug.Log("VURDU: " + hitPlayer.name); // Konsola yazdırır
            }
        }

        yield return new WaitForSeconds(0.5f); 
        isBusy = false; 
    }

    IEnumerator CastRoutine()
    {
        isBusy = true;
        StopMovement();
        anim.SetTrigger("cast"); 
        
        yield return new WaitForSeconds(castAnimDelay);
        
        if (!isDead && player != null && spellPrefab != null)
        {
            Vector3 spawnPos = player.position; 
            spawnPos.y += 6f; 
            Instantiate(spellPrefab, spawnPos, Quaternion.identity);
        }
        
        yield return new WaitForSeconds(0.5f);
        isBusy = false;
    }

    void MoveToPlayer(int dir)
    {
        anim.SetBool("walk", true);
        FacePlayer(dir);
        // linearVelocity yerine velocity kullandım (Her sürümde çalışır)
        rb.linearVelocity = new Vector2(dir * moveSpeed, rb.linearVelocity.y);
    }

    void StopMovement()
    {
        // linearVelocity yerine velocity kullandım
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        anim.SetBool("walk", false);
    }

    void FacePlayer(int direction)
    {
        if (visualChild != null)
        {
            Vector3 scale = visualChild.localScale;
            // Scale pozitif/negatif yaparak çevirme
            if (direction > 0) scale.x = Mathf.Abs(scale.x) * (spriteYonuTers ? -1 : 1);
            else scale.x = -Mathf.Abs(scale.x) * (spriteYonuTers ? -1 : 1);
            visualChild.localScale = scale;
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;
        currentHealth -= damage;
        if (!isBusy) { anim.SetTrigger("hurt"); StopMovement(); }
        if (currentHealth <= 0) Die();
    }

    void Die()
    {
        if (isDead) return;
        isDead = true; isBusy = true; 
        anim.SetTrigger("death");
        rb.linearVelocity = Vector2.zero; 
        rb.gravityScale = 0;
        GetComponent<Collider2D>().enabled = false;
        
        if (deathText != null) deathText.gameObject.SetActive(true);
        if (YouDied_Sound != null) audioSource.PlayOneShot(YouDied_Sound);
        
        Destroy(gameObject, 5f); 
    }

    void OnDrawGizmosSelected()
    {
        // Vuruş alanını Editörde Kırmızı Çizgi Olarak Göster
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + new Vector3(0, 0.5f, 0), attackRadius);
    }
}