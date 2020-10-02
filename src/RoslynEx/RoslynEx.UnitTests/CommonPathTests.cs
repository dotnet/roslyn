using System.Collections.Generic;
using System.Linq;
using Roslyn.Test.Utilities;
using Xunit;

namespace RoslynEx.UnitTests
{
    public class CommonPathTests
    {
        [Fact]
        public void GetPrefixSame()
        {
            var path = new[] { "foo", "bar", "baz" };

            Assert.Equal(path, CommonPath.GetPrefix(path, path).ToArray());
        }

        [Fact]
        public void GetPrefixPrefix()
        {
            var path1 = new[] { "foo", "bar", "baz" };
            var path2 = new[] { "foo", "bar" };

            Assert.Equal(new[] { "foo", "bar" }, CommonPath.GetPrefix(path1, path2).ToArray());
        }

        [Fact]
        public void GetPrefixSomewhatDifferent()
        {
            var path1 = new[] { "foo", "bar", "baz" };
            var path2 = new[] { "foo", "bar", "quux" };

            Assert.Equal(new[] { "foo", "bar" }, CommonPath.GetPrefix(path1, path2).ToArray());
        }

        [Fact]
        public void GetPrefixCompletelyDifferent()
        {
            var path1 = new[] { "foo", "bar", "baz" };
            var path2 = new[] { "quux" };

            Assert.Empty(CommonPath.GetPrefix(path1, path2).ToArray());
        }

        private static IEnumerable<string?[][]> PrefixRemoverData()
        {
            yield return new[] { new[] { @"C:\code\RoslynEx\src\file1.cs", @"C:\code\RoslynEx\src\file2.cs" }, new[] { "file1.cs", "file2.cs" } };
            yield return new[] { new[] { @"C:\code\RoslynEx\src\file1.cs", @"E:\code\RoslynEx\src\file2.cs" }, new[] { @"C\code\RoslynEx\src\file1.cs", @"E\code\RoslynEx\src\file2.cs" } };
            yield return new[] { new[] { @"C:\code\RoslynEx\src\file1.cs", @"C:\code\RoslynEx\src\file2.cs", @"C:\code\other.cs" }, new[] { @"RoslynEx\src\file1.cs", @"RoslynEx\src\file2.cs", "other.cs" } };
            yield return new[] { new[] { @"C:\code\RoslynEx\src\file.cs", "generated.cs" }, new[] { "file.cs", "generated.cs" } };
            yield return new[] { new[] { @"C:\code\RoslynEx\src\file.cs", null }, new[] { "file.cs", "" } };
            yield return new[] { new[] { @"C:\code\RoslynEx\src\file.cs", "" }, new[] { "file.cs", "" } };
        }

        [ConditionalTheory(typeof(WindowsOnly)), MemberData(nameof(PrefixRemoverData))]
        public void PrefixRemover(string?[] paths, string[] expected)
        {
            var prefixRemover = CommonPath.MakePrefixRemover(paths);

            var actual = paths.Select(prefixRemover);

            Assert.Equal(expected, actual);
        }
    }
}
