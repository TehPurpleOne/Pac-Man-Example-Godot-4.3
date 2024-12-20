using Godot;
using System;

public partial class Clyde : Ghost
{
	public override void _Ready() {
		g = (Game)GetParent();
        sprite = (AnimatedSprite2D)GetNode("Sprite");

		//SetState(states.INIT);
	}
}
