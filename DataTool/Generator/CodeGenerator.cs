using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataTool.Generator
{
    public abstract class CodeGenerator
    {
        public abstract void Generate(ref string outFilePath, ref string usingNamespace, ref ConcurrentDictionary<string, DataSchema> schemaInfos, bool server = false);
        protected abstract void MakeDefaultClass(ref StringBuilder sb, ref string indent);
        protected abstract void GenerateClass(ref StringBuilder sb, ref string indent, ref DataSchema schemaInfo, bool server = false);
        protected abstract void GetField(ref StringBuilder sb, ref string indent, ref FieldInfo field);
        protected abstract void GenerateJsonParser(ref StringBuilder sb, ref string indent, ref DataSchema schemaInfo, bool server = false);
        protected abstract void GetParseJsonField(ref StringBuilder sb, ref string indent, ref FieldInfo field);
        protected void MakeDefaultClass(ref StringBuilder sb, int indentCount)
        {
            var indent = MakeIndent(indentCount);
            MakeDefaultClass(ref sb, ref indent);
        }
        protected void GenerateClass(ref StringBuilder sb, int indentCount, ref DataSchema schemaInfo, bool server = false)
        {
            var indent = MakeIndent(indentCount);
            GenerateClass(ref sb, ref indent, ref schemaInfo, server);
        }
        protected void GenerateJsonParser(ref StringBuilder sb, int indentCount, ref DataSchema schemaInfo, bool server = false)
        {
            var indent = MakeIndent(indentCount);
            GenerateJsonParser(ref sb, ref indent, ref schemaInfo, server);
        }
        protected static string MakeIndent(int indentCount)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < indentCount; i++)
                sb.Append("\t");

            return sb.ToString();
        }
    }
}
