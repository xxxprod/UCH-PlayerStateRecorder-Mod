using System;
using System.Collections.Generic;
using UnityEngine;

namespace UCHPlayerStateRecorderMod;

public class CharacterWrapper : InputReceiver, IDisposable
{
	public CharacterWrapper(GamePlayer player)
	{
		Character = player.CharacterInstance;
		RigidBody = Character.GetComponent<Rigidbody2D>();
		Controller = player.Control;
		Controller.AddReceiver(this);
	}

	public Character Character { get; }
	public Rigidbody2D RigidBody { get; }
	public Controller Controller { get; }
	public HashSet<InputEvent.InputKey> Inputs { get; } = new();
	public Dictionary<InputEvent.InputKey, float> AnalogInputs { get; } = new();


	public void ReceiveEvent(InputEvent e)
	{
		if (!e.Changed)
			return;

		AnalogInputs[e.Key] = e.Valuef;

		if (e.Valueb)
		{
			Inputs.Add(e.Key);
		}
		else
		{
			Inputs.Remove(e.Key);
		}
	}

	public void Dispose()
	{
		Controller.RemoveReceiver(this);
	}
}