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

    public class FieldInfo
    {
        public int Index { get; set; } = 0;
        public string Name { get; set; } = "";
        public ValueType TypeId { get; set; } = 0;
        public bool Container { get; set; } = false;
        public bool Server { get; set; } = false;
        public bool Client { get; set; } = false;
        public string RefSheetName { get; set; } = "";
        public bool Required { get; set; } = false;
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
        VEC2,
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
        public bool Container;
        public bool Required;
        public bool Server;
        public bool Client;
    }

    public struct Vec3
    {
        public float x, y, z;
    }
    public struct Vec2
    {
        public float x, y;
    }

    public class DataSchema
    {
        public DataSchema(string sheetName)
        {
            SheetName = sheetName;
            var idField = new FieldInfo();
            idField.Name = "ID";
            idField.TypeId = ValueType.INT;
            idField.Server = true;
            idField.Client = true;
            idField.Required = true;
            AddFieldInfo(idField);
        }

        public static Dictionary<string, ValueType> TypeMap = new()
        {
            { "int", ValueType.INT },
            { "float", ValueType.FLOAT },
            { "string", ValueType.STRING },
            { "bool", ValueType.BOOL },
            { "datetime", ValueType.DATETIME },
            { "vec3", ValueType.VEC3 },
            { "vec2", ValueType.VEC2 },
        };

        public static Dictionary<string, IValidatable> SchemaHeaderMap = new()
        {
            { "id", new ValidInt() },
            { "name", new ValidString() },
            { "type", new ValidType() },
            { "container", new ValidBool() },
            { "required", new ValidBool() },
            { "ref", new ValidReference() },
            { "server", new ValidBool() },
            { "client", new ValidBool() },
        };

        public bool AddFieldInfo(FieldInfo info)
        {
            return FieldInfos.TryAdd(info.Name.ToLower(), info);
        }

        public FieldInfo? GetFieldInfo(string name)
        {
            FieldInfo? findInfo;
            FieldInfos.TryGetValue(name.ToLower(), out findInfo);

            return findInfo;
        }

        public string SheetName { get; private set; }
        public Dictionary<string, FieldInfo> FieldInfos { get; } = new Dictionary<string, FieldInfo>();
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

        public string MakeJson(bool server)
        {
            JArray jArray = new JArray();
            foreach(var pair in _data)
            {
                var rowObj = new JObject();
                var value = pair.Value;
                for (int i = 0; i < _header.Count; i++)
                {
                    if (server)
                    {
                        if (false == _header[i].Server)
                            continue;

                        var key = _header[i].Name;
                        value[i].GetJson(ref key, ref rowObj);
                    }
                    else
                    {
                        if (false == _header[i].Client)
                            continue;

                        var key = _header[i].Name;
                        value[i].GetJson(ref key, ref rowObj);
                    }
                }
                jArray.Add(rowObj);
            }

            return jArray.ToString();
        }

        public string SheetName { get; private set; }
        private List<DataColumn> _header = new List<DataColumn>();
        private Dictionary<int, List<IValue>> _data = new Dictionary<int, List<IValue>>();
    }
}
