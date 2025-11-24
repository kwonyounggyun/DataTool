// See https://aka.ms/new-console-template for more information
using DataTool;
using System.Collections.Concurrent;

Console.WriteLine("Hello, World!");
ExcelReader reader = new ExcelReader();
reader.Open("D:/Projects/Test.xlsx");

ConcurrentDictionary<string, DataSchema> schema = new ConcurrentDictionary<string, DataSchema>();
reader.ReadSchema();

reader.ReadData();