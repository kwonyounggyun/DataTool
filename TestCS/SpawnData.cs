using Newtonsoft.Json;
namespace GameData
{
	public sealed class SpawnData
	{
		public static bool Load(ref string fileDir, out Dictionary<int, SpawnData> dic)
		{
			dic = new Dictionary<int, SpawnData>();
			string filePath = fileDir + "/SpawnData.json";
			try {
				string fileContent = File.ReadAllText(filePath);
				var list = JsonConvert.DeserializeObject<List<SpawnData>>(fileContent);
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
		[JsonProperty("name")]
		public string Name { get; set; }
		[JsonProperty("pos")]
		public Vec3 Pos { get; set; }
		[JsonProperty("state")]
		public int _State { get; set; }
		[JsonIgnore]
		public State State { get; set; }
	}

}
