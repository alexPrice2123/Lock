using Godot;
using System;
 using System.Linq;

public partial class Player : CharacterBody3D
{
    // --- CONSTANTS ---
    public float CamSense = 0.002f;                  // Camera mouse sensitivity
    public float TurnSense = 0.008f;                  // Camera mouse sensitivity

    // --- NODE REFERENCES ---
    private Node3D _head;                            // Player head node (handles rotation)
    private Camera3D _cam;                           // Player camera node
    private RayCast3D _ray;
    private Node3D _holdPosition;
    private Control _ui;
    private Button _leftButton;
    private Button _rightButton;
    private Button _downButton;
    private Button _upButton;
    private Button _comboKeyA;
    private Button _comboKeyD;
    private ColorRect _crosshair;
    private Label _comboCodeText;
    private AudioStreamPlayer3D _sound;
    private GpuParticles3D _opened;
    private Label _moneyTimer;


    // --- VARIABLES ---
    private float _bobTime = 0.0f;                   // Time accumulator for head-bob effect
    private CharacterBody3D _lastSeen;
    private bool _holdingItem = false;
    private string _spinDirection;
    private int _currentComboNumber;
    private int? _comboCodeInput;
    private int _comboCode;
    private double _comboDigitCount;
    public int tweenTimer = 0;
    public string _face;
    private Transform3D ogPosition;
    private CharacterBody3D _hovering;
    private RandomNumberGenerator _rng = new RandomNumberGenerator();
    private int _oldComboNumber;


    // --- READY ---
    public override void _Ready()
    {
        Input.MouseMode = Input.MouseModeEnum.Captured;      // Capture mouse cursor at start
        _head = GetNode<Node3D>("Head");
        _cam = GetNode<Camera3D>("Head/Camera3D");
        _ray = GetNode<RayCast3D>("Head/Camera3D/Ray");
        _holdPosition = GetNode<Node3D>("HoldPosition");
        _leftButton = GetNode<Button>("UI/TurnLeft");
        _rightButton = GetNode<Button>("UI/TurnRight");
        _downButton = GetNode<Button>("UI/TurnDown");
        _upButton = GetNode<Button>("UI/TurnUp");
        _comboKeyA = GetNode<Button>("UI/ComboKeyA");
        _comboKeyD = GetNode<Button>("UI/ComboKeyD");
        _ui = GetNode<Control>("UI");
        _crosshair = GetNode<ColorRect>("UI/Crosshair");
        _comboCodeText = GetNode<Label>("UI/ComboCode");
        _sound = GetNode<AudioStreamPlayer3D>("Sound");
        _opened = GetNode<GpuParticles3D>("Opened");
        _moneyTimer = GetNode<Label>("UI/MoneyTimer");
        if (_ui is Ui ui) { ui._player = this; }
        _rng.Randomize();
        _comboCode = _rng.RandiRange(1000, 9999);
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

            if (_holdingItem == true)
            {
                _moneyTimer.Visible = false;
                _holdingItem = false;
                _lastSeen.GlobalTransform = ogPosition;
                _lastSeen = null;
                Input.MouseMode = Input.MouseModeEnum.Captured;
                _leftButton.Visible = false;
                _rightButton.Visible = false;
                _upButton.Visible = false;
                _downButton.Visible = false;
                _crosshair.Visible = true;
            }
        }

        else if (@event is InputEventMouseButton click
                 && Input.MouseMode == Input.MouseModeEnum.Captured
                 && click.Pressed
                 && _lastSeen != null)
        {
            _holdingItem = true;
            Input.MouseMode = Input.MouseModeEnum.Visible;
            _leftButton.Visible = true;
            _rightButton.Visible = true;
            _upButton.Visible = true;
            _downButton.Visible = true;
            _moneyTimer.Visible = true;
            LockStart();
            _crosshair.Visible = false;
        }

        else if (@event is InputEventKey aKey && aKey.Keycode == Key.A && aKey.Pressed && _holdingItem == true)
        {
            if (_hovering != null)
            {
                if (_hovering.Name == "ComboLock")
                {
                    if (_spinDirection == null)
                    {
                        _spinDirection = "Left";
                    }
                    else if (_spinDirection == "Right")
                    {
                        _spinDirection = "Left";
                        string combinedString = _comboCodeInput.ToString() + _currentComboNumber.ToString();
                            _comboCodeInput = int.Parse(combinedString);
                            GD.Print(_comboCodeInput);
                            _comboCodeText.Text = _comboCodeInput.ToString();
                            if (_comboCodeInput == 0) { _comboDigitCount = 1; }
                            else
                            {
                                _comboDigitCount = Math.Floor(Math.Log10((double)_comboCodeInput)) + 1;
                            if (_comboDigitCount >= 4 && _comboCodeInput != _comboCode)
                            {
                                _comboCodeInput = null;
                                _comboCodeText.Text = "Incorrect!";
                                _comboDigitCount = 0;
                            }
                            else if (_comboCodeInput == _comboCode)
                            {
                                _opened.Emitting = true;
                                CharacterBody3D toDestory = _hovering;
                                _hovering = null;
                                toDestory.QueueFree();
                                GetMouseCollision();
                                _comboKeyD.Visible = false;
                                _comboKeyA.Visible = false;
                                _comboCodeText.Visible = false;
                            }
                        }
                    }
                    _hovering.GetNode<MeshInstance3D>("Mesh/Cylinder_001").RotateX(Mathf.DegToRad(10f));
                    if (_hovering.GetNode<Area3D>("Mesh/Cylinder/Pick").GetOverlappingAreas().Count > 0)
                    {
                        _currentComboNumber = int.Parse(_hovering.GetNode<Area3D>("Mesh/Cylinder/Pick").GetOverlappingAreas()[0].Name);
                    }
                }
            }
        }

        else if (@event is InputEventKey dKey && dKey.Keycode == Key.D && dKey.Pressed && _holdingItem == true)
        {
            if (_hovering != null)
            {
                if (_hovering.Name == "ComboLock")
                {
                    if (_spinDirection == null)
                    {
                        _spinDirection = "Right";
                    }
                    else if (_spinDirection == "Left")
                    {
                        _spinDirection = "Right";
                        string combinedString = _comboCodeInput.ToString() + _currentComboNumber.ToString();
                            _comboCodeInput = int.Parse(combinedString);
                            GD.Print(_comboCodeInput);
                            _comboCodeText.Text = _comboCodeInput.ToString();
                            if (_comboCodeInput == 0) { _comboDigitCount = 1; }
                            else
                            {
                                _comboDigitCount = Math.Floor(Math.Log10((double)_comboCodeInput)) + 1;
                            if (_comboDigitCount >= 4 && _comboCodeInput != _comboCode)
                            {
                                _comboCodeInput = null;
                                _comboCodeText.Text = "Incorrect!";
                                _comboDigitCount = 0;
                            }
                            else if (_comboCodeInput == _comboCode)
                            {
                                _opened.Emitting = true;
                                CharacterBody3D toDestory = _hovering;
                                _hovering = null;
                                toDestory.QueueFree();
                                GetMouseCollision();
                                _comboKeyD.Visible = false;
                                _comboKeyA.Visible = false;
                                _comboCodeText.Visible = false;
                            }
                        }
                    }
                    _hovering.GetNode<MeshInstance3D>("Mesh/Cylinder_001").RotateX(Mathf.DegToRad(-10f));
                    if (_hovering.GetNode<Area3D>("Mesh/Cylinder/Pick").GetOverlappingAreas().Count > 0)
                    {
                        _currentComboNumber = int.Parse(_hovering.GetNode<Area3D>("Mesh/Cylinder/Pick").GetOverlappingAreas()[0].Name);
                    }
                }
            }
        }

    }

    // --- PHYSICS LOOP ---
    public override void _PhysicsProcess(double delta)
    {
        if (_oldComboNumber != _currentComboNumber)
        {
            _oldComboNumber = _currentComboNumber;
            int reversed = ReverseNumber(_comboCode);
            int divisor = (int)Math.Pow(10, _comboDigitCount);
            int tempNumber = reversed / divisor;
            int digit = tempNumber % 10;
            GD.Print(digit);
            if (_oldComboNumber == digit)
            {
                _sound.Stream = GD.Load<AudioStream>("res://Assets/Sounds/ComboClick.mp3");
                _sound.Play();
            }

        }

        if (_holdingItem == true && _lastSeen.GetChildren().OfType<CharacterBody3D>().ToList().Count <= 0)
        {
            CharacterBody3D toDestroy = _lastSeen;
            _lastSeen = null;
            toDestroy.QueueFree();
            _holdingItem = false;
            Input.MouseMode = Input.MouseModeEnum.Captured;
            _leftButton.Visible = false;
            _rightButton.Visible = false;
            _upButton.Visible = false;
            _downButton.Visible = false;
            _crosshair.Visible = true;
            _holdPosition.GlobalRotation = Vector3.Zero;
            _comboCodeInput = null;
            _comboCode = _rng.RandiRange(1000, 9999);
            _comboDigitCount = 0;
            _comboCodeText.Text = "";
        }

        if (GetMouseCollision() != null && _holdingItem == false)
        {
            CharacterBody3D targetNode = GetMouseCollision();
            if (targetNode is Safe safe)
            {
                safe.GetNode<MeshInstance3D>("Glow").Visible = true;
                safe.GetNode<Label3D>("Prompt").Visible = true;
                _lastSeen = safe;
                ogPosition = _lastSeen.GlobalTransform;
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
            _face = GetMostFacingFace();
            if (_face == "Top")
            {
                _leftButton.Disabled = true; _leftButton.GetNode<Sprite2D>("Arrow").Visible = false;
                _rightButton.Disabled = true; _rightButton.GetNode<Sprite2D>("Arrow").Visible = false;
                _downButton.Disabled = false; _downButton.GetNode<Sprite2D>("Arrow").Visible = true;
                _upButton.Disabled = true; _upButton.GetNode<Sprite2D>("Arrow").Visible = false;
            }
            else if (_face == "Left" || _face == "Right" || _face == "Back")
            {
                _leftButton.Disabled = false; _leftButton.GetNode<Sprite2D>("Arrow").Visible = true;
                _rightButton.Disabled = false; _rightButton.GetNode<Sprite2D>("Arrow").Visible = true;
                _downButton.Disabled = true; _downButton.GetNode<Sprite2D>("Arrow").Visible = false;
                _upButton.Disabled = true; _upButton.GetNode<Sprite2D>("Arrow").Visible = false;
            }
            else if (_face == "Bottom")
            {
                _leftButton.Disabled = true; _leftButton.GetNode<Sprite2D>("Arrow").Visible = false;
                _rightButton.Disabled = true; _rightButton.GetNode<Sprite2D>("Arrow").Visible = false;
                _downButton.Disabled = true; _downButton.GetNode<Sprite2D>("Arrow").Visible = false;
                _upButton.Disabled = false; _upButton.GetNode<Sprite2D>("Arrow").Visible = true;
            }
            else
            {
                _leftButton.Disabled = false; _leftButton.GetNode<Sprite2D>("Arrow").Visible = true;
                _rightButton.Disabled = false; _rightButton.GetNode<Sprite2D>("Arrow").Visible = true;
                _downButton.Disabled = false; _downButton.GetNode<Sprite2D>("Arrow").Visible = true;
                _upButton.Disabled = false; _upButton.GetNode<Sprite2D>("Arrow").Visible = true;
            }
            if (_ui is Ui buttons)
            {
                if (buttons._direction != 0)
                {
                    LockStart();
                    if (buttons._direction == 1) //Left
                    {
                        tweenTimer += 1;
                        _holdPosition.GlobalRotation += new Vector3(0f, Mathf.DegToRad(5f), 0f);
                        if (tweenTimer >= 18)
                        {
                            buttons._direction = 0;
                            tweenTimer = 0;
                        }

                    }
                    else if (buttons._direction == 2) //Up
                    {
                        tweenTimer += 1;
                        _holdPosition.GlobalRotation += new Vector3(Mathf.DegToRad(5f), 0f, 0f);
                        if (tweenTimer >= 18)
                        {
                            buttons._direction = 0;
                            tweenTimer = 0;
                            if (_face == "Front")
                            {
                                if (_lastSeen is Safe safe) { _holdPosition.GlobalRotation = safe._direction; }
                            }
                        }
                    }
                    else if (buttons._direction == 3) //Down
                    {
                        tweenTimer += 1;
                        if (_face == "Bottom")
                        {
                            _holdPosition.GlobalRotation += new Vector3(Mathf.DegToRad(-4.9f), 0f, 0f);
                        }
                        else
                        {
                            _holdPosition.GlobalRotation += new Vector3(Mathf.DegToRad(-5f), 0f, 0f);
                        }
                        if (tweenTimer >= 18)
                        {
                            buttons._direction = 0;
                            tweenTimer = 0;
                        }
                    }
                    else if (buttons._direction == 4) //Right
                    {
                        tweenTimer += 1;
                        _holdPosition.GlobalRotation += new Vector3(0f, Mathf.DegToRad(-5f), 0f);
                        if (tweenTimer >= 18)
                        {
                            buttons._direction = 0;
                            tweenTimer = 0;
                        }
                    }
                    else if (buttons._direction == 5) //CombLock D
                    {
                        if (_spinDirection == null)
                        {
                            _spinDirection = "Right";
                        }
                        else if (_spinDirection == "Left")
                        {
                            _spinDirection = "Right";
                             string combinedString = _comboCodeInput.ToString() + _currentComboNumber.ToString();
                            _comboCodeInput = int.Parse(combinedString);
                            GD.Print(_comboCodeInput);
                            _comboCodeText.Text = _comboCodeInput.ToString();
                            if (_comboCodeInput == 0) { _comboDigitCount = 1; }
                            else
                            {
                                _comboDigitCount = Math.Floor(Math.Log10((double)_comboCodeInput)) + 1;
                            if (_comboDigitCount >= 4 && _comboCodeInput != _comboCode)
                            {
                                _comboCodeInput = null;
                                _comboCodeText.Text = "Incorrect!";
                                _comboDigitCount = 0;
                            }
                            else if (_comboCodeInput == _comboCode)
                            {
                                _opened.Emitting = true;
                                CharacterBody3D toDestory = _hovering;
                                _hovering = null;
                                toDestory.QueueFree();
                                GetMouseCollision();
                                _comboKeyD.Visible = false;
                                _comboKeyA.Visible = false;
                                _comboCodeText.Visible = false;
                            }
                        }

                        }
                        _hovering.GetNode<MeshInstance3D>("Mesh/Cylinder_001").RotateX(Mathf.DegToRad(-2f));
                        if (_hovering.GetNode<Area3D>("Mesh/Cylinder/Pick").GetOverlappingAreas().Count > 0)
                        {
                            _currentComboNumber = int.Parse(_hovering.GetNode<Area3D>("Mesh/Cylinder/Pick").GetOverlappingAreas()[0].Name);
                        }
                    }
                    else if (buttons._direction == 6) //CombLock A
                    {
                        if (_spinDirection == null)
                        {
                            _spinDirection = "Left";
                        }
                        else if (_spinDirection == "Right")
                        {
                            _spinDirection = "Left";
                            string combinedString = _comboCodeInput.ToString() + _currentComboNumber.ToString();
                            _comboCodeInput = int.Parse(combinedString);
                            GD.Print(_comboCodeInput);
                            _comboCodeText.Text = _comboCodeInput.ToString();
                            if (_comboCodeInput == 0) { _comboDigitCount = 1; }
                            else
                            {
                                _comboDigitCount = Math.Floor(Math.Log10((double)_comboCodeInput)) + 1;
                            if (_comboDigitCount >= 4 && _comboCodeInput != _comboCode)
                            {
                                _comboCodeInput = null;
                                _comboCodeText.Text = "Incorrect!";
                                _comboDigitCount = 0;
                            }
                            else if (_comboCodeInput == _comboCode)
                            {
                                _opened.Emitting = true;
                                CharacterBody3D toDestory = _hovering;
                                _hovering = null;
                                toDestory.QueueFree();
                                GetMouseCollision();
                                _comboKeyD.Visible = false;
                                _comboKeyA.Visible = false;
                                _comboCodeText.Visible = false;
                            }
                        }

                        }
                        _hovering.GetNode<MeshInstance3D>("Mesh/Cylinder_001").RotateX(Mathf.DegToRad(2f));
                        if (_hovering.GetNode<Area3D>("Mesh/Cylinder/Pick").GetOverlappingAreas().Count > 0)
                        {
                            _currentComboNumber = int.Parse(_hovering.GetNode<Area3D>("Mesh/Cylinder/Pick").GetOverlappingAreas()[0].Name);
                        }
                    }
                }
            }

            _lastSeen.GlobalPosition = _lastSeen.GlobalPosition.Lerp(_holdPosition.GlobalPosition, (float)delta * 5f);
            _lastSeen.GlobalRotation = _holdPosition.GlobalRotation;

            _head.GlobalRotation = _head.GlobalRotation.Lerp(new Vector3(0f, 0f, 0f), (float)delta * 5f);
            _cam.GlobalRotation = _cam.GlobalRotation.Lerp(new Vector3(0f, 0f, 0f), (float)delta * 5f);

            _lastSeen.GetNode<Label3D>("Prompt").Visible = false;

            /*Vector2 mousePosition = GetViewport().GetMousePosition();

            Vector3 rayOrigin = _cam.ProjectRayOrigin(mousePosition);
            Vector3 rayNormal = _cam.ProjectRayNormal(mousePosition);
            Vector3 rayEnd = rayOrigin + (rayNormal * 100f);

            var spaceState = GetWorld3D().DirectSpaceState;
            var query = PhysicsRayQueryParameters3D.Create(rayOrigin, rayEnd);
            query.CollideWithBodies = true; // Include PhysicsBody3D nodes

            var result = spaceState.IntersectRay(query);
            if (result == null)
            {
                return;
            }

            if (result.Count > 0)
            {
                CharacterBody3D collider = result["collider"].As<CharacterBody3D>();

                if (collider.FindChild("Mesh", false) != null)
                {
                    _hovering = collider;
                }
                else
                {
                    _hovering = null;
                }
            }*/
        }
    }

    // --- CUSTOM FUNCTIONS ---
    public CharacterBody3D GetMouseCollision()
    {
        if (_ray.IsColliding())
        {
            if (_ray.GetCollider().GetClass() == "CharacterBody3D" && _ray.GetCollider() != this)
            {
                _hovering = (CharacterBody3D)_ray.GetCollider();
                return (CharacterBody3D)_ray.GetCollider();
            }
        }
        return null;
    }

    private readonly System.Collections.Generic.Dictionary<Vector3, string> _faceNames =
        new System.Collections.Generic.Dictionary<Vector3, string>
    {
        { Vector3.Left, "Right" },
        { Vector3.Right, "Left" },
        { Vector3.Up, "Bottom" },
        { Vector3.Down, "Top" },
        { Vector3.Forward, "Front" },
        { Vector3.Back, "Back" }
    };

    public string GetMostFacingFace()
    {
        if (_lastSeen == null)
        {
            return "No box available.";
        }

        float highestDot = -1.0f;
        string mostFacingFace = "None";

        // The direction the camera is looking in global space.
        // Godot's cameras look down their negative Z-axis by default.
        Vector3 cameraDirection = -_cam.GlobalTransform.Basis.Z.Normalized();

        // Check each face of the cube
        foreach (var entry in _faceNames)
        {
            // Convert the cube's local normal to a global normal
            Vector3 globalFaceNormal = _lastSeen.GlobalTransform.Basis * entry.Key;

            // Calculate the dot product
            float dot = cameraDirection.Dot(globalFaceNormal);

            // If this face is more aligned with the camera's view, update the result
            if (dot > highestDot)
            {
                highestDot = dot;
                mostFacingFace = entry.Value;
            }
        }

        return mostFacingFace;
    }

    public void LockStart()
    {
        if (_hovering != null)
        {
            if (_hovering.Name == "ComboLock")
            {
                _comboKeyD.Visible = true;
                _comboKeyA.Visible = true;
                _comboCodeText.Visible = true;
            }
            else
            {
                _comboKeyD.Visible = false;
                _comboKeyA.Visible = false;
                _comboCodeText.Visible = false;
            }
        }
    }
    
    public int ReverseNumber(int number)
    {
        bool isNegative = number < 0;
        number = Math.Abs(number); // Work with the absolute value

        int reversedNumber = 0;
        while (number != 0)
        {
            int lastDigit = number % 10;
            reversedNumber = (reversedNumber * 10) + lastDigit;
            number /= 10;
        }

        return isNegative ? -reversedNumber : reversedNumber; // Apply the original sign
    }
}
