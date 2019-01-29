﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    [Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public class SafeOnlyKeywordRecommenderTests : KeywordRecommenderTests
    {
        [Fact]
        [WorkItem(31130, "https://github.com/dotnet/roslyn/issues/31130")]
        public async Task TestAfterNullable()
        {
            await VerifyKeywordAsync(@"#nullable $$");
        }

        [Fact]
        [WorkItem(31130, "https://github.com/dotnet/roslyn/issues/31130")]
        public async Task TestNotAfterNullableAndNewline()
        {
            await VerifyAbsenceAsync(@"
#nullable 
$$
");
        }

        [Fact]
        [WorkItem(31130, "https://github.com/dotnet/roslyn/issues/31130")]
        public async Task TestNotAfterHash()
        {
            await VerifyAbsenceAsync(@"#$$");
        }

        [Fact]
        public async Task TestNotAtRoot_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script, @"$$");
        }

        [Fact]
        public async Task TestNotAfterClass_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
@"class C { }
$$");
        }

        [Fact]
        public async Task TestNotAfterGlobalStatement_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
@"System.Console.WriteLine();
$$");
        }

        [Fact]
        public async Task TestNotAfterGlobalVariableDeclaration_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
@"int i = 0;
$$");
        }

        [Fact]
        public async Task TestNotInUsingAlias()
        {
            await VerifyAbsenceAsync(@"using Goo = $$");
        }

        [Fact]
        public async Task TestNotInEmptyStatement()
        {
            await VerifyAbsenceAsync(AddInsideMethod(@"$$"));
        }

        [Fact]
        public async Task TestNotAfterPragma()
        {
            await VerifyAbsenceAsync(@"#pragma $$");
        }

        [Fact]
        public async Task TestAfterPragmaWarning()
        {
            await VerifyKeywordAsync(@"#pragma warning $$");
        }

        [Fact]
        public async Task TestNotAfterPragmaWarningSafeOnly()
        {
            await VerifyAbsenceAsync(@"#pragma warning safeonly $$");
        }
    }
}
