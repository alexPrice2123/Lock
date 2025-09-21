using Godot;
using System;

public partial class Ui : Control
{

    private ColorRect _transition;
    private float _transitionGoal = -0f;
    private float _transitionLerpStrength = -0.01f;
    private ShaderMaterial _transitionMat;

    public override void _Ready()
    {
        _transition = GetNode<ColorRect>("Transition");
        _transitionMat = _transition.Material as ShaderMaterial;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_transitionMat != null)
        {
            if (IsNumberInRange((float)_transitionMat.GetShaderParameter("progress"), _transitionGoal - 0.05f, _transitionGoal + 0.05f)) { _transitionMat.SetShaderParameter("progress", _transitionGoal); _transition.Visible = false; return; }
            _transitionMat.SetShaderParameter("progress", (float)_transitionMat.GetShaderParameter("progress") + _transitionLerpStrength);
        }

    }

    public bool IsNumberInRange(float number, float min, float max)
    {
        if (number >= min && number <= max)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public int _direction = 0;
    public CharacterBody3D _player;

    public void _on_turn_left_button_up()
    {
        if (_player is Player player) { if (player.tweenTimer > 0) { return; } }
        _direction = 1;
    }
    public void _on_turn_up_button_up()
    {
        if (_player is Player player) { if (player.tweenTimer > 0) { return; } }
        _direction = 2;
    }
    public void _on_turn_down_button_up()
    {
        if (_player is Player player) { if (player.tweenTimer > 0) { return; } }
        _direction = 3;
    }
    public void _on_turn_right_button_up()
    {
        if (_player is Player player) { if (player.tweenTimer > 0) { return; } }
        _direction = 4;
    }
    public void _on_combo_key_d_button_down()
    {
        if (_player is Player player) { if (player.tweenTimer > 0) { return; } }
        _direction = 5;
    }
    public void _on_combo_key_a_button_down()
    {
        if (_player is Player player) { if (player.tweenTimer > 0) { return; } }
        _direction = 6;
    }
    public void _on_combo_key_d_button_up()
    {
        _direction = 0;
    }
    public void _on_combo_key_a_button_up()
    {
        _direction = 0;
    }
    public void _on_lock_pick_button_up()
    {
        if (_player is Player player) { if (player.tweenTimer > 0) { return; } }
        _direction =  7;
    }
    public void _on_click_button_down()
    {
        if (_player is Player player) { if (player.tweenTimer > 0) { return; } }
        _direction =  8;
    }
} 
