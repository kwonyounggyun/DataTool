using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.EMMA;
using DocumentFormat.OpenXml.Office.PowerPoint.Y2021.M06.Main;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Threading.Tasks;

namespace DataTool
{
    public interface IValidatable
    {
        bool Validate(string value);
    }

    public class ValidInt : IValidatable
    {
        public bool Validate(string value)
        {
            try
            {
                Convert.ToInt32(value);
            }
            catch
            {
                return false;
            }

            return true;
        }
    }

    public class ValidString : IValidatable
    {
        public bool Validate(string value)
        {
            return true;
        }
    }

    public class ValidType : IValidatable
    {
        public bool Validate(string value)
        {
            var lower = value.ToLower();
            return DataSchema.TypeMap.ContainsKey(lower);
        }
    }

    public class ValidReference : IValidatable
    {
        public bool Validate(string value)
        {
            return true;
        }
    }

    public class ValidBool : IValidatable
    {
        public bool Validate(string value)
        {
            var lower = value.ToLower();
            return lower.CompareTo("true") == 0 || lower.CompareTo("false") == 0;
        }
    }

    class FieldInfo
    {
        public int Index { get; set; } = 0;
        public string Name { get; set; } = "";
        public ValueType TypeId { get; set; } = 0;
        public bool Server { get; set; } = false;
        public bool Client { get; set; } = false;
        public string RefSheetName { get; set; } = "";
        public bool Required { get; set; } = false;

        public void GetCPP(ref StringBuilder sb, string indent)
        {
            var newName = char.ToUpper(Name[0]) + Name.Substring(1);
            switch (TypeId)
            {
                case ValueType.INT:
                    sb.Append($"{indent}int {newName} = 0;\r\n");
                    break;
                case ValueType.FLOAT:
                    sb.Append($"{indent}float {newName} = 0.0f;\r\n");
                    break;
                case ValueType.STRING:
                    sb.Append($"{indent}string {newName} = \"\";\r\n");
                    break;
                case ValueType.BOOL:
                    sb.Append($"{indent}bool {newName} = false;\r\n");
                    break;
                case ValueType.DATETIME:
                    sb.Append($"{indent}std::tm {newName};\r\n");
                    break;
                case ValueType.VEC3:
                    sb.Append($"{indent}Vec3 {newName};\r\n");
                    break;
                case ValueType.LIST:
                    sb.Append($"{indent}std::map<int, const {RefSheetName}*> {newName};\r\n");
                    break;
            }
        }

        public void GetJsonParseCPP(ref StringBuilder sb, string indent)
        {
            var newName = char.ToUpper(Name[0]) + Name.Substring(1);
            switch (TypeId)
            {
                case ValueType.INT:
                    sb.Append($"{indent}dataObj.{newName} = j.at(\"{Name}\").get<int>();\r\n");
                    break;
                case ValueType.FLOAT:
                    sb.Append($"{indent}dataObj.{newName} = j.at(\"{Name}\").get<float>();\r\n");
                    break;
                case ValueType.STRING:
                    sb.Append($"{indent}dataObj.{newName} = j.at(\"{Name}\").get<std::string>();;\r\n");
                    break;
                case ValueType.BOOL:
                    sb.Append($"{indent}dataObj.{newName} = j.at(\"{Name}\").get<bool>();\r\n");
                    break;
                case ValueType.DATETIME:
                    sb.Append($"{indent}dataObj.{newName} = j.at(\"{Name}\").get<bool>();\r\n");
                    break;
                case ValueType.VEC3:
                    sb.Append($"{indent}dataObj.{newName} = j.at(\"{Name}\").get<bool>();\r\n");
                    break;
                case ValueType.LIST:
                    sb.Append($"{indent}{{\r\n");
                    sb.Append($"{indent + "\t"}auto ids = j.at(\"{Name}\").get<std::list<int>>();\r\n");
                    sb.Append($"{indent + "\t"}for(auto id : ids) dataObj.{newName}[id] = nullptr;\r\n");
                    sb.Append($"{indent}}}\r\n");
                    break;
            }
        }
    }

    public enum ValueType
    {
        NONE,
        INT,
        FLOAT,
        STRING,
        BOOL,
        DATETIME,
        VEC3,
        LIST,
    }
    public struct SchemaColumn
    {
        public string Name;
        public int ColumnNum;
        public IValidatable Validatable;
    }

    public struct DataColumn
    {
        public string Name;
        public int ColumnNum;
        public ValueType TypeId;
        public bool required;
    }

    public struct Vec3
    {
        public float x, y, z;
    }

    class DataSchema
    {
        public DataSchema(string sheetName)
        {
            SheetName = sheetName;
        }

        public static Dictionary<string, ValueType> TypeMap = new()
        {
            { "int", ValueType.INT },
            { "float", ValueType.FLOAT },
            { "string", ValueType.STRING },
            { "bool", ValueType.BOOL },
            { "datetime", ValueType.DATETIME },
            { "vec3", ValueType.VEC3 },
            { "list", ValueType.LIST },
        };

        public static Dictionary<string, IValidatable> SchemaHeaderMap = new()
        {
            { "id", new ValidInt() },
            { "name", new ValidString() },
            { "type", new ValidType() },
            { "required", new ValidBool() },
            { "ref", new ValidReference() },
            { "server", new ValidBool() },
            { "client", new ValidBool() },
        };

        public bool AddFieldInfo(FieldInfo info)
        {
            FieldInfo? findInfo;
            if (true == SchemaInfo.TryGetValue(info.Name, out findInfo))
                return false;

            SchemaInfo.Add(info.Name, info);
            return true;
        }

        public FieldInfo? GetFieldInfo(string name)
        {
            FieldInfo? findInfo;
            SchemaInfo.TryGetValue(name, out findInfo);

            return findInfo;
        }

        public void GetCPP(ref StringBuilder sb, int indentCount, bool server = false)
        {
            string indent = "";
            for (int i = 0; i < indentCount; i++)
                indent += "\t";

            sb.Append($"{indent}class {SheetName}\r\n");
            sb.Append($"{indent}{{\r\n");
            sb.Append($"{indent}public:\r\n");
            foreach (var pair in SchemaInfo)
            {
                var field = pair.Value;
                if (server)
                {
                    if (field.Server == false)
                        continue;

                    field.GetCPP(ref sb, indent + "\t");
                }
                else
                {
                    if (field.Client == false)
                        continue;

                    field.GetCPP(ref sb, indent + "\t");
                }
            }

            sb.Append($"{indent}}};\r\n\r\n");
        }

        public void GetJsonParseCPP(ref StringBuilder jsonParser, int indentCount, bool server = false)
        {
            string indent = "";
            for (int i = 0; i < indentCount; i++)
                indent += "\t";

            jsonParser.Append($"{indent}void from_json(const json& j, {SheetName}& dataObj)\r\n");
            jsonParser.Append($"{indent}{{\r\n");

            foreach (var pair in SchemaInfo)
            {
                var field = pair.Value;
                if (server)
                {
                    if (field.Server == false)
                        continue;

                    field.GetJsonParseCPP(ref jsonParser, indent + "\t");
                }
                else
                {
                    if (field.Client == false)
                        continue;

                    field.GetJsonParseCPP(ref jsonParser, indent + "\t");
                }
            }

            jsonParser.Append($"{indent }}}\r\n");
        }

        public string SheetName { get; private set; }
        private Dictionary<string, FieldInfo> SchemaInfo = new Dictionary<string, FieldInfo>();
    }

    public class RowData
    {
        public RowData(string sheetName, List<DataColumn> header)
        {
            SheetName = sheetName;
            _header = header;
        }

        public bool AddData(int id, List<IValue> rowValue)
        {
            return _data.TryAdd(id, rowValue);
        }
        public bool AddData(RowData data)
        {
            foreach (var pair in data._data)
            {
                if (false == AddData(pair.Key, pair.Value))
                    return false;
            }

            return true;
        }

        public bool ContainsKey(int id)
        {
            return _data.ContainsKey(id);
        }

        public string MakeJson()
        {
            JArray jArray = new JArray();
            foreach(var pair in _data)
            {
                var rowObj = new JObject();
                var value = pair.Value;
                for (int i = 0; i < _header.Count; i++)
                {
                    var key = _header[i].Name;
                    value[i].GetJson(ref key, ref rowObj);
                }
                jArray.Add(rowObj);
            }

            var dataObject = new JObject();
            dataObject.Add(new JProperty(SheetName, jArray));

            return dataObject.ToString();
        }

        public string SheetName { get; private set; }
        private List<DataColumn> _header = new List<DataColumn>();
        private Dictionary<int, List<IValue>> _data = new Dictionary<int, List<IValue>>();
    }
}
