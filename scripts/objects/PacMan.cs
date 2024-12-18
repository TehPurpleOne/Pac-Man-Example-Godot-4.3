using Godot;
using System;

public partial class PacMan : Node2D
{
	private AnimatedSprite2D sprite;
	private Game g;

	private float speed = 80;
	private float speedMod = 1;
	private Vector2I direction = Vector2I.Left;
	private Vector2I oldDirection = Vector2I.Left;
	public Vector2I desiredDir = Vector2I.Zero;
	public Vector2I gridPos = Vector2I.Zero;
	public int moveDelay = 0;

	public enum states {NULL, INIT, ACTIVE, DEAD}
	public states currentState = states.NULL;
	public states previousState = states.NULL;


    public override void _Ready() {
		g = (Game)GetParent();
        sprite = (AnimatedSprite2D)GetNode("Sprite");

		SetState(states.INIT);
    }

    public override void _PhysicsProcess(double delta) {
		if(currentState != states.NULL) {
			StateLogic(delta);
			states t = GetTransition(delta);
			if(t != states.NULL) {
				SetState(t);
			}
		}
    }

	private void StateLogic(double delta) {
		switch(currentState) {
			case states.ACTIVE:
				gridPos = GetGridPosition(Position);

				if(gridPos.X <= 5 && gridPos.Y == 17 && speedMod != 0.75f || gridPos.X >= 22 && gridPos.Y == 17 && speedMod != 0.75f) speedMod = 0.75f; else speedMod = 1;

				// Handle Input
				if(Input.IsActionPressed("ui_up")) {
					desiredDir = Vector2I.Up;
				} else if(Input.IsActionPressed("ui_down")) {
					desiredDir = Vector2I.Down;
				} else if(Input.IsActionPressed("ui_left")) {
					desiredDir = Vector2I.Left;
				} else if(Input.IsActionPressed("ui_right")) {
					desiredDir = Vector2I.Right;
				} else desiredDir = direction;

				if(g.IsDirectionValid(gridPos, desiredDir) && TileCenter()) {
					direction = desiredDir;

					if(direction != oldDirection) {
						PlayAnim(direction);
						oldDirection = direction;
					}
				}

				if(g.DetectCollision(gridPos, direction)) {
					AlignToGrid(gridPos, direction);
				}

				if(moveDelay == 0) Position += (Vector2)direction * (speed * speedMod) * (float)delta;

				if(direction.X == -1 && Position.X < -8) Position = new Vector2(232, Position.Y);
				if(direction.X == 1 && Position.X > 232) Position = new Vector2(-8, Position.Y);

				if(moveDelay > 0) moveDelay--;
				break;
		}
	}

	private states GetTransition(double delta) {


		return states.NULL;
	}

	private void EnterState(states newState, states oldState) {
		switch(newState) {
			case states.ACTIVE:
				PlayAnim(direction);
				break;
		}
	}

	private void ExitState(states oldState, states newState) {

	}

	public void SetState(states newState) {
		previousState = currentState;
		currentState = newState;

		ExitState(previousState, newState);
		EnterState(currentState, previousState);
	}

	private Vector2I GetGridPosition(Vector2 pos) {
		return new Vector2I((int)Mathf.Floor(pos.X / 8), (int)Mathf.Floor(pos.Y / 8));
	}

	private bool TileCenter() {
		Vector2I currentTile = GetGridPosition(Position);
		Vector2 tileCenter = new Vector2(currentTile.X * 8 + 4, currentTile.Y * 8 + 4);
		float margin = 1.5f;

		return Mathf.Abs(Position.X - tileCenter.X) < margin && Mathf.Abs(Position.Y - tileCenter.Y) < margin;
	}

	private void AlignToGrid(Vector2I pos, Vector2I dir) {
		if(dir.Y == 0) Position = new Vector2(pos.X * 8 + 4, Position.Y);
		if(dir.X == 0) Position = new Vector2(Position.X, pos.Y * 8 + 4);

		direction = Vector2I.Zero;
		sprite.Pause();
	}

	private void PlayAnim(Vector2I dir) {
		switch(dir) {
			case Vector2I v when dir == Vector2I.Up:
				sprite.Play("UP");
				break;
			
			case Vector2I v when dir == Vector2I.Down:
				sprite.Play("DOWN");
				break;
			
			case Vector2I v when dir == Vector2I.Left:
				sprite.Play("LEFT");
				break;
			
			case Vector2I v when dir == Vector2I.Right:
				sprite.Play("RIGHT");
				break;
		}
	}
}
