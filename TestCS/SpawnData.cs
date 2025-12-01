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
				list?.ForEach(data => { refDic.TryAdd(data.Id, data); });
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
				if (item.Value.__State == 0) return;
				State? refItem = null;
				if (false == refDic.TryGetValue(item.Value.__State, out refItem) || refItem == null) return;
				item.Value._State = refItem;
			}
		}

		[JsonProperty("id")]
		public int Id { get; init; } = 0;
		[JsonProperty("name")]
		public string Name { get; init; } = "";
		[JsonProperty("pos")]
		public Vec3 Pos { get; init; } = new Vec3();
		[JsonProperty("resource")]
		public string Resource { get; init; } = "";
		[JsonProperty("state")]
		private int __State { get; init; } = 0;
		[JsonIgnore]
		private State? _State = null;
		public ref readonly State? State => ref _State;
	}

}
