using Godot;
using System;

public partial class PacMan : Node2D
{
	private AnimatedSprite2D sprite;

	private float speed = 80;
	private float speedMod = 1;
	private Vector2 direction = Vector2.Zero;
	private Vector2 desiredDir = Vector2.Zero;
	public Vector2I gridPos = Vector2I.Zero;


    public override void _Ready() {
        sprite = (AnimatedSprite2D)GetNode("Sprite");
    }

    public override void _PhysicsProcess(double delta) {
		gridPos = GetGridPosition(Position);
		
		// Handle Input
		if(Input.IsActionPressed("ui_up")) {
			direction = Vector2.Up;
		}
		if(Input.IsActionPressed("ui_down")) {
			direction = Vector2.Down;
		}
		if(Input.IsActionPressed("ui_left")) {
			direction = Vector2.Left;
		}
		if(Input.IsActionPressed("ui_right")) {
			direction = Vector2.Right;
		}

		// Check for valid movements.

		Position += direction * (speed * speedMod) * (float)delta;
    }

	private Vector2I GetGridPosition(Vector2 pos) {
		return new Vector2I((int)Mathf.Floor(pos.X / 8), (int)Mathf.Floor(pos.Y / 8));
	}
}
