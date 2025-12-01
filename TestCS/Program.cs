// See https://aka.ms/new-console-template for more information
using GameData;

Console.WriteLine("Hello, World!");


Dictionary<int, GameData.Event> dic;
var path = Path.GetFullPath(".");
GameData.Event.Load(path, out dic);

StaticData data = new StaticData();
data.Load("D:\\Projects\\DataTool\\TestCS");

Console.WriteLine("Hello, World!");