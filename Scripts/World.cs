using Godot;
using System;

public partial class World : Node3D
{
    private PackedScene _tutBox = GD.Load<PackedScene>("res://Scenes/tutBox.tscn");
    private Node3D _spawn;
    private Node3D _safeHolder;
    public override void _Ready()
    {
        _spawn = GetNode<Node3D>("BoxSpawn");
        _safeHolder = GetNode<Node3D>("SafeHolder");
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_safeHolder.GetChildren().Count == 0)
        {
            CharacterBody3D safeInstance = _tutBox.Instantiate<CharacterBody3D>(); // Create monster instance
            _safeHolder.AddChild(safeInstance);                                             // Add monster to holder node
            safeInstance.GlobalPosition = _spawn.GlobalPosition + new Vector3(0f, 5f, 0f);
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
        tween.TweenProperty(toTween, "position", _spawn.GlobalPosition, 1f);
    }
}
