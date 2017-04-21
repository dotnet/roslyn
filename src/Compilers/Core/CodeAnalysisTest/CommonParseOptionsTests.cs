// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
