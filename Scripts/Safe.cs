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

    public override void _Ready()
    {
        _prompt = GetNode<Label3D>("Prompt");
        _glow = GetNode<MeshInstance3D>("Glow");
        if (Name == "tutBox")
        {
            _money = 100f;
            _moneyLossPF = 0.005f;
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
