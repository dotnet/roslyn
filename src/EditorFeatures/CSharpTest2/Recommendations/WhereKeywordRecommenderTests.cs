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
        public void TestNotAtRoot_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterClass_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"class C { }
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterGlobalStatement_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"System.Console.WriteLine();
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterGlobalVariableDeclaration_Interactive()
        {
            VerifyAbsence(SourceCodeKind.Script,
@"int i = 0;
$$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotInUsingAlias()
        {
            VerifyAbsence(
@"using Goo = $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotInEmptyStatement()
        {
            VerifyAbsence(AddInsideMethod(
@"$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNewClause()
        {
            VerifyKeyword(AddInsideMethod(
@"var q = from x in y
          $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterPreviousClause()
        {
            VerifyKeyword(AddInsideMethod(
@"var v = from x in y
          where x > y
          $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterPreviousContinuationClause()
        {
            VerifyKeyword(AddInsideMethod(
@"var v = from x in y
          group x by y into g
          $$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAtEndOfPreviousClause()
        {
            VerifyAbsence(AddInsideMethod(
@"var q = from x in y$$"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestBetweenClauses()
        {
            VerifyKeyword(AddInsideMethod(
@"var q = from x in y
          $$
          from z in w"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterWhere()
        {
            VerifyAbsence(AddInsideMethod(
@"var q = from x in y
          where $$
          from z in w"));
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterClass()
        {
            VerifyAbsence(
@"class C $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterGenericClass()
        {
            VerifyKeyword(
@"class C<T> $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterClassBaseList()
        {
            VerifyAbsence(
@"class C : IGoo $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterGenericClassBaseList()
        {
            VerifyKeyword(
@"class C<T> : IGoo $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterDelegate()
        {
            VerifyAbsence(
@"delegate void D() $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterGenericDelegate()
        {
            VerifyKeyword(
@"delegate void D<T>() $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterPreviousClassConstraint()
        {
            VerifyKeyword(
@"class C<T> where T : class $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterPreviousStructConstraint()
        {
            VerifyKeyword(
@"class C<T> where T : struct $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterPreviousNewConstraint()
        {
            VerifyKeyword(
@"class C<T> where T : new() $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterPreviousConstraint()
        {
            VerifyKeyword(
@"class C<T> where T : IList<T> $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterPreviousDelegateClassConstraint()
        {
            VerifyKeyword(
@"delegate void D<T>() where T : class $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterPreviousDelegateStructConstraint()
        {
            VerifyKeyword(
@"delegate void D<T>() where T : struct $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterPreviousDelegateNewConstraint()
        {
            VerifyKeyword(
@"delegate void D<T>() where T : new() $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterPreviousDelegateConstraint()
        {
            VerifyKeyword(
@"delegate void D<T>() where T : IList<T> $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterMethod()
        {
            VerifyAbsence(
@"class C {
    void D() $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterGenericMethod()
        {
            VerifyKeyword(
@"class C {
    void D<T>() $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterPreviousMethodClassConstraint()
        {
            VerifyKeyword(
@"class C {
    void D<T>() where T : class $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterPreviousMethodStructConstraint()
        {
            VerifyKeyword(
@"class C {
    void D<T>() where T : struct $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterPreviousMethodNewConstraint()
        {
            VerifyKeyword(
@"class C {
    void D<T>() where T : new() $$");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestAfterPreviousMethodConstraint()
        {
            VerifyKeyword(
@"class C {
    void D<T>() where T : IList<T> $$");
        }

        [WorkItem(550715, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/550715")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterWhereTypeConstraint()
        {
            VerifyAbsence(
@"public class Goo<T> : System.Object where $$
{
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterWhereWhere()
        {
            VerifyAbsence(
@"public class Goo<T> : System.Object where where $$
{
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNotAfterWhereWhereWhere()
        {
            VerifyAbsence(
@"public class Goo<T> : System.Object where where where $$
{
}");
        }

        [WorkItem(550720, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/550720")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public void TestNoWhereAfterDot()
        {
            VerifyAbsence(
@"public class Goo<where> : System.$$
{
}");
        }
    }
}
