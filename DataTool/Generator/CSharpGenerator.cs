using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataTool.Generator
{
    public sealed class CSharpGenerator : CodeGenerator
    {
        public override void Generate(ref string outFilePath, ref string usingNamespace, ref ConcurrentDictionary<string, DataSchema> schemaInfos, bool server = false)
        {
            StringBuilder mainHeader = new StringBuilder();
            mainHeader.Append("using System.Collections.Generic;");
            mainHeader.Append($"namespace {usingNamespace}\r\n");
            mainHeader.Append("{\r\n");
            MakeDefaultClass(ref mainHeader, 1);
            mainHeader.Append("}\r\n");

            foreach (var pair in schemaInfos)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append($"namespace {usingNamespace}\r\n");
                sb.Append("{\r\n");
                var schema = pair.Value;
                GenerateClass(ref sb, 1, ref schema, server);
                GenerateJsonParser(ref sb, 1, ref schema, server);
                sb.Append("}\r\n");
                Console.WriteLine($"{sb.ToString()}");

                var outHeader = pair.Value.SheetName + ".cs";
                try
                {
                    // 파일에 content 내용을 씁니다. 파일이 이미 있다면 덮어씁니다.
                    File.WriteAllText(outFilePath + "/" + outHeader, sb.ToString());
                    Console.WriteLine($"파일 쓰기 완료: {outHeader}");
                    Console.WriteLine($"{sb.ToString()}");
                }
                catch (IOException ex)
                {
                    Console.WriteLine($"{outHeader} 파일 쓰기 오류: {ex.Message}");
                }
            }

            var header = usingNamespace + ".cs";
            try
            {
                // 파일에 content 내용을 씁니다. 파일이 이미 있다면 덮어씁니다.
                File.WriteAllText(outFilePath + "/" + header, mainHeader.ToString());
                Console.WriteLine($"파일 쓰기 완료: {header}");
                Console.WriteLine($"{mainHeader.ToString()}");
            }
            catch (IOException ex)
            {
                Console.WriteLine($"{header} 파일 쓰기 오류: {ex.Message}");
            }
        }

        protected override void GenerateClass(ref StringBuilder sb, ref string indent, ref DataSchema schemaInfo, bool server = false)
        {
            sb.Append($"{indent}public sealed class {schemaInfo.SheetName}\r\n");
            sb.Append($"{indent}{{\r\n");

            var fieldIndent = indent + "\t";
            foreach (var pair in schemaInfo.FieldInfos)
            {
                var field = pair.Value;
                if (server)
                {
                    if (field.Server == false)
                        continue;

                    GetField(ref sb, ref fieldIndent, ref field);
                }
                else
                {
                    if (field.Client == false)
                        continue;

                    GetField(ref sb, ref fieldIndent, ref field);
                }
            }

            sb.Append($"{indent}}};\r\n\r\n");
        }

        protected override void GenerateJsonParser(ref StringBuilder sb, ref string indent, ref DataSchema schemaInfo, bool server = false)
        {
            return;
        }

        protected override void GetField(ref StringBuilder sb, ref string indent, ref FieldInfo field)
        {
            var newName = char.ToUpper(field.Name[0]) + field.Name.Substring(1);
            switch (field.TypeId)
            {
                case ValueType.INT:
                    sb.Append($"{indent}[JsonProperty(\"{field.Name}\")]\r\n");
                    sb.Append($"{indent}int {newName} {{ get; set; }}\r\n");
                    break;
                case ValueType.FLOAT:
                    sb.Append($"{indent}[JsonProperty(\"{field.Name}\")]\r\n");
                    sb.Append($"{indent}float {newName} {{ get; set; }}\r\n");
                    break;
                case ValueType.STRING:
                    sb.Append($"{indent}[JsonProperty(\"{field.Name}\")]\r\n");
                    sb.Append($"{indent}string {newName} {{ get; set; }};\r\n");
                    break;
                case ValueType.BOOL:
                    sb.Append($"{indent}[JsonProperty(\"{field.Name}\")]\r\n");
                    sb.Append($"{indent}bool {newName} {{ get; set; }};\r\n");
                    break;
                case ValueType.DATETIME:
                    sb.Append($"{indent}[JsonProperty(\"{field.Name}\")]\r\n");
                    sb.Append($"{indent}DateTime {newName} {{ get; set; }};\r\n");
                    break;
                case ValueType.VEC3:
                    sb.Append($"{indent}[JsonProperty(\"{field.Name}\")]\r\n");
                    sb.Append($"{indent}Vec3 {newName} {{ get; set; }};\r\n");
                    break;
                case ValueType.VEC2:
                    sb.Append($"{indent}[JsonProperty(\"{field.Name}\")]\r\n");
                    sb.Append($"{indent}Vec2 {newName} {{ get; set; }};\r\n");
                    break;
                case ValueType.LIST:
                    sb.Append($"{indent}[JsonProperty(\"{field.Name}\")]\r\n");
                    sb.Append($"{indent}List<int> {newName} {{ get; set; }}\r\n");
                    sb.Append($"{indent}[JsonIgnore]\r\n");
                    sb.Append($"{indent}Dictionary<int, {field.RefSheetName}> Dic{newName} {{ get; set; }}\r\n");
                    break;
            }
        }

        protected override void GetParseJsonField(ref StringBuilder sb, ref string indent, ref FieldInfo field)
        {
            return;
        }

        protected override void MakeDefaultClass(ref StringBuilder sb, ref string indent)
        {
            sb.Append($"{indent}public struct Vec3\r\n");
            sb.Append($"{indent}{{\r\n");
            sb.Append($"{indent + "\t"}[JsonProperty(\"x\")]\r\n");
            sb.Append($"{indent + "\t"}public float X {{ get; set; }}\r\n");
            sb.Append($"{indent + "\t"}[JsonProperty(\"y\")]\r\n");
            sb.Append($"{indent + "\t"}public float Y {{ get; set; }}\r\n");
            sb.Append($"{indent + "\t"}[JsonProperty(\"z\")]\r\n");
            sb.Append($"{indent + "\t"}public float Z {{ get; set; }}\r\n");
            sb.Append($"{indent}}};\r\n");
            sb.Append($"\r\n");

            sb.Append($"{indent}struct Vec2\r\n");
            sb.Append($"{indent}{{\r\n");
            sb.Append($"{indent + "\t"}[JsonProperty(\"x\")]\r\n");
            sb.Append($"{indent + "\t"}float X {{ get; set; }}\r\n");
            sb.Append($"{indent + "\t"}[JsonProperty(\"y\")]\r\n");
            sb.Append($"{indent + "\t"}float Y {{ get; set; }}\r\n");
            sb.Append($"{indent}}};\r\n");
            sb.Append($"\r\n");
        }
    }
}
