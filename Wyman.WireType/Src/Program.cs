using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Wyman.WireType.Cli
{
    internal class Program
    {
        private const string AnsiCExtension = ".c";
        private const string CsharpExtension = ".cs";
        private static readonly IReadOnlyList<string> _defaultFiles = Array.Empty<string>();
        private static readonly Dictionary<string, (string, Emitter.Emitter)> _langEmitters;

        static Program()
        {
            var ansiCEmitter = new Emitter.AnciCEmitter();
            var cshapeEmitter = new Emitter.CSharpEmitter();

            _langEmitters = new Dictionary<string, (string, Emitter.Emitter)>(StringComparer.OrdinalIgnoreCase)
            {
                { "csharp", (CsharpExtension, cshapeEmitter) },
                { "c_sharp", (CsharpExtension, cshapeEmitter) },
                { "cs", (CsharpExtension, cshapeEmitter) },
                { ".net", (CsharpExtension, cshapeEmitter) },
                { "c", (AnsiCExtension, ansiCEmitter) },
                { "clang", (AnsiCExtension, ansiCEmitter) },
                { "c_lang", (AnsiCExtension, ansiCEmitter) },
                { "ansic", (AnsiCExtension, ansiCEmitter) },
                { "ansi_c", (AnsiCExtension, ansiCEmitter) },
            };
        }

        public Program()
        {
            _symbolTables = new Queue<SymbolTable>();
            _syncpoint = new object();
        }

        internal static IReadOnlyDictionary<string, (string extension, Emitter.Emitter emitter)> LangEmitters
            => _langEmitters;

        private readonly Queue<SymbolTable> _symbolTables;
        private readonly object _syncpoint;

        [CommandLine.OptionArray('f', "files", HelpText = "IDL file to be parsed, and transpiled into code.", MutuallyExclusiveSet = "help")]
        public string[] inputFiles { get; set; }

        [CommandLine.Option('i', "input", HelpText = "Folder for all input files to read from. Defaults to the current working directory if not specified.")]
        public string inputLocation { get; set; }

        [CommandLine.Option('l', "lang", HelpText = "Language for the output files to written in.", MutuallyExclusiveSet = "help")]
        public string outputLanguage { get; set; }

        [CommandLine.Option('o', "output", HelpText = "Folder for all generated files to be placed. Defaults to the current working directory if not specified.")]
        public string OutputLocation { get; set; }

        public static Program Create(string[] args)
        {
            var program = new Program();

            CommandLine.Parser.Default.ParseArguments(args, program);

            if (program.inputFiles is null)
            {
                program.inputFiles = Array.Empty<string>();
            }

            if (string.IsNullOrWhiteSpace(program.inputLocation))
            {
                program.inputLocation = Directory.GetCurrentDirectory();
            }

            if (string.IsNullOrWhiteSpace(program.OutputLocation))
            {
                program.OutputLocation = Directory.GetCurrentDirectory();
            }

            return program;
        }

        public void Compile()
        {
            var symbolTable = new SymbolTable();

            var dirInfo = new DirectoryInfo(OutputLocation);

            if (!dirInfo.Exists)
            {
                dirInfo.Create();
                dirInfo.Refresh();
            }

            Parallel.ForEach(inputFiles, (string path) =>
            {
                path = Path.Combine(inputLocation, path);

                ParseFile(path, symbolTable);
            });

            var enums = symbolTable.GetAll<grammar.EnumType>();
            var structs = symbolTable.GetAll<grammar.StructType>();

            var enumsStructs = new List<grammar.BaseType>();
            enumsStructs.AddRange(enums);
            enumsStructs.AddRange(structs);

            var emitter = GetEmitter();
            var extension = GetExtension();

            object syncpoint = new object();

            Parallel.ForEach(enumsStructs, (grammar.BaseType value) =>
            {
                lock (syncpoint)
                {
                    var path = GetFileName(value.Name(), extension, extension.Equals(AnsiCExtension, StringComparison.Ordinal));

                    output_file(emitter, symbolTable, value, path);
                }
            });

            var headers = emitter.GetHeaders();

            if (!(headers is null) && headers.Count > 0)
            {
                foreach (var fileName in headers.Keys)
                {
                    var path = GetFileName(fileName, ".h", true);

                    var fileInfo = new FileInfo(path);
                    var typeList = headers[fileName];
                    bool success = false;

                    try
                    {
                        using (var stream = fileInfo.Open(FileMode.Create, FileAccess.Write, FileShare.Read))
                        using (var writer = new StreamWriter(stream, System.Text.Encoding.UTF8))
                        {
                            success = emitter.WriteHeader(symbolTable.Get(fileName) as grammar.NamespaceType, typeList, symbolTable, writer);
                        }
                    }
                    catch
                    {
                        success = false;

                        throw;
                    }
                    finally
                    {
                        if (!success)
                        {
                            fileInfo.Refresh();

                            if (fileInfo.Exists)
                            {
                                fileInfo.Delete();
                            }
                        }
                    }
                }
            }
        }

        [CommandLine.HelpOption('?', "help")]
        public string GetUsage()
        {
            var help = new CommandLine.Text.HelpText
            {
                Heading = new CommandLine.Text.HeadingInfo("<<app title>>", "<<app version>>"),
                Copyright = new CommandLine.Text.CopyrightInfo("<<app author>>", DateTime.UtcNow.Year),
                AdditionalNewLineAfterOption = true,
                AddDashesToOption = true
            };
            help.AddPreOptionsLine("<<license details here.>>");
            help.AddPreOptionsLine("Usage: app -p Someone");
            help.AddOptions(this);
            return help;
        }

        internal Emitter.Emitter GetEmitter()
        {
            if (LangEmitters.TryGetValue(outputLanguage, out (string, Emitter.Emitter emitter) value))
                return value.emitter;

            throw new Exception("NEED TYPED EXCEPTION HERE");
        }

        internal string GetExtension()
        {
            if (LangEmitters.TryGetValue(outputLanguage, out (string extension, Emitter.Emitter) value))
                return value.extension;

            throw new Exception("NEED TYPED EXCEPTION HERE");
        }

        internal string GetFileName(string baseName, string extension, bool toLower = false)
        {
            string fileName = baseName.Trim();
            fileName = fileName.Replace('.', '_');
            fileName = fileName + extension;

            if (toLower)
            {
                fileName = fileName.ToLower();
            }

            fileName = Path.Combine(OutputLocation, fileName);

            return fileName;
        }

        private void output_file(Emitter.Emitter emitter, SymbolTable symbolTable, grammar.BaseType baseType, string path)
        {
            var fileInfo = new FileInfo(path);
            bool success = false;

            try
            {
                using (var stream = fileInfo.Open(FileMode.Create, FileAccess.Write, FileShare.Read))
                using (var writer = new StreamWriter(stream, System.Text.Encoding.UTF8))
                {
                    switch (baseType)
                    {
                        case grammar.EnumType enum_type:
                            success = emitter.WriteType(enum_type, symbolTable, writer);
                            break;

                        case grammar.StructType struct_type:
                            success = emitter.WriteType(struct_type, symbolTable, writer);
                            break;

                        default:
                            throw new Exception("NEED TYPED EXEPCTION HERE");
                    }
                }
            }
            catch
            {
                success = false;

                throw;
            }
            finally
            {
                if (!success)
                {
                    fileInfo.Refresh();

                    if (fileInfo.Exists)
                    {
                        fileInfo.Delete();
                    }
                }
            }
        }

        private void ParseFile(string file, SymbolTable tables)
        {
            var filePath = inputFiles[0];
            var fileInfo = new FileInfo(filePath);

            filePath = fileInfo.FullName;

            if (!fileInfo.Exists)
                throw new FileNotFoundException(filePath);

            var parser = new grammar.Parser();
            var table = parser.GetSymbolsFromFile(filePath);

            tables.Add(table);
        }

        private static void Main(string[] args)
        {
            Program program = Create(args);
        }
    }
}
