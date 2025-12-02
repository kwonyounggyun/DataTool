using ClosedXML.Excel;
using DataTool.Generator;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.CustomXmlSchemaReferences;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Office2010.PowerPoint;
using DocumentFormat.OpenXml.Office2010.Word.DrawingGroup;
using DocumentFormat.OpenXml.Office2013.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Vml;
using DocumentFormat.OpenXml.Wordprocessing;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using static DataTool.DataSchema;
using static System.Reflection.Metadata.BlobBuilder;

namespace DataTool
{
    public class ConcurrentDataMap<T> : IEnumerable<KeyValuePair<string, T>>
    {
        public bool Find(string key, out T? value)
        {
            return schema.TryGetValue(key.ToLower(), out value);
        }

        public bool Add(string key, T item)
        {
            return schema.TryAdd(key.ToLower(), item);
        }

        IEnumerator<KeyValuePair<string, T>> IEnumerable<KeyValuePair<string, T>>.GetEnumerator()
        {
            return schema.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return schema.GetEnumerator();
        }

        public int Count { get { return schema.Count; } }
        private ConcurrentDictionary<string, T> schema = new ConcurrentDictionary<string, T>();
    }

    public class DataMap<T> : IEnumerable<KeyValuePair<string, T>>
    {
        public bool Find(string key, out T? value)
        {
            return schema.TryGetValue(key.ToLower(), out value);
        }

        public bool Add(string key, T item)
        {
            return schema.TryAdd(key.ToLower(), item);
        }

        IEnumerator<KeyValuePair<string, T>> IEnumerable<KeyValuePair<string, T>>.GetEnumerator()
        {
            return schema.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return schema.GetEnumerator();
        }

        public int Count { get { return schema.Count; } }
        private Dictionary<string, T> schema = new Dictionary<string, T>();
    }

    internal class ExcelReader
    {
        public static ConcurrentDataMap<DataSchema> schema = new ConcurrentDataMap<DataSchema>();
        public static ConcurrentDataMap<RowData> rowDatas = new ConcurrentDataMap<RowData>();

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

        public bool ReadSchemaHeader(IXLWorksheet item, out DataMap<SchemaColumn> readColumns)
        {
            bool result = true;
            readColumns = new DataMap<SchemaColumn> ();
            foreach (var row in item.RowsUsed())
            {
                foreach (var cell in row.CellsUsed())
                {
                    var colName = cell.GetValue<string>();
                    IValidatable outValue;
                    if (false == DataSchema.SchemaHeaderMap.TryGetValue(colName.ToLower(), out outValue))
                        continue;

                    var schemaColumn = new SchemaColumn();
                    schemaColumn.Name = colName;
                    schemaColumn.ColumnNum = cell.WorksheetColumn().ColumnNumber();
                    schemaColumn.Validatable = outValue;
                    if (false == readColumns.Add(schemaColumn.Name, schemaColumn))
                    {
                        Console.WriteLine($"[Error] Duplicate Column Name Detected: {schemaColumn.Name} in Sheet: {item.Name}");
                        result = false;
                    }
                }

                break;
            }

            return result;
        }

        public bool ReadDataHeader(IXLWorksheet item, DataSchema dataSchema, out DataMap<DataColumn> dataHeader)
        {
            bool result = true;
            dataHeader = new DataMap<DataColumn>();
            foreach (var row in item.RowsUsed())
            {
                foreach (var cell in row.CellsUsed())
                {
                    var colName = cell.GetValue<string>();
                    var fieldInfo = dataSchema.GetFieldInfo(colName);
                    if (fieldInfo == null)
                        continue;

                    if (fieldInfo.Name.CompareTo(colName) != 0)
                    {
                        Console.WriteLine($"[Error] Field name \'{colName}\' not match for schema field name \'{fieldInfo.Name}\' : in Sheet: {item.Name}");
                        result = false;
                        continue;
                    }

                    var dataColumn = new DataColumn();
                    dataColumn.Index = fieldInfo.Index;
                    dataColumn.Name = colName;
                    dataColumn.ColumnNum = cell.WorksheetColumn().ColumnNumber();
                    dataColumn.TypeId = fieldInfo.TypeId;
                    dataColumn.Container = fieldInfo.Container;
                    dataColumn.Required = fieldInfo.Required;
                    dataColumn.Server = fieldInfo.Server;
                    dataColumn.Client = fieldInfo.Client;
                    if (false == dataHeader.Add(dataColumn.Name, dataColumn))
                    {
                        Console.WriteLine($"[Error] Duplicate Column Name Detected: {dataColumn.Name} in Sheet: {item.Name}");
                        result = false;
                    }
                }

                break;
            }

            if(dataHeader.Count != dataSchema.FieldInfos.Count)
            {
                foreach (var field in dataSchema.FieldInfos)
                {
                    DataColumn find;
                    if (false == dataHeader.Find(field.Key, out find))
                    {
                        Console.WriteLine($"[Error] Field Name \'{field.Value.Name}\' not in Sheet: {item.Name}");
                        result = false;
                        continue;
                    }

                    if(field.Value.Name.CompareTo(find.Name) != 0)
                    {
                        Console.WriteLine($"[Error] Field Name \'{find.Name}\' not match shema field name \'{field.Value.Name}\' in Sheet: {item.Name}");
                        result = false;
                    }
                }
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

                DataMap<SchemaColumn> readColumns;
                if (false == ReadSchemaHeader(item, out readColumns))
                    return false;

                foreach (var pair in DataSchema.SchemaHeaderMap)
                {
                    if (readColumns.Find(pair.Key, out var field) == false)
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
                            case "index":
                                fieldInfo.Index = cell.GetValue<int>();
                                break;
                            case "name":
                                fieldInfo.Name = cell.GetValue<string>();
                                break;
                            case "type":
                                fieldInfo.TypeId = DataSchema.TypeMap[cell.GetValue<string>()];
                                break;
                            case "container":
                                fieldInfo.Container = cell.GetValue<string>().ToLower() == "true" ? true : false;
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

                    if(fieldInfo.TypeId != ValueType.INT && fieldInfo.RefSheetName.Length > 0)
                    {
                        Console.WriteLine($"[Error] Only int have to be ref value : Row Num : {row.RowNumber()} in Sheet: {item.Name}");
                        schemaDataError = true;
                        continue;
                    }

                    if (fieldInfo.Container == true &&
                        fieldInfo.TypeId != ValueType.INT &&
                        fieldInfo.TypeId != ValueType.FLOAT &&
                        fieldInfo.TypeId != ValueType.STRING &&
                        fieldInfo.TypeId != ValueType.BOOL)
                    {
                        Console.WriteLine($"[Error] Container have to be int, float, string, bool : Row Num : {row.RowNumber()} in Sheet: {item.Name}");
                        schemaDataError = true;
                        continue;
                    }

                    schemaData.AddFieldInfo(fieldInfo);
                }

                if (schemaDataError)
                    return false;

                if(false == schema.Add(schemaData.SheetName, schemaData))
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
                if (false == schema.Find(item.Name, out schemaInfo) || schemaInfo == null)
                {
                    Console.WriteLine($"[Error] Schema Info Not Found. {item.Name} in Excel: {excelPath}");
                    return false;
                }

                DataMap<DataColumn> readDataHeader;
                if (false == ReadDataHeader(item, schemaInfo, out readDataHeader))
                    return false;

                var header = new List<DataColumn>();
                foreach (var pair in readDataHeader)
                    header.Add(pair.Value);

                header.Sort((a, b) => a.Index.CompareTo(b.Index));

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

                        if (columnInfo.Name.ToLower().CompareTo("id") == 0)
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
                            if (!columnInfo.Container)
                            {
                                switch (columnInfo.TypeId)
                                {
                                    case ValueType.INT:
                                        {
                                            if (!columnInfo.Container)
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
                                            else
                                            {
                                                List<int> list = new List<int>();
                                                if (cell.IsEmpty())
                                                {
                                                    var tVal = new ListValue<int>(list);
                                                    dataRow.Add(tVal);
                                                }
                                                else
                                                {
                                                    var val = cell.GetValue<string>();
                                                    var split = val.Split(',');

                                                    foreach (var s in split)
                                                        list.Add(int.Parse(s));

                                                    var tVal = new ListValue<int>(list);
                                                    dataRow.Add(tVal);
                                                }
                                            }
                                        }
                                        break;
                                    case ValueType.FLOAT:
                                        {
                                            if (!columnInfo.Container)
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
                                            else
                                            {
                                                List<float> list = new List<float>();
                                                if (cell.IsEmpty())
                                                {
                                                    var tVal = new ListValue<float>(list);
                                                    dataRow.Add(tVal);
                                                }
                                                else
                                                {
                                                    var val = cell.GetValue<string>();
                                                    var split = val.Split(',');

                                                    foreach (var s in split)
                                                        list.Add(float.Parse(s));

                                                    var tVal = new ListValue<float>(list);
                                                    dataRow.Add(tVal);
                                                }
                                            }
                                        }
                                        break;
                                    case ValueType.STRING:
                                        {
                                            if (!columnInfo.Container)
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
                                            else
                                            {
                                                List<string> list = new List<string>();
                                                if (cell.IsEmpty())
                                                {
                                                    var tVal = new ListValue<string>(list);
                                                    dataRow.Add(tVal);
                                                }
                                                else
                                                {
                                                    var val = cell.GetValue<string>();
                                                    var split = val.Split(',');

                                                    foreach (var s in split)
                                                        list.Add(s);

                                                    var tVal = new ListValue<string>(list);
                                                    dataRow.Add(tVal);
                                                }
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
                                    default:
                                        {
                                            Console.WriteLine($"[Error] No Match Type : {item.Name}, row : {row.RowNumber()}, field : {columnInfo.Name} in Excel: {excelPath}");
                                            readError = true;
                                        }
                                        break;
                                }
                            }
                            else
                            {
                                switch (columnInfo.TypeId)
                                {
                                    case ValueType.INT:
                                        {
                                            List<int> list = new List<int>();
                                            if (cell.IsEmpty())
                                            {
                                                var tVal = new ListValue<int>(list);
                                                dataRow.Add(tVal);
                                            }
                                            else
                                            {
                                                var val = cell.GetValue<string>();
                                                var split = val.Split(',');

                                                foreach (var s in split)
                                                    list.Add(int.Parse(s));

                                                var tVal = new ListValue<int>(list);
                                                dataRow.Add(tVal);
                                            }
                                        }
                                        break;
                                    case ValueType.FLOAT:
                                        {
                                            List<float> list = new List<float>();
                                            if (cell.IsEmpty())
                                            {
                                                var tVal = new ListValue<float>(list);
                                                dataRow.Add(tVal);
                                            }
                                            else
                                            {
                                                var val = cell.GetValue<string>();
                                                var split = val.Split(',');

                                                foreach (var s in split)
                                                    list.Add(float.Parse(s));

                                                var tVal = new ListValue<float>(list);
                                                dataRow.Add(tVal);
                                            }
                                        }
                                        break;
                                    case ValueType.STRING:
                                        {
                                            List<string> list = new List<string>();
                                            if (cell.IsEmpty())
                                            {
                                                var tVal = new ListValue<string>(list);
                                                dataRow.Add(tVal);
                                            }
                                            else
                                            {
                                                var val = cell.GetValue<string>();
                                                var split = val.Split(',');

                                                foreach (var s in split)
                                                    list.Add(s);

                                                var tVal = new ListValue<string>(list);
                                                dataRow.Add(tVal);
                                            }
                                        }
                                        break;
                                    case ValueType.BOOL:
                                        {
                                            List<bool> list = new List<bool>();
                                            if (cell.IsEmpty())
                                            {
                                                var tVal = new ListValue<bool>(list);
                                                dataRow.Add(tVal);
                                            }
                                            else
                                            {
                                                var val = cell.GetValue<string>();
                                                var split = val.Split(',');

                                                foreach (var s in split)
                                                    list.Add(bool.Parse(s));

                                                var tVal = new ListValue<bool>(list);
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

                while (false == rowDatas.Add(rowData.SheetName, rowData))
                {
                    if (true == rowDatas.Find(rowData.SheetName, out var outData))
                        outData?.AddData(rowData);
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
                rowDatas.Find(s.SheetName, out data);
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
