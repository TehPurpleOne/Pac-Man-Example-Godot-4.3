using Godot;
using System;

public partial class Game : Node2D
{
	private Master m;
	private TileMapLayer tml;
	private PacMan p;

	private enum states {NULL, INIT, SHOWTEXT, SHOWACTORS, SCATTER, CHASE, LOSE, WIN, GAMEOVER}
	private states currentState = states.NULL;
	private states previousState = states.NULL;

	private int ticks = 0;
	private int ticksToNext = 0;
	private int phase = 0;
	private int dotEatSample = 0;

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
					}
				}
			}
			lastGridPos = p.gridPos;
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
				break;

			case states.CHASE:
				if(ticks == ticksToNext) return states.SCATTER;
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
		GD.Print("Attempting to play sample: ",name);
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
		} else GD.Print("Sound sample ",name," not found!");
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
}
