using ClosedXML.Excel;
using DocumentFormat.OpenXml.CustomXmlSchemaReferences;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Office2010.Word.DrawingGroup;
using DocumentFormat.OpenXml.Spreadsheet;
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

        struct SchemaColumn
        {
            public string Name;
            public int ColumnNum;
            public IValidatable Validatable;
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

                bool schemaHeaderError = false;
                Dictionary<string, SchemaColumn> readColumns = new Dictionary<string, SchemaColumn>();
                foreach (var row in item.RowsUsed())
                {
                    for (var i = 0; i < row.CellCount(); i++)
                    {
                        var cell = row.Cell(i + 1);
                        var colName = cell.GetValue<string>().ToLower();
                        IValidatable outValue;
                        if (false == DataSchema.SchemaHeaderMap.TryGetValue(colName, out outValue))
                            continue;

                        var schemaColumn = new SchemaColumn();
                        schemaColumn.Name = colName;
                        schemaColumn.ColumnNum = i + 1;
                        schemaColumn.Validatable = outValue;
                        if (false == readColumns.TryAdd(schemaColumn.Name, schemaColumn))
                            Console.WriteLine($"[Error] Duplicate Column Name Detected: {schemaColumn.Name} in Sheet: {item.Name}");
                    }

                    break;
                }

                if (schemaHeaderError)
                    return false;

                foreach (var pair in DataSchema.SchemaHeaderMap)
                {
                    if (readColumns.ContainsKey(pair.Key) == false)
                    {
                        Console.WriteLine($"[Error] '{pair.Key}' Column Not Found in Sheet: {item.Name}");
                        return false;
                    }
                }

                DataSchema schemaData = new DataSchema();
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
                            continue;
                        }

                        switch(pair.Key)
                        {
                            case "name":
                                fieldInfo.Name = cell.GetValue<string>();
                                break;
                            case "type":
                                fieldInfo.TypeId = DataSchema.TypeMap[cell.GetValue<string>()];
                                break;
                            case "ref":
                                var value = cell.GetValue<string>();
                                if (value.Length > 0)
                                {
                                    var subs = cell.GetValue<string>().Split(':');
                                    fieldInfo.RefSheetName = subs[0];
                                    fieldInfo.RefFieldName = subs[1];
                                }
                                break;
                            case "required":
                                fieldInfo.Required = cell.GetValue<string>() == "true" ? true : false;
                                break;
                        }
                    }

                    schemaData.AddFieldInfo(fieldInfo);
                }

                if (schemaDataError)
                    return false;

                if(false == schema.TryAdd(item.Name.Substring(1), schemaData))
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

                DataSchema schemaInfo;
                if (false == schema.TryGetValue(item.Name, out schemaInfo))
                {
                    Console.WriteLine($"[Error] Schema Info Not Found. {item.Name} in Excel: {excelPath}");
                    return false;
                }

                //foreach (var row in item.RowsUsed())
                //{
                //    for (var i = 0; i < row.CellCount(); i++)
                //    {
                //        var cell = row.Cell(i + 1);
                //        var colName = cell.GetValue<string>().ToLower();
                //        IValidatable outValue;
                //        if (false == DataSchema.SchemaHeaderMap.TryGetValue(colName, out outValue))
                //            continue;

                //        var schemaColumn = new SchemaColumn();
                //        schemaColumn.Name = colName;
                //        schemaColumn.ColumnNum = i + 1;
                //        schemaColumn.Validatable = outValue;
                //        if (false == readColumns.TryAdd(schemaColumn.Name, schemaColumn))
                //            Console.WriteLine($"[Error] Duplicate Column Name Detected: {schemaColumn.Name} in Sheet: {item.Name}");
                //    }

                //    break;
                //}
            }

            return true;
        }

        private XLWorkbook? wb;
        private string? excelPath;
    }
}
