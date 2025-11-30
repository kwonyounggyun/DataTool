using Newtonsoft.Json;
namespace GameData
{
	public sealed class MonsterState
	{
		public static bool Load(ref string fileDir, out Dictionary<int, MonsterState> dic)
		{
			dic = new Dictionary<int, MonsterState>();
			string filePath = fileDir + "/MonsterState.json";
			try {
				string fileContent = File.ReadAllText(filePath);
				var list = JsonConvert.DeserializeObject<List<MonsterState>>(fileContent);
				foreach (var item in list)
					dic.Add(item.Id, item);
			} catch (FileNotFoundException) {
				Console.WriteLine($"FileNotFound: {filePath}");
				return false;
			} catch (Exception ex) {
				Console.WriteLine($"{filePath} read error: {ex.Message}");
				return false;
			}
			return true;
		}

		[JsonProperty("id")]
		public int Id { get; set; }
		[JsonProperty("params")]
		public List<int> _Params { get; set; }
		[JsonIgnore]
		public Dictionary<int, State> Params { get; set; }
	}

}
