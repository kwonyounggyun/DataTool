using ClosedXML.Excel;
using DataTool.Generator;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.CustomXmlSchemaReferences;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Office2010.Word.DrawingGroup;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Vml;
using DocumentFormat.OpenXml.Wordprocessing;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static DataTool.DataSchema;

namespace DataTool
{
    internal class ExcelReader
    {
        public static ConcurrentDictionary<string, DataSchema> schema = new ConcurrentDictionary<string, DataSchema>();
        public static ConcurrentDictionary<string, RowData> rowDatas = new ConcurrentDictionary<string, RowData>();

        // 파일이 열려있는 상태에서도 툴을 사용할 수 있게하기 위해 메모리에 올린 후 실행
        public void Open(string path)
        {
            excelPath = path;
            using var fs = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite
            );

            using var ms = new MemoryStream();
            fs.CopyTo(ms);
            ms.Position = 0;

            wb = new XLWorkbook(ms);
        }

        public bool ReadSchemaHeader(IXLWorksheet item, out Dictionary<string, SchemaColumn> readColumns)
        {
            bool result = true;
            readColumns = new Dictionary<string, SchemaColumn>();
            foreach (var row in item.RowsUsed())
            {
                foreach (var cell in row.CellsUsed())
                {
                    var colName = cell.GetValue<string>().ToLower();
                    IValidatable outValue;
                    if (false == DataSchema.SchemaHeaderMap.TryGetValue(colName, out outValue))
                        continue;

                    var schemaColumn = new SchemaColumn();
                    schemaColumn.Name = colName;
                    schemaColumn.ColumnNum = cell.WorksheetColumn().ColumnNumber();
                    schemaColumn.Validatable = outValue;
                    if (false == readColumns.TryAdd(schemaColumn.Name, schemaColumn))
                    {
                        Console.WriteLine($"[Error] Duplicate Column Name Detected: {schemaColumn.Name} in Sheet: {item.Name}");
                        result = false;
                    }
                }

                break;
            }

            return result;
        }

        public bool ReadDataHeader(IXLWorksheet item, DataSchema dataSchema, out Dictionary<string, DataColumn> dataHeader)
        {
            bool result = true;
            dataHeader = new Dictionary<string, DataColumn>();
            foreach (var row in item.RowsUsed())
            {
                foreach (var cell in row.CellsUsed())
                {
                    var colName = cell.GetValue<string>().ToLower();
                    var fieldInfo = dataSchema.GetFieldInfo(colName);
                    if (fieldInfo == null)
                        continue;

                    var dataColumn = new DataColumn();
                    dataColumn.Name = colName;
                    dataColumn.ColumnNum = cell.WorksheetColumn().ColumnNumber();
                    dataColumn.TypeId = fieldInfo.TypeId;
                    dataColumn.Required = fieldInfo.Required;
                    dataColumn.Server = fieldInfo.Server;
                    dataColumn.Client = fieldInfo.Client;
                    if (false == dataHeader.TryAdd(dataColumn.Name, dataColumn))
                    {
                        Console.WriteLine($"[Error] Duplicate Column Name Detected: {dataColumn.Name} in Sheet: {item.Name}");
                        result = false;
                    }
                }

                break;
            }

            return result;
        }

        public bool ReadSchema()
        {
            foreach (var item in wb.Worksheets)
            {
                if (item.Name.Contains(' '))
                {
                    Console.WriteLine($"[Error] Do not include blank in sheet name : {item.Name}");
                    return false;
                }

                if (item.Name.ElementAt(0) != '_')
                    continue;

                Dictionary<string, SchemaColumn> readColumns;
                if (false == ReadSchemaHeader(item, out readColumns))
                    return false;

                foreach (var pair in DataSchema.SchemaHeaderMap)
                {
                    if (readColumns.ContainsKey(pair.Key) == false)
                    {
                        Console.WriteLine($"[Error] '{pair.Key}' Column Not Found in Sheet: {item.Name}");
                        return false;
                    }
                }

                DataSchema schemaData = new DataSchema(item.Name.Substring(1));
                bool schemaDataError = false;
                int rowCount = 0;
                foreach (var row in item.RowsUsed())
                {
                    rowCount++;
                    if (rowCount == 1)
                        continue;

                    FieldInfo fieldInfo = new FieldInfo();
                    foreach (var pair in readColumns)
                    {
                        var columnInfo = pair.Value;
                        var cell = row.Cell(columnInfo.ColumnNum);
                        if (false == columnInfo.Validatable.Validate(cell.GetValue<string>()))
                        {
                            Console.WriteLine($"[Error] Wrong Value. Field Name : {pair.Key} Row Num : {row.RowNumber()} in Sheet: {item.Name}");
                            schemaDataError = true;
                            continue;
                        }

                        switch(pair.Key)
                        {
                            case "name":
                                fieldInfo.Name = cell.GetValue<string>().ToLower();
                                break;
                            case "type":
                                fieldInfo.TypeId = DataSchema.TypeMap[cell.GetValue<string>()];
                                break;
                            case "ref":
                                fieldInfo.RefSheetName = cell.GetValue<string>();
                                break;
                            case "required":
                                fieldInfo.Required = cell.GetValue<string>().ToLower() == "true" ? true : false;
                                break;
                            case "server":
                                fieldInfo.Server = cell.GetValue<string>().ToLower() == "true" ? true : false;
                                break;
                            case "client":
                                fieldInfo.Client = cell.GetValue<string>().ToLower() == "true" ? true : false;
                                break;
                        }
                    }

                    if(fieldInfo.TypeId != ValueType.INT && fieldInfo.TypeId != ValueType.LIST && fieldInfo.RefSheetName.Length > 0)
                    {
                        Console.WriteLine($"[Error] Only int and list have to be ref value : Row Num : {row.RowNumber()} in Sheet: {item.Name}");
                        schemaDataError = true;
                        continue;
                    }

                    if (fieldInfo.TypeId == ValueType.LIST && fieldInfo.RefSheetName.Length <= 0)
                    {
                        Console.WriteLine($"[Error] List type have to need ref : Row Num : {row.RowNumber()} in Sheet: {item.Name}");
                        schemaDataError = true;
                        continue;
                    }

                    schemaData.AddFieldInfo(fieldInfo);
                }

                if (schemaDataError)
                    return false;

                if(false == schema.TryAdd(schemaData.SheetName, schemaData))
                    Console.WriteLine($"[Error] Duplicate Schema Name Detected: {item.Name} in Excel: {excelPath}");
            }

            return true;
        }

        public bool ReadData()
        {
            foreach (var item in wb.Worksheets)
            {
                if (item.Name.ElementAt(0) == '_')
                    continue;

                DataSchema? schemaInfo;
                if (false == schema.TryGetValue(item.Name, out schemaInfo))
                {
                    Console.WriteLine($"[Error] Schema Info Not Found. {item.Name} in Excel: {excelPath}");
                    return false;
                }

                Dictionary<string, DataColumn> readDataHeader;
                if (false == ReadDataHeader(item, schemaInfo, out readDataHeader))
                    return false;

                var header = new List<DataColumn>();
                foreach (var pair in readDataHeader)
                    header.Add(pair.Value);

                var rowData = new RowData(item.Name, header);
                int rowCount = 0;
                bool readError = false;
                foreach (var row in item.RowsUsed())
                {
                    rowCount++;
                    if (rowCount == 1)
                        continue;

                    int id = 0;
                    var dataRow = new List<IValue>();
                    foreach(var columnInfo in header)
                    {
                        var cell = row.Cell(columnInfo.ColumnNum);

                        if(columnInfo.Required && cell.IsEmpty())
                        {
                            Console.WriteLine($"[Error] Required field is empty Sheet : {item.Name}, row : {row.RowNumber()}, field : {columnInfo.Name} in Excel: {excelPath}");
                            readError = true;
                            continue;
                        }

                        if (columnInfo.Name.CompareTo("id") == 0)
                        {
                            try
                            {
                                id = cell.GetValue<int>();
                                var tVal = new TValue<int>(id);
                                dataRow.Add(tVal);
                            }
                            catch (InvalidCastException e)
                            {
                                Console.WriteLine($"[Error] Id must be an integer : {item.Name}, row : {row.RowNumber()}, field : {columnInfo.Name} in Excel: {excelPath} Error : {e.Message}");
                                readError = true;
                            }

                            continue;
                        }

                        try
                        {
                            switch (columnInfo.TypeId)
                            {
                                case ValueType.INT:
                                    {
                                        if (cell.IsEmpty())
                                        {
                                            var tVal = new TValue<int>(0);
                                            dataRow.Add(tVal);
                                        }
                                        else
                                        {
                                            var val = cell.GetValue<int>();
                                            var tVal = new TValue<int>(val);
                                            dataRow.Add(tVal);
                                        }
                                    }
                                    break;
                                case ValueType.FLOAT:
                                    {
                                        if (cell.IsEmpty())
                                        {
                                            var tVal = new TValue<float>(0);
                                            dataRow.Add(tVal);
                                        }
                                        else
                                        {
                                            var val = cell.GetValue<float>();
                                            var tVal = new TValue<float>(val);
                                            dataRow.Add(tVal);
                                        }
                                    }
                                    break;
                                case ValueType.STRING:
                                    {
                                        if (cell.IsEmpty())
                                        {
                                            var tVal = new TValue<string>("");
                                            dataRow.Add(tVal);
                                        }
                                        else
                                        {
                                            var val = cell.GetValue<string>();
                                            var tVal = new TValue<string>(val);
                                            dataRow.Add(tVal);
                                        }
                                    }
                                    break;
                                case ValueType.BOOL:
                                    {
                                        if (cell.IsEmpty())
                                        {
                                            var tVal = new TValue<bool>(false);
                                            dataRow.Add(tVal);
                                        }
                                        else
                                        {
                                            var val = cell.GetValue<bool>();
                                            var tVal = new TValue<bool>(val);
                                            dataRow.Add(tVal);
                                        }
                                    }
                                    break;
                                case ValueType.DATETIME:
                                    {
                                        if (cell.IsEmpty())
                                        {
                                            var tVal = new TValue<DateTime>(DateTime.Parse("2025-01-01 00:00:00"));
                                            dataRow.Add(tVal);
                                        }
                                        else
                                        {
                                            var val = cell.GetValue<string>();
                                            var tVal = new TValue<DateTime>(DateTime.Parse(val));
                                            dataRow.Add(tVal);
                                        }
                                    }
                                    break;
                                case ValueType.VEC3:
                                    {
                                        if (cell.IsEmpty())
                                        {
                                            Vec3 vec = new Vec3();
                                            vec.x = 0.0f;
                                            vec.y = 0.0f;
                                            vec.z = 0.0f;
                                            var tVal = new Vec3Value(vec);
                                            dataRow.Add(tVal);
                                        }
                                        else
                                        {
                                            var val = cell.GetValue<string>();
                                            var split = val.Split(',');
                                            if (split.Length != 3)
                                            {
                                                Console.WriteLine($"[Error] Vec3 : {item.Name}, row : {row.RowNumber()}, field : {columnInfo.Name} in Excel: {excelPath}");
                                                readError = true;
                                                continue;
                                            }

                                            Vec3 vec = new Vec3();
                                            vec.x = float.Parse(split[0]);
                                            vec.y = float.Parse(split[1]);
                                            vec.z = float.Parse(split[2]);
                                            var tVal = new Vec3Value(vec);
                                            dataRow.Add(tVal);
                                        }
                                    }
                                    break;
                                case ValueType.VEC2:
                                    {
                                        if (cell.IsEmpty())
                                        {
                                            Vec2 vec = new Vec2();
                                            vec.x = 0.0f;
                                            vec.y = 0.0f;
                                            var tVal = new Vec2Value(vec);
                                            dataRow.Add(tVal);
                                        }
                                        else
                                        {
                                            var val = cell.GetValue<string>();
                                            var split = val.Split(',');
                                            if (split.Length != 2)
                                            {
                                                Console.WriteLine($"[Error] Vec2 : {item.Name}, row : {row.RowNumber()}, field : {columnInfo.Name} in Excel: {excelPath}");
                                                readError = true;
                                                continue;
                                            }

                                            Vec2 vec = new Vec2();
                                            vec.x = float.Parse(split[0]);
                                            vec.y = float.Parse(split[1]);
                                            var tVal = new Vec2Value(vec);
                                            dataRow.Add(tVal);
                                        }
                                    }
                                    break;
                                case ValueType.LIST:
                                    {
                                        List<int> idList = new List<int>();
                                        if (cell.IsEmpty())
                                        {
                                            var tVal = new ListValue(idList);
                                            dataRow.Add(tVal);
                                        }
                                        else
                                        {
                                            var val = cell.GetValue<string>();
                                            var split = val.Split(',');
                                            
                                            foreach (var s in split)
                                                idList.Add(int.Parse(s));

                                            var tVal = new ListValue(idList);
                                            dataRow.Add(tVal);
                                        }
                                    }
                                    break;
                                default:
                                    {
                                        Console.WriteLine($"[Error] No Match Type : {item.Name}, row : {row.RowNumber()}, field : {columnInfo.Name} in Excel: {excelPath}");
                                        readError = true;
                                    }
                                    break;
                            }
                        }
                        catch(InvalidCastException e)
                        {
                            Console.WriteLine($"[Error] Invalid type : {item.Name}, row : {row.RowNumber()}, field : {columnInfo.Name} in Excel: {excelPath}");
                            readError = true;
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"[Error] Error Value : {item.Name}, row : {row.RowNumber()}, field : {columnInfo.Name} in Excel: {excelPath}");
                            readError = true;
                        }
                    }

                    rowData.AddData(id, dataRow);
                }

                if (readError)
                    return false;

                while (false == rowDatas.TryAdd(rowData.SheetName, rowData))
                {
                    if (true == rowDatas.TryGetValue(rowData.SheetName, out var outData))
                        outData.AddData(rowData);
                }
            }

            return true;
        }

        public static void MakeCPP(string outFilePath, string usingNamespace, bool server = false)
        {
            var generator = new CPPGenerator();
            generator.Generate(outFilePath, usingNamespace, schema, server);
        }
        public static void MakeCSharp(string outFilePath, string usingNamespace, bool server = false)
        {
            var generator = new CSharpGenerator();
            generator.Generate(outFilePath, usingNamespace, schema, server);
        }
        public static void MakeJson(string outDirPath, bool server = false)
        {
            foreach (var pair in schema)
            {
                var s = pair.Value;
                RowData? data;
                rowDatas.TryGetValue(s.SheetName, out data);
                if (data == null)
                    continue;

                var outJson = outDirPath + "/" + data.SheetName + ".json";
                try
                {
                    // 파일에 content 내용을 씁니다. 파일이 이미 있다면 덮어씁니다.
                    File.WriteAllText(outJson, data.MakeJson(server));
                    Console.WriteLine($"파일 쓰기 완료: {outJson}");
                }
                catch (IOException ex)
                {
                    Console.WriteLine($"{outJson} 파일 쓰기 오류: {ex.Message}");
                }
            }
        }

        private XLWorkbook? wb;
        private string? excelPath;
    }
}
