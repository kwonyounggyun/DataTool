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
    public sealed class CPPGenerator : CodeGenerator
    {
        public override void Generate(ref string outFilePath, ref string usingNamespace, ref ConcurrentDictionary<string, DataSchema> schemaInfos, bool server)
        {
            StringBuilder mainHeader = new StringBuilder();
            mainHeader.Append("#pragma once\r\n");
            mainHeader.Append("#include <map>\r\n");
            mainHeader.Append("#include <list>\r\n");
            mainHeader.Append("#include <string>\r\n");
            mainHeader.Append("#include <fstream>\r\n");
            mainHeader.Append("#include <sstream>\r\n");
            mainHeader.Append("#include <nlohmann/json.hpp>\r\n");
            mainHeader.Append("using json = nlohmann::json;\r\n\r\n");

            mainHeader.Append($"namespace {usingNamespace}\r\n");
            mainHeader.Append("{\r\n");
            MakeDefaultClass(ref mainHeader, 1);
            mainHeader.Append("}\r\n");

            foreach (var pair in schemaInfos)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("#pragma once\r\n\r\n");
                sb.Append($"namespace {usingNamespace}\r\n");
                sb.Append("{\r\n");
                var schema = pair.Value;
                GenerateClass(ref sb, 1, ref schema, server);
                GenerateJsonParser(ref sb, 1, ref schema, server);
                GenerateLoadFunc(ref sb, "\t", ref schema);
                sb.Append("}\r\n");
                Console.WriteLine($"{sb.ToString()}");

                var outHeader = pair.Value.SheetName + ".h";
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

                mainHeader.Append($"#include \"{outHeader}\"\r\n");
            }

            mainHeader.Append($"namespace {usingNamespace}\r\n");
            mainHeader.Append("{\r\n");
            GenerateStaticDataClass(ref mainHeader, "\t", ref usingNamespace, ref schemaInfos, server);
            mainHeader.Append("}\r\n");

            var header = usingNamespace + ".h";
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

        protected void GenerateStaticDataClass(ref StringBuilder sb, string indent, ref string usingNamespace, ref ConcurrentDictionary<string, DataSchema> schemaInfos, bool server)
        {
            sb.Append($"{indent}class StaticData\r\n");
            sb.Append($"{indent}{{\r\n");
            sb.Append($"{indent}public:\r\n");
            sb.Append($"{indent + "\t"}static void Load(std::string jsonDir)\r\n");
            sb.Append($"{indent + "\t"}{{\r\n");
            foreach (var pair in schemaInfos)
            {
                var schema = pair.Value;
                sb.Append($"{indent + "\t\t"}std::map<int, {usingNamespace}::{schema.SheetName}*> _{schema.SheetName};\r\n");
            }
            sb.Append($"\r\n");
            foreach (var pair in schemaInfos)
            {
                var schema = pair.Value;
                sb.Append($"{indent + "\t\t"}{schema.SheetName}::Load(jsonDir, _{schema.SheetName});\r\n");
            }

            sb.Append($"\r\n");
            sb.Append($"{indent + "\t\t"}std::list<std::function<void()>> tasks;\r\n");
            foreach (var pair in schemaInfos)
            {
                var schema = pair.Value;
                foreach(var pair2 in schema.FieldInfos)
                {
                    var field = pair2.Value;
                    if (field.RefSheetName.Length <= 0)
                        continue;

                    if(server)
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
                        var newName = char.ToUpper(field.Name[0]) + field.Name.Substring(1);
                        sb.Append(
                            $"{indent + "\t\t"}tasks.push_back([&](){{\r\n" +
                            $"{indent + "\t\t\t"}for (auto& [key, value] : _{schema.SheetName})\r\n" +
                            $"{indent + "\t\t\t\t"}value->{newName} = _{field.RefSheetName}[value->_{newName}];\r\n" +
                            $"{indent + "\t\t"}}});\r\n");
                    }
                    else if (field.TypeId == ValueType.LIST)
                    {
                        var newName = char.ToUpper(field.Name[0]) + field.Name.Substring(1);
                        sb.Append(
                            $"{indent + "\t\t"}tasks.push_back([&](){{\r\n" +
                            $"{indent + "\t\t\t"}for (auto& [key, value] : _{schema.SheetName})\r\n" +
                            $"{indent + "\t\t\t"}{{\r\n" +
                            $"{indent + "\t\t\t\t"}for (auto& [key2, value2] : value->{newName})\r\n" +
                            $"{indent + "\t\t\t\t\t"}value2 = _{field.RefSheetName}[key2];\r\n" +
                            $"{indent + "\t\t\t"}}}\r\n" +
                            $"{indent + "\t\t"}}});\r\n");
                    }
                }
            }
            sb.Append($"\r\n");
            sb.Append($"{indent + "\t\t"}while(!tasks.empty()) {{ auto task = tasks.front(); tasks.pop_front(); task(); }}\r\n");
            sb.Append($"\r\n");
            foreach (var pair in schemaInfos)
            {
                var schema = pair.Value;
                sb.Append($"{indent + "\t\t"}{schema.SheetName}.insert(_{schema.SheetName}.begin(), _{schema.SheetName}.end());\r\n");
            }

            sb.Append($"{indent + "\t"}}}\r\n");

            foreach (var pair in schemaInfos)
            {
                var schema = pair.Value;
                sb.Append($"{indent + "\t"}static std::map<int, const {usingNamespace}::{schema.SheetName}*> {schema.SheetName};\r\n");
            }
            sb.Append($"{indent}}};\r\n");

            foreach (var pair in schemaInfos)
            {
                var schema = pair.Value;
                sb.Append($"{indent}std::map<int, const {usingNamespace}::{schema.SheetName}*> StaticData::{schema.SheetName};\r\n");
            }

            sb.Append($"{indent}\r\n");
        }

        protected override void MakeDefaultClass(ref StringBuilder sb, ref string indent)
        {
            sb.Append($"{indent}struct Vec3\r\n");
            sb.Append($"{indent}{{\r\n");
            sb.Append($"{indent + "\t"}float x;\r\n");
            sb.Append($"{indent + "\t"}float y;\r\n");
            sb.Append($"{indent + "\t"}float z;\r\n");
            sb.Append($"{indent}}};\r\n");
            sb.Append($"\r\n");
            sb.Append($"{indent}void from_json(const json& j, Vec3& dataObj)\r\n");
            sb.Append($"{indent}{{\r\n");
            sb.Append($"{indent + "\t"}dataObj.x = j.at(\"x\").get<float>();\r\n");
            sb.Append($"{indent + "\t"}dataObj.y = j.at(\"y\").get<float>();\r\n");
            sb.Append($"{indent + "\t"}dataObj.z = j.at(\"z\").get<float>();\r\n");
            sb.Append($"{indent}}}\r\n");
            sb.Append($"\r\n");

            sb.Append($"{indent}struct Vec2\r\n");
            sb.Append($"{indent}{{\r\n");
            sb.Append($"{indent + "\t"}float x;\r\n");
            sb.Append($"{indent + "\t"}float y;\r\n");
            sb.Append($"{indent}}};\r\n");
            sb.Append($"\r\n");
            sb.Append($"{indent}void from_json(const json& j, Vec2& dataObj)\r\n");
            sb.Append($"{indent}{{\r\n");
            sb.Append($"{indent + "\t"}dataObj.x = j.at(\"x\").get<float>();\r\n");
            sb.Append($"{indent + "\t"}dataObj.y = j.at(\"y\").get<float>();\r\n");
            sb.Append($"{indent}}}\r\n");
            sb.Append($"\r\n");
        }

        protected void GenerateLoadFunc(ref StringBuilder sb, string indent, ref DataSchema schemaInfo)
        {
            sb.Append($"{indent}void {schemaInfo.SheetName}::Load(std::string jsonDir, std::map<int, {schemaInfo.SheetName}*>&data)\r\n");
            sb.Append($"{indent}{{\r\n");
            sb.Append($"{indent + "\t"}std::ifstream inputFile(jsonDir +\"/{schemaInfo.SheetName}.json\");\r\n");
            sb.Append($"{indent + "\t"}if (inputFile.is_open())\r\n");
            sb.Append($"{indent + "\t"}{{\r\n");
            sb.Append($"{indent + "\t\t"}std::stringstream buffer;\r\n");
            sb.Append($"{indent + "\t\t"}buffer << inputFile.rdbuf();\r\n");
            sb.Append($"{indent + "\t\t"}json j = json::parse(buffer.str());\r\n");
            sb.Append($"{indent + "\t\t"}for (const auto& elem : j)\r\n");
            sb.Append($"{indent + "\t\t"}{{\r\n");
            sb.Append($"{indent + "\t\t\t"}auto item = elem.get<{schemaInfo.SheetName}>();\r\n");
            sb.Append($"{indent + "\t\t\t"}data.emplace(item.Id, new {schemaInfo.SheetName}(item));\r\n");
            sb.Append($"{indent + "\t\t"}}}\r\n");
            sb.Append($"{indent + "\t"}}}\r\n");
            sb.Append($"{indent}}}\r\n");
            sb.Append($"\r\n");
        }

        protected override void GenerateClass(ref StringBuilder sb, ref string indent, ref DataSchema schemaInfo, bool server = false)
        {
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

            foreach(var refName in set)
                sb.Append($"{indent}class {refName};\r\n");

            sb.Append($"{indent}class {schemaInfo.SheetName}\r\n");
            sb.Append($"{indent}{{\r\n");
            sb.Append($"{indent}public:\r\n");
            sb.Append($"{indent + "\t"}static void Load(std::string jsonDir, std::map<int, {schemaInfo.SheetName}*>&data);\r\n");

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

        protected override void GetField(ref StringBuilder sb, ref string indent, ref FieldInfo field)
        {
            var newName = char.ToUpper(field.Name[0]) + field.Name.Substring(1);
            switch (field.TypeId)
            {
                case ValueType.INT:
                    if(field.RefSheetName.Length > 0)
                    {
                        sb.Append($"{indent}int _{newName} = 0;\r\n");
                        sb.Append($"{indent}{field.RefSheetName}* {newName} = nullptr;\r\n");
                    }
                    else
                        sb.Append($"{indent}int {newName} = 0;\r\n");
                    break;
                case ValueType.FLOAT:
                    sb.Append($"{indent}float {newName} = 0.0f;\r\n");
                    break;
                case ValueType.STRING:
                    sb.Append($"{indent}std::string {newName} = \"\";\r\n");
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
                case ValueType.VEC2:
                    sb.Append($"{indent}Vec2 {newName};\r\n");
                    break;
                case ValueType.LIST:
                    sb.Append($"{indent}std::map<int, {field.RefSheetName}*> {newName};\r\n");
                    break;
            }
        }
        protected override void GenerateJsonParser(ref StringBuilder sb, ref string indent, ref DataSchema schemaInfo, bool server = false)
        {
            sb.Append($"{indent}void from_json(const json& j, {schemaInfo.SheetName}& dataObj)\r\n");
            sb.Append($"{indent}{{\r\n");

            var fieldIndent = indent + "\t";
            foreach (var pair in schemaInfo.FieldInfos)
            {
                var field = pair.Value;
                if (server)
                {
                    if (field.Server == false)
                        continue;

                    GetParseJsonField(ref sb, ref fieldIndent, ref field);
                }
                else
                {
                    if (field.Client == false)
                        continue;

                    GetParseJsonField(ref sb, ref fieldIndent, ref field);
                }
            }

            sb.Append($"{indent}}}\r\n");
            sb.Append($"\r\n");
        }
        protected override void GetParseJsonField(ref StringBuilder sb, ref string indent, ref FieldInfo field) 
        {
            var newName = char.ToUpper(field.Name[0]) + field.Name.Substring(1);
            switch (field.TypeId)
            {
                case ValueType.INT:
                    if (field.RefSheetName.Length > 0)
                        sb.Append($"{indent}dataObj._{newName} = j.at(\"{field.Name}\").get<int>();\r\n");
                    else
                        sb.Append($"{indent}dataObj.{newName} = j.at(\"{field.Name}\").get<int>();\r\n");
                    break;
                case ValueType.FLOAT:
                    sb.Append($"{indent}dataObj.{newName} = j.at(\"{field.Name}\").get<float>();\r\n");
                    break;
                case ValueType.STRING:
                    sb.Append($"{indent}dataObj.{newName} = j.at(\"{field.Name}\").get<std::string>();;\r\n");
                    break;
                case ValueType.BOOL:
                    sb.Append($"{indent}dataObj.{newName} = j.at(\"{field.Name}\").get<bool>();\r\n");
                    break;
                case ValueType.DATETIME:
                    sb.Append($"{indent}dataObj.{newName} = j.at(\"{field.Name}\").get<bool>();\r\n");
                    break;
                case ValueType.VEC3:
                    sb.Append($"{indent}dataObj.{newName} = j.at(\"{field.Name}\").get<Vec3>();\r\n");
                    break;
                case ValueType.VEC2:
                    sb.Append($"{indent}dataObj.{newName} = j.at(\"{field.Name}\").get<Vec2>();\r\n");
                    break;
                case ValueType.LIST:
                    sb.Append($"{indent}{{\r\n");
                    sb.Append($"{indent + "\t"}auto ids = j.at(\"{field.Name}\").get<std::list<int>>();\r\n");
                    sb.Append($"{indent + "\t"}for(auto id : ids) dataObj.{newName}[id] = nullptr;\r\n");
                    sb.Append($"{indent}}}\r\n");
                    break;
            }
        }
    }
}
