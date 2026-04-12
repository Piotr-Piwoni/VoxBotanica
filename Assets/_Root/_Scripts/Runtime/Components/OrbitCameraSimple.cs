using UnityEngine;
using UnityEngine.InputSystem;

namespace VoxBotanica.Components
{
public class OrbitCameraSimple : MonoBehaviour
{
	public Transform Target;
	[Header("Distance")]
	public float Distance = 5f;
	public float MinDistance = 1f;
	public float MaxDistance = 20f;
	public float ZoomSpeed = 10f;
	[Header("Rotation")]
	public float RotationSpeed = 0.2f;
	public float MinPitch = -80f;
	public float MaxPitch = 80f;
	[Header("Pan")]
	public float PanSpeed = 0.01f;

	private float _Pitch;
	private float _Yaw;
	private Vector3 _Pivot;


	private void Start()
	{
		if (!Target)
		{
			Debug.LogError("OrbitCamera requires a target.");
			enabled = false;
			return;
		}

		_Pivot = Target.position;
		Vector3 angles = transform.eulerAngles;
		_Yaw = angles.y;
		_Pitch = angles.x;
	}

	private void LateUpdate()
	{
		if (Keyboard.current.fKey.isPressed)
		{
			_Pivot = Target.position;
			Distance = 20f;
		}

		HandleRotation();
		HandlePan();
		HandleZoom();
		UpdateCameraPosition();
	}

	private void HandlePan()
	{
		if (!Keyboard.current.leftAltKey.isPressed ||
			!Mouse.current.middleButton.isPressed) return;

		Vector2 delta = Mouse.current.delta.ReadValue();

		Vector3 right = transform.right;
		Vector3 up = transform.up;

		Vector3 move = (-right * delta.x + -up * delta.y) * (PanSpeed * Distance);
		_Pivot += move;
	}

	private void HandleRotation()
	{
		// Alt + Left Mouse.
		if (!Keyboard.current.leftAltKey.isPressed ||
			!Mouse.current.leftButton.isPressed) return;

		Vector2 delta = Mouse.current.delta.ReadValue();

		_Yaw += delta.x * RotationSpeed;
		_Pitch -= delta.y * RotationSpeed;

		_Pitch = Mathf.Clamp(_Pitch, MinPitch, MaxPitch);
	}

	private void HandleZoom()
	{
		float scroll = Mouse.current.scroll.ReadValue().y;

		Distance -= scroll * ZoomSpeed * Time.deltaTime;
		Distance = Mathf.Clamp(Distance, MinDistance, MaxDistance);
	}

	private void UpdateCameraPosition()
	{
		Quaternion rotation = Quaternion.Euler(_Pitch, _Yaw, 0);
		Vector3 direction = rotation * Vector3.forward;

		transform.position = _Pivot - direction * Distance;
		transform.LookAt(_Pivot);
	}
}
}
