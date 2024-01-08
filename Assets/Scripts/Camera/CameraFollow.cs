using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Camera following player and clamped in given bounds.
/// By default, bounds are updated when entering a room to clamp view inside room. If room is larger than camera size, camera will smoothly follow player with.
/// </summary>
public class CameraFollow : MonoBehaviour {

    // Camera smoothed speed 
    public float lerpSpeed = 5.0f;
    // Should correspond to worldspace game dimensions
    public Vector2 cameraSize;

	private Bounds _bounds = new Bounds(Vector3.zero, Vector3.positiveInfinity);
	private Vector3 targetPosition = Vector3.zero;
    private GameObject target = null;

    /// <summary>
    /// Sets camera bounds.
    /// </summary>
    public void SetBounds(Bounds bounds)
    {
        _bounds = bounds;
        _bounds.Expand(-cameraSize);
        targetPosition = KeepInBounds(targetPosition);
    }

    /// <summary>
    /// Immediately snaps camera to its final position.
    /// </summary>
    public void SnapToTarget()
    {
        RefreshTargetPosition();
        transform.position = targetPosition;
    }

    private void Awake()
	{
		targetPosition = transform.position;
	}

    private void Start()
    {
		target = Player.Instance.gameObject;
        RefreshTargetPosition();
    }

    void Update () {
		RefreshTargetPosition();
		Vector3 position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * lerpSpeed);
        transform.position = (Vector3)position;
    }

	void RefreshTargetPosition()
	{
		if (target == null)
			return;
		targetPosition = KeepInBounds(target.transform.position);
		targetPosition.z = transform.position.z;
	}

    Vector3 KeepInBounds(Vector3 point)
    {
		float z = point.z;
		point.z = 0;
		if (!_bounds.Contains(point))
        {            
			point = _bounds.ClosestPoint(point);
        }
		point.z = z;
		return point;
	}
}
