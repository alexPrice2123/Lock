using Godot;
using System;

public partial class Player : CharacterBody3D
{
	// --- CONSTANTS ---
	public const float Speed = 6.5f;                 // Base player movement speed
	public const float BobFreq = 2.0f;               // Frequency of camera head-bob
	public const float BobAmp = 0.06f;               // Amplitude of camera head-bob
	public float CamSense = 0.002f;                  // Camera mouse sensitivity
    public float TurnSense = 0.008f;                  // Camera mouse sensitivity
    private Vector2 _lastMousePosition;

	// --- NODE REFERENCES ---
    private Node3D _head;                            // Player head node (handles rotation)
	private Camera3D _cam;                           // Player camera node
    private RayCast3D _ray;
    private Node3D _holdPosition;
    private Quaternion _startHoldRotation;
    private RayCast3D _mouseRay;

	// --- VARIABLES ---
    private float _bobTime = 0.0f;                   // Time accumulator for head-bob effect
    private CharacterBody3D _lastSeen;
    private bool _holdingItem = false;

    // --- READY ---
    public override void _Ready()
    {
        Input.MouseMode = Input.MouseModeEnum.Captured;      // Capture mouse cursor at start
        _head = GetNode<Node3D>("Head");
        _cam = GetNode<Camera3D>("Head/Camera3D");
        _ray = GetNode<RayCast3D>("Head/Camera3D/Ray");
        _holdPosition = GetNode<Node3D>("Head/Camera3D/HoldPosition");
        _mouseRay = GetNode<RayCast3D>("MouseRay");
        _startHoldRotation = _holdPosition.GlobalTransform.Basis.GetRotationQuaternion();
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

            _lastMousePosition = motion.Position;
        }
        else if (@event is InputEventMouseMotion lMotion && Input.MouseMode == Input.MouseModeEnum.Visible && !Input.IsMouseButtonPressed(MouseButton.Right))
        {
            _lastMousePosition = lMotion.Position;
            GD.Print(_lastMousePosition);
            _startHoldRotation = _holdPosition.GlobalTransform.Basis.GetRotationQuaternion();
        }
        else if (@event is InputEventMouseMotion rMotion && Input.MouseMode == Input.MouseModeEnum.Visible && _holdingItem == true)
        {
            if (Input.IsMouseButtonPressed(MouseButton.Right))
            {
                //_lastSeen.RotateObjectLocal(Vector3.Up, rMotion.Relative.X * TurnSense);
                //_lastSeen.RotateObjectLocal(Vector3.Right, -rMotion.Relative.Y * TurnSense);

                //_lastSeen.RotateY(rMotion.Relative.X * TurnSense);
                //_lastSeen.RotateX(rMotion.Relative.Y * TurnSense);

                if (_lastSeen is Safe safe)
                {
                    // Create a new rotation quaternion for a small rotation around the local Y-axis
                    Vector2 mouseDelta = rMotion.Position - _lastMousePosition;

                    Quaternion yawRotation = new Quaternion(Vector3.Up, mouseDelta.X * TurnSense);
                    Quaternion pitchRotation = new Quaternion(GlobalTransform.Basis.X, mouseDelta.Y * TurnSense);
                    Quaternion newRotation = yawRotation * pitchRotation * GlobalTransform.Basis.GetRotationQuaternion();

                    // Combine the rotations. Multiplying rotationDelta * currentRotation applies rotationDelta
                    // relative to the object's local axes.
                    Quaternion newTotalRotation = _startHoldRotation * newRotation;

                    // Apply the new rotation to the node's GlobalTransform
                    _holdPosition.GlobalTransform = new Transform3D(new Basis(newTotalRotation), _holdPosition.GlobalTransform.Origin);
                }
            }
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

        else if (@event is InputEventMouseButton click
                 && Input.MouseMode == Input.MouseModeEnum.Captured
                 && click.Pressed
                 && _lastSeen != null)
        {
            _holdingItem = true;
            Input.MouseMode = Input.MouseModeEnum.Visible;
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

        if (GetMouseCollision() != null)
        {
            CharacterBody3D targetNode = GetMouseCollision();
            if (targetNode is Safe safe)
            {
                safe.GetNode<MeshInstance3D>("Glow").Visible = true;
                safe.GetNode<Label3D>("Prompt").Visible = true;
                _lastSeen = safe;
            }
        }
        else if (_lastSeen != null && _holdingItem == false)
        {
            CharacterBody3D targetNode = _lastSeen;
           if (targetNode is Safe safe)
            {
                safe.GetNode<MeshInstance3D>("Glow").Visible = false;
                safe.GetNode<Label3D>("Prompt").Visible = false;
                _lastSeen = null;
            } 
        }

        if (_holdingItem == true)
        {
            _lastSeen.GlobalPosition = _lastSeen.GlobalPosition.Lerp(_holdPosition.GlobalPosition, (float)delta * 5f);
            //_lastSeen.GlobalRotation = _lastSeen.GlobalRotation.Lerp(_holdPosition.GlobalRotation, (float)delta * 5f);
            _lastSeen.GlobalRotation = _holdPosition.GlobalRotation;
            _lastSeen.GetNode<Label3D>("Prompt").Visible = false;
            Vector2 mousePosition = GetViewport().GetMousePosition();
            //GD.Print(mousePosition);
            _mouseRay.GlobalPosition = new Vector3(mousePosition.X, mousePosition.Y, GlobalPosition.Z);
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
	}
}
