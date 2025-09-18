using Godot;
using System;

public partial class Ui : Control
{

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
        _direction =  6;
    }
    public void _on_combo_key_d_button_up()
    {
        _direction = 0;
    }
    public void _on_combo_key_a_button_up()
    {
        _direction =  0;
    }

}
