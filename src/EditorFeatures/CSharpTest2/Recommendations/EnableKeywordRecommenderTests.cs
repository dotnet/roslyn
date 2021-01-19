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
        public async Task TestAfterNullable()
            => VerifyKeyword(@"#nullable $$");

        [Fact]
        [WorkItem(31130, "https://github.com/dotnet/roslyn/issues/31130")]
        public async Task TestNotAfterNullableAndNewline()
        {
            VerifyAbsence(@"
#nullable 
$$
");
        }

        [Fact]
        [WorkItem(31130, "https://github.com/dotnet/roslyn/issues/31130")]
        public async Task TestNotAfterHash()
            => VerifyAbsence(@"#$$");

        [Fact]
        public async Task TestNotAtRoot_Interactive()
            => VerifyAbsence(SourceCodeKind.Script, @"$$");

        [Fact]
        public async Task TestNotAfterClass_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"class C { }
$$");
        }

        [Fact]
        public async Task TestNotAfterGlobalStatement_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"System.Console.WriteLine();
$$");
        }

        [Fact]
        public async Task TestNotAfterGlobalVariableDeclaration_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"int i = 0;
$$");
        }

        [Fact]
        public async Task TestNotInUsingAlias()
            => VerifyAbsence(@"using Goo = $$");

        [Fact]
        public async Task TestNotInEmptyStatement()
            => VerifyAbsence(AddInsideMethod(@"$$"));

        [Fact]
        public async Task TestNotAfterPragma()
            => VerifyAbsence(@"#pragma $$");

        [Fact]
        public async Task TestAfterPragmaWarning()
            => VerifyKeyword(@"#pragma warning $$");

        [Fact]
        public async Task TestNotAfterPragmaWarningEnable()
            => VerifyAbsence(@"#pragma warning enable $$");
    }
}
