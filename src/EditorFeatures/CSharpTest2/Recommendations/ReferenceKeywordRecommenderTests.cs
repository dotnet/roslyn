// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    public class ReferenceKeywordRecommenderTests : KeywordRecommenderTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAtRoot_Interactive()
        {
            await VerifyAbsenceAsync(
@"$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterClass_Interactive()
        {
            await VerifyAbsenceAsync(
@"class C { }
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterGlobalStatement_Interactive()
        {
            await VerifyAbsenceAsync(
@"System.Console.WriteLine();
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterGlobalVariableDeclaration_Interactive()
        {
            await VerifyAbsenceAsync(
@"int i = 0;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInUsingAlias()
        {
            await VerifyAbsenceAsync(
@"using Goo = $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInEmptyStatement()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterHash()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Regular,
@"#$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterHash_Interactive()
        {
            await VerifyKeywordAsync(SourceCodeKind.Script,
@"#$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterHashAndSpace()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Regular,
@"# $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterHashAndSpace_Interactive()
        {
            await VerifyKeywordAsync(SourceCodeKind.Script,
@"# $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNestedPreprocessor()
        {
            await VerifyKeywordAsync(SourceCodeKind.Script,
@"#if true
    #$$
#endif");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestBeforeUsing()
        {
            await VerifyKeywordAsync(SourceCodeKind.Script,
@"#$$
using System;");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterUsing()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
@"using System;
#$$");
        }
    }
}
