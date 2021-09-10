// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using System;
using System.Collections.Immutable;
using System.IO;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class SourceFileResolverTest
    {
        [Fact]
        public void IncorrectPathmaps()
        {
            string isABaseDirectory;
            if (Path.DirectorySeparatorChar == '/')
            {
                isABaseDirectory = "/";
            }
            else
            {
                isABaseDirectory = "C://";
            }

            try
            {
                new SourceFileResolver(
                    ImmutableArray.Create(""),
                    isABaseDirectory,
                    ImmutableArray.Create(KeyValuePairUtil.Create<string, string>("key", null)));
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
                ImmutableArray.Create(KeyValuePairUtil.Create<string, string>("key", "")));
        }

        [Fact]
        public void BadBaseDirectory()
        {
            try
            {
                new SourceFileResolver(
                    ImmutableArray.Create(""),
                    "not_a_root directory",
                    ImmutableArray.Create(KeyValuePairUtil.Create<string, string>("key", "value")));
                AssertEx.Fail("Didn't throw");
            }
            catch (ArgumentException argException)
            {
                Assert.Equal(new ArgumentException(CodeAnalysisResources.AbsolutePathExpected, "baseDirectory").Message, argException.Message);
            }
        }
    }
}
