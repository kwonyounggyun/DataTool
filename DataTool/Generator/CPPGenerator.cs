using ClosedXML.Excel;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataTool.Generator
{
    sealed class CPPClassBlock : CodeBlock
    {
        public override string MakeCode(int indentCount = 0)
        {
            StringBuilder sb = new StringBuilder();

            foreach (var row in _postRows)
                WriteLine(ref sb, indentCount, row);

            if(_blockName != null)
                WriteLine(ref sb, indentCount, _blockName);

            WriteLine(ref sb, indentCount, "{");
            WriteLine(ref sb, indentCount, "public:");
            foreach (var block in _innerBlocks)
                WriteLine(ref sb, 0, block.MakeCode(indentCount + 1));

            foreach (var row in _innerRows)
                WriteLine(ref sb, indentCount + 1, row);
            WriteLine(ref sb, indentCount, "};");

            return sb.ToString();
        }
    }

    public sealed class CPPGenerator : CodeGenerator
    {
        public override void Generate(string outFilePath, string usingNamespace, ConcurrentDataMap<DataSchema> schemaInfos, bool server = false)
        {
            var mainBlock = new CodeBlock();
            mainBlock.SetBlockName($"namespace {usingNamespace}");
            mainBlock.AddPost("#pragma once");
            mainBlock.AddPost("#include <map>");
            mainBlock.AddPost("#include <list>");
            mainBlock.AddPost("#include <string>");
            mainBlock.AddPost("#include <fstream>");
            mainBlock.AddPost("#include <sstream>");
            mainBlock.AddPost("#include <chrono>");
            mainBlock.AddPost("#include <nlohmann/json.hpp>");
            mainBlock.AddPost("using json = nlohmann::json;");
            mainBlock.AddBlock(GetDefaultClasses());

            var subBlock = new CodeBlock();
            subBlock.SetBlockName($"namespace {usingNamespace}");

            foreach (var pair in schemaInfos)
            {
                CodeBlock classfile = new CodeBlock();
                classfile.AddPost("#pragma once");
                classfile.SetBlockName($"namespace {usingNamespace}");
                var schema = pair.Value;
                var classBlock = GenerateClass(ref schema, server);
                foreach (var fieldPair in pair.Value.FieldInfos)
                {
                    var field = fieldPair.Value;
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

                    if (field.RefSheetName.Length > 0)
                    {
                        if (field.TypeId == ValueType.INT)
                        {
                            if (!field.Container)
                            {
                                var linkBlock = new CodeBlock();
                                var newName = GetName(field.Name);
                                linkBlock.SetBlockName($"static void Link{newName}(std::map<int, {usingNamespace}::{schema.SheetName}*>& map{schema.SheetName}, std::map<int, {usingNamespace}::{field.RefSheetName}*>& map{field.RefSheetName})");
                                linkBlock.AddRow($"for (auto& [key, value] : map{schema.SheetName})");
                                linkBlock.AddRow($"\tif (value->_{newName} != 0)");
                                linkBlock.AddRow($"\t\tif (auto find = map{field.RefSheetName}.find(value->_{newName}); find != map{field.RefSheetName}.end())");
                                linkBlock.AddRow($"\t\t\tvalue->{newName} = find->second;");
                                classBlock.AddBlock(linkBlock);
                            }
                            else
                            {
                                var linkBlock = new CodeBlock();
                                var newName = GetName(field.Name);
                                linkBlock.SetBlockName($"static void Link{newName}(std::map<int, {usingNamespace}::{schema.SheetName}*>& map{schema.SheetName}, std::map<int, {usingNamespace}::{field.RefSheetName}*>& map{field.RefSheetName})");
                                linkBlock.AddRow($"for (auto& [key, value] : map{schema.SheetName})");
                                linkBlock.AddRow($"\tfor (auto& [key2, value2] : value->{newName})");
                                linkBlock.AddRow($"\t\tif (auto find = map{field.RefSheetName}.find(key2); find != map{field.RefSheetName}.end())");
                                linkBlock.AddRow($"\t\t\tvalue2 = find->second;");
                                classBlock.AddBlock(linkBlock);
                            }
                        }
                    }
                }

                classfile.AddBlock(classBlock);
                classfile.AddBlock(GenerateJsonParser(ref schema, server));
                classfile.AddBlock(GenerateLoadFunc(ref schema));

                var outHeader = pair.Value.SheetName + ".h";
                try
                {
                    // 파일에 content 내용을 씁니다. 파일이 이미 있다면 덮어씁니다.
                    File.WriteAllText(outFilePath + "/" + outHeader, classfile.MakeCode());
                    Console.WriteLine($"파일 쓰기 완료: {outHeader}");
                }
                catch (IOException ex)
                {
                    Console.WriteLine($"{outHeader} 파일 쓰기 오류: {ex.Message}");
                }

                subBlock.AddPost($"#include \"{outHeader}\"");
            }

            subBlock.SetBlockName($"namespace {usingNamespace}");
            subBlock.AddBlock(GenerateStaticDataClass(usingNamespace, schemaInfos, server));

            var header = "StaticData.h";
            try
            {
                // 파일에 content 내용을 씁니다. 파일이 이미 있다면 덮어씁니다.
                File.WriteAllText(outFilePath + "/" + header, mainBlock.MakeCode() + subBlock.MakeCode());
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
            var vec3 = new CPPClassBlock();
            vec3.SetBlockName("struct Vec3");
            vec3.AddRow("float x;");
            vec3.AddRow("float y;");
            vec3.AddRow("float z;");
            list.Add(vec3);

            var vec3Json = new CodeBlock();
            vec3Json.SetBlockName("void from_json(const json& j, Vec3& dataObj)");
            vec3Json.AddRow("dataObj.x = j.at(\"x\").get<float>();");
            vec3Json.AddRow("dataObj.y = j.at(\"y\").get<float>();");
            vec3Json.AddRow("dataObj.z = j.at(\"z\").get<float>();");
            list.Add(vec3Json);

            var vec2 = new CPPClassBlock();
            vec2.SetBlockName("struct Vec2");
            vec2.AddRow("float x;");
            vec2.AddRow("float y;");
            list.Add(vec2);

            var vec2Json = new CodeBlock();
            vec2Json.SetBlockName("void from_json(const json& j, Vec2& dataObj)");
            vec2Json.AddRow("dataObj.x = j.at(\"x\").get<float>();");
            vec2Json.AddRow("dataObj.y = j.at(\"y\").get<float>();");
            list.Add(vec2Json);

            return list;
        }

        protected override CodeBlock GenerateClass(ref DataSchema schemaInfo, bool server = false)
        {
            var classBlock = new CPPClassBlock();
            classBlock.SetBlockName($"class {schemaInfo.SheetName}");
            // 전방 선언
            var set = new HashSet<string>();
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

                if (field.RefSheetName.Length > 0)
                    set.Add(field.RefSheetName);
            }

            foreach (var refName in set)
                classBlock.AddPost($"class {refName};");

            classBlock.AddRow($"static void Load(std::string jsonDir, std::map<int, {schemaInfo.SheetName}*>&data);");

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
                            block.AddRow($"int _{newName} = 0;");
                            block.AddRow($"const {field.RefSheetName}* {newName} = nullptr;");
                        }
                        else
                            block.AddRow($"int {newName} = 0;");
                        break;
                    case ValueType.FLOAT:
                        block.AddRow($"float {newName} = 0.0f;");
                        break;
                    case ValueType.STRING:
                        block.AddRow($"std::string {newName} = \"\";");
                        break;
                    case ValueType.BOOL:
                        block.AddRow($"bool {newName} = false;");
                        break;
                    case ValueType.DATETIME:
                        block.AddRow($"std::tm {newName};");
                        break;
                    case ValueType.VEC3:
                        block.AddRow($"Vec3 {newName};");
                        break;
                    case ValueType.VEC2:
                        block.AddRow($"Vec2 {newName};");
                        break;
                }
            }
            else
            {
                switch (field.TypeId)
                {
                    case ValueType.INT:
                        if (field.RefSheetName.Length > 0)
                            block.AddRow($"std::map<int, const {field.RefSheetName}*> {newName};");
                        else
                            block.AddRow($"std::vector<int> {newName};");
                        break;
                    case ValueType.FLOAT:
                        block.AddRow($"std::vector<float> {newName};");
                        break;
                    case ValueType.STRING:
                        block.AddRow($"std::vector<std::string> {newName};");
                        break;
                    case ValueType.BOOL:
                        block.AddRow($"std::vector<bool> {newName};");
                        break;
                }
            }
        }

        protected override CodeBlock GenerateJsonParser(ref DataSchema schemaInfo, bool server = false)
        {
            var jsonParser = new CodeBlock();
            jsonParser.SetBlockName($"void from_json(const json& j, {schemaInfo.SheetName}& dataObj)");

            foreach (var pair in schemaInfo.FieldInfos)
            {
                var field = pair.Value;
                if (server)
                {
                    if (field.Server == false)
                        continue;

                    GetParseJsonField(ref field, jsonParser);
                }
                else
                {
                    if (field.Client == false)
                        continue;

                    GetParseJsonField(ref field, jsonParser);
                }
            }

            return jsonParser;
        }
        protected override void GetParseJsonField(ref FieldInfo field, CodeBlock block)
        {
            var newName = GetName(field.Name);
            if (!field.Container)
            {
                switch (field.TypeId)
                {
                    case ValueType.INT:
                        if (field.RefSheetName.Length > 0)
                            block.AddRow($"dataObj._{newName} = j.at(\"{field.Name}\").get<int>();");
                        else
                            block.AddRow($"dataObj.{newName} = j.at(\"{field.Name}\").get<int>();");
                        break;
                    case ValueType.FLOAT:
                        block.AddRow($"dataObj.{newName} = j.at(\"{field.Name}\").get<float>();");
                        break;
                    case ValueType.STRING:
                        block.AddRow($"dataObj.{newName} = j.at(\"{field.Name}\").get<std::string>();");
                        break;
                    case ValueType.BOOL:
                        block.AddRow($"dataObj.{newName} = j.at(\"{field.Name}\").get<bool>();");
                        break;
                    case ValueType.DATETIME:
                        block.AddRow("{");
                        block.AddRow($"\tauto dateStr = j.at(\"{field.Name}\").get<std::string>();");
                        block.AddRow($"\tstd::stringstream ss(dateStr);");
                        block.AddRow($"\tss >> std::get_time(&dataObj.{newName}, \"%Y-%m-%dT%H:%M:%S\");");
                        block.AddRow($"\tdataObj.{newName}.tm_isdst = 0;");
                        block.AddRow("}");
                        break;
                    case ValueType.VEC3:
                        block.AddRow($"dataObj.{newName} = j.at(\"{field.Name}\").get<Vec3>();");
                        break;
                    case ValueType.VEC2:
                        block.AddRow($"dataObj.{newName} = j.at(\"{field.Name}\").get<Vec2>();");
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
                            block.AddRow($"{{");
                            block.AddRow($"\tauto ids = j.at(\"{field.Name}\").get<std::vector<int>>();");
                            block.AddRow($"\tfor(auto id : ids) dataObj.{newName}[id] = nullptr;");
                            block.AddRow($"}}");
                        }
                        else
                            block.AddRow($"dataObj.{newName} = j.at(\"{field.Name}\").get<std::vector<int>>();");
                        break;
                    case ValueType.FLOAT:
                        block.AddRow($"dataObj.{newName} = j.at(\"{field.Name}\").get<std::vector<float>>();");
                        break;
                    case ValueType.STRING:
                        block.AddRow($"dataObj.{newName} = j.at(\"{field.Name}\").get<std::vector<std::string>>();");
                        break;
                    case ValueType.BOOL:
                        block.AddRow($"dataObj.{newName} = j.at(\"{field.Name}\").get<std::vector<bool>>();");
                        break;
                }
            }
        }

        protected CodeBlock GenerateLoadFunc(ref DataSchema schemaInfo)
        {
            var loadFunc = new CodeBlock();
            loadFunc.SetBlockName($"void {schemaInfo.SheetName}::Load(std::string jsonDir, std::map<int, {schemaInfo.SheetName}*>&data)");
            loadFunc.AddRow($"std::ifstream inputFile(jsonDir +\"/{schemaInfo.SheetName}.json\");");
            loadFunc.AddRow($"if (inputFile.is_open())");
            loadFunc.AddRow($"{{");
            loadFunc.AddRow($"\tstd::stringstream buffer;");
            loadFunc.AddRow($"\tbuffer << inputFile.rdbuf();");
            loadFunc.AddRow($"\tjson j = json::parse(buffer.str());");
            loadFunc.AddRow($"\tfor (const auto& elem : j)");
            loadFunc.AddRow($"\t{{");
            loadFunc.AddRow($"\t\tauto item = elem.get<{schemaInfo.SheetName}>();");
            loadFunc.AddRow($"\t\tdata.emplace(item.ID, new {schemaInfo.SheetName}(item));");
            loadFunc.AddRow($"\t}}");
            loadFunc.AddRow($"}}");
            return loadFunc;
        }

        protected CodeBlock GenerateStaticDataClass(string usingNamespace, ConcurrentDataMap<DataSchema> schemaInfos, bool server)
        {
            var classBlock = new CPPClassBlock();
            classBlock.SetBlockName($"class StaticData");

            var loadFunc = new CodeBlock();
            loadFunc.SetBlockName($"void Load(std::string jsonDir)");
            foreach (var pair in schemaInfos)
            {
                var schema = pair.Value;
                loadFunc.AddRow($"std::map <int, {usingNamespace}::{schema.SheetName}*> _{schema.SheetName};");
            }
            foreach (var pair in schemaInfos)
            {
                var schema = pair.Value;
                loadFunc.AddRow($"{schema.SheetName}::Load(jsonDir, _{schema.SheetName});");
            }

            foreach (var pair in schemaInfos)
            {
                var schema = pair.Value;
                foreach (var pair2 in schema.FieldInfos)
                {
                    var field = pair2.Value;
                    if (field.RefSheetName.Length <= 0)
                        continue;

                    if (server)
                    {
                        if (false == field.Server)
                            continue;
                    }
                    else
                    {
                        if (false == field.Client)
                            continue;
                    }

                    if (field.TypeId == ValueType.INT)
                    {
                        var newName = GetName(field.Name);
                        loadFunc.AddRow($"{schema.SheetName}::Link{newName}(_{schema.SheetName}, _{field.RefSheetName});");
                    }
                }
            }

            foreach (var pair in schemaInfos)
            {
                var schema = pair.Value;
                loadFunc.AddRow($"{schema.SheetName}.insert(_{schema.SheetName}.begin(), _{schema.SheetName}.end());");
            }

            classBlock.AddBlock(loadFunc);

            foreach (var pair in schemaInfos)
            {
                var schema = pair.Value;
                classBlock.AddRow($"std::map<int, const {usingNamespace}::{schema.SheetName}*> {schema.SheetName};");
            }

            return classBlock;
        }
    }
}
