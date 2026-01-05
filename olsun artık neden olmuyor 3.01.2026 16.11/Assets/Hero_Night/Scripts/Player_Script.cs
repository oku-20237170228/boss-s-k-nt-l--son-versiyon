using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Player_Script : MonoBehaviour
{
    [Header("Bileşenler")]
    public Player_HealthBar healthBar;
    public Animator animator;
    private Rigidbody2D rgb;
    private SpriteRenderer sr;
    private AudioSource audioSource;

    [Header("Genel Sesler")]
    public AudioClip jumpSound;
    public AudioClip stepSound;
    public AudioClip hurtSound;
    public AudioClip dieSound;
    public AudioClip rollSound;

    [Header("Blok Sesleri")]
    public AudioClip blockRaiseSound;
    public AudioClip blockHitSound;

    [Header("Saldırı Sesleri")]
    public AudioClip attack1Sound;
    public AudioClip attack2Sound;
    public AudioClip attack3Sound;
    public AudioClip coinSound;

    [Header("Hareket Ayarları")]
    public float baseSpeed = 5f; 
    public float runSpeed = 8f;  
    private float currentSpeed;
    public float jumpForce = 6f;
    private float moveInput;

    [Header("Yuvarlanma (Roll)")]
    public float rollSpeed = 8f;
    public float rollDuration = 0.8f;
    private bool isRolling = false;

    [Header("Zemin Kontrolü")]
    public Transform groundCheck;
    public float radius = 0.2f;
    public LayerMask groundLayer;
    [SerializeField] private bool isGrounded;

    [Header("Saldırı Sistemi")]
    public Transform rightAttackPoint;
    public Transform leftAttackPoint;
    private Transform currentAttackPoint;
    public float attackRange = 0.5f;
    public LayerMask enemyLayers;
    public int attackDamage = 10; // Örn: 10 ise ağır saldırı 20 vuracak
    public float normalAttackDelay = 0.25f;
    public float heavyAttackDelay = 0.4f;

    // Durumlar
    private bool died = false;
    private float holdTimer = 0f;
    private int clickCount = 0;
    private float comboResetTimer = 0f;
    private bool isHolding = false;
    public bool isBlocking = false;
    private bool isHurting = false;

    // UI & Manager
    public TMP_Text diedText;
    public CoinManager coinManager;

    void Start()
    {
        animator = GetComponentInChildren<Animator>();
        sr = GetComponentInChildren<SpriteRenderer>();
        rgb = GetComponent<Rigidbody2D>();

        Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Enemy"), false);

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        if (healthBar == null) healthBar = FindFirstObjectByType<Player_HealthBar>();

        currentAttackPoint = rightAttackPoint;
        currentSpeed = baseSpeed;

        if (diedText != null) diedText.gameObject.SetActive(false);
    }

    void Update()
    {
        if (died) return;
        if (isRolling) return; 

        // --- BLOK ---
        if (Input.GetMouseButton(1))
        {
            if (!isBlocking) StartBlock();
            return; 
        }
        else
        {
            if (isBlocking) StopBlock();
        }

        // --- YUVARLANMA ---
        if (Input.GetKeyDown(KeyCode.Z) && isGrounded && !isRolling && !isBlocking)
        {
            StartCoroutine(RollRoutine());
            return;
        }

        // --- HAREKET ---
        HandleMovement();

        // --- ZIPLAMA ---
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            Jump();
        }

        // --- SALDIRI ---
        HandleAttackInput();

        // --- KOŞMA ---
        if (Input.GetKey(KeyCode.LeftShift)) currentSpeed = runSpeed;
        else currentSpeed = baseSpeed;

        // --- ÖLÜM ---
        if (transform.position.y < -50) Die();
        if (healthBar != null && healthBar.isDead && !died) Die();
    }

    void FixedUpdate()
    {
        if (died) return;

        if (isRolling)
        {
            int direction = sr.flipX ? -1 : 1;
            rgb.linearVelocity = new Vector2(direction * rollSpeed, rgb.linearVelocity.y);
            return;
        }

        if (!isBlocking)
        {
            rgb.linearVelocity = new Vector2(moveInput * currentSpeed, rgb.linearVelocity.y);
        }
        else
        {
            rgb.linearVelocity = new Vector2(0, rgb.linearVelocity.y);
        }

        if (groundCheck != null)
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, radius, groundLayer);
    }

    void HandleMovement()
    {
        if (isBlocking) return;

        moveInput = Input.GetAxisRaw("Horizontal");

        if (moveInput != 0)
        {
            animator.SetBool("runn", true);
            if (moveInput > 0)
            {
                sr.flipX = false;
                currentAttackPoint = rightAttackPoint;
            }
            else if (moveInput < 0)
            {
                sr.flipX = true;
                currentAttackPoint = leftAttackPoint;
            }
        }
        else
        {
            animator.SetBool("runn", false);
        }
    }

    IEnumerator RollRoutine()
    {
        isRolling = true;
        animator.SetTrigger("roll");
        PlaySound(rollSound);

        int playerLayer = LayerMask.NameToLayer("Player");
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, true);

        yield return new WaitForSeconds(rollDuration);

        Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, false);
        isRolling = false;
    }

    public void TriggerHurt(float damageAmount)
    {
        if (died || isHurting) return;
        if (isRolling) return; 

        if (isBlocking)
        {
            TriggerBlockHit();
            PlaySound(blockHitSound);
        }
        else
        {
            if (healthBar != null) healthBar.GetDamage(damageAmount);

            if (healthBar != null && healthBar.isDead)
            {
                Die();
            }
            else
            {
                animator.SetTrigger("hurt");
                PlaySound(hurtSound);
                StartCoroutine(HurtCooldown());
            }
        }
    }

    // --- SALDIRI SİSTEMİ (GÜNCELLENDİ: ARTIK ÇARPAN ALIYOR) ---
    void Attack(int damageMultiplier)
    {
        if (currentAttackPoint == null) return;
        if (died || isHurting) return;

        // Son hasarı hesapla (Örn: 10 * 2 = 20)
        int finalDamage = attackDamage * damageMultiplier;

        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(currentAttackPoint.position, attackRange, enemyLayers);

        foreach (Collider2D enemy in hitEnemies)
        {
            // 1. BOSS
            BossEnemyAI boss = enemy.GetComponent<BossEnemyAI>();
            if (boss == null) boss = enemy.GetComponentInParent<BossEnemyAI>();
            if (boss != null) { boss.TakeDamage(finalDamage); continue; }

            // 2. GOBLIN
            goblinyapayzeka goblin = enemy.GetComponent<goblinyapayzeka>();
            if (goblin == null) goblin = enemy.GetComponentInParent<goblinyapayzeka>();
            if (goblin != null) { goblin.TakeDamage(finalDamage); continue; }

            // 3. SKELETON
            skeletonyapayzeka skeleton = enemy.GetComponent<skeletonyapayzeka>();
            if (skeleton == null) skeleton = enemy.GetComponentInParent<skeletonyapayzeka>();
            if (skeleton != null) { skeleton.TakeDamage(finalDamage); continue; }

            // 4. UÇAN GÖZ
            Uçangöz eye = enemy.GetComponent<Uçangöz>();
            if (eye == null) eye = enemy.GetComponentInParent<Uçangöz>();
            if (eye != null) { eye.TakeDamage(finalDamage); continue; }

            // 5. MANTAR
            MushroomYapayZeka mushroom = enemy.GetComponent<MushroomYapayZeka>();
            if (mushroom == null) mushroom = enemy.GetComponentInParent<MushroomYapayZeka>();
            if (mushroom != null) 
            { 
                mushroom.TakeDamage(finalDamage); 
                Debug.Log("Mantar Vuruldu: " + finalDamage + " Hasar");
                continue; 
            }
        }
    }

    void StartBlock()
    {
        isBlocking = true;
        moveInput = 0;
        animator.SetBool("runn", false);
        animator.ResetTrigger("blockHit");
        animator.SetBool("block", true);
        PlaySound(blockRaiseSound);
    }

    void StopBlock()
    {
        isBlocking = false;
        animator.SetBool("block", false);
    }

    void Jump()
    {
        rgb.linearVelocity = new Vector2(rgb.linearVelocity.x, jumpForce);
        animator.SetTrigger("jump");
        PlaySound(jumpSound);
    }

    void HandleAttackInput()
    {
        if (isBlocking) return;

        if (Input.GetMouseButtonDown(0)) { holdTimer = 0f; isHolding = true; }
        if (Input.GetMouseButton(0)) holdTimer += Time.deltaTime;

        if (Input.GetMouseButtonUp(0))
        {
            // --- AĞIR SALDIRI (2 KAT HASAR) ---
            if (holdTimer > 0.4f)
            {
                animator.SetTrigger("attack3");
                // Çarpan olarak 2 gönderiyoruz
                StartCoroutine(GecikmeliSaldiri(heavyAttackDelay, 2)); 
                clickCount = 0;
            }
            // --- NORMAL SALDIRI (NORMAL HASAR) ---
            else
            {
                clickCount++;
                comboResetTimer = 1f;
                if (clickCount == 1)
                {
                    animator.SetTrigger("attack1");
                    // Çarpan olarak 1 gönderiyoruz
                    StartCoroutine(GecikmeliSaldiri(normalAttackDelay, 1));
                }
                else if (clickCount >= 2)
                {
                    animator.SetTrigger("attack2");
                    // Çarpan olarak 1 gönderiyoruz
                    StartCoroutine(GecikmeliSaldiri(normalAttackDelay, 1));
                    clickCount = 0;
                }
            }
            isHolding = false;
        }

        if (comboResetTimer > 0)
        {
            comboResetTimer -= Time.deltaTime;
            if (comboResetTimer <= 0) clickCount = 0;
        }
    }

    // Gecikmeli saldırıya "Multiplier" (Çarpan) parametresi ekledik
    IEnumerator GecikmeliSaldiri(float delay, int multiplier)
    {
        yield return new WaitForSeconds(delay);
        Attack(multiplier); // Çarpanı Attack fonksiyonuna iletiyoruz
    }

    public void PlayComboSound(int comboNo)
    {
        if (comboNo == 1) PlaySound(attack1Sound);
        else if (comboNo == 2) PlaySound(attack2Sound);
        else if (comboNo == 3) PlaySound(attack3Sound);
    }

    public void PlayFootstep()
    {
        if (isGrounded) PlaySound(stepSound);
    }

    void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
            audioSource.PlayOneShot(clip);
    }

    void Die()
    {
        if (died) return;
        died = true;
        animator.SetTrigger("deathT"); 
        PlaySound(dieSound);

        rgb.simulated = false;
        this.enabled = false;

        if (diedText != null) diedText.gameObject.SetActive(true);
        StartCoroutine(GoMenuDelay());
    }

    IEnumerator GoMenuDelay()
    {
        yield return new WaitForSeconds(2f);
        SceneManager.LoadScene("MainMenu_Scene");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Coin"))
        {
            PlaySound(coinSound);
            Destroy(other.gameObject);
            if (coinManager != null) coinManager.CoinCount++;
        }
    }

    public void TriggerBlockHit() { animator.SetTrigger("blockHit"); }
    IEnumerator HurtCooldown() { isHurting = true; yield return new WaitForSeconds(0.5f); isHurting = false; }

    void OnDrawGizmosSelected()
    {
        if (rightAttackPoint != null) { Gizmos.color = Color.blue; Gizmos.DrawWireSphere(rightAttackPoint.position, attackRange); }
        if (leftAttackPoint != null) { Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(leftAttackPoint.position, attackRange); }
    }
}