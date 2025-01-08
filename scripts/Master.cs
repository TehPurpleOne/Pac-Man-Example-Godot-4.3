using Godot;
using System;
using System.Collections.Generic;

public partial class Master : Node2D
{
	private AudioStreamPlayer activeLoop;
	private List<AudioStreamPlayer> activeSingles = new List<AudioStreamPlayer>();
	public List<Vector2I> eatenDotCoords = new List<Vector2I>();
	public List<Vector2I> savedEatenDotCoords = new List<Vector2I>();

	public int level = 1;
	public int savedLevel = 1;
	public int players = 1;
	public int currentPlayer = 1;
	public int p1Lives = 3;
	public int p2Lives = 3;
	public int p1Score = 0;
	public int p2Score = 0;
	public int highScore = 0;
	public bool p1ExtraLife = false;
	public bool p2ExtraLife = false;

	public void PlaySingleSound(AudioStream name) {
		// Create a new AudioStreamPlayer for the sound effect.
		AudioStreamPlayer singlePlayer = new AudioStreamPlayer();
		AddChild(singlePlayer);
		singlePlayer.Stream = name;
		singlePlayer.Bus = "Single";

		// To prevent multiple instances of the same sound sample playing at once, check the list to see if another of the same sample is playing.
		// If so, remove it.
		for(int i = 0; i < activeSingles.Count; i++) {
			AudioStreamPlayer testSample = activeSingles[i];
			if(testSample.Stream == name) {
				testSample.QueueFree();
				activeSingles.Remove(testSample);
				break;
			}
		}

		// Once cleared, play the sample.
		singlePlayer.Play();

		// Connect the signal so finished sound samples get removed from the list.
		singlePlayer.Finished += () => OnSingleDone(singlePlayer);

		// Add the sample to the list.
		activeSingles.Add(singlePlayer);
	}

	public void PlayLoopSound(AudioStream name) {
		// Check to see if a loop is already playing. If so, stop it and null the variable.
		if(activeLoop != null) {
			activeLoop.Stop();
		}

		// Create a new AudioStreamPlayer for the loop.
		AudioStreamPlayer loopPlayer = new AudioStreamPlayer();
		AddChild(loopPlayer);
		loopPlayer.Stream = name;
		loopPlayer.Bus = "Loop";
		loopPlayer.Play();

		activeLoop = loopPlayer;
	}

	public void StopLoop() {
		activeLoop.Stop();
		activeLoop = null;
	}

	private void OnSingleDone(AudioStreamPlayer sfx) {
		sfx.QueueFree();
		activeSingles.Remove(sfx);
	}

	public void ReloadScene() {
		Control sm = (Control)GetNode("SceneManager");
		
		if(sm.GetChildCount() > 0) {
			sm.GetChild(0).QueueFree();
		}

		PackedScene game = (PackedScene)ResourceLoader.Load("res://scenes/game.tscn");

		Node2D result = (Node2D)game.Instantiate();

		sm.AddChild(result);
	}
}
