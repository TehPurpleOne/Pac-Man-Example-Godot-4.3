using Godot;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public partial class Inky : Ghost
{
	public override void _Ready() {
		m = (Master)GetNode("/root/Master");
		g = (Game)GetParent();
        sprite = (AnimatedSprite2D)GetNode("Sprite");

		basePalette = 2;
		direction = Vector2I.Up;
		desiredDir = Vector2I.Left;

		SetExitDots();
		SetState(states.HOME);
		SetProcess(false);
		SetPhysicsProcess(false);
	}
}
