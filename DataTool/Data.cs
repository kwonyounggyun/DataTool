using DocumentFormat.OpenXml.Office.PowerPoint.Y2021.M06.Main;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Runtime.InteropServices;
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
            if (value.Length <= 0)
                return true;

            return value.Contains(":");
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
        public int TypeId { get; set; } = 0;
        public bool Server { get; set; } = false;
        public bool Client { get; set; } = false;
        public string RefSheetName { get; set; } = "";
        public string RefFieldName { get; set; } = "";
        public bool Required { get; set; } = false;
    }

    class DataSchema
    {
        public static Dictionary<string, int> TypeMap = new()
        {
            { "int", 1 },
            { "float", 2 },
            { "string", 3 },
            { "bool", 4 },
            { "vector2", 5 },
            { "vector3", 6 },
            { "vector4", 7 },
            { "color", 8 },
            { "datetime", 9 },
            { "list", 10 },
            { "dictionary", 11 }
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
            if (SchemaInfo[info.Name] != null)
                return false;

            SchemaInfo.Add(info.Name, info);
            return true;
        }

        public FieldInfo GetFieldInfo(string name)
        {
            return SchemaInfo[name];
        }

        private Dictionary<string, FieldInfo> SchemaInfo = new Dictionary<string, FieldInfo>();
    }

    class IValue
    {
        
    }
}
