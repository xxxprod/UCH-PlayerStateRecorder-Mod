using System.Linq;

namespace UCHPlayerStateRecorderMod;

public static class UchTools
{
	public static CharacterWrapper[] GetCharacters()
	{
		return LobbyManager.instance.CurrentGameController.CurrentPlayerQueue
			.OrderBy(a => a.networkNumber)
			.Select(a => new CharacterWrapper(a))
			.ToArray();
	}
}