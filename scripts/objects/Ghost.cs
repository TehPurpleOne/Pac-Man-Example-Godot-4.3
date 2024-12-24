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
	public int currentPalette = 0;
	public bool forceReverse = false;

	public enum states {NULL, INIT, HOME, EXIT, SEEK, SCARED, EATEN, ENTER, CHOOSEDIR}
	public states currentState = states.NULL;
	public states previousState = states.NULL;

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
		targetPos = SetTargetTile();
		switch(currentState) {
			case states.SCARED:
				if(g.scaredTicks > 180) SetPalette(5);

				if(g.scaredTicks <= 120 && g.scaredTicks % 5 == 0) {
					int getCurrent = currentPalette;
					getCurrent++;
					if(getCurrent > 6) getCurrent = 5;
					SetPalette(getCurrent);
				}
				break;
		}

		speedMod = SpeedModifier();

		if(currentState != states.INIT) Position += (Vector2)direction * (speed * speedMod) * (float)delta;
		Wrap();
		gridPos = GetGridPosition(Position);
		PositionCheck();
	}

	private states GetTransition(double delta) {
		switch(currentState) {
			case states.INIT:
				if(g.currentState == Game.states.SCATTER) return states.SEEK;
				break;
			
			case states.SEEK:
			case states.SCARED:
			case states.EATEN:
				if(currentState == states.SCARED && g.scaredTicks == 0) return states.SEEK;
				if(TileCenter() && gridPos != oldPos) return states.CHOOSEDIR;
				break;
			
			case states.CHOOSEDIR:
				if(direction != Vector2I.Zero) return previousState;
				break;
		}

		return states.NULL;
	}

	private void EnterState(states newState, states oldState) {
		switch(newState) {
			case states.INIT:
				SetPalette(basePalette);
				break;
			
			case states.SEEK:
				SetPalette(-1);
				break;
				
			case states.SCARED:
				if(sprite.Animation != "SCARED") sprite.Play("SCARED");
				break;
			
			case states.EATEN:
				SetPalette(4);
				break;
			
			case states.CHOOSEDIR:
				AlignToGrid(gridPos);
				Vector2I saveDir = direction;
				saveDir = -saveDir;

				switch(oldState) {
					case states.SEEK:
						direction = ChooseShortestDir();
						break;
					
					case states.SCARED:
						direction = ChooseRandomDir();
						break;
					
					case states.EATEN:
						if(forceReverse) forceReverse = false;
						direction = ChooseShortestDir();
						break;
				}

				if(forceReverse) {
					direction = -saveDir;
					forceReverse = false;
				}
				
				PlayAnim(direction);
				break;
		}
	}

	private void ExitState(states oldState, states newState) {

	}

	public void SetState(states newState) {
		previousState = currentState;
		currentState = newState;

		//GD.Print(Name," has entered state ",currentState," from state ",previousState);

		ExitState(previousState, currentState);
		EnterState(currentState, previousState);
	}

	private Vector2I SetTargetTile() {
		Vector2I targetTile = Vector2I.Zero;

		switch(g.currentState) {
			case Game.states.SCATTER:
				switch(Name) {
					case string t when Name == "Blinky":
						targetTile = new Vector2I(25, 0);
						break;
					
					case string t when Name == "Inky":
						
						break;
					
					case string t when Name == "Pinky":
						
						break;
					
					case string t when Name == "Clyde":
						
						break;
				}
				break;
			
			case Game.states.CHASE:
				switch(Name) {
					case string t when Name == "Blinky":
						targetTile = g.p.gridPos;
						break; 
					
					case string t when Name == "Inky":
						
						break;
					
					case string t when Name == "Pinky":
						
						break;
					
					case string t when Name == "Clyde":
						
						break;
				}
				break;
		}

		if(currentState == states.EATEN) targetTile = new Vector2I(13, 14);

		return targetTile;
	}

	public void SetPalette(int value) {
		if(value == -1) value = basePalette;
		ShaderMaterial sm = (ShaderMaterial)Material;
		currentPalette = (int)sm.GetShaderParameter("palette_index");
		if(value != currentPalette) sm.SetShaderParameter("palette_index", value);
	}

	public Vector2I GetGridPosition(Vector2 pos) {
		return new Vector2I((int)Mathf.Floor(pos.X / 8), (int)Mathf.Floor(pos.Y / 8));
	}

	public bool TileCenter() {
		Vector2I currentTile = GetGridPosition(Position);
		Vector2 tileCenter = new Vector2(currentTile.X * 8 + 4, currentTile.Y * 8 + 4);
		float margin = 0.75f;

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
		Vector2I reverse = -direction;
		bool upAllowed = gridPos + Vector2I.Up != new Vector2I(12, 13)
					  || gridPos + Vector2I.Up != new Vector2I(15, 13)
					  || gridPos + Vector2I.Up != new Vector2I(12, 25)
					  || gridPos + Vector2I.Up != new Vector2I(15, 25);

		// Check up
		if(g.IsDirectionValid(gridPos, Vector2I.Up) && upAllowed && reverse != Vector2I.Up) dirs.Add(Vector2I.Up);

		// Check down
		if(g.IsDirectionValid(gridPos, Vector2I.Down) && reverse != Vector2I.Down) dirs.Add(Vector2I.Down);

		// Check left
		if(g.IsDirectionValid(gridPos, Vector2I.Left) && reverse != Vector2I.Left) dirs.Add(Vector2I.Left);

		// Check right
		if(g.IsDirectionValid(gridPos, Vector2I.Right) && reverse != Vector2I.Right) dirs.Add(Vector2I.Right);

		// Check to make sure there's at least one entry in the direction list. if not, something went wrong.
		if(dirs.Count == 0) {
			return Vector2I.Zero;
		}

		float shortestDist = (gridPos + dirs[0]).DistanceTo(targetPos);
		int entry = 0;

		if(dirs.Count > 1) {
			for(int i = 0; i < dirs.Count; i++) {
				float dist = (gridPos + dirs[i]).DistanceTo(targetPos);

				if(dist < shortestDist) {
					entry = i;
					shortestDist = dist;
				}
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
		oldPos = gridPos;

		return newDir;
	}

	public void PlayAnim(Vector2I dir) {
		string oldAnim = sprite.Animation;
		string newAnim = oldAnim;

		if(oldAnim != "SCARED" || oldAnim == "SCARED" && previousState != states.SCARED) {
			switch(dir) {
				case Vector2I v when dir == Vector2I.Up:
					newAnim = "UP";
					break;
				
				case Vector2I v when dir == Vector2I.Down:
					newAnim = "DOWN";
					break;
				
				case Vector2I v when dir == Vector2I.Left:
					newAnim = "LEFT";
					break;
				
				case Vector2I v when dir == Vector2I.Right:
					newAnim = "RIGHT";
					break;
			}
		}

		if(newAnim != oldAnim) sprite.Play(newAnim);

		// Set palette if needed.
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
		if(currentState == states.EATEN) newMod = 2f;

		return newMod;
	}

	public void PositionCheck() {
		if(g.p.gridPos == gridPos) {
			switch(currentState) {
				case states.SEEK:
					//g.SetState(Game.states.LOSE);
					break;
				
				case states.SCARED:
					g.p.Hide();
					Hide();
					g.SetState(Game.states.GHOSTEATEN);
					break;
			}
		}
	}
}
