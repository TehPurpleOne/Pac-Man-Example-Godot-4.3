using Godot;
using System;
using System.Collections.Generic;
using System.Data.Common;

public partial class Ghost : Node2D
{
	public AnimatedSprite2D sprite;
	public Game g;

	[Export] public float speed = 75.75757625f;
	public float speedMod = 1;
	public Vector2I direction = Vector2I.Left;
	public Vector2I oldDirection = Vector2I.Left;
	public Vector2I desiredDir = Vector2I.Zero;
	public Vector2I gridPos = Vector2I.Zero;
	public Vector2I oldPos = Vector2I.Zero;
	public Vector2I targetPos = Vector2I.Zero;
	public int moveDelay = 0;
	public int basePalette = 0;
	public bool forceReverse = false;

	public enum states {NULL, INIT, HOME, EXIT, SEEK, SCARED, EATEN, ENTER, CHOOSEDIR}
	public states currentState = states.NULL;
	public states previousState = states.NULL;

	public void SetPalette(int value) {
		if(value == -1) value = basePalette;
		ShaderMaterial sm = (ShaderMaterial)this.Material;
		sm.SetShaderParameter("palette_index", value);
	}

	public Vector2I GetGridPosition(Vector2 pos) {
		return new Vector2I((int)Mathf.Floor(pos.X / 8), (int)Mathf.Floor(pos.Y / 8));
	}

	public bool TileCenter() {
		Vector2I currentTile = GetGridPosition(Position);
		Vector2 tileCenter = new Vector2(currentTile.X * 8 + 4, currentTile.Y * 8 + 4);
		float margin = 0.5f;

		return Mathf.Abs(Position.X - tileCenter.X) < margin && Mathf.Abs(Position.Y - tileCenter.Y) < margin;
	}

	public void AlignToGrid(Vector2I pos) {
		Position = new Vector2(gridPos.X * 8 + 4, gridPos.Y * 8 + 4);
	}

	public void Wrap() {
		if(direction.X == -1 && Position.X < -8) Position = new Vector2(232, Position.Y);
		if(direction.X == 1 && Position.X > 232) Position = new Vector2(-8, Position.Y);
	}

	public Vector2I ChooseShortestDir() {
		Vector2I newDir = Vector2I.Zero;

		// Choose direction that's the shortest distance between ghost and target tile.
		AlignToGrid(gridPos);

		// Add possible directions to a list.
		List<Vector2I> dirs = new List<Vector2I> ();
		if(g.IsDirectionValid(gridPos, Vector2I.Up)) dirs.Add(Vector2I.Up);
		if(g.IsDirectionValid(gridPos, Vector2I.Down)) dirs.Add(Vector2I.Down);
		if(g.IsDirectionValid(gridPos, Vector2I.Left)) dirs.Add(Vector2I.Left);
		if(g.IsDirectionValid(gridPos, Vector2I.Right)) dirs.Add(Vector2I.Right);

		// Ghosts cannot reverse themselves unless under specific conditions. Delete reversed directions.
		Vector2I reverse = -direction;
		if(dirs.Contains(reverse)) dirs.Remove(reverse);

		// Prevent Ghosts from going up at certain coordinates.
		if(dirs.Contains(Vector2I.Up)) {
			GD.Print(Name," is trying to go up. Coordinates are: ",gridPos + Vector2.Up);
			if(gridPos + Vector2I.Up == new Vector2I(12, 13)
			|| gridPos + Vector2I.Up == new Vector2I(15, 13)
			|| gridPos + Vector2I.Up == new Vector2I(12, 25)
			|| gridPos + Vector2I.Up == new Vector2I(15, 25)) {
				dirs.Remove(Vector2I.Up);
			}
		}

		float shortestDist = (gridPos + dirs[0]).DistanceTo(targetPos);
		int entry = 0;

		for(int i = 0; i < dirs.Count; i++) {
			float dist = (gridPos + dirs[i]).DistanceTo(targetPos);

			if(dist < shortestDist) {
				entry = i;
				shortestDist = dist;
			}
		}

		newDir = dirs[entry];
		oldPos = gridPos;

		return newDir;
	}

	public Vector2I ChooseRandomDir() {
		Vector2I newDir = Vector2I.Zero;
		Random RNGesus = new Random();

		GD.Print("Choosing random direction...");

		 // Choose a direction randomly from available directions.
		AlignToGrid(gridPos);

		// Add possible directions to a list.
		List<Vector2I> dirs = new List<Vector2I> ();
		if(g.IsDirectionValid(gridPos, Vector2I.Up)) dirs.Add(Vector2I.Up);
		if(g.IsDirectionValid(gridPos, Vector2I.Down)) dirs.Add(Vector2I.Down);
		if(g.IsDirectionValid(gridPos, Vector2I.Left)) dirs.Add(Vector2I.Left);
		if(g.IsDirectionValid(gridPos, Vector2I.Right)) dirs.Add(Vector2I.Right);

		// Ghosts cannot reverse themselves unless under specific conditions. Delete reversed directions.
		Vector2I reverse = -direction;
		if(dirs.Contains(reverse)) dirs.Remove(reverse);

		newDir = dirs[RNGesus.Next(0, dirs.Count - 1)];

		GD.Print("Direction chosen is ",newDir);

		return newDir;
	}

	public void PlayAnim(Vector2I dir) {
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

		if(g.scaredMode) sprite.Play("SCARED");
	}
}
