using Godot;
using System;
using System.Collections.Generic;

public partial class Game : Node2D
{
	private Master m;
	private TileMapLayer tml;
	public PacMan p;

	public enum states {NULL, INIT, SHOWTEXT, SHOWACTORS, SCATTER, CHASE, GHOSTEATEN, LOSE, WIN, NEXTLEVEL, LIVESCHECK, GAMEOVER};
	public states currentState = states.NULL;
	private states previousState = states.NULL;

	private enum soundStates {NULL, SIREN1, SIREN2, SIREN3, SIREN4, SIREN5, FRIGHTENED, EYES, EXTEND};
	private soundStates currentLoop = soundStates.NULL;

	private List<int> levelIcons = new List<int>() {
		0,
		1,
		2,
		2,
		3,
		3,
		4,
		4,
		5,
		5,
		6,
		6,
		7,
		7,
		7,
		7,
		7,
		7,
		7
	};

	private int ticks = 0;
	private int ticksToNext = 0;
	private int phase = 0;
	private int dotEatSample = 0;
	public int scaredTicks = 0;
	public int extendTicks = 0;
	public int dotsEaten = 0;
	private int ticksSinceLastDot = 0;
	private int bigDotsEaten = 0;
	public int eatenGhosts = 0;
	public int ghostsInPlay = 0;
	private int mazePalette = 0;
	private int[] saveTicks = new int[] {0, 0};

	public bool scaredMode = false;
	public bool eyesMode = false;
	private bool extraLife = false;

	private Vector2I lastGridPos = Vector2I.Zero;

	public List<Ghost> ghosts = new List<Ghost>();
	private List<Sprite2D> ghostTargets = new List<Sprite2D>();

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
		ticksSinceLastDot++; // A failsafe to prevent the player from camping to stop the ghosts exiting the house.
		if(extendTicks > 0) extendTicks--;
		if(scaredTicks > 0 && currentState != states.GHOSTEATEN) scaredTicks--;
		if(scaredTicks == 0 && eatenGhosts > 0) eatenGhosts = 0;

		FlashScore();

		switch(currentState) {
			case states.SCATTER:
			case states.CHASE:
				SoundLoops();

				ForceGhostExit();

				if(lastGridPos != p.gridPos) {
					if(tml.GetCellSourceId(p.gridPos) == 0) {
						TileData data = tml.GetCellTileData(p.gridPos);
						bool smallDot = (bool)data.GetCustomData("small");
						bool largeDot = (bool)data.GetCustomData("large");
						if(smallDot || largeDot) {
							tml.SetCell(p.gridPos, -1);

							if(smallDot) {
								p.moveDelay = 1; // Delay Pac-Man by a single frame.
								UpdateScores(10);
							} else {
								p.moveDelay = 3;
								bigDotsEaten++;
								scaredTicks = SetFrightenedMode();
								UpdateScores(50);
							}
							// Fun fact, the eating noises actually alternate between two samples in the arcade original. This is recreated here.
							PlaySingle("eat_dot_" + dotEatSample.ToString());
							dotEatSample++;
							if(dotEatSample > 1) dotEatSample = 0;
							dotsEaten++;

							for(int i = 0; i < ghosts.Count; i++) {
								ghosts[i].dotsToExit--;
								if(ghosts[i].dotsToExit < 0) ghosts[i].dotsToExit = 0;
							}
							ticksSinceLastDot = 0;
							m.eatenDotCoords.Add(p.gridPos);
						}
					}
					lastGridPos = p.gridPos;
				}
				break;
			
			case states.LOSE:
				if(ticks == 60) {
					GetTree().Paused = false;
					for(int i = 0; i < ghosts.Count; i++) {
						ghosts[i].SetProcess(false);
						ghosts[i].SetPhysicsProcess(false);
						ghosts[i].Hide();
					}

					p.SetState(PacMan.states.DEADA);
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
				//if(ticks == 1) return states.SHOWTEXT;
				if(ticks == 1 && m.level == 1 && m.eatenDotCoords.Count == 0) return states.SHOWTEXT;
				if(ticks == 1 && m.level > 1 || m.eatenDotCoords.Count > 0) return states.SHOWACTORS;
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
			
			case states.LOSE:
				if(ticks == 300) return states.LIVESCHECK;
				break;
			
			case states.GAMEOVER:
				if(ticks == 300) m.ReloadScene();
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
			case states.INIT:
				// First, check the eaten dots coorindates stored in Master.
				for(int i = 0; i < m.eatenDotCoords.Count; i++) {
					tml.SetCell(m.eatenDotCoords[i], -1);
					if(m.eatenDotCoords[i] == new Vector2I(1, 6)
					|| m.eatenDotCoords[i] == new Vector2I(26, 6)
					|| m.eatenDotCoords[i] == new Vector2I(1, 26)
					|| m.eatenDotCoords[i] == new Vector2I(26, 26)) bigDotsEaten++;
				}

				dotsEaten = m.eatenDotCoords.Count;

				UpdateScores(0);
				UpdateLowerUI();
				break;

			case states.SHOWTEXT:
				if(m.currentPlayer == 1) pText.Text = "PLAYER ONE"; else pText.Text = "PLAYER TWO";

				pText.Show();
				rText.Show();

				PlaySingle("start");
				break;
			
			case states.SHOWACTORS:
				pText.Hide();
				rText.Show();
				p.Show();

				for(int i = 0; i < GetTree().GetNodesInGroup("ghost").Count; i++) {
					Ghost boo = (Ghost)GetTree().GetNodesInGroup("ghost")[i];
					ghosts.Add(boo);

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

				gs.s.Frame = eatenGhosts;
				gs.Position = p.Position;
				gs.Show();

				UpdateScores((eatenGhosts + 1) * 200);

				eatenGhosts++;

				p.SetPhysicsProcess(false);
				for(int i = 0; i < ghosts.Count; i++) {
					ghosts[i].SetProcess(false);
					ghosts[i].SetPhysicsProcess(false);
				}

				break;
			
			case states.LOSE:
				m.StopLoop();
				GetTree().Paused = true;
				break;
			
			case states.LIVESCHECK:
				if(m.currentPlayer == 1) m.p1Lives--; else m.p2Lives--;

				int saveCurrentPlayer = m.currentPlayer;

				// First, check to see if the game is in two player mode. If so, check the next player's lives.
				if(m.players == 2 && m.currentPlayer == 1 && m.p2Lives > 0
				|| m.players == 2 && m.currentPlayer == 2 && m.p1Lives > 0) {
					int getCurrentPlayerLvl = m.level;
					m.level = m.savedLevel;
					m.savedLevel = getCurrentPlayerLvl;

					List<Vector2I> getCurrentEatenDots = m.eatenDotCoords;
					m.eatenDotCoords = m.savedEatenDotCoords;
					m.savedEatenDotCoords = getCurrentEatenDots;

					m.currentPlayer++;
					if(m.currentPlayer > 2) m.currentPlayer = 1;
				}

				// Now that the level has been adjusted and saved, see if the game will automatically reload or go into Game Over mode.
				if(saveCurrentPlayer == 1 && m.p1Lives > 0
				|| saveCurrentPlayer == 2 && m.p2Lives > 0) m.ReloadScene(); else SetState(states.GAMEOVER);

				break;
			
			case states.GAMEOVER:
				gText.Show();
				m.eatenDotCoords.Clear();
				if(m.p1Lives == 0) {
					m.p1Score = 0;
					m.level = 1;
					m.p1Lives = 3;
				}
				if(m.p2Lives == 0) {
					m.p2Score = 0;
					m.level = 1;
					m.p2Lives = 3;
				}
				break;
			
			case states.WIN:
				m.StopLoop();
				GetTree().Paused = true;
				m.eatenDotCoords.Clear();
				break;
			
			case states.NEXTLEVEL:
				m.level++;
				GetTree().Paused = false;
				m.ReloadScene();
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
				for(int i = 0; i < ghosts.Count; i++) {
					ghosts[i].SetProcess(true);
					ghosts[i].SetPhysicsProcess(true);
				}
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
				GhostScore gs = (GhostScore)GetNode("GhostScore");

				ticks = saveTicks[0];
				scaredTicks = saveTicks[1];
				p.Show();
				p.SetPhysicsProcess(true);

				gs.Hide();

				for(int i = 0; i < ghosts.Count; i++) {
					if(!ghosts[i].Visible) {
						ghosts[i].eaten = true;
						ghosts[i].Show();
					}
					ghosts[i].SetProcess(true);
					ghosts[i].SetPhysicsProcess(true);
				}
				break;
		}
	}

	public void SetState(states newState) {
		previousState = currentState;
		currentState = newState;

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

	public void PlayLoop(string name) {
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

		for(int i = 0; i < ghosts.Count; i++) {
			if(!ghosts[i].frightened) ghosts[i].forceReverse = true;
			if(!ghosts[i].eaten) ghosts[i].frightened = true;
		}

		return newValue;
	}

	private void SoundLoops() {
		int sGhosts = 0;
		int eGhosts = 0;
		soundStates newLoop = soundStates.NULL;

		for(int i = 0; i < ghosts.Count; i++) {
			if(ghosts[i].frightened) sGhosts++;
			if(ghosts[i].eaten) eGhosts++;
		}

		if(sGhosts > 0) newLoop = soundStates.FRIGHTENED;
		if(eGhosts > 0) newLoop = soundStates.EYES;

		if(sGhosts == 0 && scaredTicks > 0) {
			scaredTicks = 0;
		}

		if(sGhosts == 0 && eGhosts == 0) {
			switch(bigDotsEaten) {
				case 0:
					newLoop = soundStates.SIREN1;
					break;
				
				case 1:
					newLoop = soundStates.SIREN2;
					break;
				
				case 2:
					newLoop = soundStates.SIREN3;
					break;
				
				case 3:
					newLoop = soundStates.SIREN4;
					break;
				
				case 4:
					newLoop = soundStates.SIREN5;
					break;
			}
		}

		if(extendTicks > 0) newLoop = soundStates.EXTEND;

		if(newLoop != currentLoop) {
			switch(newLoop) {
				case soundStates.SIREN1:
					PlayLoop("siren0");
					break;
				
				case soundStates.SIREN2:
					PlayLoop("siren1");
					break;

				case soundStates.SIREN3:
					PlayLoop("siren2");
					break;

				case soundStates.SIREN4:
					PlayLoop("siren3");
					break;
				
				case soundStates.SIREN5:
					PlayLoop("siren4");
					break;

				case soundStates.FRIGHTENED:
					PlayLoop("fright");
					break;
				
				case soundStates.EYES:
					PlayLoop("eyes");
					break;
				
				case soundStates.EXTEND:
					PlayLoop("extend");
					break;
			}

			currentLoop = newLoop;
		}
	}

	public void UpdateScores(int value) {
		Label p1 = (Label)GetNode("TileMapLayer/UpperUI/1upScore");
		Label p2Parent = (Label)GetNode("TileMapLayer/UpperUI/2up");
		Label p2 = (Label)GetNode("TileMapLayer/UpperUI/2upScore");
		Label hi = (Label)GetNode("TileMapLayer/UpperUI/HighScore/HighScore");

		if(m.players == 2) {
			p2Parent.Show();
			p2.Show();
		}

		if(m.currentPlayer == 1) m.p1Score += value; else m.p2Score += value;

		// Add lives. The original game had a dip switch that would allow for one free extra life at 10000, 15000, 20000, or not at all.
		if(m.p1Score >= 10000 && !m.p1ExtraLife
		|| m.p2Score >= 10000 && !m.p2ExtraLife) {
			extendTicks = 70;
			if(m.currentPlayer == 1) m.p1Lives++; else m.p2Lives++;
			UpdateLowerUI();
			if(m.currentPlayer == 1) m.p1ExtraLife = true ; else m.p2ExtraLife = true;
		}

		if(m.p1Score > m.highScore) m.highScore = m.p1Score;
		if(m.p2Score > m.highScore) m.highScore = m.p2Score;

		p1.Text = m.p1Score.ToString();
		p2.Text = m.p2Score.ToString();
		hi.Text = m.highScore.ToString();
	}

	private void UpdateLowerUI() {
		Control lowUI = (Control)GetNode("LowerUI");
		int getLives = 0;

		if(m.currentPlayer == 1) getLives = m.p1Lives; else getLives = m.p2Lives;

		// Clear the current icons.
		for(int i = 0; i < lowUI.GetChildCount(); i++) {
			lowUI.GetChild(0).QueueFree();
		}

		// Populate Lives icons.
		for(int i = 0; i < getLives - 1; i++) {
			PackedScene el = (PackedScene)ResourceLoader.Load("res://scenes/objects/extra_life.tscn");
			Sprite2D result = (Sprite2D)el.Instantiate();
			lowUI.AddChild(result);
			result.Position = new Vector2(8 + (i * 16), 280);
		}

		// Populate the Level Icons.
		int pos = 0;
		switch(m.level) {
			case int v when m.level < 8:
				for(int i = 0; i < m.level; i++) {
					PackedScene li = (PackedScene)ResourceLoader.Load("res://scenes/objects/level_icon.tscn");
					Sprite2D result = (Sprite2D)li.Instantiate();
					lowUI.AddChild(result);
					result.Position = new Vector2(216 - (i * 16), 280);
					result.Frame = levelIcons[i];
				}
				break;
			
			case int v when m.level > 7 && m.level < 19:
				for(int i = m.level - 7; i < m.level; i++) {
					PackedScene li = (PackedScene)ResourceLoader.Load("res://scenes/objects/level_icon.tscn");
					Sprite2D result = (Sprite2D)li.Instantiate();
					lowUI.AddChild(result);
					result.Position = new Vector2(216 - (pos * 16), 280);
					result.Frame = levelIcons[i];
					pos++;
				}
				break;
			
			case int v when m.level >= 19:
				for(int i = 12; i < 19; i++) {
					PackedScene li = (PackedScene)ResourceLoader.Load("res://scenes/objects/level_icon.tscn");
					Sprite2D result = (Sprite2D)li.Instantiate();
					lowUI.AddChild(result);
					result.Position = new Vector2(216 - (pos * 16), 280);
					result.Frame = levelIcons[i];
					pos++;
				}
				break;
		}
	}

	private void FlashScore() {
		Label p1 = (Label)GetNode("TileMapLayer/UpperUI/1up");
		Label p2 = (Label)GetNode("TileMapLayer/UpperUI/2up");

		if(ticks % 10 == 0) {
			switch(m.currentPlayer) {
				case 1:
					p1.Visible = !p1.Visible;
					break;
				
				case 2:
					p2.Visible = !p2.Visible;
					break;
			}
		}
	}

	private void ForceGhostExit() {
		if(ticksSinceLastDot == 300) {
			switch(ghostsInPlay) {
				case 1:
					ghosts[1].dotsToExit = 0;
					break;
				
				case 2:
					ghosts[2].dotsToExit = 0;
					break;
				
				case 3:
					ghosts[3].dotsToExit = 0;
					break;
			}
			ticksSinceLastDot = 0;
		}	
	}
}
