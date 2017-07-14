using System;
using System.IO;
using System.Text;
using Wyman.WireType.grammar;
using Xunit;

namespace Wyman.WireType.Test
{
    public class ParserTests
    {
        [Fact]
        public void EnumMatch()
        {
            var source = new SourceStream("enum name { value = 1, second = 2 }".ToCharArray());
            var parser = new Parser();

            Assert.True(parser.Match(source, out EnumType result));
            Assert.NotNull(result);
            Assert.Equal("name", result.Name(), StringComparer.Ordinal);
            validate.Count(2, result.members());
            validate.All(((string name, byte ordinal) member) =>
                           {
                               return !(member.name is null)
                                   && member.ordinal > 0;
                           },
                           result.members());
        }

        [Fact]
        public void BigMatch()
        {
            const string content = @"
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
		list single Values = 4;
	}
}";
            var source = new SourceStream(content.ToCharArray());
            var parser = new Parser();

            Assert.True(parser.MatchNamespace(source, out BaseType result));

            if (result is NamespaceType ns)
            {
                var table = ns.create_table();
            }
        }

        [Fact]
        public void GoBig()
        {
            const string content1 = @"
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
	}
}";
            const string content2 = @"
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
        type Person Mother = 4;
    }
}";
            using (var stream = File.Open("sample.bbd", FileMode.Create, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
            {
                writer.Write(content1);
            }

            using (var stream = File.Open("sample-ex.bbd", FileMode.Create, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
            {
                writer.Write(content2);
            }

            var parser = new Parser();
            var table = parser.GetSymbolsFromFile("sample-ex.bbd");

            Assert.NotNull(table);
            Assert.NotEmpty(table);
            Assert.Equal(16, table.Count());

            Assert.All(table, x => Assert.True((x.type is NamespaceType && x.type.Name().IsLegalFullName()) || x.type.Name().IsNameLegal()));
            Assert.All(table, x => Assert.True(StringComparer.Ordinal.Equals(x.name, x.type.FullName())));
            Assert.All(table, x => Assert.True(!(x.type is MemberType) || (x.type is MemberType m && (m.Kind() & ~MemberKind.List) != 0)));
        }
    }
}
