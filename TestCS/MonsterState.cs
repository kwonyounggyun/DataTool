using Newtonsoft.Json;
namespace GameData
{
	public sealed class MonsterState
	{
		public static bool Load(string fileDir, out Dictionary<int, MonsterState> dic)
		{
			dic = new Dictionary<int, MonsterState>();
			var refDic = dic;
			string filePath = fileDir + "/MonsterState.json";
			try {
				string fileContent = File.ReadAllText(filePath);
				var list = JsonConvert.DeserializeObject<List<MonsterState>>(fileContent);
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

		public static void LinkParams(ref Dictionary<int, MonsterState> dic, IReadOnlyDictionary<int, State> refDic)
		{
			foreach (var item in dic)
			{
				item.Value.__Params?.ForEach(data => {
					State? refItem = null;
					if (false == refDic.TryGetValue(data, out refItem) || refItem == null) return;
					item.Value._Params.TryAdd(refItem.ID, refItem);
				});
			}
		}

		[JsonProperty("ID")]
		public int ID { get; init; } = 0;
		[JsonProperty("Params")]
		private List<int>? __Params;
		[JsonIgnore]
		private Dictionary<int, State> _Params = new Dictionary<int, State>();
		[JsonIgnore]
		public IReadOnlyDictionary<int, State> Params { get { return _Params; } }
	}

}
