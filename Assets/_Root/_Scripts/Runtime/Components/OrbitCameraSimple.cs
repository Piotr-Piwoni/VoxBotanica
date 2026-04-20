using UnityEngine;
using UnityEngine.InputSystem;

namespace VoxBotanica.Components
{
/// <summary>
///     Simple orbit-style camera controller that allows rotating, panning, and zooming around a target.
///     <para>
///         <para>
///             Controls:
///             <list type="bullet">
///                 <item>
///                     <description>Alt + Left Mouse: Orbit around the pivot.</description>
///                 </item>
///                 <item>
///                     <description>Alt + Middle Mouse: Pan the pivot position.</description>
///                 </item>
///                 <item>
///                     <description>Scroll Wheel: Zoom in and out.</description>
///                 </item>
///                 <item>
///                     <description>F key: Reset pivot to target and set default distance.</description>
///                 </item>
///             </list>
///         </para>
///     </para>
///     The camera maintains a pivot point (initially the target position) and orbits around it using
///     yaw and pitch rotations. Distance controls how far the camera is from the pivot.
/// </summary>
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

	/// <summary>
	///     Initialises the camera pivot and rotation based on the current transform and target.
	///     Disables the component if no target is assigned.
	/// </summary>
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

	/// <summary>
	///     Handles input and updates camera behaviour after all other updates.
	///     Resets pivot when pressing F, then processes rotation, panning, zoom, and positioning.
	/// </summary>
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

	/// <summary>
	///     Handles camera panning using Alt + Middle Mouse input.
	///     Moves the pivot point relative to the camera's right and up vectors.
	/// </summary>
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

	/// <summary>
	///     Handles camera rotation using Alt + Left Mouse input.
	///     Adjusts yaw and pitch while clamping pitch to defined limits.
	/// </summary>
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

	/// <summary>
	///     Handles zooming using the mouse scroll wheel.
	///     Adjusts distance with scaling and clamps it within valid bounds.
	/// </summary>
	private void HandleZoom()
	{
		float scroll = Mouse.current.scroll.ReadValue().y;

		const float SCALE_FACTOR = 0.01f;
		Distance *= 1f - scroll * SCALE_FACTOR * ZoomSpeed;
		Distance = Mathf.Clamp(Distance, MinDistance, MaxDistance);
	}

	/// <summary>
	///     Updates the camera position and orientation based on pivot, rotation, and distance.
	///     Positions the camera behind the pivot and makes it look at the pivot point.
	/// </summary>
	private void UpdateCameraPosition()
	{
		Quaternion rotation = Quaternion.Euler(_Pitch, _Yaw, 0);
		Vector3 direction = rotation * Vector3.forward;

		transform.position = _Pivot - direction * Distance;
		transform.LookAt(_Pivot);
	}
}
}
