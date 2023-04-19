using System.Collections.Generic;

namespace UCHPlayerStateRecorderMod;

public class PlayerDataRecording
{
	public PlayerDataRecording(CharacterWrapper character)
	{
		Animal = character.Character.CharacterSprite.ToString();
	}

	public string Animal { get; set; }
	public List<Dictionary<string, object>> Data { get; } = new();
}