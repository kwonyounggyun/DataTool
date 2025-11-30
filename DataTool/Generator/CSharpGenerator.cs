using ClosedXML.Excel;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.InkML;
using DocumentFormat.OpenXml.Wordprocessing;
using Newtonsoft.Json;
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
        public override void Generate(string outFilePath, string usingNamespace, ConcurrentDictionary<string, DataSchema> schemaInfos, bool server = false)
        {
            var main = new CodeBlock();
            main.AddPost("using System.Collections.Generic;");
            main.AddPost("using Newtonsoft.Json;");
            main.SetBlockName($"namespace {usingNamespace}");
            main.AddBlock(GetDefaultClasses());

            foreach (var pair in schemaInfos)
            {
                var fileBlock = new CodeBlock();
                fileBlock.AddPost($"using Newtonsoft.Json;");
                fileBlock.SetBlockName($"namespace {usingNamespace}");
                var schema = pair.Value;
                fileBlock.AddBlock(GenerateClass(ref schema, server));

                var outHeader = pair.Value.SheetName + ".cs";
                try
                {
                    // 파일에 content 내용을 씁니다. 파일이 이미 있다면 덮어씁니다.
                    File.WriteAllText(outFilePath + "/" + outHeader, fileBlock.MakeCode());
                    Console.WriteLine($"파일 쓰기 완료: {outHeader}");
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
                File.WriteAllText(outFilePath + "/" + header, main.MakeCode());
                Console.WriteLine($"파일 쓰기 완료: {header}");
            }
            catch (IOException ex)
            {
                Console.WriteLine($"{header} 파일 쓰기 오류: {ex.Message}");
            }
        }
        protected override List<CodeBlock> GetDefaultClasses() 
        {
            var list = new List<CodeBlock>();
            var vec3 = new CodeBlock();
            vec3.SetBlockName($"public struct Vec3");
            vec3.AddRow($"[JsonProperty(\"x\")]");
            vec3.AddRow($"public float X {{ get; set; }}");
            vec3.AddRow($"[JsonProperty(\"y\")]");
            vec3.AddRow($"public float Y {{ get; set; }}");
            vec3.AddRow($"[JsonProperty(\"z\")]");
            vec3.AddRow($"public float Z {{ get; set; }}");
            list.Add(vec3);

            var vec2 = new CodeBlock();
            vec2.SetBlockName($"struct Vec2");
            vec2.AddRow($"[JsonProperty(\"x\")]");
            vec2.AddRow($"float X {{ get; set; }}");
            vec2.AddRow($"[JsonProperty(\"y\")]");
            vec2.AddRow($"float Y {{ get; set; }}");
            list.Add(vec2);

            return list;
        }
        protected override CodeBlock GenerateClass(ref DataSchema schemaInfo, bool server = false) 
        {
            var classBlock = new CodeBlock();
            classBlock.SetBlockName($"public sealed class {schemaInfo.SheetName}");

            var loadBlock = new CodeBlock();
            loadBlock.SetBlockName($"public static bool Load(ref string fileDir, out Dictionary<int, {schemaInfo.SheetName}> dic)");
            loadBlock.AddRow($"dic = new Dictionary<int, {schemaInfo.SheetName}>();");
            loadBlock.AddRow($"string filePath = fileDir + \"/{schemaInfo.SheetName}.json\";");
            loadBlock.AddRow($"try {{");
            loadBlock.AddRow($"\tstring fileContent = File.ReadAllText(filePath);");
            loadBlock.AddRow($"\tvar list = JsonConvert.DeserializeObject<List<{schemaInfo.SheetName}>>(fileContent);");
            loadBlock.AddRow($"\tforeach (var item in list)");
            loadBlock.AddRow($"\t\tdic.Add(item.Id, item);");
            loadBlock.AddRow($"}} catch (FileNotFoundException) {{");
            loadBlock.AddRow($"\tConsole.WriteLine($\"FileNotFound: {{filePath}}\");");
            loadBlock.AddRow($"\treturn false;");
            loadBlock.AddRow($"}} catch (Exception ex) {{");
            loadBlock.AddRow($"\tConsole.WriteLine($\"{{filePath}} read error: {{ex.Message}}\");");
            loadBlock.AddRow($"\treturn false;");
            loadBlock.AddRow($"}}");
            loadBlock.AddRow($"return true;");

            classBlock.AddBlock(loadBlock);

            foreach (var pair in schemaInfo.FieldInfos)
            {
                var field = pair.Value;
                if (server)
                {
                    if (field.Server == false)
                        continue;

                    GetField(ref field, classBlock);
                }
                else
                {
                    if (field.Client == false)
                        continue;

                    GetField(ref field, classBlock);
                }
            }

            return classBlock;
        }
        protected override void GetField(ref FieldInfo field, CodeBlock block) 
        {
            var newName = char.ToUpper(field.Name[0]) + field.Name.Substring(1);
            switch (field.TypeId)
            {
                case ValueType.INT:
                    if (field.RefSheetName.Length > 0)
                    {
                        block.AddRow($"[JsonProperty(\"{field.Name}\")]");
                        block.AddRow($"public int _{newName} {{ get; set; }}");
                        block.AddRow($"[JsonIgnore]");
                        block.AddRow($"public {field.RefSheetName} {newName} {{ get; set; }}");
                    }
                    else
                    {
                        block.AddRow($"[JsonProperty(\"{field.Name}\")]");
                        block.AddRow($"public int {newName} {{ get; set; }}");
                    }
                    break;
                case ValueType.FLOAT:
                    block.AddRow($"[JsonProperty(\"{field.Name}\")]");
                    block.AddRow($"public float {newName} {{ get; set; }}");
                    break;
                case ValueType.STRING:
                    block.AddRow($"[JsonProperty(\"{field.Name}\")]");
                    block.AddRow($"public string {newName} {{ get; set; }}");
                    break;
                case ValueType.BOOL:
                    block.AddRow($"[JsonProperty(\"{field.Name}\")]");
                    block.AddRow($"public bool {newName} {{ get; set; }}");
                    break;
                case ValueType.DATETIME:
                    block.AddRow($"[JsonProperty(\"{field.Name}\")]");
                    block.AddRow($"public DateTime {newName} {{ get; set; }}");
                    break;
                case ValueType.VEC3:
                    block.AddRow($"[JsonProperty(\"{field.Name}\")]");
                    block.AddRow($"public Vec3 {newName} {{ get; set; }}");
                    break;
                case ValueType.VEC2:
                    block.AddRow($"[JsonProperty(\"{field.Name}\")]");
                    block.AddRow($"public Vec2 {newName} {{ get; set; }}");
                    break;
                case ValueType.LIST:
                    block.AddRow($"[JsonProperty(\"{field.Name}\")]");
                    block.AddRow($"public List<int> _{newName} {{ get; set; }}");
                    block.AddRow($"[JsonIgnore]");
                    block.AddRow($"public Dictionary<int, {field.RefSheetName}> {newName} {{ get; set; }}");
                    break;
            }
        }
        protected override CodeBlock GenerateJsonParser(ref DataSchema schemaInfo, bool server = false) { return new CodeBlock(); }
        protected override void GetParseJsonField(ref FieldInfo field, CodeBlock block) { return; }
    }
}
