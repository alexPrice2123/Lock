using Godot;
using System;

public partial class Player : CharacterBody3D
{
	// --- CONSTANTS ---
	public const float Speed = 6.5f;                 // Base player movement speed
	public const float BobFreq = 2.0f;               // Frequency of camera head-bob
	public const float BobAmp = 0.06f;               // Amplitude of camera head-bob
	public float CamSense = 0.002f;                  // Camera mouse sensitivity

	// --- NODE REFERENCES ---
	private Node3D _head;                            // Player head node (handles rotation)
	private Camera3D _cam;                           // Player camera node

	// --- VARIABLES ---
	private float _bobTime = 0.0f;                   // Time accumulator for head-bob effect
	
	// --- READY ---
	public override void _Ready()
	{
		Input.MouseMode = Input.MouseModeEnum.Captured;      // Capture mouse cursor at start
		_head = GetNode<Node3D>("Head");
		_cam = GetNode<Camera3D>("Head/Camera3D");
	}

	// --- INPUT HANDLER ---
	public override void _Input(InputEvent @event)
	{
		// --- Camera look ---
		if (@event is InputEventMouseMotion motion && Input.MouseMode == Input.MouseModeEnum.Captured)
		{
			_head.RotateY(-motion.Relative.X * CamSense);
			_cam.RotateX(-motion.Relative.Y * CamSense);

			Vector3 camRot = _cam.Rotation;
			camRot.X = Mathf.Clamp(camRot.X, Mathf.DegToRad(-80f), Mathf.DegToRad(80f));
			_cam.Rotation = camRot;
		}

		// --- Pause menu toggle ---
		else if (@event is InputEventKey escapeKey && escapeKey.Keycode == Key.Escape && escapeKey.Pressed)
		{

			if (Input.MouseMode == Input.MouseModeEnum.Captured)
			{
				Input.MouseMode = Input.MouseModeEnum.Visible;
			}
			else
			{
				Input.MouseMode = Input.MouseModeEnum.Captured;
			}
		}
	}

	// --- PHYSICS LOOP ---
	public override void _PhysicsProcess(double delta)
	{
		Vector3 velocity = Velocity;

		// --- Movement input ---
		Vector2 inputDir = Input.GetVector("move_left", "move_right", "move_forward", "move_back");
		Vector3 direction = (_head.GlobalTransform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();
		if (direction != Vector3.Zero)
		{
			velocity.X = direction.X * Speed;
			velocity.Z = direction.Z * Speed;
		}
		else
		{
			velocity = velocity.Lerp(new Vector3(0f, velocity.Y, 0f), (float)delta * 10f);
		}

		// --- Camera FOV scaling ---
		float fovGoal = Mathf.Lerp(_cam.Fov, Velocity.Length() + 80, (float)delta * 10f);
		_cam.Fov = fovGoal;

		// --- Head bob ---
		Transform3D camTransformGoal = _cam.Transform;
		_bobTime += (float)delta * velocity.Length() * (Convert.ToInt32(IsOnFloor()) + 0.2f);
		camTransformGoal.Origin = HeadBob(_bobTime);
		_cam.Transform = camTransformGoal;
		
		// --- Gravity ---
		if (!IsOnFloor()) { velocity += GetGravity() * (float)delta; }
			
		// --- Apply movement ---
		Velocity = velocity;
		MoveAndSlide();
	}

	// --- CUSTOM FUNCTIONS ---
	private Vector3 HeadBob(float bobTime)
	{
		Vector3 pos = Vector3.Zero;
		pos.Y = Mathf.Sin(bobTime * BobFreq) * BobAmp;
		pos.X = Mathf.Cos(bobTime * BobFreq / 2) * BobAmp;
		return pos;
	}
/*
	public CharacterBody3D GetMouseCollision()
	{
		if (_ray.IsColliding())
		{
			if (_ray.GetCollider().GetClass() == "CharacterBody3D" && _ray.GetCollider() != this)
			{
				return (CharacterBody3D)_ray.GetCollider();
			}
		}
		return null;
	}*/
}
