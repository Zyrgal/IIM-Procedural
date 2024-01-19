using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Player component. Manages inputs, character states and associated game flow.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Player : MonoBehaviour
{
    public static Player Instance = null;

    [System.Serializable]
    public class MovementParameters
    {
        public float speedMax = 2.0f;
        public float acceleration = 12.0f;
        public float decceleration = 12.0f;
    }

    // Possible orientation for player aiming : 4 direction, 8 direction (for keyboards or D-pads) or free direction (for analogic joysticks)
    public enum ORIENTATION
    {
        FREE,
        DPAD_8,
        DPAD_4
    }

    // Character can only be at one state at a time. For example, he can't attack and be stunned at the same time.
    public enum STATE
    {
        IDLE = 0,
        ATTACKING = 1,
        STUNNED = 2,
        DEAD = 3,
        DASHING = 4,
    }

    // Life and hit related attributes
    [Header("Life")]
    public int life = 3;
    public float invincibilityDuration = 1.0f;
    public float invincibilityBlinkPeriod = 0.2f;
    public LayerMask hitLayers;
    public float knockbackSpeed = 3.0f;
    public float knockbackDuration = 0.5f;
    public Color deadColor = Color.gray;

    private float _lastHitTime = float.MinValue;
    private List<SpriteRenderer> _spriteRenderers = new List<SpriteRenderer>();
    private Coroutine _blinkCoroutine = null;


    // Movement attributes
    [Header("Movement")]
    public MovementParameters defaultMovement = new MovementParameters();
    public MovementParameters stunnedMovement = new MovementParameters();
    public MovementParameters dashMovement = new MovementParameters();

    private float currentSpeed;
    private Rigidbody2D _body = null;
    private Vector2 _direction = Vector2.zero;
    private float inputPower = 0;
    private MovementParameters _currentMovement = null;

    [Space(10)]

    [Header("Dash")]
    [SerializeField] Image dashReadyImage;
    [SerializeField] private float dashDuration = 0.03f;
    [SerializeField] private float dashForce = 50;
    private float currentDashTimer = 0f;
    private float dashCooldown = 1f;
    private float currentDashCooldown = 1f;
    private bool isDashing = false;
    private bool canDash = true;

    //private bool canDash => currentDashCooldown < 0 ;

    [Header("Modifiers base setup"),Space(10)]
    [SerializeField] int baseDamage = 1;
    [SerializeField] float baseRange = 1; 
    [SerializeField] private float baseEphemeralSpeedDuration;
    private float currentEphemeralSpeedDuration;
    public Alterable2<int> CurrentDamage { get; private set; }
    public Alterable2<float> CurrentRange { get; private set; }
    public Alterable2<float> CurrentMovespeedMax { get; private set; }
    public Alterable2<float> CurrentMoveAcceleration { get; private set; }
    public Alterable2<float> CurrentDashSpeedMax { get; private set; }
    public Alterable2<float> CurrentDashAcceleration { get; private set; }

    [Header("Values for stats increase")]
    [SerializeField] private int attackIncreaseValue = 1;
    [SerializeField] private int rangeIncreaseValue = 1;
    [SerializeField] private int moveSpeedIncreaseValue = 1;
    [SerializeField] private int accelerationIncreaseValue = 1;
    [SerializeField] private int permanentMoveSpeedIncreaseValue = 1;
    [SerializeField] private int permanentAccelerationIncreaseValue = 1;
    [SerializeField] private int dashSpeedIncreaseValue = 1;
    [SerializeField] private int dashAccelerationIncreaseValue = 1;

    [Header("Others")]

    [SerializeField] TextMeshProUGUI damageTXT;
    [SerializeField] TextMeshProUGUI rangeTXT;
    [SerializeField] TextMeshProUGUI speedTXT;

    public GameObject attackPrefab = null;
    public GameObject attackSpawnPoint = null;
    public float attackCooldown = 0.3f;
    public ORIENTATION orientation = ORIENTATION.FREE;
    private float lastAttackTime = float.MinValue;

    // Input attributes
    [Header("Input")]
    [Range(0.0f, 1.0f)]
    public float controllerDeadZone = 0.3f;

    // State attributes
    private STATE _state = STATE.IDLE;

    // Collectible attributes
    private int _keyCount;
    public int KeyCount { get { return _keyCount; } set { _keyCount = value; } }

    // Dungeon position
    private Room _room = null;
    public Room Room { get { return _room; } }

    private void Awake()
    {
        Instance = this;
        _body = GetComponent<Rigidbody2D>();
        GetComponentsInChildren<SpriteRenderer>(true, _spriteRenderers);
    }

    private void Start()
    {
        SetState(STATE.IDLE);
        currentEphemeralSpeedDuration = -1;
        CurrentDamage = new Alterable2<int>(baseDamage);
        CurrentRange = new Alterable2<float>(baseRange);
        CurrentMovespeedMax = new Alterable2<float>(defaultMovement.speedMax);
        CurrentMoveAcceleration = new Alterable2<float>(defaultMovement.acceleration);
        CurrentDashAcceleration = new Alterable2<float>(dashMovement.acceleration);
        CurrentDashSpeedMax = new Alterable2<float>(dashMovement.acceleration);
    }

    private void Update()
    {
        UpdateEphemeralSpeed();
        DashInput();
        UpdateDashReadyVisual();
        UpdateState();
        UpdateInputs();
        UpdateRoom();
        UpdateStatsText();
    }

    private void FixedUpdate()
    {
        FixedUpdateMovement();
        UpdateDashState();
    }

    public void DashInput()
    {

        if (_state == STATE.DEAD)
        {
            return;
        }

        if (currentDashCooldown > 0)
        {
            currentDashCooldown -= Time.deltaTime;
            return;
        }
        else
        {
            canDash = true;
        }

        //Ne pas mettre les inputs dans le fixUpdate, sinon les inputs ne seront pas bien lu
        if (Input.GetKeyDown(KeyCode.LeftShift) && canDash)
        {
            canDash = false;
            currentDashCooldown = dashCooldown;
            SetState(STATE.DASHING);
            isDashing = true;
            currentDashTimer = dashDuration;
        }
    }

    private void UpdateStatsText()
    {
        damageTXT.text = CurrentDamage.CalculValue().ToString();
        rangeTXT.text = CurrentRange.CalculValue().ToString();
        speedTXT.text = CurrentMovespeedMax.CalculValue().ToString();
    }

    private void UpdateDashReadyVisual()
    {
        if (canDash)
        {
            dashReadyImage.color = Color.green;
        }
        else
        {
            dashReadyImage.color = Color.red;
        }
    }

    /// <summary>
    /// Updates any room related behaviours. By default, move from one room to another when reaching 
    /// </summary>
	private void UpdateRoom()
    {
        Bounds roomBounds = _room.GetWorldBounds();
        Room nextRoom = null;
        if (transform.position.x > roomBounds.max.x)
        {
            nextRoom = _room.GetAdjacentRoom(Utils.ORIENTATION.EAST, transform.position);
        }
        else if (transform.position.x < roomBounds.min.x)
        {
            nextRoom = _room.GetAdjacentRoom(Utils.ORIENTATION.WEST, transform.position);
        }
        else if (transform.position.y > roomBounds.max.y)
        {
            nextRoom = _room.GetAdjacentRoom(Utils.ORIENTATION.NORTH, transform.position);
        }
        else if (transform.position.y < roomBounds.min.y)
        {
            nextRoom = _room.GetAdjacentRoom(Utils.ORIENTATION.SOUTH, transform.position);
        }

        if (nextRoom != null)
        {
            EnterRoom(nextRoom);
        }
    }

    /// <summary>
    /// Updates inputs
    /// </summary>
    private void UpdateInputs()
    {
        if (CanMove())
        {
            float x = Input.GetAxis("Horizontal");
            float y = Input.GetAxis("Vertical");

            bool isMoving = (x != 0 || y != 0);

            Vector2 input = (Vector2.right * x) + (Vector2.up * y);

            if (isMoving)
            {
                _direction = input;
            }     

            if (input.magnitude < controllerDeadZone)
                input = Vector2.zero;

            inputPower = input.magnitude;

            if (Input.GetButtonDown("Fire1"))
                Attack();
        }
        else
            inputPower = 0;

    }

    /// <summary>
    /// Updates current state
    /// </summary>
    private void UpdateState()
    {
        switch (_state)
        {
            case STATE.ATTACKING:
                //SpawnAttackPrefab();
                //SetState(STATE.IDLE);
                break;
            default: break;
        }
    }

    private void UpdateEphemeralSpeed()
    {
        if (currentEphemeralSpeedDuration < 0)
            return;

        currentEphemeralSpeedDuration -= Time.deltaTime;

        if (currentEphemeralSpeedDuration <= 0)
        {
            RemoveEphemeralSpeed();
        }
    }

    /// <summary>
    /// Changes current state to a new given state. Instructions related to exiting and entering a state should be coded in the two "switch(_state){...}" of this method.
    /// </summary>    
    private void SetState(STATE state)
    {
        _state = state;

        // Entering new state
        switch (_state)
        {
            case STATE.STUNNED:
                _currentMovement = stunnedMovement;
                break;
            case STATE.DEAD:
                EndBlink();
                SetColor(deadColor);
                break;
            case STATE.DASHING:
                _currentMovement = dashMovement;
                break;
            default: _currentMovement = defaultMovement; break;
        }

        // Reset direction if player cannot move in this state
        if (!CanMove())
        {
            _direction = Vector2.zero;
        }
    }

    private void UpdateDashState()
    {
        if (isDashing)
        {
            float moveX = Input.GetAxisRaw("Horizontal");
            float moveY = Input.GetAxisRaw("Vertical");
            Vector2 moveDirection;

            if (!Mathf.Approximately(moveX, 0) && !Mathf.Approximately(moveY, 0))
                moveDirection = new Vector2(moveX, moveY).normalized;
            else
                moveDirection = transform.right;

            _body.velocity = new Vector2(moveDirection.x * dashForce, moveDirection.y * dashForce);

            currentDashTimer -= Time.deltaTime;

            if (currentDashTimer <= 0)
            {
                isDashing = false;
                //Ne pas changer l'ordre des 2 lignes du dessous. La state doit être set avant de clamp la vélocité car sinon il prend la valeur de la state Dash qui est bcp trop grande et le personnage va continuer de glisser
                SetState(STATE.IDLE);
                _body.velocity = Vector2.ClampMagnitude(_body.velocity, CurrentMovespeedMax.CalculValue());
            }
        }
    }

    /// <summary>
    /// Updates velocity and frictions
    /// </summary>
    void FixedUpdateMovement()
    {
        float frictionMagnitude = _currentMovement.decceleration * Time.fixedDeltaTime;
        if (inputPower <= 0)
        {
            currentSpeed = Mathf.Lerp(currentSpeed, 0, frictionMagnitude);
        }
        else
        {
            if (_currentMovement == defaultMovement)
                currentSpeed = Mathf.Lerp(currentSpeed, CurrentMovespeedMax.CalculValue(), CurrentMoveAcceleration.CalculValue() * Time.fixedDeltaTime);
            else if (_currentMovement == dashMovement)
                currentSpeed = Mathf.Lerp(currentSpeed, CurrentDashSpeedMax.CalculValue(), CurrentDashAcceleration.CalculValue() * Time.fixedDeltaTime);
            else
                currentSpeed = Mathf.Lerp(currentSpeed, CurrentMovespeedMax.CalculValue(), CurrentMoveAcceleration.CalculValue() * Time.fixedDeltaTime);

            transform.eulerAngles = new Vector3(0.0f, 0.0f, ComputeOrientationAngle(_direction));
        }

        _body.velocity = _direction.normalized * currentSpeed;
    }

    /// <summary>
	/// Sets player in attack state. Attack prefab will be spawned when calling SpawnAttackPrefab method a little later.
    /// </summary>
	private void Attack()
    {
        if (Time.time - lastAttackTime < attackCooldown)
            return;
        lastAttackTime = Time.time;
        SetState(STATE.ATTACKING);
        SpawnAttackPrefab();
    }

    /// <summary>
    /// Spawns the associated "attack" prefab on attackSpawnPoint.
    /// </summary>
    private void SpawnAttackPrefab()
    {
        if (attackPrefab == null)
            return;

        // transform used for spawn is attackSpawnPoint.transform if attackSpawnPoint is not null. Else it's transform.
        Transform spawnTransform = attackSpawnPoint ? attackSpawnPoint.transform : transform;
        GameObject instance = GameObject.Instantiate(attackPrefab, spawnTransform.position, spawnTransform.rotation);

        instance.GetComponent<Attack>().damage = CurrentDamage.CalculValue();
        instance.GetComponent<Attack>().range = CurrentRange.CalculValue();
        RemoveEphemeralAttackBonus();

        SetState(STATE.IDLE);
    }

    private void RemoveEphemeralAttackBonus()
    {
        CurrentDamage.RemoveAlterationByString("DamageEphemeral");
        CurrentRange.RemoveAlterationByString("RangeEphemeral");
    }

    private void RemoveEphemeralSpeed()
    {
        CurrentMoveAcceleration.RemoveAllAlteration();
        CurrentMovespeedMax.RemoveAllAlteration();
    }

    /// <summary>
    /// Called when player takes a hit (ie from enemy hitbox or spikes).
    /// </summary>
    public void ApplyHit(Attack attack)
    {
        if (_state == STATE.DASHING)
        {
            return;
        }

        if (Time.time - _lastHitTime < invincibilityDuration)
            return;
        _lastHitTime = Time.time;

        life -= (attack != null ? attack.damage : 1);
        if (life <= 0)
        {
            SetState(STATE.DEAD);
        }
        else
        {
            if (attack != null && attack.knockbackDuration > 0.0f)
            {
                StartCoroutine(ApplyKnockBackCoroutine(attack.knockbackDuration, attack.transform.right * attack.knockbackSpeed));
            }
            EndBlink();
            _blinkCoroutine = StartCoroutine(BlinkCoroutine());
        }
    }

    /// <summary>
    /// Puts player in STUNNED state and sets a velocity to knockback player. It resume to IDLE state after a fixed duration. STUNNED state has his own movement parameters that allow to redefine frictions when character is knocked.
    /// </summary>
    private IEnumerator ApplyKnockBackCoroutine(float duration, Vector3 velocity)
    {
        SetState(STATE.STUNNED);
        _body.velocity = velocity;
        yield return new WaitForSeconds(duration);
        SetState(STATE.IDLE);
    }

    /// <summary>
    /// Makes all sprite renderers in the player hierarchy blink from enabled to disabled with a fixed period over a fixed time.  
    /// </summary>
    private IEnumerator BlinkCoroutine()
    {
        float invincibilityTimer = 0;
        while (invincibilityTimer < invincibilityDuration)
        {
            invincibilityTimer += Time.deltaTime;
            bool isVisible = ((int)(invincibilityTimer / invincibilityBlinkPeriod)) % 2 == 1;
            foreach (SpriteRenderer spriteRenderer in _spriteRenderers)
            {
                spriteRenderer.enabled = isVisible;
            }
            yield return null; // wait next frame
        }
        EndBlink();
    }

    /// <summary>
    /// Stops current blink coroutine if any is started and set all sprite renderers to enabled.
    /// </summary>
    private void EndBlink()
    {
        if (_blinkCoroutine == null)
            return;
        foreach (SpriteRenderer spriteRenderer in _spriteRenderers)
        {
            spriteRenderer.enabled = true;
        }
        StopCoroutine(_blinkCoroutine);
        _blinkCoroutine = null;

    }

    /// <summary>
    /// Sets the tint color of all SpriteRenderers to a given tint color.
    /// </summary>
    private void SetColor(Color color)
    {
        foreach (SpriteRenderer spriteRenderer in _spriteRenderers)
        {
            spriteRenderer.color = color;
        }
    }

    /// <summary>
    /// Transforms the orientation vector into a discrete angle.
    /// </summary>
    private float ComputeOrientationAngle(Vector2 direction)
    {
        float angle = Vector2.SignedAngle(Vector2.right, direction);
        switch (orientation)
        {
            case ORIENTATION.DPAD_8: return Utils.DiscreteAngle(angle, 45); // Only 0 45 90 135 180 225 270 315
            case ORIENTATION.DPAD_4: return Utils.DiscreteAngle(angle, 90); // Only 0 90 180 270
            default: return angle;
        }
    }

    /// <summary>
    /// Returns whether or not player can moves and attack
    /// </summary>
    private bool CanMove()
    {
        return _state == STATE.IDLE;
    }

    /// <summary>
    /// Called to enter a room
    /// </summary>
	public void EnterRoom(Room room)
    {
        Room previous = _room;
        _room = room;
        room.OnEnterRoom(previous);

        DungeonGenerator.Minimap.Instance.EnterRoom(_room.position);

        //ApplyKnockBackCoroutine(1);
    }
    public void RandomUpgrade()
    {
        int random = UnityEngine.Random.Range(0, 3);

        if (random == 0)
        {
            baseDamage++;
        }
        else if (random == 1)
        {
            baseRange++;
        }
        else
        {
            CurrentMovespeedMax.AddCustomAlteration(f => f + permanentMoveSpeedIncreaseValue, 1, "SpeedPermanent");
            CurrentMoveAcceleration.AddCustomAlteration(f => f + permanentAccelerationIncreaseValue, 1, "AccelerationPermanent");

            //_currentMovement.speedMax++;
            //_currentMovement.acceleration++;
        }
    }


    /// <summary>
    /// Checks if player gets hit by any attack hitbox. Applies attack data (damages, knockback, ...) to player.
    /// </summary>
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (((1 << collision.gameObject.layer) & hitLayers) != 0)
        {
            Attack attack = collision.gameObject.GetComponent<Attack>();
            Bullet bulletComponent = collision.gameObject.GetComponent<Bullet>();

            if (bulletComponent == null)
            {
                ApplyHit(attack);
            }
            else
            {

                if (_state == STATE.DASHING)
                {
                    if (bulletComponent.attackData.attackType == AttackType.ABSORBABLE)
                    {
                        switch (bulletComponent.attackData.attackBonusType)
                        {
                            case AttackBonusType.DAMAGE:
                                CurrentDamage.AddCustomAlteration(f => f + attackIncreaseValue, 1, "DamageEphemeral");
                                Destroy(bulletComponent.gameObject);
                                break;
                            case AttackBonusType.RANGE:
                                CurrentRange.AddCustomAlteration(f => f + rangeIncreaseValue, 1, "RangeEphemeral");
                                Destroy(bulletComponent.gameObject);
                                break;
                            case AttackBonusType.MOVESPEED:
                                CurrentMovespeedMax.AddCustomAlteration(f => f + moveSpeedIncreaseValue, 1, "SpeedEphemeral");
                                CurrentMoveAcceleration.AddCustomAlteration(f => f + accelerationIncreaseValue, 1, "AccelerationEphemeral");
                                CurrentDashAcceleration.AddCustomAlteration(f => f + dashSpeedIncreaseValue, 1, "DashAccelerationEphemeral");
                                CurrentDashSpeedMax.AddCustomAlteration(f => f + dashSpeedIncreaseValue, 1, "DashSpeedEphemeral");
                                currentEphemeralSpeedDuration = baseEphemeralSpeedDuration;
                                Destroy(bulletComponent.gameObject);
                                break;
                            default:
                                break;
                        }
                    }
                }
                else
                {
                    ApplyHit(attack);
                }
            }
        }
    }
}
