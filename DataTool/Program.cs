// See https://aka.ms/new-console-template for more information
using DataTool;
using System.Collections.Concurrent;

Console.WriteLine("Hello, World!");
ExcelReader reader = new ExcelReader();
reader.Open("D:/Projects/Test.xlsx");

ConcurrentDictionary<string, DataSchema> schema = new ConcurrentDictionary<string, DataSchema>();
reader.ReadSchema();

reader.ReadData();

string outpath = Path.GetFullPath(".");
string nameSpace = "GameData";

string serverPath = outpath + "/Server";
string clientPath = outpath + "/Client";
Directory.CreateDirectory(serverPath);
Directory.CreateDirectory(clientPath);
ExcelReader.MakeCPP(ref serverPath, ref nameSpace, true);
ExcelReader.MakeCPP(ref clientPath, ref nameSpace, false);
ExcelReader.MakeCSharp(ref serverPath, ref nameSpace, true);
ExcelReader.MakeCSharp(ref clientPath, ref nameSpace, false);