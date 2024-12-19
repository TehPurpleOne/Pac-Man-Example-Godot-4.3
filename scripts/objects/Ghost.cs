using Godot;
using System;

public partial class Ghost : Node2D
{
	public AnimatedSprite2D sprite;
	public Game g;

	[Export] private float speed = 75.75757625f;
	private float speedMod = 1;
	private Vector2I direction = Vector2I.Left;
	private Vector2I oldDirection = Vector2I.Left;
	public Vector2I desiredDir = Vector2I.Zero;
	public Vector2I gridPos = Vector2I.Zero;
	public Vector2I targetPos = Vector2I.Zero;
	public int moveDelay = 0;
	private int palette = 0;

	public enum states {NULL, INIT, HOME, EXIT, SEEK, SCARED, EATEN, ENTER}
	public states currentState = states.NULL;
	public states previousState = states.NULL;

	private Vector2I GetGridPosition(Vector2 pos) {
		return new Vector2I((int)Mathf.Floor(pos.X / 8), (int)Mathf.Floor(pos.Y / 8));
	}

	private bool TileCenter() {
		Vector2I currentTile = GetGridPosition(Position);
		Vector2 tileCenter = new Vector2(currentTile.X * 8 + 4, currentTile.Y * 8 + 4);
		float margin = 1.0f;

		return Mathf.Abs(Position.X - tileCenter.X) < margin && Mathf.Abs(Position.Y - tileCenter.Y) < margin;
	}

	private void AlignToGrid(Vector2I pos, Vector2I dir) {
		Position = new Vector2(gridPos.X * 8 + 4, gridPos.Y * 8 + 4);
	}

	private void PlayAnim(Vector2I dir) {
		/* switch(dir) {
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
		} */
	}
}
