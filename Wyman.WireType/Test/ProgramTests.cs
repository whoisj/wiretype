using System;
using System.IO;
using System.Text;
using Xunit;

namespace Wyman.WireType.Test
{
    public class ProgramTests
    {
        const string Content1 = @"
/* namespaces use the C# format of declaration */
namespace Sample
{
    enum Values
    {
        Foo = 1,
        Bar = 2,
        Baz = 3,
    }

	struct Basic
	{
		type string Text = 1;
		type i32 Value = 2;
        type Values Enum = 3;
		// an array of 32 bit floating values is easy to declare
		list f32 Values = 4;
        list u64 List = 5;
        list Values Enums = 6;
	}
}";
        const string Content2 = @"
import ""sample.bbd""

namespace Sample
{
    /* structs can nest themselves */
    struct Complex
    {
        /* referencing of types from imported files uses the same rules as .NET */
        list Basic   Leaves = 1; // ordinals can be any number 1-255
        list Complex Branches = 4; // ordinals do not need to be sequential
    }
}

/* namespaces can be complex when needed */
namespace Sample.More
{
    struct Person
    {
        type string Name = 1;
        type timestamp DoB = 2;
        type timespan Len = 3;
        list blob Images = 567; // Ordinals can be out of order, and skip values!
        type Person Mother = 4;
        type blob Raw = 8;
        list string Friends = 7;
    }
}";

        [Fact(DisplayName = "program.compile - basic.c")]
        public void AnsiCGenerate()
        {
            using (var stream = File.Open("sample.bbd", FileMode.Create, FileAccess.Write, FileShare.Read))
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
            {
                writer.Write(Content1);
            }

            using (var stream = File.Open("sample-ex.bbd", FileMode.Create, FileAccess.Write, FileShare.Read))
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
            {
                writer.Write(Content2);
            }

            var program = Cli.Program.Create(new[] { "-l", "c", "-f", "sample-ex.bbd", "-o", "basic_ansi-c" });
            Assert.NotNull(program);
            Assert.Single(program.inputFiles);
            Assert.Equal("sample-ex.bbd", program.inputFiles[0], StringComparer.OrdinalIgnoreCase);
            Assert.Equal("basic_ansi-c", program.OutputLocation, StringComparer.OrdinalIgnoreCase);
            Assert.Equal("c", program.outputLanguage, StringComparer.OrdinalIgnoreCase);

            program.Compile();

            Assert.True(File.Exists("basic_ansi-c/basic.c"), "Expected output: \"basic_ansi-c/basic.c\" not found.");
            Assert.True(File.Exists("basic_ansi-c/complex.c"), "Expected output: \"basic_ansi-c/complex.c\" not found.");
            Assert.True(File.Exists("basic_ansi-c/person.c"), "Expected output: \"basic_ansi-c/person.c\" not found.");
        }

        [Fact(DisplayName = "program.compile - basic.cs")]
        public void CSharpGenerate()
        {
            using (var stream = File.Open("sample.bbd", FileMode.Create, FileAccess.Write, FileShare.Read))
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
            {
                writer.Write(Content1);
            }

            using (var stream = File.Open("sample-ex.bbd", FileMode.Create, FileAccess.Write, FileShare.Read))
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
            {
                writer.Write(Content2);
            }

            var program = Cli.Program.Create(new[] { "-l", "cs", "-f", "sample-ex.bbd", "-o", "basic_csharp" });
            Assert.NotNull(program);
            Assert.Single(program.inputFiles);
            Assert.Equal("sample-ex.bbd", program.inputFiles[0], StringComparer.OrdinalIgnoreCase);
            Assert.Equal("basic_csharp", program.OutputLocation, StringComparer.OrdinalIgnoreCase);
            Assert.Equal("cs", program.outputLanguage, StringComparer.OrdinalIgnoreCase);

            program.Compile();

            Assert.True(File.Exists("basic_csharp/basic.cs"), "Expected output: \"basic_csharp/basic.cs\" not found.");
            Assert.True(File.Exists("basic_csharp/complex.cs"), "Expected output: \"basic_csharp/complex.cs\" not found.");
            Assert.True(File.Exists("basic_csharp/person.cs"), "Expected output: \"basic_csharp/person.cs\" not found.");
            Assert.True(File.Exists("basic_csharp/values.cs"), "Expected output: \"basic_csharp/enum.cs\" not found.");
        }

        [Fact(DisplayName = "program.usage")]
        public void HelpParse()
        {
            var program = Cli.Program.Create(new[] { "help" });
            Assert.NotNull(program);

            Assert.NotNull(program.GetUsage());
        }

        static public object[][] OptionsData
        {
            get
            {
                return new object[][]
                {
                    new object[]
                    {
                        new string[] { "-f", "foo/bar" }, 1, (string)null,
                    },
                    new object[]
                    {
                        new string[] { "--files", "foo/bar" }, 1, (string)null,
                    },
                    new object[]
                    {
                        new string[] { "-files", "foo/bar", "--lang", "cs" }, 2, (string)null,
                    },
                    new object[]
                    {
                        new string[] { "-f", "foo/bar", "src/code", "src/code2", "foo/bar2", "src/code3", "src/code4", "-l", "C" }, 6, "C",
                    },
                    new object[]
                    {
                        new string[] { "-l", "CSharp", "--lang", "CS" }, 0, "CS",
                    },
                    new object[]
                    {
                        new string[] { "-l", "C", "--lang", "CS" }, 0, "CS",
                    },
                };
            }
        }

        [Theory(DisplayName = "program.options")]
        [MemberData(nameof(OptionsData), DisableDiscoveryEnumeration = false)]
        public void OptionsParse(string[] options, int expectedFileCount, string expectedLanguage)
        {
            var program = Cli.Program.Create(options);
            Assert.NotNull(program);

            Assert.Equal(expectedFileCount, program.inputFiles.Length);
            Assert.Equal(expectedLanguage, program.outputLanguage, StringComparer.Ordinal);
        }
    }
}
