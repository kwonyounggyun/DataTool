// See https://aka.ms/new-console-template for more information
Console.WriteLine("Hello, World!");


Dictionary<int, GameData.SpawnData> dic;
var path = Path.GetFullPath(".");
GameData.SpawnData.Load(ref path, out dic);

Console.WriteLine("Hello, World!");