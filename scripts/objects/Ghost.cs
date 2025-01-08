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
	public Vector2I gHouseEntryPnt = new Vector2I(112, 116);
	public float distToGhostHouse = 0;
	public int dotsToExit = 0;
	public int moveDelay = 0;
	public int basePalette = 0;
	public int currentPalette = 0;
	public int timeSinceLastDot = 0;
	public bool forceReverse = false;
	public bool frightened = false;
	public bool eaten = false;
	public bool inPlay = false;

	public enum states {NULL, INIT, HOME, EXIT, SEEK, ENTER}
	public states currentState = states.NULL;
	public states previousState = states.NULL;

    public override void _Process(double delta) {
        // Set the appropriate palette.
		if(!frightened && !eaten && currentPalette != basePalette) SetPalette(basePalette); // Set the base palette

		if(frightened) { // Set the frightened palette and flash the ghosts when the timer is almost done.
			switch(g.scaredTicks) {
				case int v when g.scaredTicks > 120:
					if(currentPalette != 5) SetPalette(5);
					break;
				
				case int v when g.scaredTicks <= 120 && g.scaredTicks > 0:
					if(g.scaredTicks % 5 == 0) {
						int getCurrent = currentPalette;
						getCurrent++;
						if(getCurrent > 6) getCurrent = 5;
						if(getCurrent < 5) getCurrent = 6;
						SetPalette(getCurrent);
					}
					break;
				
				case 0:
					frightened = false;
					break;
			}
		}

		if(eaten && currentPalette != 4) SetPalette(4); // Set the eaten palette.

		// Set the appropriate animation.
		string newAnim = "";

		if(!frightened) { // Play the animation
			switch(direction) {
				case Vector2I v when direction == Vector2I.Up:
					newAnim = "UP";
					break;
				
				case Vector2I v when direction == Vector2I.Down:
					newAnim = "DOWN";
					break;
				
				case Vector2I v when direction == Vector2I.Left:
					newAnim = "LEFT";
					break;
				
				case Vector2I v when direction == Vector2I.Right:
					newAnim = "RIGHT";
					break;
			}
		} else {
			newAnim = "SCARED";
		}

		if(newAnim != sprite.Animation && newAnim != "") sprite.Play(newAnim);
		if(!sprite.IsPlaying()) sprite.Play();
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
		targetPos = SetTargetTile(); // Get the current target tile.
		speedMod = SpeedModifier(); // Set the speed modifier
		if(currentState != states.INIT) Position += (Vector2)direction * (speed * speedMod) * (float)delta; // Move the ghost as needed.
		Wrap(); // Wrap the ghost along the play field.
		gridPos = GetGridPosition(Position); // Update grid position
		PositionCheck(); // Check to see if the ghost is overlapping the player.

		switch(currentState) {
			case states.HOME:
				if(Position.Y <= 136 || Position.Y >= 144) direction.Y = -direction.Y;
				break;

			case states.SEEK:
				if(TileCenter() && oldPos != gridPos) { // Ghost is close to the center of a tile.
					AlignToGrid(gridPos); // Align the ghost to the grid in the center of the tile.
					Vector2I reverse = -direction; // Save the reversed direction;
					Vector2I oldDirection = direction;

					if(eaten && forceReverse) forceReverse = false;

					if(frightened) direction = ChooseRandomDir(); else direction = ChooseShortestDir();

					if(forceReverse) {
						direction = reverse;
						forceReverse = false;
					}
					
					oldPos = gridPos;
				}
				break;
		}

		// If the scared ticks hit 0 and the ghost hasn't been eaten, set frightened flag to false.
		// As a failsafe, if the eaten flag is true alongside frightened, disabled frighrned.
		if(g.scaredTicks <= 0 && frightened
		|| eaten && frightened) frightened = false;
	}

	private states GetTransition(double delta) {
		switch(currentState) {			
			case states.SEEK:
				if(distToGhostHouse <= 1.5 && eaten) return states.ENTER;
				break;
			
			case states.ENTER:
				if(Position.Y >= 144) return states.HOME;
				break;
			
			case states.HOME:
				if(dotsToExit == 0) {
					switch(Name) {
						case "Blinky":
							return states.EXIT;

						case "Pinky":
							if(g.ghostsInPlay > 0) return states.EXIT;
							break;
						
						case "Inky":
							if(g.ghostsInPlay > 1) return states.EXIT;
							break;
						
						case "Clyde":
							if(g.ghostsInPlay > 2) return states.EXIT;
							break;
					}
				}
				break;
			
			case states.EXIT:
				if(Position.Y <= 116) return states.SEEK;
				break;
		}

		return states.NULL;
	}

	private void EnterState(states newState, states oldState) {
		switch(newState) {
			case states.SEEK:
				if(!inPlay) {
					g.ghostsInPlay++;
					inPlay = true;
				}
				forceReverse = false;
				break;

			case states.ENTER:
				Position = new Vector2I(112, 116);
				direction = Vector2I.Down;
				break;

			case states.HOME:
				eaten = false;
				break;
			
			case states.EXIT:
				Position = new Vector2I(112, (int)Position.Y);
				direction = Vector2I.Up;
				break;
		}
	}

	private void ExitState(states oldState, states newState) {
		switch(oldState) {
			case states.EXIT:
				// Always send the ghosts to the left.
				Position = new Vector2I(112, 116);
				direction = Vector2I.Left;
				PlayAnim(direction);
				break;
		}
	}

	public void SetState(states newState) {
		previousState = currentState;
		currentState = newState;

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
					
					case string t when Name == "Pinky":
						targetTile = new Vector2I(2, 0);
						break;
					
					case string t when Name == "Inky":
						targetTile = new Vector2I(27, 34);
						break;
					
					case string t when Name == "Clyde":
						targetTile = new Vector2I(0, 34);
						break;
				}
				break;
			
			case Game.states.CHASE:
				switch(Name) {
					case string t when Name == "Blinky":
						targetTile = g.p.gridPos;
						break; 
					
					case string t when Name == "Pinky":
						switch(g.p.ghostAIDir) {
							case Vector2I v when g.p.ghostAIDir == Vector2I.Up:
								targetTile = g.p.gridPos + new Vector2I(-4, -4); // Due to a glitch in the original game, Pinky's target tile when Pac-man is facing up is wildly off the mark. This is replicvated here.
								break;
							
							case Vector2I v when g.p.ghostAIDir == Vector2I.Down:
								targetTile = g.p.gridPos + new Vector2I(0, 4);
								break;
							
							case Vector2I v when g.p.ghostAIDir == Vector2I.Left:
								targetTile = g.p.gridPos + new Vector2I(-4, 0);
								break;
							
							case Vector2I v when g.p.ghostAIDir == Vector2I.Right:
								targetTile = g.p.gridPos + new Vector2I(4, 0);
								break;
						}
						break;
					
					case string t when Name == "Inky":
						// This one is the most complicated of the bunch. First get the spcae 2 tiles ahead of pacman, including the bugged up direction.
						Vector2I firstPhase = Vector2I.Zero;

						switch(g.p.ghostAIDir) {
							case Vector2I v when g.p.ghostAIDir == Vector2I.Up:
								firstPhase = g.p.gridPos + new Vector2I(-2, -2);
								break;
							
							case Vector2I v when g.p.ghostAIDir == Vector2I.Down:
								firstPhase = g.p.gridPos + new Vector2I(0, 2);
								break;
							
							case Vector2I v when g.p.ghostAIDir == Vector2I.Left:
								firstPhase = g.p.gridPos + new Vector2I(-2, 0);
								break;
							
							case Vector2I v when g.p.ghostAIDir == Vector2I.Right:
								firstPhase = g.p.gridPos + new Vector2I(2, 0);
								break;
						}

						Vector2I secondPhase = g.ghosts[0].gridPos - firstPhase; // get the grid position of Blinky to phase 1.

						secondPhase = -secondPhase; // Reverse it.
						secondPhase = secondPhase * 2; // Double it.

						targetTile = g.ghosts[0].gridPos + secondPhase;
						break;
					
					case string t when Name == "Clyde":
						float distToPacMan = gridPos.DistanceTo(g.p.gridPos);

						if(distToPacMan >= 8) targetTile = g.p.gridPos; else targetTile = new Vector2I(0, 34);
						break;
				}
				break;
		}

		if(eaten) targetTile = new Vector2I(13, 14);

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
		float margin = 1.25f;

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

		// Add possible directions to a list.
		List<Vector2I> dirs = new List<Vector2I> ();
		Vector2I reverse = -direction;
		bool upAllowed = gridPos + Vector2I.Up != new Vector2I(12, 13)
					  && gridPos + Vector2I.Up != new Vector2I(15, 13)
					  && gridPos + Vector2I.Up != new Vector2I(12, 25)
					  && gridPos + Vector2I.Up != new Vector2I(15, 25);

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

		if(newAnim != oldAnim) sprite.Play(newAnim);

		// Set palette if needed.
	}

	public float SpeedModifier() {
		float newMod = 1;
		int dotsRemaining = 244 - g.dotsEaten;

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

		// For Blinky only. Set modifier for Cruise Elroy speeds.
		if(this.Name == "Blinky") {
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
		if(frightened) {
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
		if(eaten) newMod = 2f;

		// If exiting.
		if(currentState == states.EXIT
		|| currentState == states.HOME) newMod = 0.5f;

		return newMod;
	}

	public void PositionCheck() {
		if(g.p.gridPos == gridPos) {
			switch(frightened) {
				case false:
					if(!eaten) g.SetState(Game.states.LOSE);
					break;
				
				case true:
					g.p.Hide();
					Hide();
					g.SetState(Game.states.GHOSTEATEN);
					frightened = false;
					eaten = true;
					break;
			}
		}

		distToGhostHouse = Position.DistanceTo(gHouseEntryPnt);
	}

	public void SetExitDots() {
		bool lostLife = m.eatenDotCoords.Count > 0;

		switch(Name) {
			case "Blinky":
				dotsToExit = 0;
				break;
			
			case "Pinky":
				if(!lostLife) dotsToExit = 0; else dotsToExit = 7;
				break;
			
			case "Inky":
				if(!lostLife) {
					if(m.level == 1) dotsToExit = 30; else dotsToExit = 0;
				} else dotsToExit = 17;
				break;
			
			case "Clyde":
				if(!lostLife) {
					if(m.level ==1) dotsToExit = 60; else if(m.level == 2) dotsToExit = 50; else dotsToExit = 0;
				} else dotsToExit = 32;
				break;
		}
	}


}
