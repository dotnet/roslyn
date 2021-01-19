// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    [Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public class EnableKeywordRecommenderTests : KeywordRecommenderTests
    {
        [Fact]
        [WorkItem(31130, "https://github.com/dotnet/roslyn/issues/31130")]
        public void TestAfterNullable()
            => VerifyKeyword(@"#nullable $$");

        [Fact]
        [WorkItem(31130, "https://github.com/dotnet/roslyn/issues/31130")]
        public void TestNotAfterNullableAndNewline()
        {
            VerifyAbsence(@"
#nullable 
$$
");
        }

        [Fact]
        [WorkItem(31130, "https://github.com/dotnet/roslyn/issues/31130")]
        public void TestNotAfterHash()
            => VerifyAbsence(@"#$$");

        [Fact]
        public void TestNotAtRoot_Interactive()
            => VerifyAbsence(SourceCodeKind.Script, @"$$");

        [Fact]
        public void TestNotAfterClass_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"class C { }
$$");
        }

        [Fact]
        public void TestNotAfterGlobalStatement_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"System.Console.WriteLine();
$$");
        }

        [Fact]
        public void TestNotAfterGlobalVariableDeclaration_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"int i = 0;
$$");
        }

        [Fact]
        public void TestNotInUsingAlias()
            => VerifyAbsence(@"using Goo = $$");

        [Fact]
        public void TestNotInEmptyStatement()
            => VerifyAbsence(AddInsideMethod(@"$$"));

        [Fact]
        public void TestNotAfterPragma()
            => VerifyAbsence(@"#pragma $$");

        [Fact]
        public void TestAfterPragmaWarning()
            => VerifyKeyword(@"#pragma warning $$");

        [Fact]
        public void TestNotAfterPragmaWarningEnable()
            => VerifyAbsence(@"#pragma warning enable $$");
    }
}
