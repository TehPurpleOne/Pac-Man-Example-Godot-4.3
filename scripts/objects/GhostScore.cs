using Godot;
using System;

public partial class GhostScore : Node2D
{
	public Sprite2D s;

    public override void _Ready() {
        s = (Sprite2D)GetNode("Sprite2D");
    }
}
