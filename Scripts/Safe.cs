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

    public override void _Ready()
    {
        _prompt = GetNode<Label3D>("Prompt");
        _glow = GetNode<MeshInstance3D>("Glow");
        _player = GetNode<Node3D>("../Player");
    }
    public override void _PhysicsProcess(double delta)
    {
        if (_glow.Visible == true && _prompt.Visible == false && looking == false)
        {
            looking = true;
            //LookAt(_player.GetNode<Camera3D>("Head/Camera3D").GlobalPosition);
            _direction = GlobalRotation;
        }
    }
}
