using Godot;
using System;

public partial class Game : Node2D
{
	private Master m;
	private Label debug;

	private enum states {NULL, INIT, SHOWTEXT, SHOWACTORS, SCATTER, CHASE, LOSE, WIN, GAMEOVER}
	private states currentState = states.INIT;
	private states previousState = states.INIT;

	private int ticks = 0;
	private int ticksToNext = 0;
	private int phase = 0;

	public override void _Ready() {
		m = (Master)GetNode("/root/Master");
		debug = (Label)GetNode("Debug");
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
		ticks++;
	}

	private states GetTransition(double delta) {
		switch(currentState) {
			case states.INIT:
				return states.SCATTER;
			
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
		switch(newState) {
			case states.SHOWTEXT:
				break;
			
			case states.SHOWACTORS:
				break;

			case states.SCATTER:
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
		
	}

	private void SetState(states newState) {
		previousState = currentState;
		currentState = newState;

		GD.Print("Moving to state ",currentState,": ",ticks,", ",ticksToNext,", ",phase);

		ticks = 0;

		ExitState(previousState, currentState);
		EnterState(currentState, previousState);
	}
}
