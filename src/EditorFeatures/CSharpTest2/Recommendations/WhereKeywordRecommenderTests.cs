// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    public class WhereKeywordRecommenderTests : KeywordRecommenderTests
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
@"using Goo = $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInEmptyStatement()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNewClause()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var q = from x in y
          $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterPreviousClause()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var v = from x in y
          where x > y
          $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterPreviousContinuationClause()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var v = from x in y
          group x by y into g
          $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAtEndOfPreviousClause()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"var q = from x in y$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestBetweenClauses()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var q = from x in y
          $$
          from z in w"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterWhere()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"var q = from x in y
          where $$
          from z in w"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterClass()
        {
            await VerifyAbsenceAsync(
@"class C $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterGenericClass()
        {
            await VerifyKeywordAsync(
@"class C<T> $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterClassBaseList()
        {
            await VerifyAbsenceAsync(
@"class C : IGoo $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterGenericClassBaseList()
        {
            await VerifyKeywordAsync(
@"class C<T> : IGoo $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterDelegate()
        {
            await VerifyAbsenceAsync(
@"delegate void D() $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterGenericDelegate()
        {
            await VerifyKeywordAsync(
@"delegate void D<T>() $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterPreviousClassConstraint()
        {
            await VerifyKeywordAsync(
@"class C<T> where T : class $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterPreviousStructConstraint()
        {
            await VerifyKeywordAsync(
@"class C<T> where T : struct $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterPreviousNewConstraint()
        {
            await VerifyKeywordAsync(
@"class C<T> where T : new() $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterPreviousConstraint()
        {
            await VerifyKeywordAsync(
@"class C<T> where T : IList<T> $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterPreviousDelegateClassConstraint()
        {
            await VerifyKeywordAsync(
@"delegate void D<T>() where T : class $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterPreviousDelegateStructConstraint()
        {
            await VerifyKeywordAsync(
@"delegate void D<T>() where T : struct $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterPreviousDelegateNewConstraint()
        {
            await VerifyKeywordAsync(
@"delegate void D<T>() where T : new() $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterPreviousDelegateConstraint()
        {
            await VerifyKeywordAsync(
@"delegate void D<T>() where T : IList<T> $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterMethod()
        {
            await VerifyAbsenceAsync(
@"class C {
    void D() $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterGenericMethod()
        {
            await VerifyKeywordAsync(
@"class C {
    void D<T>() $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterPreviousMethodClassConstraint()
        {
            await VerifyKeywordAsync(
@"class C {
    void D<T>() where T : class $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterPreviousMethodStructConstraint()
        {
            await VerifyKeywordAsync(
@"class C {
    void D<T>() where T : struct $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterPreviousMethodNewConstraint()
        {
            await VerifyKeywordAsync(
@"class C {
    void D<T>() where T : new() $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterPreviousMethodConstraint()
        {
            await VerifyKeywordAsync(
@"class C {
    void D<T>() where T : IList<T> $$");
        }

        [WorkItem(550715, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/550715")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterWhereTypeConstraint()
        {
            await VerifyAbsenceAsync(
@"public class Goo<T> : System.Object where $$
{
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterWhereWhere()
        {
            await VerifyAbsenceAsync(
@"public class Goo<T> : System.Object where where $$
{
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterWhereWhereWhere()
        {
            await VerifyAbsenceAsync(
@"public class Goo<T> : System.Object where where where $$
{
}");
        }

        [WorkItem(550720, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/550720")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNoWhereAfterDot()
        {
            await VerifyAbsenceAsync(
@"public class Goo<where> : System.$$
{
}");
        }
    }
}
