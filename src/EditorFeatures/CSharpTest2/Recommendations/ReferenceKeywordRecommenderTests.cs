// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    public class ReferenceKeywordRecommenderTests : KeywordRecommenderTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAtRoot_Interactive()
        {
            VerifyAbsence(
@"$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterClass_Interactive()
        {
            VerifyAbsence(
@"class C { }
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterGlobalStatement_Interactive()
        {
            VerifyAbsence(
@"System.Console.WriteLine();
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterGlobalVariableDeclaration_Interactive()
        {
            VerifyAbsence(
@"int i = 0;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInUsingAlias()
        {
            VerifyAbsence(
@"using Foo = $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotInEmptyStatement()
        {
            VerifyAbsence(AddInsideMethod(
@"$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterHash()
        {
            VerifyAbsence(SourceCodeKind.Regular,
@"#$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterHash_Interactive()
        {
            VerifyKeyword(SourceCodeKind.Script,
@"#$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterHashAndSpace()
        {
            VerifyAbsence(SourceCodeKind.Regular,
@"# $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void AfterHashAndSpace_Interactive()
        {
            VerifyKeyword(SourceCodeKind.Script,
@"# $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NestedPreprocessor()
        {
            VerifyKeyword(SourceCodeKind.Script,
@"#if true
    #$$
#endif");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void BeforeUsing()
        {
            VerifyKeyword(SourceCodeKind.Script,
@"#$$
using System;");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void NotAfterUsing()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"using System;
#$$");
        }
    }
}
