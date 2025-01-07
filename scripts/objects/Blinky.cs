using Godot;
using System;

public partial class Blinky : Ghost
{
	public override void _Ready() {
		m = (Master)GetNode("/root/Master");
		g = (Game)GetParent();
        sprite = (AnimatedSprite2D)GetNode("Sprite");

		basePalette = 0;
		direction = Vector2I.Left;
		desiredDir = Vector2I.Left;

		SetExitDots();
		SetState(states.SEEK);
		SetProcess(false);
		SetPhysicsProcess(false);
	}
}
