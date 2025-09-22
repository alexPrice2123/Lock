using Godot;
using System;

public partial class World : Node3D
{
    private Node3D _spawn;
    private Node3D _safeHolder;
    private int _level = 0;
    public override void _Ready()
    {
        _spawn = GetNode<Node3D>("BoxSpawn");
        _safeHolder = GetNode<Node3D>("SafeHolder");
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_safeHolder.GetChildren().Count == 0)
        {
            _level += 1;
            if (_level > 4)
            {
                if (GetNode<CharacterBody3D>("Player") is Player player) { player._finished = true; }
                return;
            }
            PackedScene _box = GD.Load<PackedScene>("res://Scenes/box"+_level.ToString()+".tscn");
            CharacterBody3D safeInstance = _box.Instantiate<CharacterBody3D>(); // Create monster instance
            _safeHolder.AddChild(safeInstance);                                             // Add monster to holder node
            safeInstance.GlobalPosition = _spawn.GlobalPosition + new Vector3(0f, 5.5f, 0f);
            safeInstance.GlobalRotation = _spawn.GlobalRotation;
            StartBounce(safeInstance);
        }
    }

    public async void StartBounce(CharacterBody3D toTween)
    {
        await ToSignal(GetTree().CreateTimer(2f), "timeout");
        Tween tween = CreateTween();
        tween.SetTrans(Tween.TransitionType.Bounce);
        tween.SetEase(Tween.EaseType.Out);
        tween.TweenProperty(toTween, "position", _spawn.GlobalPosition, 1.5f);
        await ToSignal(GetTree().CreateTimer(0.5f), "timeout");
        toTween.GetNode<CollisionShape3D>("Collision").Disabled = false;
    }
}
