using ClosedXML.Excel;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.InkML;
using DocumentFormat.OpenXml.Wordprocessing;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace DataTool.Generator
{
    public sealed class CSharpGenerator : CodeGenerator
    {
        public override void Generate(string outFilePath, string usingNamespace, ConcurrentDataMap<DataSchema> schemaInfos, bool server = false)
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

            main.AddBlock(GenerateStaticDataClass(usingNamespace, schemaInfos, server));

            var header = "StaticData.cs";
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
            vec3.SetBlockName($"public class Vec3");
            vec3.AddRow($"[JsonProperty(\"x\")]");
            vec3.AddRow($"public float X {{ get; init; }} = 0;");
            vec3.AddRow($"[JsonProperty(\"y\")]");
            vec3.AddRow($"public float Y {{ get; init; }} = 0;");
            vec3.AddRow($"[JsonProperty(\"z\")]");
            vec3.AddRow($"public float Z {{ get; init; }} = 0;");
            list.Add(vec3);

            var vec2 = new CodeBlock();
            vec2.SetBlockName($"public class Vec2");
            vec2.AddRow($"[JsonProperty(\"x\")]");
            vec2.AddRow($"float X {{ get; init; }} = 0;");
            vec2.AddRow($"[JsonProperty(\"y\")]");
            vec2.AddRow($"float Y {{ get; init; }} = 0;");
            list.Add(vec2);

            return list;
        }
        protected override CodeBlock GenerateClass(ref DataSchema schemaInfo, bool server = false) 
        {
            var classBlock = new CodeBlock();
            classBlock.SetBlockName($"public sealed class {schemaInfo.SheetName}");

            var loadBlock = new CodeBlock();
            loadBlock.SetBlockName($"public static bool Load(string fileDir, out Dictionary<int, {schemaInfo.SheetName}> dic)");
            loadBlock.AddRow($"dic = new Dictionary<int, {schemaInfo.SheetName}>();");
            loadBlock.AddRow($"var refDic = dic;");
            loadBlock.AddRow($"string filePath = fileDir + \"/{schemaInfo.SheetName}.json\";");
            loadBlock.AddRow($"try {{");
            loadBlock.AddRow($"\tstring fileContent = File.ReadAllText(filePath);");
            loadBlock.AddRow($"\tvar list = JsonConvert.DeserializeObject<List<{schemaInfo.SheetName}>>(fileContent);");
            loadBlock.AddRow($"\tlist?.ForEach(data => {{ refDic.TryAdd(data.ID, data); }});");
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
                }
                else
                {
                    if (field.Client == false)
                        continue;
                }

                GetField(ref field, classBlock);
                if (field.RefSheetName.Length > 0)
                {
                    if(field.TypeId == ValueType.INT)
                    {
                        if (!field.Container)
                        {
                            var newName = GetName(field.Name);
                            var linkBlock = new CodeBlock();
                            linkBlock.SetBlockName($"public static void Link{newName}(ref Dictionary<int, {schemaInfo.SheetName}> dic, IReadOnlyDictionary<int, {field.RefSheetName}> refDic)");
                            linkBlock.AddRow($"foreach (var item in dic)");
                            linkBlock.AddRow($"{{");
                            linkBlock.AddRow($"\tif (item.Value.__{newName} == 0) return;");
                            linkBlock.AddRow($"\t{field.RefSheetName}? refItem = null;");
                            linkBlock.AddRow($"\tif (false == refDic.TryGetValue(item.Value.__{newName}, out refItem) || refItem == null) return;");
                            linkBlock.AddRow($"\titem.Value._{newName} = refItem;");
                            linkBlock.AddRow($"}}");
                            classBlock.AddBlock(linkBlock);
                        }
                        else
                        {
                            var newName = GetName(field.Name);
                            var linkBlock = new CodeBlock();
                            linkBlock.SetBlockName($"public static void Link{newName}(ref Dictionary<int, {schemaInfo.SheetName}> dic, IReadOnlyDictionary<int, {field.RefSheetName}> refDic)");
                            linkBlock.AddRow($"foreach (var item in dic)");
                            linkBlock.AddRow($"{{");
                            linkBlock.AddRow($"\titem.Value.__{newName}?.ForEach(data => {{");
                            linkBlock.AddRow($"\t\t{field.RefSheetName}? refItem = null;");
                            linkBlock.AddRow($"\t\tif (false == refDic.TryGetValue(data, out refItem) || refItem == null) return;");
                            linkBlock.AddRow($"\t\titem.Value._{newName}.TryAdd(refItem.ID, refItem);");
                            linkBlock.AddRow($"\t}});");
                            linkBlock.AddRow($"}}");
                            classBlock.AddBlock(linkBlock);
                        }
                    }
                }
            }

            return classBlock;
        }
        protected override void GetField(ref FieldInfo field, CodeBlock block) 
        {
            var newName = GetName(field.Name);

            if (!field.Container)
            {
                switch (field.TypeId)
                {
                    case ValueType.INT:
                        if (field.RefSheetName.Length > 0)
                        {
                            block.AddRow($"[JsonProperty(\"{field.Name}\")]");
                            block.AddRow($"private int __{newName} {{ get; init; }} = 0;");
                            block.AddRow($"[JsonIgnore]");
                            block.AddRow($"private {field.RefSheetName}? _{newName} = null;");
                            block.AddRow($"public ref readonly {field.RefSheetName}? {newName} => ref _{newName};");
                        }
                        else
                        {
                            block.AddRow($"[JsonProperty(\"{field.Name}\")]");
                            block.AddRow($"public int {newName} {{ get; init; }} = 0;");
                        }
                        break;
                    case ValueType.FLOAT:
                        block.AddRow($"[JsonProperty(\"{field.Name}\")]");
                        block.AddRow($"public float {newName} {{ get; init; }} = 0;");
                        break;
                    case ValueType.STRING:
                        block.AddRow($"[JsonProperty(\"{field.Name}\")]");
                        block.AddRow($"public string {newName} {{ get; init; }} = \"\";");
                        break;
                    case ValueType.BOOL:
                        block.AddRow($"[JsonProperty(\"{field.Name}\")]");
                        block.AddRow($"public bool {newName} {{ get; init; }} = false;");
                        break;
                    case ValueType.DATETIME:
                        block.AddRow($"[JsonProperty(\"{field.Name}\")]");
                        block.AddRow($"public DateTime {newName} {{ get; init; }} = DateTime.Now.AddYears(-125);");
                        break;
                    case ValueType.VEC3:
                        block.AddRow($"[JsonProperty(\"{field.Name}\")]");
                        block.AddRow($"public Vec3 {newName} {{ get; init; }} = new Vec3();");
                        break;
                    case ValueType.VEC2:
                        block.AddRow($"[JsonProperty(\"{field.Name}\")]");
                        block.AddRow($"public Vec2 {newName} {{ get; init; }} = new Vec2();");
                        break;
                }
            }
            else
            {
                switch (field.TypeId)
                {
                    case ValueType.INT:
                        if (field.RefSheetName.Length > 0)
                        {
                            block.AddRow($"[JsonProperty(\"{field.Name}\")]");
                            block.AddRow($"private List<int>? __{newName};");
                            block.AddRow($"[JsonIgnore]");
                            block.AddRow($"private Dictionary<int, {field.RefSheetName}> _{newName} = new Dictionary<int, {field.RefSheetName}>();");
                            block.AddRow($"[JsonIgnore]");
                            block.AddRow($"public IReadOnlyDictionary<int, {field.RefSheetName}> {newName} {{ get {{ return _{newName}; }} }}");
                        }
                        else
                        {
                            block.AddRow($"[JsonProperty(\"{field.Name}\")]");
                            block.AddRow($"private List<int> _{newName} = new List<int>();");
                            block.AddRow($"[JsonIgnore]");
                            block.AddRow($"public IReadOnlyList<int> {newName} {{ get {{ return _{newName}; }} }}");
                        }
                        break;
                    case ValueType.FLOAT:
                        block.AddRow($"[JsonProperty(\"{field.Name}\")]");
                        block.AddRow($"private List<float> _{newName} = new List<float>();");
                        block.AddRow($"[JsonIgnore]");
                        block.AddRow($"public IReadOnlyList<float> {newName} {{ get {{ return _{newName}; }} }}");
                        break;
                    case ValueType.STRING:
                        block.AddRow($"[JsonProperty(\"{field.Name}\")]");
                        block.AddRow($"private List<string> _{newName} = new List<string>();");
                        block.AddRow($"[JsonIgnore]");
                        block.AddRow($"public IReadOnlyList<string> {newName} {{ get {{ return _{newName}; }} }}"); ;
                        break;
                    case ValueType.BOOL:
                        block.AddRow($"[JsonProperty(\"{field.Name}\")]");
                        block.AddRow($"private List<bool> _{newName} = new List<bool>();");
                        block.AddRow($"[JsonIgnore]");
                        block.AddRow($"public IReadOnlyList<bool> {newName} {{ get {{ return _{newName}; }} }}");
                        break;
                }
            }
        }
        protected override CodeBlock GenerateJsonParser(ref DataSchema schemaInfo, bool server = false) { return new CodeBlock(); }
        protected override void GetParseJsonField(ref FieldInfo field, CodeBlock block) { return; }
        protected CodeBlock GenerateStaticDataClass(string usingNamespace, ConcurrentDataMap<DataSchema> schemaInfos, bool server)
        {
            var dataClass = new CodeBlock();
            dataClass.SetBlockName("public class StaticData");

            var loadFunc = new CodeBlock();
            loadFunc.SetBlockName("public void Load(string jsonDir)");
            foreach (var item in schemaInfos)
            {
                var schema = item.Value;
                loadFunc.AddRow($"{usingNamespace}.{schema.SheetName}.Load(jsonDir, out _{schema.SheetName});");
            }

            foreach (var item in schemaInfos)
            {
                var schema = item.Value;
                foreach (var item2 in schema.FieldInfos)
                {
                    var field = item2.Value;
                    if (field.RefSheetName.Length > 0)
                        loadFunc.AddRow($"{usingNamespace}.{schema.SheetName}.Link{GetName(field.Name)}(ref _{schema.SheetName}, _{field.RefSheetName});");
                }
            }

            dataClass.AddBlock(loadFunc);

            foreach (var item in schemaInfos)
            {
                var schema = item.Value;
                dataClass.AddRow($"private Dictionary<int, {usingNamespace}.{schema.SheetName}> _{schema.SheetName};");
                dataClass.AddRow($"public IReadOnlyDictionary<int, {usingNamespace}.{schema.SheetName}> {schema.SheetName} {{ get {{ return _{schema.SheetName}; }} }}");
            }

            return dataClass;
        }
    }
}
