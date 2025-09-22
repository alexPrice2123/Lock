using Godot;
using System;

public partial class Safe : CharacterBody3D
{
    // --- VARIABLES ---
    private MeshInstance3D _glow;
    private Label3D _prompt;
    private Node3D _player;
    private bool looking = false;
    public Vector3 _direction;
    public float _money;
    public float _moneyLossPF;
    private RandomNumberGenerator _rng = new RandomNumberGenerator();

    public override void _Ready()
    {
        _prompt = GetNode<Label3D>("Prompt");
        _glow = GetNode<MeshInstance3D>("Glow");
        _rng.Randomize();
        int notBox1 = _rng.RandiRange(1, 6);
        int notBox2 = _rng.RandiRange(1, 6);
        if (notBox1 == notBox2) {
            notBox1 += 1;
            if (notBox1 > 6)
            {
                notBox1 = 1;
            }
        }
        if (Name == "box1")
        {
            _money = 100f;
            _moneyLossPF = 0.05f;
            GetNode<Label3D>("Number" + notBox1).Visible = false;
            GetNode<Label3D>("Number" + notBox2).Visible = false;
        }
        else if (Name == "box2")
        {
            _money = 100f * 2f;
            _moneyLossPF = 0.05f * 1.5f;
        }
        else if (Name == "box3")
        {
            _money = 100f * 4f;
            _moneyLossPF = 0.05f * (1.5f * 2f);
        }
    }
    public override void _PhysicsProcess(double delta)
    {
        if (_glow.Visible == true && _prompt.Visible == false && looking == false)
        {
            looking = true;
            _direction = GlobalRotation;
        }
    }
}
