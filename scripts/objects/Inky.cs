using Godot;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public partial class Inky : Ghost
{
	public override void _Ready() {
		m = (Master)GetNode("/root/Master");
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
		targetPos = SetTargetTile();
		switch(currentState) {
			case states.SEEK:
				
				break;
		}

		speedMod = SpeedModifier();

		if(currentState != states.INIT) Position += (Vector2)direction * (speed * speedMod) * (float)delta;
		Wrap();
		gridPos = GetGridPosition(Position);
	}

	private states GetTransition(double delta) {
		switch(currentState) {
			case states.INIT:
				if(g.currentState == Game.states.SCATTER) return states.SEEK;
				break;
			
			case states.SEEK:
			case states.SCARED:
			case states.EATEN:
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
				basePalette = 0;
				SetPalette(basePalette);
				direction = Vector2I.Left;
				desiredDir = Vector2I.Left;
				break;
			
			case states.CHOOSEDIR:
				AlignToGrid(gridPos);
				Vector2I saveDir = direction;
				saveDir = -saveDir;
				
				if(!forceReverse) {
					switch(g.scaredMode) {
						case true:
							direction = ChooseRandomDir();
							break;
						
						case false:
							direction = ChooseShortestDir();
							break;
					}
				} else {
					direction = saveDir;
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
				targetTile = new Vector2I(25, 0);
				break;
			
			case Game.states.CHASE:
				targetTile = g.p.gridPos;
				break;
		}

		return targetTile;
	}
}
