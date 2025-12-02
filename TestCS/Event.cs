using Newtonsoft.Json;
namespace GameData
{
	public sealed class Event
	{
		public static bool Load(string fileDir, out Dictionary<int, Event> dic)
		{
			dic = new Dictionary<int, Event>();
			var refDic = dic;
			string filePath = fileDir + "/Event.json";
			try {
				string fileContent = File.ReadAllText(filePath);
				var list = JsonConvert.DeserializeObject<List<Event>>(fileContent);
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
		[JsonProperty("time")]
		public DateTime time { get; init; } = DateTime.Now.AddYears(-125);
		[JsonProperty("value")]
		public int value { get; init; } = 0;
	}

}
