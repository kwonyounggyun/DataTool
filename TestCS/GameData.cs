using System.Collections.Generic;
using Newtonsoft.Json;
namespace GameData
{
	public struct Vec3
	{
		[JsonProperty("x")]
		public float X { get; set; }
		[JsonProperty("y")]
		public float Y { get; set; }
		[JsonProperty("z")]
		public float Z { get; set; }
	}

	struct Vec2
	{
		[JsonProperty("x")]
		float X { get; set; }
		[JsonProperty("y")]
		float Y { get; set; }
	}

}
