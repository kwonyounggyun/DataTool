using System.Collections.Generic;
using Newtonsoft.Json;
namespace GameData
{
	public class Vec3
	{
		[JsonProperty("x")]
		public float X { get; init; } = 0;
		[JsonProperty("y")]
		public float Y { get; init; } = 0;
		[JsonProperty("z")]
		public float Z { get; init; } = 0;
	}

	public class Vec2
	{
		[JsonProperty("x")]
		float X { get; init; } = 0;
		[JsonProperty("y")]
		float Y { get; init; } = 0;
	}

	public class StaticData
	{
		public void Load(string jsonDir)
		{
			GameData.SpawnData.Load(jsonDir, out _SpawnData);
			GameData.State.Load(jsonDir, out _State);
			GameData.MonsterState.Load(jsonDir, out _MonsterState);
			GameData.Event.Load(jsonDir, out _Event);
			GameData.SpawnData.LinkState(ref _SpawnData, _State);
			GameData.MonsterState.LinkParams(ref _MonsterState, _State);
		}

		private Dictionary<int, GameData.SpawnData> _SpawnData;
		public IReadOnlyDictionary<int, GameData.SpawnData> SpawnData { get { return _SpawnData; } }
		private Dictionary<int, GameData.State> _State;
		public IReadOnlyDictionary<int, GameData.State> State { get { return _State; } }
		private Dictionary<int, GameData.MonsterState> _MonsterState;
		public IReadOnlyDictionary<int, GameData.MonsterState> MonsterState { get { return _MonsterState; } }
		private Dictionary<int, GameData.Event> _Event;
		public IReadOnlyDictionary<int, GameData.Event> Event { get { return _Event; } }
	}

}
