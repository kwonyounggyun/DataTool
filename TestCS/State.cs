using Newtonsoft.Json;
namespace GameData
{
	public sealed class State
	{
		public static bool Load(string fileDir, out Dictionary<int, State> dic)
		{
			dic = new Dictionary<int, State>();
			var refDic = dic;
			string filePath = fileDir + "/State.json";
			try {
				string fileContent = File.ReadAllText(filePath);
				var list = JsonConvert.DeserializeObject<List<State>>(fileContent);
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

		[JsonProperty("ID")]
		public int ID { get; init; } = 0;
		[JsonProperty("Type")]
		public int Type { get; init; } = 0;
		[JsonProperty("Value")]
		public int Value { get; init; } = 0;
	}

}
