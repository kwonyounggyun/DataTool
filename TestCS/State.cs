using Newtonsoft.Json;
namespace GameData
{
	public sealed class State
	{
		public static bool Load(ref string fileDir, out Dictionary<int, State> dic)
		{
			dic = new Dictionary<int, State>();
			string filePath = fileDir + "/State.json";
			try {
				string fileContent = File.ReadAllText(filePath);
				var list = JsonConvert.DeserializeObject<List<State>>(fileContent);
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
		[JsonProperty("type")]
		public int Type { get; set; }
		[JsonProperty("value")]
		public int Value { get; set; }
	}

}
