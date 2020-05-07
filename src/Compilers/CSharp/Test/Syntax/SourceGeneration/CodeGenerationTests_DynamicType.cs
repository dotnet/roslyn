// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.SourceGeneration;
using Roslyn.Test.Utilities;
using Xunit;
using static Microsoft.CodeAnalysis.SourceGeneration.CodeGenerator;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.SourceGeneration
{
    public partial class CodeGenerationTests
    {
        [Fact]
        public void TestDynamic()
        {
            AssertEx.AreEqual(
"dynamic",
DynamicType().GenerateTypeString());
        }

        [Fact]
        public void TestNullableDynamic()
        {
            AssertEx.AreEqual(
"dynamic?",
DynamicType(nullableAnnotation: CodeAnalysis.NullableAnnotation.Annotated).GenerateTypeString());
        }
    }
}
