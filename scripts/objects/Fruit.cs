using Godot;
using System;

public partial class Fruit : Node2D
{
	private Master m;
	private Game g;
	private Sprite2D sprite;

	private int ticks = 0;
	private int dotsEaten = 0;

	private float dist = 0;

	private enum states {NULL, INACTIVE, ACTIVE, EATEN}

	private states currentState = states.NULL;
	private states previousState = states.NULL;

    public override void _Ready() {
        m = (Master)GetNode("/root/Master");
		g = (Game)GetParent();
		sprite = (Sprite2D)GetNode("Sprite2D");

		SetState(states.INACTIVE);
    }

    public override void _PhysicsProcess(double delta) {
        if(currentState != states.NULL) {
			StateLogic(delta);
			states t = GetTransition(delta);
			if(t != states.NULL) SetState(t);
		}
    }

	private void StateLogic(double delta) {
		if(currentState == states.ACTIVE
		|| currentState == states.EATEN) ticks--;

		if(currentState == states.ACTIVE) {
			dist = Position.DistanceTo(g.p.Position);
		}

		if(currentState == states.INACTIVE && dotsEaten != g.dotsEaten) dotsEaten = g.dotsEaten;
	}

	private states GetTransition(double delta) {
		switch(currentState) {
			case states.ACTIVE:
				if(dist <= 4) return states.EATEN;
				if(ticks == 0) return states.INACTIVE;
				break;

			case states.EATEN:
				if(ticks == 0) return states.INACTIVE;
				break;
			
			case states.INACTIVE:
				if(dotsEaten == 70 || dotsEaten == 170) return states.ACTIVE;
				break;
		}

		return states.NULL;
	}

	private void EnterState(states newState, states oldState) {
		switch(newState) {
			case states.INACTIVE:
				Hide();
				break;

			case states.ACTIVE:
				Random RNGesus = new Random();
				ticks = RNGesus.Next(540, 601);
				switch(m.level) {
					case 1:
						sprite.Frame = 0;
						break;
					
					case 2:
						sprite.Frame = 1;
						break;
					
					case 3:
					case 4:
						sprite.Frame = 2;
						break;
					
					case 5:
					case 6:
						sprite.Frame = 3;
						break;
					
					case 7:
					case 8:
						sprite.Frame = 4;
						break;
					
					case 9:
					case 10:
						sprite.Frame = 5;
						break;
					
					case 11:
					case 12:
						sprite.Frame = 6;
						break;
					
					case int v when m.level > 12:
						sprite.Frame = 7;
						break;
				}
				Show();
				break;
			
			case states.EATEN:
				ticks = 180;
				g.PlaySingle("eat_fruit");
				sprite.Frame = sprite.Frame + 8;
				switch(sprite.Frame) {
					case 8:
						g.UpdateScores(100);
						break;

					case 9:
						g.UpdateScores(300);
						break;
					
					case 10:
						g.UpdateScores(500);
						break;
					
					case 11:
						g.UpdateScores(700);
						break;
					
					case 12:
						g.UpdateScores(1000);
						break;
					
					case 13:
						g.UpdateScores(2000);
						break;
					
					case 14:
						g.UpdateScores(3000);
						break;
					
					case 15:
						g.UpdateScores(5000);
						break;
				}
				break;
		}
	}

	private void ExitState(states oldState, states newState) {

	}

	private void SetState(states newState) {
		previousState = currentState;
		currentState = newState;

		ticks = 0;

		ExitState(previousState, currentState);
		EnterState(currentState, previousState);
	}
}
