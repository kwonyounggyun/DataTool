// See https://aka.ms/new-console-template for more information
using DataTool;
using System.Collections.Concurrent;
using System.ComponentModel;

Console.WriteLine("Hello, World!");
ExcelReader reader = new ExcelReader();
reader.Open("D:/Projects/Test.xlsx");

ConcurrentDictionary<string, DataSchema> schema = new ConcurrentDictionary<string, DataSchema>();
reader.ReadSchema();

string outpath = Path.GetFullPath(".");
string nameSpace = "GameData";

reader.ReadData();

string serverPath = outpath + "/Server";
string clientPath = outpath + "/Client";
string serverJsonPath = outpath + "/ServerJson";
string clientJsonPath = outpath + "/ClientJson";
Directory.CreateDirectory(serverPath);
Directory.CreateDirectory(clientPath);
Directory.CreateDirectory(serverJsonPath);
Directory.CreateDirectory(clientJsonPath);
ExcelReader.MakeCPP(serverPath, nameSpace, true);
ExcelReader.MakeCPP(clientPath, nameSpace, false);
ExcelReader.MakeCSharp(serverPath, nameSpace, true);
ExcelReader.MakeCSharp(clientPath, nameSpace, false);
ExcelReader.MakeJson(serverJsonPath, true);
ExcelReader.MakeJson(clientJsonPath, false);
