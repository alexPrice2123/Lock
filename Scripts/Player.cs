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
    public RayCast3D _ray;
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
    private Label _moneyEarned;
    private Label _playerMoney;
    private MeshInstance3D _lockPick;
    private MeshInstance3D _screwdriver;
    private Button _lockButton;
    private Button _clickLockButton;
    private Label _minigamePoints;
    private Control _screws;
    private Control _wires;


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
    private float _money;
    public bool _hasLockPick = false;
    private bool _inMiniGame = false;
    private int _minigameSpin = 1;
    private int _minigameClicks = 0;
    private bool _fingerLockOpen = false;
    private bool _wireOneDone = false;
    private bool _wireTwoDone = false;
    private bool _wireThreeDone = false;
    public bool _finished = false;
    public bool _hasScrewdriver = false;




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
        _moneyEarned = GetNode<Label>("UI/MoneyEarned");
        _playerMoney = GetNode<Label>("UI/Money");
        _playerMoney.Text = "$" + _money;
        _lockPick = GetNode<MeshInstance3D>("Head/Camera3D/LockPick");
        _lockButton = GetNode<Button>("UI/LockPick");
        _clickLockButton = GetNode<Button>("UI/Click");
        _minigamePoints = GetNode<Label>("UI/Click/MinigameClicks");
        _screws = GetNode<Control>("UI/Screws");
        _wires = GetNode<Control>("UI/Wires");
        _screwdriver = GetNode<MeshInstance3D>("Head/Camera3D/Screwdriver");
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

            if (_holdingItem == true && _inMiniGame == false)
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
            _crosshair.Visible = false;
            if (_lastSeen.Name == "box1")
            {
                _hasLockPick = true;
            }
            else if (_lastSeen.Name == "box3")
            {
                _hasScrewdriver = true;
            }
            if (_lastSeen is Safe safe) { _moneyTimer.Text = "$" + safe._money.ToString(); }
            LockStart();
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
                        ComboLockHandler();
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
                        ComboLockHandler();
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
        if (_finished == true)
        {
            _ui.GetNode<Label>("Done").Text = "Congrats! You saved $" + _money + " from the safes!";
            _ui.GetNode<Label>("Done").Visible = true;
            return;
        }
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
                _hovering.GetNode<GpuParticles3D>("Mesh/Cylinder/ClickW").Emitting = true;
                _hovering.GetNode<GpuParticles3D>("Mesh/Cylinder/ClickB").Emitting = true;
            }

        }

        if (_hasLockPick == true && _lockPick.Visible == false)
        {
            _lockPick.Visible = true;
        }

        if (_hasScrewdriver == true && _screwdriver.Visible == false)
        {
            _screwdriver.Visible = true;
        }

        if (_holdingItem == true && _moneyTimer.Visible == true)
        {
            if (_lastSeen is Safe safe) { safe._money -= safe._moneyLossPF; _moneyTimer.Text = "$" + Math.Ceiling(safe._money).ToString(); }
        }

        if (_holdingItem == true && _lastSeen.GetChildren().OfType<CharacterBody3D>().ToList().Count <= 0)
        {
            if (_ui is Ui buttons) { buttons._direction = 999; }

        }

        if (_fingerLockOpen == true && _leftButton.Visible == true)
        {
            _leftButton.Visible = false;
            _rightButton.Visible = false;
            _upButton.Visible = false;
            _downButton.Visible = false;
            _screws.Visible = false;
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

        if (_lastSeen != null && _holdingItem == false && _crosshair.Visible == false)
        {
            _lastSeen.GetNode<Label3D>("Prompt").Visible = false;
            _lastSeen.GetNode<MeshInstance3D>("Glow").Visible = false;
        }

        if (_inMiniGame == true)
        {
            _hovering.GetNode<Node3D>("RotationPoint/Mesh/MiniGame/Pick").Rotation += new Vector3(0f, 0.05f * (float)_minigameSpin, 0f);
        }

        if (_wireOneDone == true && _wireTwoDone == true && _wireThreeDone == true)
        {
            _wireOneDone = false;
            EndFingerLock();
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
                    if (buttons._direction == 1) //Left
                    {
                        tweenTimer += 1;
                        _holdPosition.GlobalRotation += new Vector3(0f, Mathf.DegToRad(5f), 0f);
                        LockStart();
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
                        LockStart();
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
                        LockStart();
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
                        LockStart();
                        if (tweenTimer >= 18)
                        {
                            buttons._direction = 0;
                            tweenTimer = 0;
                        }
                    }
                    else if (buttons._direction == 7) //LockPick
                    {
                        TweenKeyLock(_hovering.GetNode<Node3D>("RotationPoint"), -90f);
                        buttons._direction = 0;
                        _hovering.GetNode<MeshInstance3D>("RotationPoint/Mesh/MiniGame").Visible = true;
                        _leftButton.Visible = false;
                        _rightButton.Visible = false;
                        _upButton.Visible = false;
                        _downButton.Visible = false;
                        _lockButton.Visible = false;
                        _hovering.GetNode<Node3D>("RotationPoint/Mesh/MiniGame/Hole").Rotation += new Vector3(0f, (float)Mathf.DegToRad(_rng.RandiRange(0, 360)), 0f);
                        _inMiniGame = true;
                        _clickLockButton.Visible = true;
                        _hasLockPick = false;
                        _lockPick.Visible = false;

                    }
                    else if (buttons._direction == 8) //Click LockPick mini game
                    {
                        buttons._direction = 0;
                        GD.Print(_hovering.GetNode<Area3D>("RotationPoint/Mesh/MiniGame/Pick/Pick").GetOverlappingAreas().Count());
                        if (_hovering.GetNode<Area3D>("RotationPoint/Mesh/MiniGame/Pick/Pick").GetOverlappingAreas().Count() >= 1)
                        {
                            _minigameSpin *= -1;
                            _hovering.GetNode<Node3D>("RotationPoint/Mesh/MiniGame/Hole").Rotation += new Vector3(0f, (float)Mathf.DegToRad(_rng.RandiRange(0, 360)), 0f);
                            _minigameClicks += 1;
                            _minigamePoints.Text = _minigameClicks.ToString();
                            _hovering.GetNode<GpuParticles3D>("RotationPoint/Mesh/MiniGame/Pick/ClickW").Emitting = true;
                            _hovering.GetNode<GpuParticles3D>("RotationPoint/Mesh/MiniGame/Pick/ClickB").Emitting = true;
                            _sound.Stream = GD.Load<AudioStream>("res://Assets/Sounds/ComboClick.mp3");
                            _sound.Play();

                            if (_minigameClicks >= 5)
                            {
                                _opened.Emitting = true;
                                CharacterBody3D toDestory = _hovering;
                                _hovering = null;
                                toDestory.QueueFree();
                                GetMouseCollision();
                                _leftButton.Visible = true;
                                _rightButton.Visible = true;
                                _upButton.Visible = true;
                                _downButton.Visible = true;
                                _clickLockButton.Visible = false;
                                _inMiniGame = false;
                                _minigameClicks = 0;
                                _minigamePoints.Text = _minigameClicks.ToString();

                            }
                        }
                        else
                        {
                            _minigameClicks = 0;
                            _minigamePoints.Text = _minigameClicks.ToString();
                            _sound.Stream = GD.Load<AudioStream>("res://Assets/Sounds/KeyWrong.mp3");
                            _sound.Play();
                        }
                    }
                    else if (buttons._direction == 11) //Screw One
                    {
                        if (_hasScrewdriver == false)
                        {
                            buttons._direction = 0;
                        }
                        else
                        {
                            tweenTimer += 1;
                            _screws.GetNode<Button>("Unscrew1").Rotation -= 3;
                            _screws.GetNode<Button>("Unscrew1").Disabled = true;
                            if (tweenTimer >= 20)
                            {
                                buttons._direction = 0;
                                tweenTimer = 0;
                                FingerLockHandler();
                            }
                        }
                    }
                    else if (buttons._direction == 12) //Screw Two
                    {
                        if (_hasScrewdriver == false)
                        {
                            buttons._direction = 0;
                        }
                        else
                        {
                            tweenTimer += 1;
                            _screws.GetNode<Button>("Unscrew2").Rotation -= 3;
                            _screws.GetNode<Button>("Unscrew2").Disabled = true;
                            if (tweenTimer >= 20)
                            {
                                buttons._direction = 0;
                                tweenTimer = 0;
                                FingerLockHandler();
                            }
                        }
                    }
                    else if (buttons._direction == 13) //Screw Three
                    {
                        if (_hasScrewdriver == false)
                        {
                            buttons._direction = 0;
                        }
                        else
                        {
                            tweenTimer += 1;
                            _screws.GetNode<Button>("Unscrew3").Rotation -= 3;
                            _screws.GetNode<Button>("Unscrew3").Disabled = true;
                            if (tweenTimer >= 20)
                            {
                                buttons._direction = 0;
                                tweenTimer = 0;
                                FingerLockHandler();
                            }
                        }
                    }
                    else if (buttons._direction == 14) //Screw Four
                    {
                        if (_hasScrewdriver == false)
                        {
                            buttons._direction = 0;
                        }
                        else
                        {
                            tweenTimer += 1;
                            _screws.GetNode<Button>("Unscrew4").Rotation -= 3;
                            _screws.GetNode<Button>("Unscrew4").Disabled = true;
                            if (tweenTimer >= 20)
                            {
                                buttons._direction = 0;
                                tweenTimer = 0;
                                FingerLockHandler();
                            }
                        }
                    }
                    else if (buttons._direction == 21 && _wireOneDone == false) //Wire One Down
                    {
                        Vector2 mousePos = GetViewport().GetMousePosition();
                        _wires.GetNode<Line2D>("WireTop1/Line").SetPointPosition(1, mousePos - _wires.Position - new Vector2(13, 0));
                        _wires.GetNode<Line2D>("WireTop1/Line").DefaultColor = _wires.GetNode<ColorRect>("WireTop1").Color;
                    }
                    else if (buttons._direction == 31 && _wireOneDone == false) //Wire One Up
                    {
                        Vector2 mousePos = GetViewport().GetMousePosition();
                        if (IsNear(_wires.GetNode<ColorRect>("WireBottom1").Position + _wires.Position, mousePos, 25f) && _wires.GetNode<ColorRect>("WireBottom1").Color == _wires.GetNode<Line2D>("WireTop1/Line").DefaultColor)
                        {
                            _wires.GetNode<Line2D>("WireTop1/Line").SetPointPosition(1, _wires.GetNode<ColorRect>("WireBottom1").Position);
                            buttons._direction = 0;
                            _wireOneDone = true;
                            _sound.Stream = GD.Load<AudioStream>("res://Assets/Sounds/ComboClick.mp3");
                            _sound.Play();
                        }
                        else if (IsNear(_wires.GetNode<ColorRect>("WireBottom2").Position + _wires.Position, mousePos, 25f) && _wires.GetNode<ColorRect>("WireBottom2").Color == _wires.GetNode<Line2D>("WireTop1/Line").DefaultColor)
                        {
                            _wires.GetNode<Line2D>("WireTop1/Line").SetPointPosition(1, _wires.GetNode<ColorRect>("WireBottom2").Position);
                            buttons._direction = 0;
                            _wireOneDone = true;
                            _sound.Stream = GD.Load<AudioStream>("res://Assets/Sounds/ComboClick.mp3");
                            _sound.Play();
                        }
                        else if (IsNear(_wires.GetNode<ColorRect>("WireBottom3").Position + _wires.Position, mousePos, 25f) && _wires.GetNode<ColorRect>("WireBottom3").Color == _wires.GetNode<Line2D>("WireTop1/Line").DefaultColor)
                        {
                            _wires.GetNode<Line2D>("WireTop1/Line").SetPointPosition(1, _wires.GetNode<ColorRect>("WireBottom3").Position);
                            buttons._direction = 0;
                            _wireOneDone = true;
                            _sound.Stream = GD.Load<AudioStream>("res://Assets/Sounds/ComboClick.mp3");
                            _sound.Play();
                        }
                        else
                        {
                            _wires.GetNode<Line2D>("WireTop1/Line").SetPointPosition(1, _wires.GetNode<Line2D>("WireTop1/Line").GetPointPosition(0));
                            buttons._direction = 0;
                            _sound.Stream = GD.Load<AudioStream>("res://Assets/Sounds/KeyWrong.mp3");
                            _sound.Play();
                        }
                    }

                    else if (buttons._direction == 22 && _wireTwoDone == false) //Wire Two Down
                    {
                        Vector2 mousePos = GetViewport().GetMousePosition();
                        _wires.GetNode<Line2D>("WireTop2/Line").SetPointPosition(1, mousePos - _wires.Position - new Vector2(62, 0));
                        _wires.GetNode<Line2D>("WireTop2/Line").DefaultColor = _wires.GetNode<ColorRect>("WireTop2").Color;
                    }
                    else if (buttons._direction == 32 && _wireTwoDone == false) //Wire Two Up
                    {
                        Vector2 mousePos = GetViewport().GetMousePosition();
                        if (IsNear(_wires.GetNode<ColorRect>("WireBottom1").Position + _wires.Position, mousePos, 15f) && _wires.GetNode<ColorRect>("WireBottom1").Color == _wires.GetNode<Line2D>("WireTop2/Line").DefaultColor)
                        {
                            _wires.GetNode<Line2D>("WireTop2/Line").SetPointPosition(1, _wires.GetNode<ColorRect>("WireBottom1").Position - new Vector2(58, 0));
                            buttons._direction = 0;
                            _wireTwoDone = true;
                            _sound.Stream = GD.Load<AudioStream>("res://Assets/Sounds/ComboClick.mp3");
                            _sound.Play();
                        }
                        else if (IsNear(_wires.GetNode<ColorRect>("WireBottom2").Position + _wires.Position, mousePos, 15f) && _wires.GetNode<ColorRect>("WireBottom2").Color == _wires.GetNode<Line2D>("WireTop2/Line").DefaultColor)
                        {
                            _wires.GetNode<Line2D>("WireTop2/Line").SetPointPosition(1, _wires.GetNode<ColorRect>("WireBottom2").Position - new Vector2(58, 0));
                            buttons._direction = 0;
                            _wireTwoDone = true;
                            _sound.Stream = GD.Load<AudioStream>("res://Assets/Sounds/ComboClick.mp3");
                            _sound.Play();
                        }
                        else if (IsNear(_wires.GetNode<ColorRect>("WireBottom3").Position + _wires.Position, mousePos, 15f) && _wires.GetNode<ColorRect>("WireBottom3").Color == _wires.GetNode<Line2D>("WireTop2/Line").DefaultColor)
                        {
                            _wires.GetNode<Line2D>("WireTop2/Line").SetPointPosition(1, _wires.GetNode<ColorRect>("WireBottom3").Position - new Vector2(58, 0));
                            buttons._direction = 0;
                            _wireTwoDone = true;
                            _sound.Stream = GD.Load<AudioStream>("res://Assets/Sounds/ComboClick.mp3");
                            _sound.Play();
                        }
                        else
                        {
                            _wires.GetNode<Line2D>("WireTop2/Line").SetPointPosition(1, _wires.GetNode<Line2D>("WireTop2/Line").GetPointPosition(0));
                            buttons._direction = 0;
                            _sound.Stream = GD.Load<AudioStream>("res://Assets/Sounds/KeyWrong.mp3");
                            _sound.Play();
                        }
                    }

                    else if (buttons._direction == 23 && _wireThreeDone == false) //Wire Three Down
                    {
                        Vector2 mousePos = GetViewport().GetMousePosition();
                        _wires.GetNode<Line2D>("WireTop3/Line").SetPointPosition(1, mousePos - _wires.Position - new Vector2(109, 0));
                        _wires.GetNode<Line2D>("WireTop3/Line").DefaultColor = _wires.GetNode<ColorRect>("WireTop3").Color;
                    }
                    else if (buttons._direction == 33 && _wireThreeDone == false) //Wire Three Up
                    {
                        Vector2 mousePos = GetViewport().GetMousePosition();
                        if (IsNear(_wires.GetNode<ColorRect>("WireBottom1").Position + _wires.Position, mousePos, 15f) && _wires.GetNode<ColorRect>("WireBottom1").Color == _wires.GetNode<Line2D>("WireTop3/Line").DefaultColor)
                        {
                            _wires.GetNode<Line2D>("WireTop3/Line").SetPointPosition(1, _wires.GetNode<ColorRect>("WireBottom1").Position - new Vector2(102, 0));
                            buttons._direction = 0;
                            _wireThreeDone = true;
                            _sound.Stream = GD.Load<AudioStream>("res://Assets/Sounds/ComboClick.mp3");
                            _sound.Play();
                        }
                        else if (IsNear(_wires.GetNode<ColorRect>("WireBottom2").Position + _wires.Position, mousePos, 15f) && _wires.GetNode<ColorRect>("WireBottom2").Color == _wires.GetNode<Line2D>("WireTop3/Line").DefaultColor)
                        {
                            _wires.GetNode<Line2D>("WireTop3/Line").SetPointPosition(1, _wires.GetNode<ColorRect>("WireBottom2").Position - new Vector2(102, 0));
                            buttons._direction = 0;
                            _wireThreeDone = true;
                            _sound.Stream = GD.Load<AudioStream>("res://Assets/Sounds/ComboClick.mp3");
                            _sound.Play();
                        }
                        else if (IsNear(_wires.GetNode<ColorRect>("WireBottom3").Position + _wires.Position, mousePos, 15f) && _wires.GetNode<ColorRect>("WireBottom3").Color == _wires.GetNode<Line2D>("WireTop3/Line").DefaultColor)
                        {
                            _wires.GetNode<Line2D>("WireTop3/Line").SetPointPosition(1, _wires.GetNode<ColorRect>("WireBottom3").Position - new Vector2(102, 0));
                            buttons._direction = 0;
                            _wireThreeDone = true;
                            _sound.Stream = GD.Load<AudioStream>("res://Assets/Sounds/ComboClick.mp3");
                            _sound.Play();
                        }
                        else
                        {
                            _wires.GetNode<Line2D>("WireTop3/Line").SetPointPosition(1, _wires.GetNode<Line2D>("WireTop3/Line").GetPointPosition(0));
                            buttons._direction = 0;
                            _sound.Stream = GD.Load<AudioStream>("res://Assets/Sounds/KeyWrong.mp3");
                            _sound.Play();
                        }
                    }
                    else if (buttons._direction == 999) //Safe Complete
                    {
                        if (_lastSeen.Name == "box1" || _lastSeen.Name == "box2" || _lastSeen.Name == "box3" || _lastSeen.Name == "box4")
                        {
                            tweenTimer += 1;
                            _holdPosition.GlobalRotation += new Vector3(Mathf.DegToRad(5f), 0f, 0f);
                            if (tweenTimer >= 18)
                            {
                                buttons._direction = 0;
                                tweenTimer = 0;
                                TutorialBoxOpened(_lastSeen);
                            }
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
                            ComboLockHandler();

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
                            ComboLockHandler();

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

            if (IsNear3(_cam.GlobalRotation, new Vector3(0f,0f,0f), 0.05f) == false && IsNear3(_lastSeen.GlobalPosition, _holdPosition.GlobalPosition, 0.05f) == false)
            {
                LockStart();
            }

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

    public async void LockStart()
    {
        if (_hovering != null)
        {
            if (_hovering.Name == "ComboLock")
            {
                DisableAll();
                _comboKeyD.Visible = true;
                _comboKeyA.Visible = true;
                _comboCodeText.Visible = true;
            }
            else if (_hovering.Name == "KeyLock")
            {
                DisableAll();
                _lockButton.Visible = true;
                if (_hasLockPick == true)
                {
                    _lockButton.Disabled = false;
                }
                else
                {
                    _lockButton.Disabled = true;
                }
            }
            else if (_hovering.Name == "FingerLock")
            {
                DisableAll();
                _screws.Visible = true;
            }
            else
            {
                DisableAll();
            }
        }
    }

    public void DisableAll()
    {
        _comboKeyD.Visible = false;
        _comboKeyA.Visible = false;
        _comboCodeText.Visible = false;
        _lockButton.Visible = false;
        _screws.Visible = false;
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

    public async void TutorialBoxOpened(CharacterBody3D box)
    {
        _holdingItem = false;
        _moneyTimer.Visible = false;
        _moneyEarned.Text = _moneyTimer.Text;
        _leftButton.Visible = false;
        _rightButton.Visible = false;
        _upButton.Visible = false;
        _downButton.Visible = false;
        _crosshair.Visible = false;
        _comboCodeInput = null;
        _comboCode = _rng.RandiRange(1000, 9999);
        _comboDigitCount = 0;
        _comboCodeText.Text = "";

        TweenBoxRotation(box.GetNode<MeshInstance3D>("Box/top1"), -145f);
        TweenBoxRotation(box.GetNode<MeshInstance3D>("Box/Cube_002"), 145f);
        await ToSignal(GetTree().CreateTimer(0.5f), "timeout");
        box.GetNode<GpuParticles3D>("Opened").Emitting = true;
        await ToSignal(GetTree().CreateTimer(0.2f), "timeout");
        _moneyEarned.Visible = true;
        await ToSignal(GetTree().CreateTimer(1.5f), "timeout");
        _moneyEarned.Visible = false;
        if (_lastSeen is Safe safe) { _money += (float)Math.Ceiling(safe._money); }
        _playerMoney.Text = "$" + _money.ToString();
        _lastSeen = null;
        box.QueueFree();
        _crosshair.Visible = true;
        _holdPosition.GlobalRotation = Vector3.Zero;
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    public async void TweenBoxRotation(MeshInstance3D toTween, float rotateValue)
    {
        await ToSignal(GetTree().CreateTimer(0.5f), "timeout");
        Tween tween = CreateTween();
        tween.SetTrans(Tween.TransitionType.Elastic);
        tween.SetEase(Tween.EaseType.Out);
        tween.TweenProperty(toTween, "rotation", toTween.Rotation + new Vector3(toTween.Rotation.X + Mathf.DegToRad(rotateValue), toTween.Rotation.Y, toTween.Rotation.Z), 1f);
    }

    public async void TweenKeyLock(Node3D toTween, float rotateValue)
    {
        Tween tween = CreateTween();
        tween.SetTrans(Tween.TransitionType.Elastic);
        tween.SetEase(Tween.EaseType.Out);
        tween.TweenProperty(toTween, "rotation", toTween.Rotation + new Vector3(toTween.Rotation.X + Mathf.DegToRad(rotateValue), toTween.Rotation.Y, toTween.Rotation.Z), 1f);
    }

    public void ComboLockHandler()
    {
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
                _sound.Stream = GD.Load<AudioStream>("res://Assets/Sounds/KeyWrong.mp3");
                _sound.Play();
            }
            else if (_comboCodeInput == _comboCode)
            {
                _opened.Emitting = true;
                if (_lastSeen.Name == "tutBox")
                {
                    _hasLockPick = true;
                }
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

    public async void FingerLockHandler()
    {
        if (_screws.GetNode<Button>("Unscrew1").Disabled == true
        && _screws.GetNode<Button>("Unscrew2").Disabled == true
        && _screws.GetNode<Button>("Unscrew3").Disabled == true
        && _screws.GetNode<Button>("Unscrew4").Disabled == true)
        {
            _fingerLockOpen = true;
            _hasScrewdriver = false;
            _screwdriver.Visible = false;
            TweenFingerLock(_hovering.GetNode<Node3D>("Mesh/Rotate"), -165f);
            await ToSignal(GetTree().CreateTimer(0.2f), "timeout");
            _wires.Visible = true;
            int randomColor = _rng.RandiRange(1, 3);
            if (randomColor == 1)
            {
                _wires.GetNode<ColorRect>("WireTop1").Color = new Color(1.0f, 0.0f, 0.0f, 1.0f);
                _wires.GetNode<ColorRect>("WireTop2").Color = new Color(0.0f, 1.0f, 0.0f, 1.0f);
                _wires.GetNode<ColorRect>("WireTop3").Color = new Color(0.0f, 0.0f, 1.0f, 1.0f);

                _wires.GetNode<ColorRect>("WireBottom2").Color = new Color(1.0f, 0.0f, 0.0f, 1.0f);
                _wires.GetNode<ColorRect>("WireBottom1").Color = new Color(0.0f, 1.0f, 0.0f, 1.0f);
                _wires.GetNode<ColorRect>("WireBottom3").Color = new Color(0.0f, 0.0f, 1.0f, 1.0f);
            }
            else if (randomColor == 2)
            {
                _wires.GetNode<ColorRect>("WireTop2").Color = new Color(1.0f, 0.0f, 0.0f, 1.0f);
                _wires.GetNode<ColorRect>("WireTop3").Color = new Color(0.0f, 1.0f, 0.0f, 1.0f);
                _wires.GetNode<ColorRect>("WireTop1").Color = new Color(0.0f, 0.0f, 1.0f, 1.0f);

                _wires.GetNode<ColorRect>("WireBottom3").Color = new Color(1.0f, 0.0f, 0.0f, 1.0f);
                _wires.GetNode<ColorRect>("WireBottom2").Color = new Color(0.0f, 1.0f, 0.0f, 1.0f);
                _wires.GetNode<ColorRect>("WireBottom1").Color = new Color(0.0f, 0.0f, 1.0f, 1.0f);
            }
            else
            {
                _wires.GetNode<ColorRect>("WireTop3").Color = new Color(1.0f, 0.0f, 0.0f, 1.0f);
                _wires.GetNode<ColorRect>("WireTop1").Color = new Color(0.0f, 1.0f, 0.0f, 1.0f);
                _wires.GetNode<ColorRect>("WireTop2").Color = new Color(0.0f, 0.0f, 1.0f, 1.0f);

                _wires.GetNode<ColorRect>("WireBottom1").Color = new Color(1.0f, 0.0f, 0.0f, 1.0f);
                _wires.GetNode<ColorRect>("WireBottom3").Color = new Color(0.0f, 1.0f, 0.0f, 1.0f);
                _wires.GetNode<ColorRect>("WireBottom2").Color = new Color(0.0f, 0.0f, 1.0f, 1.0f);
            }

        }
    }

    public async void EndFingerLock()
    {

        TweenFingerLockClosed(_hovering.GetNode<Node3D>("Mesh/Rotate"));
        await ToSignal(GetTree().CreateTimer(0.2f), "timeout");
        _wires.Visible = false;
        await ToSignal(GetTree().CreateTimer(1f), "timeout");
        _sound.Stream = GD.Load<AudioStream>("res://Assets/Sounds/FingerDone.mp3");
        _sound.Play();
        await ToSignal(GetTree().CreateTimer(2f), "timeout");
        CharacterBody3D toDestory = _hovering;
        _hovering = null;
        _opened.Emitting = true;
        toDestory.QueueFree();
        GetMouseCollision();
        _leftButton.Visible = true;
        _rightButton.Visible = true;
        _upButton.Visible = true;
        _downButton.Visible = true;
        _fingerLockOpen = false;
        _wireOneDone = false;
        _wireTwoDone = false;
        _wireThreeDone = false;
        _screws.GetNode<Button>("Unscrew1").Disabled = false; _screws.GetNode<Button>("Unscrew1").Rotation = 0;
        _screws.GetNode<Button>("Unscrew2").Disabled = false; _screws.GetNode<Button>("Unscrew2").Rotation = 0;
        _screws.GetNode<Button>("Unscrew3").Disabled = false; _screws.GetNode<Button>("Unscrew3").Rotation = 0;
        _screws.GetNode<Button>("Unscrew4").Disabled = false; _screws.GetNode<Button>("Unscrew4").Rotation = 0;
    }

    public void TweenFingerLock(Node3D toTween, float rotateValue)
    {
        GD.Print(_hovering.GetNode<Node3D>("Mesh/Rotate").Name);
        Tween tween = CreateTween();
        tween.SetTrans(Tween.TransitionType.Elastic);
        tween.SetEase(Tween.EaseType.Out);
        tween.TweenProperty(toTween, "rotation", toTween.Rotation + new Vector3(toTween.Rotation.X, toTween.Rotation.Y, toTween.Rotation.Z + Mathf.DegToRad(rotateValue)), 2f);
    }

    public void TweenFingerLockClosed(Node3D toTween)
    {
        GD.Print(_hovering.GetNode<Node3D>("Mesh/Rotate").Name);
        Tween tween = CreateTween();
        tween.SetTrans(Tween.TransitionType.Elastic);
        tween.SetEase(Tween.EaseType.Out);
        tween.TweenProperty(toTween, "rotation", new Vector3(toTween.Rotation.X, toTween.Rotation.Y, 0f), 1f);
    }
    public bool IsNear(Vector2 vector1, Vector2 vector2, float threshold)
    {
        GD.Print(vector1.DistanceTo(vector2));
        return vector1.DistanceTo(vector2) < threshold;
    }
    
    public bool IsNear3(Vector3 vector1, Vector3 vector2, float threshold)
    {
        GD.Print(vector1.DistanceTo(vector2));
        return vector1.DistanceTo(vector2) < threshold;
    }
}
