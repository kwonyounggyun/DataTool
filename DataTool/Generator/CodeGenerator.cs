using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataTool.Generator
{
    public class CodeBlock
    {
        public void AddPost(string line)
        {
            _postRows.Add(line);
        }
        public void AddBlock(CodeBlock block)
        {
            _innerBlocks.Add(block);
        }
        public void AddBlock(List<CodeBlock> blocks)
        {
            _innerBlocks.AddRange(blocks);
        }
        public void AddRow(string line)
        {
            _innerRows.Add(line);
        }
        public void SetBlockName(string blockName) { _blockName = blockName; }
        public virtual string MakeCode(int indentCount = 0)
        {
            StringBuilder sb = new StringBuilder();

            foreach (var row in _postRows)
                WriteLine(ref sb, indentCount, row);

            if (_blockName != null)
                WriteLine(ref sb, indentCount, _blockName);
            WriteLine(ref sb, indentCount, "{");
            foreach (var block in _innerBlocks)
                WriteLine(ref sb, 0, block.MakeCode(indentCount + 1));

            foreach (var row in _innerRows)
                WriteLine(ref sb, indentCount + 1, row);
            WriteLine(ref sb, indentCount, "}");

            return sb.ToString();
        }
        protected void WriteLine(ref StringBuilder sb, int indentCount, string str)
        {
            sb.AppendLine(str.PadLeft(str.Length + indentCount, '\t'));
        }

        protected List<string> _postRows = new List<string>();
        protected List<CodeBlock> _innerBlocks = new List<CodeBlock>();
        protected List<string> _innerRows = new List<string>();
        protected string? _blockName;
    }

    public abstract class CodeGenerator
    {
        public abstract void Generate(string outFilePath, string usingNamespace, ConcurrentDictionary<string, DataSchema> schemaInfos, bool server = false);
        protected abstract List<CodeBlock> GetDefaultClasses();
        protected abstract CodeBlock GenerateClass(ref DataSchema schemaInfo, bool server = false);
        protected abstract void GetField(ref FieldInfo field, CodeBlock block);
        protected abstract CodeBlock GenerateJsonParser(ref DataSchema schemaInfo, bool server = false);
        protected abstract void GetParseJsonField(ref FieldInfo field, CodeBlock block);
    }
}
