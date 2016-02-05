using Roslyn.Test.Utilities;
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
            string isABaseDirectory;
            if (PortableShim.Path.DirectorySeparatorChar == '/')
            {
                isABaseDirectory = "/";
            }
            else
            {
                isABaseDirectory = "C://";
            }

            try {
                new SourceFileResolver(
                    ImmutableArray.Create(""),
                    isABaseDirectory,
                    ImmutableArray.Create(KeyValuePair.Create<string, string>("key", null)));
                AssertEx.Fail("Didn't throw");
            }
            catch (ArgumentException argException)
            {
                Assert.Equal(new ArgumentException(CodeAnalysisResources.NullValueInPathMap, "pathMap").Message, argException.Message);
            }

            // Empty pathmap value doesn't throw
            new SourceFileResolver(
                ImmutableArray.Create(""), 
                isABaseDirectory, 
                ImmutableArray.Create(KeyValuePair.Create<string, string>("key", "")));
        }

        [Fact]
        public void BadBaseDirectory()
        {
            try {
                new SourceFileResolver(
                    ImmutableArray.Create(""),
                    "not_a_root directory",
                    ImmutableArray.Create(KeyValuePair.Create<string, string>("key", "value")));
                AssertEx.Fail("Didn't throw");
            }
            catch (ArgumentException argExeption)
            {
                Assert.Equal(new ArgumentException(CodeAnalysisResources.AbsolutePathExpected, "baseDirectory").Message, argExeption.Message);
            }
        }
    }
}
