using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CapsuleCollider2D))]
public class PlayerRuneAbilities : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private KeyCode dashKey = KeyCode.LeftShift;
    [SerializeField] private KeyCode backDodgeKey = KeyCode.Z;

    [Header("Dash terrestre (Runa amarilla)")]
    [SerializeField] private float groundDashSpeed = 14f;
    [SerializeField] private float groundDashDuration = 0.16f;
    [SerializeField] private float groundDashCooldown = 0.85f;

    [Header("Back dodge (Runa celeste)")]
    [SerializeField] private float backDodgeSpeed = 12f;
    [SerializeField] private float backDodgeDuration = 0.14f;
    [SerializeField] private float backDodgeCooldown = 1.1f;

    [Header("Represalia feroz (Runa roja)")]
    [SerializeField] private float reprisalWindowDuration = 2.5f;
    [SerializeField] private int reprisalAttackDamage = 3;

    [Header("Dash colisiones")]
    [SerializeField] private LayerMask dashObstacleLayers;
    [SerializeField] private float dashCollisionSkin = 0.03f;

    [Header("Dash daño")]
    [SerializeField] private int dashDamage = 1;
    [SerializeField] private LayerMask dashEnemyLayers;

    [Header("UI")]
    [SerializeField] private AbilityHUD abilityHud;

    private PlayerController _player;
    private Rigidbody2D _rb;
    private CapsuleCollider2D _capsule;
    private Animator _animator;

    private float _groundCooldownTimer;
    private float _backDodgeCooldownTimer;
    private float _reprisalTimer;
    private bool _isDashing;
    private bool _isBackDodging;
    private readonly HashSet<IDamageable> _dashDamaged = new HashSet<IDamageable>();
    private readonly RaycastHit2D[] _castHits = new RaycastHit2D[12];
    private static readonly Collider2D[] OverlapScratch = new Collider2D[12];

    public bool IsInRuneMovement => _isDashing || _isBackDodging;
    public bool IsDashing => _isDashing;
    public bool HasReprisalReady => _reprisalTimer > 0f && RuneProgress.IsUnlocked(RuneType.Red);

    public bool TryConsumeReprisalAttack(out int reprisalDamage)
    {
        reprisalDamage = 0;
        if (_reprisalTimer <= 0f || !RuneProgress.IsUnlocked(RuneType.Red))
            return false;

        _reprisalTimer = 0f;
        reprisalDamage = reprisalAttackDamage;
        return true;
    }

    private void Awake()
    {
        _player = GetComponent<PlayerController>();
        _rb = GetComponent<Rigidbody2D>();
        _capsule = GetComponent<CapsuleCollider2D>();
        _animator = GetComponentInChildren<Animator>();

        if (abilityHud == null)
            abilityHud = GetComponent<AbilityHUD>();

        if (dashObstacleLayers.value == 0)
            dashObstacleLayers = LayerMask.GetMask("Ground", "Default");

        if (dashEnemyLayers.value == 0)
            dashEnemyLayers = LayerMask.GetMask("Enemy", "EnemyHitBox");
    }

    private void OnEnable()
    {
        RuneProgress.OnRuneUnlocked += HandleRuneUnlocked;
        RefreshFromProgress();
    }

    private void OnDisable()
    {
        RuneProgress.OnRuneUnlocked -= HandleRuneUnlocked;
        _player.SetRuneDodgeInvulnerable(false);
    }

    private void Update()
    {
        if (_player.IsDead() || IsInRuneMovement)
            return;

        TickCooldowns();
        TryStartGroundDash();
        TryStartBackDodge();
    }

    public void RefreshFromProgress()
    {
        if (abilityHud != null)
            abilityHud.RefreshSlots();
    }

    private void HandleRuneUnlocked(RuneType rune)
    {
        RefreshFromProgress();
    }

    private void TryStartGroundDash()
    {
        if (!Input.GetKeyDown(dashKey))
            return;

        if (!_player.IsGrounded || !RuneProgress.IsUnlocked(RuneType.Yellow) || _groundCooldownTimer > 0f)
            return;

        if (!TryGetDashDirection(out float direction))
            return;

        StartCoroutine(DashRoutine(direction, groundDashSpeed, groundDashDuration, groundDashCooldown));
    }

   

    private static bool TryGetDashDirection(out float direction)
    {
        direction = 0f;

        bool left = Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow);
        bool right = Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow);

        if (left == right)
            return false;

        direction = left ? -1f : 1f;
        return true;
    }

    private IEnumerator DashRoutine(float direction, float speed, float duration, float cooldown)
    {
        _isDashing = true;
        _dashDamaged.Clear();
        if (_animator != null)
        {
            _animator.SetTrigger("Dash"); 
        }

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(AudioManager.Instance.playerDash);

        float timer = duration;
        while (timer > 0f)
        {
            float step = speed * Time.fixedDeltaTime;
            float moveDistance = step;

            if (TryGetBlockedDistance(direction, step, out float blockedDistance))
            {
                moveDistance = blockedDistance;
                if (moveDistance > 0f)
                    ApplyDashStep(direction, moveDistance);

                DealDashDamageAlongPath(direction, moveDistance);
                break;
            }

            ApplyDashStep(direction, moveDistance);
            DealDashDamageAlongPath(direction, moveDistance);

            timer -= Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        EndHorizontalRuneMovement();
        _isDashing = false;
        _groundCooldownTimer = cooldown;

        if (abilityHud != null)
            abilityHud.StartCooldown(RuneType.Yellow, cooldown);
    }

   private void TryStartBackDodge()
{
    if (!Input.GetKeyDown(backDodgeKey))
        return;

    if (!_player.IsGrounded || !RuneProgress.IsUnlocked(RuneType.Celeste) || _backDodgeCooldownTimer > 0f)
        return;

    if (_animator != null)
    {
        _animator.Play("backDodge", 0, 0f); 
    }

    float direction = -_player.FacingDirection;
    StartCoroutine(BackDodgeRoutine(direction));
}

private IEnumerator BackDodgeRoutine(float direction)
{
    _isBackDodging = true;
    _player.SetRuneDodgeInvulnerable(true);

  

    if (AudioManager.Instance != null)
        AudioManager.Instance.PlaySFX(AudioManager.Instance.playerDash, 0.85f);

    float timer = backDodgeDuration;
    while (timer > 0f)
    {
        float step = backDodgeSpeed * Time.fixedDeltaTime;
        float moveDistance = step;

        if (TryGetBlockedDistance(direction, step, out float blockedDistance))
        {
            moveDistance = blockedDistance;
            if (moveDistance > 0f)
                ApplyDashStep(direction, moveDistance);

            break;
        }

        ApplyDashStep(direction, moveDistance);

        timer -= Time.fixedDeltaTime;
        yield return new WaitForFixedUpdate();
    }

    EndHorizontalRuneMovement();
    _isBackDodging = false;
    _player.SetRuneDodgeInvulnerable(false);
    _backDodgeCooldownTimer = backDodgeCooldown;

    if (abilityHud != null)
        abilityHud.StartCooldown(RuneType.Celeste, backDodgeCooldown);

    if (RuneProgress.IsUnlocked(RuneType.Red))
        _reprisalTimer = reprisalWindowDuration;
}

    private void EndHorizontalRuneMovement()
    {
        float vy = _rb.velocity.y;
        if (_player.IsGrounded)
            vy = Mathf.Min(vy, 0f);

        _rb.velocity = new Vector2(0f, vy);
    }

    private void ApplyDashStep(float direction, float distance)
    {
        Vector2 pos = _rb.position;
        pos.x += direction * distance;
        _rb.MovePosition(pos);

        float vy = _rb.velocity.y;
        if (_player.IsGrounded)
            vy = Mathf.Min(vy, 0f);

        _rb.velocity = new Vector2(0f, vy);
    }

    private bool TryGetBlockedDistance(float direction, float distance, out float allowedDistance)
    {
        allowedDistance = distance;

        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(dashObstacleLayers);
        filter.useTriggers = false;

        int count = _rb.Cast(new Vector2(direction, 0f), filter, _castHits, distance + dashCollisionSkin);
        if (count <= 0)
            return false;

        float minDistance = _castHits[0].distance;
        for (int i = 1; i < count; i++)
        {
            if (_castHits[i].distance < minDistance)
                minDistance = _castHits[i].distance;
        }

        allowedDistance = Mathf.Max(0f, minDistance - dashCollisionSkin);
        return true;
    }

    private void DealDashDamageAlongPath(float direction, float distance)
    {
        if (distance <= 0f)
            return;

        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(dashEnemyLayers);
        filter.useTriggers = true;

        int count = _rb.Cast(new Vector2(direction, 0f), filter, _castHits, distance);
        for (int i = 0; i < count; i++)
        {
            Collider2D col = _castHits[i].collider;
            if (col == null)
                continue;

            TryDamageEnemy(col);
        }

        ContactFilter2D overlapFilter = new ContactFilter2D();
        overlapFilter.SetLayerMask(dashEnemyLayers);
        overlapFilter.useTriggers = true;

        int overlapCount = Physics2D.OverlapCollider(_capsule, overlapFilter, OverlapScratch);
        for (int i = 0; i < overlapCount; i++)
        {
            if (OverlapScratch[i] != null)
                TryDamageEnemy(OverlapScratch[i]);
        }
    }

    private void TryDamageEnemy(Collider2D col)
    {
        IDamageable damageable = col.GetComponentInParent<IDamageable>();
        if (damageable == null || damageable is PlayerController)
            return;

        if (!_dashDamaged.Add(damageable))
            return;

        damageable.TakeDamage(dashDamage);
    }

    private void TickCooldowns()
    {
        if (_groundCooldownTimer > 0f)
            _groundCooldownTimer -= Time.deltaTime;

        if (_backDodgeCooldownTimer > 0f)
            _backDodgeCooldownTimer -= Time.deltaTime;

        if (_reprisalTimer > 0f)
            _reprisalTimer -= Time.deltaTime;

        if (abilityHud == null)
            return;

        float yellowNormalized = RuneProgress.IsUnlocked(RuneType.Yellow) && groundDashCooldown > 0f
            ? Mathf.Clamp01(_groundCooldownTimer / groundDashCooldown)
            : 0f;
        abilityHud.SetCooldownNormalized(RuneType.Yellow, yellowNormalized);

        float celesteNormalized = RuneProgress.IsUnlocked(RuneType.Celeste) && backDodgeCooldown > 0f
            ? Mathf.Clamp01(_backDodgeCooldownTimer / backDodgeCooldown)
            : 0f;
        abilityHud.SetCooldownNormalized(RuneType.Celeste, celesteNormalized);
    }
}
