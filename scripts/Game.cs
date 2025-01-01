using Godot;
using System;
using System.Collections.Generic;

public partial class Game : Node2D
{
	private Master m;
	private TileMapLayer tml;
	public PacMan p;

	public enum states {NULL, INIT, SHOWTEXT, SHOWACTORS, SCATTER, CHASE, GHOSTEATEN, LOSE, WIN, NEXTLEVEL, GAMEOVER}
	public states currentState = states.NULL;
	private states previousState = states.NULL;

	private int ticks = 0;
	private int ticksToNext = 0;
	private int phase = 0;
	private int dotEatSample = 0;
	public int scaredTicks = 0;
	public int dotsEaten = 0;
	private int bigDotsEaten = 0;
	public int eatenGhosts = 0;
	private int mazePalette = 0;
	private int[] saveTicks = new int[] {0, 0};

	public bool scaredMode = true;
	public bool eyesMode = false;

	private Vector2I lastGridPos = Vector2I.Zero;

	private List<Ghost> ghosts = new List<Ghost>();
	private List<Sprite2D> ghostTargets = new List<Sprite2D>();

	public override void _Ready() {
		m = (Master)GetNode("/root/Master");
		tml = (TileMapLayer)GetNode("TileMapLayer");
		p = (PacMan)GetNode("PacMan");

		SetState(states.INIT);
	}

    public override void _Draw() {
        for(int i = 0; i < ghostTargets.Count; i++) {
			Color c = new Color();
			switch(i) {
				case 0:
					c = new Color("#ff0000");
					break;
				case 1:
					c = new Color("#00ffff");
					break;
				case 2:
					c = new Color("#ffb7ff");
					break;
				case 3:
					c = new Color("#ffb751");
					break;
			}
			DrawLine(ghostTargets[i].Position, ghosts[i].Position, c, 1.0f);
		}
    }

    public override void _Process(double delta) {
        QueueRedraw();
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
		ticks++; // Ticks will always count up regardless of the current state. Use for timing of events.
		if(scaredTicks > 0) scaredTicks--;

		UpdateTargetTiles();

		switch(currentState) {
			case states.SCATTER:
			case states.CHASE:
				// Handle the looping sound effects
				// Siren
				if(scaredMode && scaredTicks == 0 || eyesMode && eatenGhosts == 0) {
					switch(bigDotsEaten) {
						case 0:
							PlayLoop("siren0");
							break;
						
						case 1:
							PlayLoop("siren1");
							break;
						
						case 2:
							PlayLoop("siren2");
							break;
						
						case 3:
							PlayLoop("siren3");
							break;
						
						case 4:
							PlayLoop("siren4");
							break;
					}
					scaredMode = false;
					eyesMode = false;
				}

				if(lastGridPos != p.gridPos) {
					if(tml.GetCellSourceId(p.gridPos) == 0) {
						TileData data = tml.GetCellTileData(p.gridPos);
						bool smallDot = (bool)data.GetCustomData("small");
						bool largeDot = (bool)data.GetCustomData("large");
						if(smallDot || largeDot) {
							tml.SetCell(p.gridPos, -1);

							if(smallDot) {
								p.moveDelay = 1; // Delay Pac-Man by a single frame.
							} else {
								p.moveDelay = 3;
								bigDotsEaten++;
								scaredTicks = SetFrightenedMode();
							}
							// Fun fact, the eating noises actually alternate between two samples in the arcade original. This is recreated here.
							PlaySingle("eat_dot_" + dotEatSample.ToString());
							dotEatSample++;
							if(dotEatSample > 1) dotEatSample = 0;
							dotsEaten++;
						}
					}
					lastGridPos = p.gridPos;
				}
				break;

			case states.WIN:
				switch(ticks) {
					case 60:
						p.Hide();
						for(int i = 0; i < GetTree().GetNodesInGroup("ghost").Count; i++) {
							Ghost boo = (Ghost)GetTree().GetNodesInGroup("ghost")[i];
							boo.Hide();
						}
						break;
				}

				if(ticks >= 60 && ticks % 10 == 0) {
					mazePalette++;
					if(mazePalette > 1) mazePalette = 0;

					ShaderMaterial sm = (ShaderMaterial)tml.Material;
					sm.SetShaderParameter("palette_index", mazePalette);
				}
				break;
		}

		
	}

	private states GetTransition(double delta) {
		

		switch(currentState) {
			case states.INIT:
				if(ticks == 1) return states.SHOWTEXT;
				break;
			
			case states.SHOWTEXT:
				if(ticks == 150) return states.SHOWACTORS;
				break;
			
			case states.SHOWACTORS:
				if(ticks == 105) return states.SCATTER;
				break;

			case states.SCATTER:
				if(ticks == ticksToNext) return states.CHASE;
				if(dotsEaten == 244) return states.WIN;
				break;

			case states.CHASE:
				if(ticks == ticksToNext) return states.SCATTER;
				if(dotsEaten == 244) return states.WIN;
				break;
			
			case states.GHOSTEATEN:
				if(ticks == 45) return previousState;
				break;
			
			case states.WIN:
				if(ticks == 150) return states.NEXTLEVEL;
				break;
		}

		return states.NULL;
	}

	private void EnterState(states newState, states oldState) {
		Label pText = (Label)GetNode("Player");
		Label rText = (Label)GetNode("Ready");
		Label gText = (Label)GetNode("GameOver");

		switch(newState) {
			case states.SHOWTEXT:
				if(m.currentPlayer == 1) pText.Text = "PLAYER ONE"; else pText.Text = "PLAYER TWO";

				pText.Show();
				rText.Show();

				PlaySingle("start");
				break;
			
			case states.SHOWACTORS:
				pText.Hide();
				p.Show();

				for(int i = 0; i < GetTree().GetNodesInGroup("ghost").Count; i++) {
					Ghost boo = (Ghost)GetTree().GetNodesInGroup("ghost")[i];
					ghosts.Add(boo);

					if(i == 0) boo.followGhost = ghosts[0]; else boo.followGhost = ghosts[i - 1];

					boo.Show();
				}

				for(int i = 0; i < GetTree().GetNodesInGroup("target").Count; i++) {
					Sprite2D target = (Sprite2D)GetTree().GetNodesInGroup("target")[i];
					ghostTargets.Add(target);
				}
				break;

			case states.SCATTER:
				// Update the ghost's target positions to the four corners. No further position calculations are necessary.
				switch(phase) {
					case 0:
					case 2:
						if(m.level < 5) ticksToNext = 420; else ticksToNext = 300;
						break;
					
					case 4:
						ticksToNext = 300;
						break;
					
					case 6:
						if(m.level == 1) ticksToNext = 300; else ticksToNext = 1;
						break;
				}
				phase++;
				break;
			
			case states.CHASE:
				switch(phase) {
					case 1:
					case 3:
						ticksToNext = 1200;
						break;
					
					case 5:
						if(m.level == 1) ticksToNext = 1200; else if(m.level > 1 && m.level < 5) ticksToNext = 61980; else ticksToNext = 62220;
						break;
					
					case 7:
						ticksToNext = -1;
						break;
				}
				phase++;
				break;
			
			case states.GHOSTEATEN:
				GhostScore gs = (GhostScore)GetNode("GhostScore");

				PlaySingle("eat_ghost");

				saveTicks[0] = ticks;
				saveTicks[1] = scaredTicks;

				eatenGhosts++;

				gs.s.Frame = eatenGhosts - 1;
				gs.Position = p.Position;
				gs.Show();

				p.SetPhysicsProcess(false);
				for(int i = 0; i < ghosts.Count; i++) {
					ghosts[i].SetPhysicsProcess(false);
				}

				//GetTree().Paused = true;

				break;
			
			case states.WIN:
				GetTree().Paused = true;
				break;
			
			case states.NEXTLEVEL:
				m.level++;
				GetTree().Paused = false;
				GetTree().ReloadCurrentScene();
				break;
		}
	}

	private void ExitState(states oldState, states newState) {
		Label pText = (Label)GetNode("Player");
		Label rText = (Label)GetNode("Ready");
		Label gText = (Label)GetNode("GameOver");

		switch(oldState) {
			case states.SHOWACTORS:
				rText.Hide();
				p.SetState(PacMan.states.ACTIVE);
				break;
			
			case states.SCATTER:
			case states.CHASE:
				if(scaredTicks == 0) {
					for(int i = 0; i < ghosts.Count; i++) {
						ghosts[i].forceReverse = true;
					}
				}
				break;
			
			case states.GHOSTEATEN:
				PlayLoop("eyes");
				ticks = saveTicks[0];
				scaredTicks = saveTicks[1];
				p.Show();
				p.SetPhysicsProcess(true);

				for(int i = 0; i < ghosts.Count; i++) {
					if(!ghosts[i].Visible) {
						ghosts[i].SetState(Ghost.states.EATEN);
						ghosts[i].Show();
					}
					ghosts[i].SetPhysicsProcess(true);
				}
				break;
		}
	}

	public void SetState(states newState) {
		previousState = currentState;
		currentState = newState;

		GD.Print("Moving to state ",currentState,": ",ticks,", ",ticksToNext,", ",phase);

		ticks = 0;

		ExitState(previousState, currentState);
		EnterState(currentState, previousState);
	}

	public void PlaySingle(string name) {
		// This function will call a non-looping sound sample.
		string path = "res://assets/audio/" + name + ".wav";
		if(ResourceLoader.Exists(path)) {
			AudioStream singleSFX = (AudioStream)GD.Load(path);
			m.PlaySingleSound(singleSFX);
		} else GD.Print("Sound sample ",name," not found!");
	}

	private void PlayLoop(string name) {
		// This function will call a looping sound sample.
		string path = "res://assets/audio/" + name + ".wav";
		if(ResourceLoader.Exists(path)) {
			AudioStream loopSFX = (AudioStream)GD.Load(path);
			m.PlayLoopSound(loopSFX);
		} else GD.Print("Loop sample ",name," not found!");
	}

	public bool IsDirectionValid(Vector2I pos, Vector2I dir) {
		bool allowed = false;
		Vector2I newTile = pos + dir;

		if(tml.GetCellSourceId(newTile) == -1) {
			allowed = true;
		}

		if(tml.GetCellSourceId(newTile) == 0) {
			TileData data = tml.GetCellTileData(newTile);
			bool collectible = (bool)data.GetCustomData("small") || (bool)data.GetCustomData("large");
			if(collectible) {
				allowed = true;
			}
		}

		return allowed;
	}

	public bool DetectCollision(Vector2I pos, Vector2I dir) {
		Vector2I newTile = pos + dir;
		if(tml.GetCellSourceId(newTile) == 0) {
			TileData data = tml.GetCellTileData(newTile);
			bool wall = (bool)data.GetCustomData("wall");
			if(wall) return true;
		}

		return false;
	}

	private int SetFrightenedMode() {
		int newValue = 0;
		scaredMode = true;

		switch(m.level) {
			case 1:
				newValue = 360;
				break;
			
			case 2:
			case 6:
			case 10:
				newValue = 300;
				break;
			
			case 3:
				newValue = 240;
				break;
			
			case 4:
			case 14:
				newValue = 180;
				break;
			
			case 5:
			case 7:
			case 8:
			case 11:
				newValue = 120;
				break;
			
			case 9:
			case 12:
			case 13:
			case 15:
			case 16:
			case 18:
				newValue = 60;
				break;
		}

		// Add trigger to send Ghosts into frightened mode here.
		for(int i = 0; i < ghosts.Count; i++) {
			if(ghosts[i].currentState == Ghost.states.SEEK) {
				ghosts[i].SetState(Ghost.states.SCARED);
			}
		}

		if(newValue > 0) {
			PlayLoop("fright");
		}

		return newValue;
	}

	private void UpdateTargetTiles() {
		for(int i = 0; i < ghostTargets.Count; i++) {
			ghostTargets[i].Position = tml.MapToLocal(ghosts[i].targetPos);
		}
	}
}
