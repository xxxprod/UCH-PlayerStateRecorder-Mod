using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BepInEx;
using BepInEx.Configuration;
using Newtonsoft.Json;
using UnityEngine;

namespace UCHPlayerStateRecorderMod;

[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
	protected readonly Dictionary<InputEvent.InputKey, string> InputMappings = new()
	{
		[InputEvent.InputKey.Jump] = "Jump",
		[InputEvent.InputKey.Left] = "Left",
		[InputEvent.InputKey.Right] = "Right",
		[InputEvent.InputKey.Up] = "Up",
		[InputEvent.InputKey.Down] = "Down",
		[InputEvent.InputKey.Sprint] = "Sprint",
		[InputEvent.InputKey.Inventory] = "Dance",
		[InputEvent.InputKey.Back] = "Back"
	};


	private bool _recording;
	private int _frame;
	private float _time;
	private CharacterWrapper[] _characters;
	private PlayerDataRecording[] _recordings;
	private ConfigEntry<bool> _recorderEnabled;
	private ConfigEntry<bool> _positionEnabled;
	private ConfigEntry<bool> _velocityEnabled;
	private ConfigEntry<string> _outputDirectory;
	private ConfigEntry<KeyCode> _recorderHotkey;
	private ConfigEntry<bool> _registeredActionsEnabled;
	private ConfigEntry<bool> _recordOnlyChanges;
	private ConfigEntry<bool> _registeredAnalogActionsEnabled;
	private ConfigEntry<bool> _collidersEnabled;
	private ConfigEntry<bool> _playerMetaEnabled;

	private void Awake()
	{
		_recorderEnabled = Config.Bind("General", "Enabled", true, "Enables/Disables this mod");
		_recordOnlyChanges = Config.Bind("General", "Record only Changes", true, "Records only data if anything changed");
		_recorderHotkey = Config.Bind("General", "Recorder Hotkey", KeyCode.F10, "The Hotkey to start and stop recordings");
		_outputDirectory = Config.Bind("General", "Output Directory", GetDefaultOutputPath(), "The directory where recordings should be stored");

		_positionEnabled = Config.Bind("Recorded Data", "Position", true, "Records the Player Position at each frame");
		_velocityEnabled = Config.Bind("Recorded Data", "Velocity", true, "Records the Player Velocity at each frame");
		_registeredActionsEnabled = Config.Bind("Recorded Data", "Digital Actions", true, "Records the Players current registered digital inputs");
		_registeredAnalogActionsEnabled = Config.Bind("Recorded Data", "Analog Actions", false, "Records the Players current registered inputs as analog values");
		_collidersEnabled = Config.Bind("Recorded Data", "Colliders", false, "Records the Players top, bottom, left and right colliders which indicate if the player collides with any object at that side");
		_playerMetaEnabled = Config.Bind("Recorded Data", "Player Meta", false, "Records the additional meta data from the player like OnGround, OnWall, CanJump and others");


		// Plugin startup logic
		Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
	}

	private void Update()
	{
		if (!_recorderEnabled.Value || LobbyManager.instance?.CurrentGameController == null)
		{
			ResetRecorder();
			return;
		}

		if (Input.GetKeyDown(_recorderHotkey.Value))
		{
			if (!_recording)
			{
				StartRecording();
				UserMessageManager.Instance.UserMessage("Recording Started");
			}
			else
			{
				SaveRecordings();
			}
		}
	}

	private void FixedUpdate()
	{
		if (_recording)
		{
			for (int i = 0; i < _characters.Length; i++)
			{
				Dictionary<string, object> data = GetPlayerData(_characters[i]);
				if (_recordOnlyChanges.Value)
				{
					Dictionary<string, object> prevData = _recordings[i].Data.LastOrDefault();
					if (prevData != null && !DataChanged(data, prevData))
						continue;
				}

				_recordings[i].Data.Add(data);
			}

			_frame++;
			_time += Time.fixedDeltaTime;
		}
	}

	private Dictionary<string, object> GetPlayerData(CharacterWrapper character)
	{
		_ = character.Character ?? throw new ArgumentNullException(nameof(character.Character));
		_ = character.RigidBody ?? throw new ArgumentNullException(nameof(character.RigidBody));

		Dictionary<string, object> data = new()
		{
			["Frame"] = _frame,
			["Time"] = _time,
		};

		if (_positionEnabled.Value)
		{
			data.Add("PositionX", character.Character.transform.position.x);
			data.Add("PositionY", character.Character.transform.position.y);
		}

		if (_velocityEnabled.Value)
		{
			data.Add("VelocityX", character.RigidBody.velocity.x);
			data.Add("VelocityY", character.RigidBody.velocity.y);
		}

		if (_registeredActionsEnabled.Value)
		{
			string[] actions = character.Inputs
				.Where(a => InputMappings.ContainsKey(a))
				.Select(a => InputMappings[a])
				.OrderBy(a => a)
				.ToArray();
			data.Add("Actions", actions);
		}

		if (_registeredAnalogActionsEnabled.Value)
		{
			Dictionary<string, object> analogActions = character.AnalogInputs
				.Where(a => InputMappings.ContainsKey(a.Key) && Mathf.Abs(a.Value) > 0.1)
				.ToDictionary(a => InputMappings[a.Key], a => (object)a.Value);

			data.Add("AnalogActions", analogActions);
		}

		if (_collidersEnabled.Value)
		{
			data["LeftColliding"] = character.Character.Left.Colliding | character.Character.Left.CollidingWall | character.Character.Left.CollidingHazard;
			data["RightColliding"] = character.Character.Right.Colliding | character.Character.Right.CollidingWall | character.Character.Right.CollidingHazard;
			data["HeadColliding"] = character.Character.Head.Colliding | character.Character.Head.CollidingWall | character.Character.Head.CollidingHazard;
			data["FeetColliding"] = character.Character.Feet.Colliding | character.Character.Feet.CollidingWall | character.Character.Feet.CollidingHazard;
		}

		if (_playerMetaEnabled.Value)
		{
			data["OnGround"] = character.Character.OnGround;
			data["OnWall"] = character.Character.GetField<bool>("onWall");
			data["JustLanded"] = character.Character.GetField<bool>("justLanded");
			data["CanJump"] = character.Character.CanJump;
			data["Jumping"] = character.Character.GetField<bool>("jumping");
			data["LookingUp"] = character.Character.GetField<bool>("lookingUp");
			data["CrouchingDown"] = character.Character.GetField<bool>("crouchingDown");
			data["InCannon"] = character.Character.InCannon;
			data["InBlackHole"] = character.Character.InBlackHole;
			data["Dancing"] = character.Character.GetField<bool>("dancing");
			data["Walking"] = character.Character.GetField<bool>("walking");
			data["Sprinting"] = character.Character.GetField<bool>("sprinting");
		}

		return data;
	}

	private static bool DataChanged(Dictionary<string, object> data1, Dictionary<string, object> data2)
	{
		if (data1.Count != data2.Count)
		{
			Debug.Log("count changed");
			return true;
		}

		foreach (string key in data1.Keys)
		{
			if (key is "Frame" or "Time")
				continue;

			if (!data2.ContainsKey(key))
				return true;

			if (data1[key] is string[] strings1 && data2[key] is string[] strings2)
			{
				if (!strings1.SequenceEqual(strings2))
					return true;
			}
			else if (data1[key] is Dictionary<string, object> dict1 && data2[key] is Dictionary<string, object> dict2)
			{
				if (DataChanged(dict1, dict2))
					return true;
			}
			else if (!data1[key].Equals(data2[key]))
				return true;
		}

		return false;
	}

	private void StartRecording()
	{
		_characters = UchTools.GetCharacters();

		_recordings = _characters.Select(a => new PlayerDataRecording(a)).ToArray();

		_frame = 0;
		_time = 0;
		_recording = true;
	}

	private void SaveRecordings()
	{
		_recording = false;

		try
		{
			string json = JsonConvert.SerializeObject(_recordings, Formatting.Indented);
			string path = Path.Combine(_outputDirectory.Value,
				"UCHRecorder." + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".json");
			File.WriteAllText(path, json, Encoding.UTF8);
			UserMessageManager.Instance.UserMessage("Recording Saved");
		}
		catch (Exception ex)
		{
			UserMessageManager.Instance.UserMessage("Error: " + ex.Message);
			Debug.LogError(ex.Message + ex.StackTrace);
		}
		finally
		{
			ResetRecorder();
		}
	}

	private void ResetRecorder()
	{
		_recording = false;

		if (_characters != null)
		{
			foreach (CharacterWrapper character in _characters)
				character.Dispose();
			_characters = null;
		}
		_recordings = null;
	}

	private static string GetDefaultOutputPath()
	{
		string defaultPath = Path.Combine(Path.GetDirectoryName(typeof(Plugin).Assembly.Location)!, "Recordings");
		defaultPath = defaultPath.Substring(Environment.CurrentDirectory.Length + 1);
		if (!Directory.Exists(defaultPath))
			Directory.CreateDirectory(defaultPath);
		return defaultPath;
	}
}