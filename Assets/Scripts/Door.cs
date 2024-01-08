using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Door object. It can either be : 
/// - OPEN : Player can walk through door
/// - CLOSED : Behave like a wall. Consume a key and become OPEN if player touch a closed door.
/// - WALL : Looks and behave like a regular wall. Cannot be opened with a key. May be used to hide a door we don't want to use when spawning a room.
/// - SECRET : Looks like a wall but without any collision. You can pass through and... it's a secret ;)
/// 
/// A room is oriented depending on its room position, and leads to a next room
/// When opening a CLOSED door, it automatically open any CLOSED door on the adjacent side of the next room.
/// </summary>

public class Door : MonoBehaviour {

    public enum STATE {
        OPEN = 0,
        CLOSED = 1,
        WALL = 2,
        SECRET = 3,
    }

    public const string PLAYER_NAME = "Player";

    Utils.ORIENTATION _orientation = Utils.ORIENTATION.NONE;
	public Utils.ORIENTATION Orientation { get { return _orientation; } }

	STATE _state = STATE.OPEN;
	public STATE State { get { return _state; } }
	public GameObject closedGo = null;
    public GameObject openGo = null;
    public GameObject wallGo = null;
    public GameObject secretGo = null;

	private Room _room = null;

	public void Awake()
	{
		_room = GetComponentInParent<Room>();
        Bounds roomBounds = _room.GetLocalBounds();
        float ratio = roomBounds.size.x / roomBounds.size.y;
        Vector2 dir = transform.position - (_room.transform.position + roomBounds.center);
        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y) * ratio)
        {
            _orientation = dir.x > 0 ? Utils.ORIENTATION.EAST : Utils.ORIENTATION.WEST;
        }
        else
        {
            _orientation = dir.y > 0 ? Utils.ORIENTATION.NORTH : Utils.ORIENTATION.SOUTH;
        }
    }

	public void Start()
    {

        transform.rotation = Quaternion.Euler(0, 0, -Utils.OrientationToAngle(_orientation));
		if(closedGo.gameObject.activeSelf)
		{
			SetState(STATE.CLOSED);
		} else if (openGo.gameObject.activeSelf)
		{
			SetState(STATE.OPEN);
		} else if (wallGo.gameObject.activeSelf)
		{
			SetState(STATE.WALL);
		} else if (secretGo.gameObject.activeSelf)
		{
			SetState(STATE.SECRET);
		}
	}

    public void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.transform.parent != Player.Instance.gameObject.transform)
            return;

        if(_state == STATE.CLOSED) {
            TryUnlock();
        }
    }

    private void TryUnlock()
    {
        if (_state != STATE.CLOSED || Player.Instance.KeyCount <= 0)
            return;

        Player.Instance.KeyCount--;
        SetState(STATE.OPEN);

        Room nextRoom = _room.GetAdjacentRoom(_orientation, transform.position);
        if (nextRoom)
        {
            Door nextDoor = nextRoom.GetDoor(Utils.OppositeOrientation(_orientation), transform.position);
            if (nextDoor)
            {
                nextDoor.SetState(STATE.OPEN);
            }
        }
    }

    /// <summary>
    /// Sets the state of a door : OPEN, CLOSED, WALL or SECRET. Please refere to door class description for more details.
    /// </summary>
    public void SetState(STATE state)
    {
        if (closedGo) { closedGo.SetActive(false); }
        if (openGo) { openGo.SetActive(false); }
        if (wallGo) { wallGo.SetActive(false); }
        if (secretGo) { secretGo.SetActive(false); }
        _state = state;
        switch(_state)
        {
            case STATE.CLOSED:
                if (closedGo) { closedGo.SetActive(true); }
                break;
            case STATE.OPEN:
                if (openGo) { openGo.SetActive(true); }
                break;
            case STATE.WALL:
                if (wallGo) { wallGo.SetActive(true); }
                break;
            case STATE.SECRET:
                if (secretGo) { secretGo.SetActive(true); }
                break;
        }
    }

}
