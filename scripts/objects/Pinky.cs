using Godot;
using System;

public partial class Pinky : Ghost
{
	public override void _Ready() {
		m = (Master)GetNode("/root/Master");
		g = (Game)GetParent();
        sprite = (AnimatedSprite2D)GetNode("Sprite");

		basePalette = 1;
		direction = Vector2I.Down;
		desiredDir = Vector2I.Left;

		SetExitDots();
		SetState(states.HOME);
		SetProcess(false);
		SetPhysicsProcess(false);
	}
}
