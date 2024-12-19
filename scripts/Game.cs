using Godot;
using System;

public partial class Game : Node2D
{
	private Master m;
	private TileMapLayer tml;
	private PacMan p;

	public enum states {NULL, INIT, SHOWTEXT, SHOWACTORS, SCATTER, CHASE, LOSE, WIN, NEXTLEVEL, GAMEOVER}
	public states currentState = states.NULL;
	private states previousState = states.NULL;

	private int ticks = 0;
	private int ticksToNext = 0;
	private int phase = 0;
	private int dotEatSample = 0;
	public int scaredTicks = 0;
	private int dotsEaten = 0;
	private int bigDotsEaten = 0;
	private int eatenGhosts = 0;
	private int mazePalette = 0;

	private bool scaredMode = true;
	private bool eyesMode = false;

	private Vector2I lastGridPos = Vector2I.Zero;

	public override void _Ready() {
		m = (Master)GetNode("/root/Master");
		tml = (TileMapLayer)GetNode("TileMapLayer");
		p = (PacMan)GetNode("PacMan");

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
		ticks++; // Ticks will always count up regardless of the current state. Use for timing of events.
		if(scaredTicks > 0) scaredTicks--;

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
							PlayLoop("siren01");
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

				// Frightened Mode
				if(scaredTicks > 0 && !scaredMode) {
					PlayLoop("fright");
					scaredMode = true;
				}

				// Eyeballs Mode
				if(eatenGhosts > 0 && !eyesMode) {
					PlayLoop("eyes");
					eyesMode = true;
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

								// Fun fact, the eating noises actually alternate between two samples in the arcade original. This is recreated here.
								PlaySingle("eat_dot_" + dotEatSample.ToString());
								dotEatSample++;
								if(dotEatSample > 1) dotEatSample = 0;

							} else {
								p.moveDelay = 3;
								bigDotsEaten++;
								scaredTicks = SetFrightenedMode();
							}
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
					GD.Print(mazePalette);

					ShaderMaterial sm = (ShaderMaterial)tml.Material;
					sm.SetShaderParameter("palette_index", mazePalette);
					GD.Print(sm.GetShaderParameter("palette_index"));
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
					boo.Show();
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
		}
	}

	private void SetState(states newState) {
		previousState = currentState;
		currentState = newState;

		GD.Print("Moving to state ",currentState,": ",ticks,", ",ticksToNext,", ",phase);

		ticks = 0;

		ExitState(previousState, currentState);
		EnterState(currentState, previousState);
	}

	private void PlaySingle(string name) {
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

		return newValue;
	}
}
