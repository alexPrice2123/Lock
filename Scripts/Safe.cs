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
		if (notBox1 == notBox2)
		{
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
		else if (Name == "box4")
		{
			_money = 100f * 8f;
			_moneyLossPF = 0.05f * (1.5f * 3f);
		}
		else if (Name == "box5")
		{
			_money = 100f * 16f;
			_moneyLossPF = 0.05f * (1.5f * 4f);
		}
		else if (Name == "box6")
		{
			_money = 100f * 32f;
			_moneyLossPF = 0.05f * (1.5f * 7f);
		}
		else if (Name == "box7")
		{
			GetNode<MeshInstance3D>("Box/Hat").Visible = true;
			_money = 100f * 64f;
			_moneyLossPF = 0.05f * (1.5f * 10f);
		}
		else if (Name == "box8")
		{
			_money = 100f * 128f;
			_moneyLossPF = 0.05f * (1.5f * 30f);
		}
		else if (Name == "box9")
		{
			_money = 100f * 258f;
			_moneyLossPF = 0.05f * (1.5f * 50f);
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
