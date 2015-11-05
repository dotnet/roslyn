using Roslyn.Utilities;
using System;
using System.Collections.Immutable;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class SourceFileResolverTest
    {
        [Fact]
        public void IncorrectPathmaps()
        {
            string isABaseDirectory = "";
            if (PortableShim.Path.DirectorySeparatorChar == '/') {
                isABaseDirectory = "/";
            }
            else
            {
                isABaseDirectory = "C://";
            }

            Assert.Throws<ArgumentNullException>(() =>
                new SourceFileResolver(
                    ImmutableArray.Create(""), 
                    isABaseDirectory, 
                    ImmutableArray.Create(KeyValuePair.Create<string, string>("key", null))));
        }

        [Fact]
        public void badBaseDirectory()
        {
            Assert.Throws<ArgumentException>(() => 
                new SourceFileResolver(
                    ImmutableArray.Create(""), 
                    "not_a_root directory", 
                    ImmutableArray.Create(KeyValuePair.Create<string, string>("key", "value"))));
        }
    }
}
