using Godot;
using System;
using System.Collections.Generic;

public partial class Master : Node2D
{
	private AudioStreamPlayer activeLoop;
	private List<AudioStreamPlayer> activeSingles = new List<AudioStreamPlayer>();

	public int level = 1;
	public int players = 1;
	public int currentPlayer = 1;
	public int p1Score = 0;
	public int p2Score = 0;
	public int p1HiScore = 0;
	public int p2HiScore = 0;

	public void PlaySingleSound(AudioStream name) {
		GD.Print("Sample received.");
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
		if(activeLoop != null && activeLoop.Playing) {
			activeLoop.Stop();
			activeLoop = null;
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
}
