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
    public class WhereKeywordRecommenderTests : KeywordRecommenderTests
    {
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAtRoot_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterClass_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"class C { }
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterGlobalStatement_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"System.Console.WriteLine();
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterGlobalVariableDeclaration_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"int i = 0;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInUsingAlias()
        {
            VerifyAbsence(
@"using Goo = $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotInEmptyStatement()
        {
            VerifyAbsence(AddInsideMethod(
@"$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNewClause()
        {
            VerifyKeyword(AddInsideMethod(
@"var q = from x in y
          $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterPreviousClause()
        {
            VerifyKeyword(AddInsideMethod(
@"var v = from x in y
          where x > y
          $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterPreviousContinuationClause()
        {
            VerifyKeyword(AddInsideMethod(
@"var v = from x in y
          group x by y into g
          $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAtEndOfPreviousClause()
        {
            VerifyAbsence(AddInsideMethod(
@"var q = from x in y$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestBetweenClauses()
        {
            VerifyKeyword(AddInsideMethod(
@"var q = from x in y
          $$
          from z in w"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterWhere()
        {
            VerifyAbsence(AddInsideMethod(
@"var q = from x in y
          where $$
          from z in w"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterClass()
        {
            VerifyAbsence(
@"class C $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterGenericClass()
        {
            VerifyKeyword(
@"class C<T> $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterClassBaseList()
        {
            VerifyAbsence(
@"class C : IGoo $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterGenericClassBaseList()
        {
            VerifyKeyword(
@"class C<T> : IGoo $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterDelegate()
        {
            VerifyAbsence(
@"delegate void D() $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterGenericDelegate()
        {
            VerifyKeyword(
@"delegate void D<T>() $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterPreviousClassConstraint()
        {
            VerifyKeyword(
@"class C<T> where T : class $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterPreviousStructConstraint()
        {
            VerifyKeyword(
@"class C<T> where T : struct $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterPreviousNewConstraint()
        {
            VerifyKeyword(
@"class C<T> where T : new() $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterPreviousConstraint()
        {
            VerifyKeyword(
@"class C<T> where T : IList<T> $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterPreviousDelegateClassConstraint()
        {
            VerifyKeyword(
@"delegate void D<T>() where T : class $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterPreviousDelegateStructConstraint()
        {
            VerifyKeyword(
@"delegate void D<T>() where T : struct $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterPreviousDelegateNewConstraint()
        {
            VerifyKeyword(
@"delegate void D<T>() where T : new() $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterPreviousDelegateConstraint()
        {
            VerifyKeyword(
@"delegate void D<T>() where T : IList<T> $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterMethod()
        {
            VerifyAbsence(
@"class C {
    void D() $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterGenericMethod()
        {
            VerifyKeyword(
@"class C {
    void D<T>() $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterPreviousMethodClassConstraint()
        {
            VerifyKeyword(
@"class C {
    void D<T>() where T : class $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterPreviousMethodStructConstraint()
        {
            VerifyKeyword(
@"class C {
    void D<T>() where T : struct $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterPreviousMethodNewConstraint()
        {
            VerifyKeyword(
@"class C {
    void D<T>() where T : new() $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterPreviousMethodConstraint()
        {
            VerifyKeyword(
@"class C {
    void D<T>() where T : IList<T> $$");
        }

        [WorkItem(550715, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/550715")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterWhereTypeConstraint()
        {
            VerifyAbsence(
@"public class Goo<T> : System.Object where $$
{
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterWhereWhere()
        {
            VerifyAbsence(
@"public class Goo<T> : System.Object where where $$
{
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterWhereWhereWhere()
        {
            VerifyAbsence(
@"public class Goo<T> : System.Object where where where $$
{
}");
        }

        [WorkItem(550720, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/550720")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNoWhereAfterDot()
        {
            VerifyAbsence(
@"public class Goo<where> : System.$$
{
}");
        }
    }
}
