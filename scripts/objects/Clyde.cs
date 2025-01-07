using Godot;
using System;

public partial class Clyde : Ghost
{
	public override void _Ready() {
		m = (Master)GetNode("/root/Master");
		g = (Game)GetParent();
        sprite = (AnimatedSprite2D)GetNode("Sprite");

		basePalette = 3;
		direction = Vector2I.Up;
		desiredDir = Vector2I.Left;

		SetExitDots();
		SetState(states.HOME);
		SetProcess(false);
		SetPhysicsProcess(false);
	}
}
