// See https://aka.ms/new-console-template for more information
using DataTool;
using System.Collections.Concurrent;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.ComponentModel;

public class Program
{
    // args는 string[] 타입이며, 여기에 모든 명령줄 인자가 배열로 저장됩니다.
    public static int Main(string[] args)
    {
        var rootCommand = new RootCommand("Excel data convert code and json");

        Option<string> excelDirOption = new("--input", "-i")
        {
            Description = "data excel included directory",
            Required = true
        };
        excelDirOption.AcceptLegalFilePathsOnly();
        rootCommand.Add(excelDirOption);

        Option<string> serverNamespaceOption = new("--server_namespace", "-sn")
        {
            Description = "server using namespace(default = GameData)",
            DefaultValueFactory = data => "GameData"
        };
        rootCommand.Add(serverNamespaceOption);

        Option<string> clientNamespaceOption = new("--client_namespace", "-cn")
        {
            Description = "client using namespace(default = GameData)",
            DefaultValueFactory = data => "GameData"
        };
        rootCommand.Add(clientNamespaceOption);

        Option<string> serverOption = new("--server", "-s")
        {
            Description = "server output type",
        };
        serverOption.AcceptOnlyFromAmong("cpp", "cs");
        rootCommand.Add(serverOption);

        Option<string> clientOption = new("--client", "-c")
        {
            Description = "client output type",
        };
        clientOption.AcceptOnlyFromAmong("cpp", "cs");
        rootCommand.Add(clientOption);

        Option<string> serverOutputOption = new("--server_code_path", "-scp")
        {
            Description = "server code output path(default = ./ServerCode)",
            DefaultValueFactory = data => "./ServerCode"
        };
        serverOutputOption.AcceptLegalFilePathsOnly();
        rootCommand.Add(serverOutputOption);

        Option<string> clientOutputOption = new("--client_code_path", "-ccp")
        {
            Description = "server code output path(default = ./ClientCode)",
            DefaultValueFactory = data => "./ClientCode"
        };
        clientOutputOption.AcceptLegalFilePathsOnly();
        rootCommand.Add(clientOutputOption);

        Option<string> serverJsonOption = new("--server_json_path", "-sjp")
        {
            Description = "server json output path(defalut = ./ServerJson)",
            DefaultValueFactory = data => "./ServerJson"
        };
        serverJsonOption.AcceptLegalFilePathsOnly();
        rootCommand.Add(serverJsonOption);

        Option<string> clientJsonOption = new("--client_json_path", "-cjp")
        {
            Description = "client json output path(defalut = ./ClientJson)",
            DefaultValueFactory = data => "./ClientJson"
        };
        clientJsonOption.AcceptLegalFilePathsOnly();
        rootCommand.Add(clientJsonOption);

        rootCommand.SetAction(parseResult =>
        {
            string[] excelExtensions = { ".xlsx", ".xls" };

            string? excelPath = Path.GetFullPath(parseResult.GetValue(excelDirOption));
            string? server = parseResult.GetValue(serverOption);
            string? client = parseResult.GetValue(clientOption);
            string? serverNamespace = parseResult.GetValue(serverNamespaceOption);
            string? clientNamespace = parseResult.GetValue(clientNamespaceOption);
            string? serverOutput = Path.GetFullPath(parseResult.GetValue(serverOutputOption));
            string? clientOutput = Path.GetFullPath(parseResult.GetValue(clientOutputOption));
            string? serverJsonOutput = Path.GetFullPath(parseResult.GetValue(serverJsonOption));
            string? clientJsonOutput = Path.GetFullPath(parseResult.GetValue(clientJsonOption));

            IEnumerable<string> files = Directory.EnumerateFiles(excelPath, "*.*", SearchOption.TopDirectoryOnly)
            .Where(s => excelExtensions.Contains(Path.GetExtension(s).ToLowerInvariant()))
            .Where(s => !Path.GetFileName(s).StartsWith("~$"));
            List<ExcelReader> readers = new List<ExcelReader>();
            foreach (string file in files)
            {
                ExcelReader reader = new ExcelReader();
                reader.Open($"{file}");
                reader.ReadSchema();
                readers.Add(reader);
            }

            foreach (var reader in readers)
                reader.ReadData();

            readers.Clear();

            if (server != null)
            {
                if (server.CompareTo("cpp") == 0)
                {
                    Directory.CreateDirectory(serverOutput);
                    ExcelReader.MakeCPP(serverOutput, serverNamespace, true);
                }
                else if (server.CompareTo("cs") == 0)
                {
                    Directory.CreateDirectory(serverOutput);
                    ExcelReader.MakeCSharp(serverOutput, serverNamespace, true);
                }

                Directory.CreateDirectory(serverJsonOutput);
                ExcelReader.MakeJson(serverJsonOutput, true);
            }

            if (client != null)
            {
                if (client?.CompareTo("cpp") == 0)
                {
                    Directory.CreateDirectory(clientOutput);
                    ExcelReader.MakeCPP(clientOutput, clientNamespace, false);
                }
                else if (client?.CompareTo("cs") == 0)
                {
                    Directory.CreateDirectory(clientOutput);
                    ExcelReader.MakeCSharp(clientOutput, clientNamespace, false);
                }

                Directory.CreateDirectory(clientJsonOutput);
                ExcelReader.MakeJson(clientJsonOutput, true);
            }
        });

        ParseResult parseResult = rootCommand.Parse(args);
        return parseResult.Invoke();
    }
}