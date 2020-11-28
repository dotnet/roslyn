// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests
{
    public class CommonParseOptionsTests
    {
        /// <summary>
        /// If this test fails, please update the <see cref="ParseOptions.GetHashCodeHelper"/>
        /// and <see cref="ParseOptions.EqualsHelper"/> methods to
        /// make sure they are doing the right thing with your new field and then update the baseline
        /// here.
        /// </summary>
        [Fact]
        public void TestFieldsForEqualsAndGetHashCode()
        {
            ReflectionAssert.AssertPublicAndInternalFieldsAndProperties(
                typeof(ParseOptions),
                "DocumentationMode",
                "Errors",
                "Features",
                "Kind",
                "Language",
                "PreprocessorSymbolNames",
                "SpecifiedKind");
        }
    }
}
