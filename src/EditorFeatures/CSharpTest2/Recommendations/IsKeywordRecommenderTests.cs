// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    public class IsKeywordRecommenderTests : KeywordRecommenderTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAtRoot_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
@"$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterClass_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
@"class C { }
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterGlobalStatement_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
@"System.Console.WriteLine();
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterGlobalVariableDeclaration_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
@"int i = 0;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInUsingAlias()
        {
            await VerifyAbsenceAsync(
@"using Foo = $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInEmptyStatement()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterExpr()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var q = foo $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterVoid()
        {
            await VerifyAbsenceAsync(
@"class C {
    void $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInForeach()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"foreach (var v $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInFrom()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"var q = from a $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInJoin()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"var q = from a in b
          join x $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterType1()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"int $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterType2()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"Foo $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterType3()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"Foo<Bar> $$"));
        }

        [WorkItem(543041, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543041")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterVarInForLoop()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"for (var $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterVarInOutArgument()
        {
            var experimentalFeatures = new System.Collections.Generic.Dictionary<string, string>(); // no experimental features to enable
            await VerifyAbsenceAsync(AddInsideMethod(
@"M(out var $$"), options: Options.Regular.WithFeatures(experimentalFeatures), scriptOptions: Options.Script.WithFeatures(experimentalFeatures));
        }

        [WorkItem(1064811, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064811")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotBeforeFirstStringHole()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"var x = ""\{0}$$\{1}\{2}"""));
        }

        [WorkItem(1064811, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064811")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotBetweenStringHoles()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"var x = ""\{0}\{1}$$\{2}"""));
        }

        [WorkItem(1064811, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064811")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterStringHoles()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"var x = ""\{0}\{1}\{2}$$"""));
        }

        [WorkItem(1064811, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1064811")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterLastStringHole()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var x = ""\{0}\{1}\{2}"" $$"));
        }

        [WorkItem(1736, "https://github.com/dotnet/roslyn/issues/1736")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotWithinNumericLiteral()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"var x = .$$0;"));
        }
    }
}
