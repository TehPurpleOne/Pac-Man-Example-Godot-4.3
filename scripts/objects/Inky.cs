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
	}
}
