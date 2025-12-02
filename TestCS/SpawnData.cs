using Newtonsoft.Json;
namespace GameData
{
	public sealed class SpawnData
	{
		public static bool Load(string fileDir, out Dictionary<int, SpawnData> dic)
		{
			dic = new Dictionary<int, SpawnData>();
			var refDic = dic;
			string filePath = fileDir + "/SpawnData.json";
			try {
				string fileContent = File.ReadAllText(filePath);
				var list = JsonConvert.DeserializeObject<List<SpawnData>>(fileContent);
				list?.ForEach(data => { refDic.TryAdd(data.ID, data); });
			} catch (FileNotFoundException) {
				Console.WriteLine($"FileNotFound: {filePath}");
				return false;
			} catch (Exception ex) {
				Console.WriteLine($"{filePath} read error: {ex.Message}");
				return false;
			}
			return true;
		}

		public static void LinkState(ref Dictionary<int, SpawnData> dic, IReadOnlyDictionary<int, State> refDic)
		{
			foreach (var item in dic)
			{
				item.Value.__State?.ForEach(data => {
					State? refItem = null;
					if (false == refDic.TryGetValue(data, out refItem) || refItem == null) return;
					item.Value._State.TryAdd(refItem.ID, refItem);
				});
			}
		}

		[JsonProperty("ID")]
		public int ID { get; init; } = 0;
		[JsonProperty("Name")]
		public string Name { get; init; } = "";
		[JsonProperty("Pos")]
		public Vec3 Pos { get; init; } = new Vec3();
		[JsonProperty("State")]
		private List<int>? __State;
		[JsonIgnore]
		private Dictionary<int, State> _State = new Dictionary<int, State>();
		[JsonIgnore]
		public IReadOnlyDictionary<int, State> State { get { return _State; } }
		[JsonProperty("Switch")]
		private List<bool> _Switch = new List<bool>();
		[JsonIgnore]
		public IReadOnlyList<bool> Switch { get { return _Switch; } }
		[JsonProperty("Childs")]
		private List<string> _Childs = new List<string>();
		[JsonIgnore]
		public IReadOnlyList<string> Childs { get { return _Childs; } }
		[JsonProperty("Values")]
		private List<float> _Values = new List<float>();
		[JsonIgnore]
		public IReadOnlyList<float> Values { get { return _Values; } }
	}

}
