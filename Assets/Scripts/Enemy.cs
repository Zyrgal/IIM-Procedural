using CreativeSpore.SuperTilemapEditor;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
//using DG.Tweening;
using static UnityEngine.GraphicsBuffer;


/// <summary>
/// Enemy component. Manages inputs, character states and associated game flow.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Enemy : MonoBehaviour
{

    [System.Serializable]
    public class MovementParameters
    {
        public float speedMax = 1.0f;
        public float acceleration = 8.0f;
        public float friction = 8.0f;
    }


    // Possible orientation for enemy aiming : 4 direction, 8 direction or free direction
    public enum ORIENTATION
    {
        FREE,
        DPAD_8,
        DPAD_4
    }

    // Enemy can only be at one state at a time. For example, he can't attack and be stunned at the same time.
    public enum STATE
    {
        IDLE = 0,
        ATTACKING = 1,
        STUNNED = 2,
        DEAD = 3,
    }


    // Life and hit related attributes
    [Header("Life")]
    public int life = 3;
    public float invincibilityDuration = 1.0f;
    public float invincibilityBlinkPeriod = 0.2f;
    public LayerMask hitLayers;
    public float knockbackSpeed = 3.0f;
    public float knockbackDuration = 0.5f;

    private float _lastHitTime = float.MinValue;
    private List<SpriteRenderer> _spriteRenderers = new List<SpriteRenderer>();
    private Coroutine _blinkCoroutine = null;


    // Movement attributes
    [Header("Movement")]
    public MovementParameters defaultMovement = new MovementParameters();
    public MovementParameters stunnedMovement = new MovementParameters();

    private Rigidbody2D _body = null;
    private Vector2 _direction = Vector2.zero;
    private MovementParameters _currentMovement = null;

    // Attack attributes
    [Header("Attack")]
    public GameObject attackPrefab = null;
    public GameObject attackSpawnPoint = null;
	public float attackWarmUp = 0.5f;
	public float attackDistance = 0.5f;
    public float attackCooldown = 1.0f;
    public ORIENTATION orientation = ORIENTATION.FREE;

    private float lastAttackTime = float.MinValue;

    [Header("Fire")]
    [SerializeField] private int shotCount = 3;
    [SerializeField] private float shotInterval = 0.8f;
    [SerializeField] public float timeBeforeThrowAttack = 3f;
    public float ElapsetimeSinceLastFire { get => Time.time - lastFireTime; }
    private float elapsetimeSinceCanFire;
    private float lastFireTime;
    private Sequence sequence;
    [SerializeField] private float fireRate = 2;
    public float FireRate => Mathf.Max(fireRate, shotCount * shotInterval);

    // State attributes
    private STATE _state = STATE.IDLE;
	private float _stateTimer = 0.0f;

	// Dungeon location
	private Room _room = null;

	public static List<Enemy> allEnemies = new List<Enemy>();

    // Use this for initialization
    private void Awake()
    {
        _body = GetComponent<Rigidbody2D>();
        GetComponentsInChildren<SpriteRenderer>(true, _spriteRenderers);
		allEnemies.Add(this);
	}

	private void OnDestroy()
	{
        if (sequence != null && sequence.IsPlaying())
        {
            sequence.Kill();
        }

        allEnemies.Remove(this);
	}

	private void Start()
    {
		foreach(Room room in Room.allRooms)
		{
			if(room.Contains(transform.position))
			{
				_room = room;
			}
		}

        elapsetimeSinceCanFire = timeBeforeThrowAttack;

        SetState(STATE.IDLE);
    }

    private void Update()
    {
        elapsetimeSinceCanFire -= Time.deltaTime;
        _stateTimer += Time.deltaTime;

        if (CanFire())
            UpdateState();
        
        UpdateAI();
    }

    public bool CanFire()
    {
        elapsetimeSinceCanFire -= Time.deltaTime;
        return ShootAvailable() && ShootStartTimePass();
    }

    public bool ShootStartTimePass()
    {
        return elapsetimeSinceCanFire < 0;
    }

    private void FixedUpdate()
    {
        FixedUpdateMovement();
    }

    /// <summary>
    /// Updates IA. Simply move toward player in straight line and attack when at range.
    /// </summary>
    private void UpdateAI()
    {
        Vector2 enemyToPlayer = (Player.Instance.transform.position - transform.position);

        float angleToTarget = Mathf.Atan2(enemyToPlayer.y, enemyToPlayer.x) * Mathf.Rad2Deg;
        _body.rotation = angleToTarget;

        if (CanMove() && Player.Instance.Room == _room)
        {
            if (enemyToPlayer.magnitude < attackDistance)
            {
                Attack();
            } else
            {
                GetComponent<PathfindingBehaviour>().FollowPlayer();
            }
        }
        else
        {
            _direction = Vector2.zero;
        }
    }

    /// <summary>
    /// Updates current state
    /// </summary>
    private void UpdateState()
    {
        //_stateTimer += Time.deltaTime;

        switch (_state)
        {
            case STATE.ATTACKING:
                Fire();
                SetState(STATE.IDLE);
				break;
            default: break;
        }
    }

    public void ResetVariables()
    {
        elapsetimeSinceCanFire = timeBeforeThrowAttack;
    }

    /// <summary>
    /// Changes current state to a new given state. Instructions related to exiting and entering a state should be coded in the two "switch(_state){...}" of this method.
    /// </summary>
    private void SetState(STATE state)
    {
        // Exit previous state
        // switch (_state)
        //{
        //}

        _state = state;
		//_stateTimer = 0.0f;
        ResetVariables();

        // Enter new state
        switch (_state)
        {
            case STATE.STUNNED: _currentMovement = stunnedMovement; break;
            case STATE.DEAD: EndBlink(); Destroy(gameObject); break;
            default: _currentMovement = defaultMovement; break;
        }

        // Reset direction if enemy cannot move in this state
        if (!CanMove())
        {
            _direction = Vector2.zero;
        }
    }


    /// <summary>
    /// Updates velocity and friction
    /// </summary>
    void FixedUpdateMovement()
    {
        if (_direction.magnitude > Mathf.Epsilon) // magnitude > 0
        {
            // If direction magnitude > 0, Accelerate in direction, then clamp velocity to max speed. Do not apply friction if character is moving toward a direction.
            _body.velocity += _direction * _currentMovement.acceleration * Time.fixedDeltaTime;
            _body.velocity = Vector2.ClampMagnitude(_body.velocity, _currentMovement.speedMax);
            transform.eulerAngles = new Vector3(0.0f, 0.0f, ComputeOrientationAngle(_direction));
        }
        else {
            // If direction magnitude == 0, Apply friction
            float frictionMagnitude = _currentMovement.friction * Time.fixedDeltaTime;
            if (_body.velocity.magnitude > frictionMagnitude)
            {
                _body.velocity -= _body.velocity.normalized * frictionMagnitude;
            }
            else
            {
                _body.velocity = Vector2.zero;
            }
        }
    }

    /// <summary>
    /// Sets enemy in attack state. Attack prefab is spawned by calling SpawnAttackPrefab method.
    /// </summary>
    private void Attack()
    {
        if (Time.time - lastAttackTime < attackCooldown)
            return;
        lastAttackTime = Time.time;
        SetState(STATE.ATTACKING);
    }

    /// <summary>
    /// Spawns the associated "attack" prefab on attackSpawnPoint.
    /// </summary>
    private void SpawnAttackPrefab()
    {
        if (attackPrefab == null)
            return;

        Vector2 directionToShoot = (Player.Instance.transform.position - transform.position);
        float angle = Vector3.Angle(Vector3.right, directionToShoot);
        if (Player.Instance.transform.position.y < transform.position.y) angle *= -1;
        Quaternion bulletRotation = Quaternion.AngleAxis(angle, Vector3.forward);

        // transform used for spawn is attackSpawnPoint.transform if attackSpawnPoint is not null. Else it's transform.
        //Transform spawnTransform = attackSpawnPoint ? attackSpawnPoint.transform : transform;
        GameObject.Instantiate(attackPrefab, attackSpawnPoint.transform.position, bulletRotation);
    }
    public bool ShootAvailable()
    {
        return ElapsetimeSinceLastFire > FireRate;
    }

    public void Fire()
    {
        if (sequence != null && sequence.IsPlaying())
        {
            sequence.Kill();
        }

        sequence = DOTween.Sequence();
        sequence.onComplete += () =>
        {
            sequence = null;
        };

        for (int i = 0; i < shotCount; i++)
        {
            sequence.AppendCallback(() =>
            {
                SpawnAttackPrefab();
            });

            sequence.AppendInterval(shotInterval);
        }
        sequence.Play();

        lastFireTime = Time.time;
    }

    /// <summary>
    /// Called when enemy touches a player attack's hitbox.
    /// </summary>
    private void ApplyHit(Attack attack)
    {
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
    /// Outs enemy in STUNNED state and sets a velocity to knockback enemy. It resume to IDLE state after a fixed duration. STUNNED state has his own movement parameters that allow to redefine frictions when character is knocked.
    /// </summary>
    private IEnumerator ApplyKnockBackCoroutine(float duration, Vector3 velocity)
    {
        SetState(STATE.STUNNED);
        _body.velocity = velocity;
        yield return new WaitForSeconds(duration);
        SetState(STATE.IDLE);
    }

    /// <summary>
    /// Makes all sprite renderers in the enemy hierarchy blink from enabled to disabled with a fixed period over a fixed time.  
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
    /// Can enemy moves or attack
    /// </summary>
    private bool CanMove()
    {
        return _state == STATE.IDLE;
    }

    /// <summary>
    /// Checks if enemy gets hit by any player attack's hitbox. Applies attack data (damages, knockback, ...) to enemy.
    /// </summary>
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if ((hitLayers & (1 << collision.gameObject.layer)) == (1 << collision.gameObject.layer))
        {
            // Collided with hitbox
            Attack attack = collision.gameObject.GetComponent<Attack>();
            ApplyHit(attack);
        }
    }
}
