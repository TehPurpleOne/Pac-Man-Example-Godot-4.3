using Godot;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Reflection.Metadata.Ecma335;
using System.Threading;

public partial class Ghost : Node2D
{
	public AnimatedSprite2D sprite;
	public Master m;
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
			if(gridPos + Vector2I.Up == new Vector2I(12, 13)
			|| gridPos + Vector2I.Up == new Vector2I(15, 13)
			|| gridPos + Vector2I.Up == new Vector2I(12, 25)
			|| gridPos + Vector2I.Up == new Vector2I(15, 25)) {
				dirs.Remove(Vector2I.Up);
			}
		}

		// Fix this bug before release

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

		return newDir;
	}

	public void PlayAnim(Vector2I dir) {
		if(!g.scaredMode) {
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

		if(g.scaredMode && sprite.Animation != "SCARED") sprite.Play("SCARED");
	}

	public float SpeedModifier() {
		float newMod = 1;
		int dotsRemaining = g.dotsEaten - 244;

		// Set the base speed for the ghost based on the level.
		switch(m.level) {
			case 1:
				newMod = 0.75f;
				break;
			
			case int v when m.level >= 2 && m.level <= 4:
				newMod = 0.85f;
				break;
			
			case int v when m.level > 4:
				newMod = 0.95f;
				break;
		}

		// For Inky only. Set modifier for Cruise Elroy speeds.
		if(this.Name == "Inky") {
			switch(m.level) {
				case 1:
					if(dotsRemaining <= 20 && dotsRemaining > 10) newMod = 0.8f; else if(dotsRemaining <= 10) newMod = 0.85f;
					break;
				
				case 2:
					if(dotsRemaining <= 30 && dotsRemaining > 15) newMod = 0.9f; else if(dotsRemaining <= 15) newMod = 0.95f;
					break;
				
				case 3:
				case 4:
					if(dotsRemaining <= 40 && dotsRemaining > 20) newMod = 0.9f; else if(dotsRemaining <= 20) newMod = 0.95f;
					break;
				
				case 5:
					if(dotsRemaining <= 40 && dotsRemaining > 20) newMod = 1f; else if(dotsRemaining <= 20) newMod = 1.05f;
					break;
				
				case 6:
				case 7:
				case 8:
					if(dotsRemaining <= 50 && dotsRemaining > 25) newMod = 1f; else if(dotsRemaining <= 25) newMod = 1.05f;
					break;
				
				case 9:
				case 10:
				case 11:
					if(dotsRemaining <= 60 && dotsRemaining > 30) newMod = 1f; else if(dotsRemaining <= 30) newMod = 1.05f;
					break;
				
				case 12:
				case 13:
				case 14:
					if(dotsRemaining <= 80 && dotsRemaining > 40) newMod = 1f; else if(dotsRemaining <= 40) newMod = 1.05f;
					break;
				
				case 15:
				case 16:
				case 17:
				case 18:
					if(dotsRemaining <= 100 && dotsRemaining > 50) newMod = 1f; else if(dotsRemaining <= 50) newMod = 1.05f;
					break;
				
				case int v when m.level > 18:
					if(dotsRemaining <= 120 && dotsRemaining > 60) newMod = 1f; else if(dotsRemaining <= 60) newMod = 1.05f;
					break;
			}
		}

		// Set frightened speeds.
		if(g.scaredMode) {
			switch(m.level) {
				case 1:
					newMod = 0.5f;
					break;
				
				case 2:
				case 3:
				case 4:
					newMod = 0.55f;
					break;
				
				case int v when m.level > 4:
					newMod = 0.6f;
					break;
			}
		}

		// Set tunnel speeds.
		bool inTunnel = gridPos.Y == 17 && gridPos.X <= 5 || gridPos.Y == 17 && gridPos.X >= 22;

		if(inTunnel) {
			switch(m.level) {
				case 1:
					newMod = 0.4f;
					break;
				
				case 2:
				case 3:
				case 4:
					newMod = 0.45f;
					break;
				
				case int v when m.level > 4:
					newMod = 0.5f;
					break;
			}
		}

		// If eaten.

		return newMod;
	}

	public void PositionCheck() {
		
	}
}
