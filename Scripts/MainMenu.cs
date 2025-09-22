using Godot;
using System;

public partial class MainMenu : Control
{
    private ColorRect _transition;
    private float _transitionGoal = 0f;
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
            if ((float)_transitionMat.GetShaderParameter("progress") >= 1f)
            {
                GetTree().ChangeSceneToFile("res://Scenes/world.tscn");
            }
            if (IsNumberInRange((float)_transitionMat.GetShaderParameter("progress"), _transitionGoal - 0.05f, _transitionGoal + 0.05f)) { _transitionMat.SetShaderParameter("progress", _transitionGoal); return; }
            _transitionMat.SetShaderParameter("progress", (float)_transitionMat.GetShaderParameter("progress") + _transitionLerpStrength);
        }

    }

    public void _on_play_button_up()
    {
        if (!IsNumberInRange((float)_transitionMat.GetShaderParameter("progress"), _transitionGoal - 0.05f, _transitionGoal + 0.05f)) { return; }
        _transitionGoal = 1f;
        _transitionLerpStrength = 0.01f;
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
}
