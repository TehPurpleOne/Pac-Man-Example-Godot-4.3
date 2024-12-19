using Godot;
using System;

public partial class Inky : Ghost
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		g = (Game)GetParent();
        sprite = (AnimatedSprite2D)GetNode("Sprite");

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

	}

	private states GetTransition(double delta) {
		switch(currentState) {
			case states.INIT:
				if(g.currentState == Game.states.SCATTER) return states.SEEK;
				break;
		}

		return states.NULL;
	}

	private void EnterState(states newState, states oldState) {

	}

	private void ExitState(states oldState, states newState) {

	}

	public void SetState(states newState) {
		previousState = currentState;
		currentState = newState;

		ExitState(previousState, currentState);
		EnterState(currentState, previousState);
	}

	private Vector2 SetTargetTile() {
		Vector2 targetTile = Vector2.Zero;

		return targetTile;
	}
}
