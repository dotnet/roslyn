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
        public async Task TestNotInGlobalUsingAlias()
        {
            await VerifyAbsenceAsync(
@"global using Goo = $$");
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

        [WorkItem(30785, "https://github.com/dotnet/roslyn/issues/30785")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterDot1()
        {
            await VerifyAbsenceAsync(
@"public class C
{
    void M<T> where T : System.$$
}");
        }

        [WorkItem(30785, "https://github.com/dotnet/roslyn/issues/30785")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterDot2()
        {
            await VerifyAbsenceAsync(
@"public class C<T> where T : System.$$
{
}");
        }

        [WorkItem(30785, "https://github.com/dotnet/roslyn/issues/30785")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterValidConstraint1()
        {
            await VerifyKeywordAsync(
@"public class C
{
    void M<T> where T : System.Exception $$
}");
        }

        [WorkItem(30785, "https://github.com/dotnet/roslyn/issues/30785")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterValidConstraint2()
        {
            await VerifyKeywordAsync(
@"public class C<T> where T : System.Exception $$
{
}");
        }

        [WorkItem(30785, "https://github.com/dotnet/roslyn/issues/30785")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterGlobal1()
        {
            await VerifyAbsenceAsync(
@"public class C
{
    void M<T> where T : global::$$
}");
        }

        [WorkItem(30785, "https://github.com/dotnet/roslyn/issues/30785")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterGlobal2()
        {
            await VerifyAbsenceAsync(
@"public class C<T> where T : global::$$
{
}");
        }

        [WorkItem(30785, "https://github.com/dotnet/roslyn/issues/30785")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterValidConstraint3()
        {
            await VerifyKeywordAsync(
@"public class C
{
    void M<T> where T : global::System.Exception $$
}");
        }

        [WorkItem(30785, "https://github.com/dotnet/roslyn/issues/30785")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterValidConstraint4()
        {
            await VerifyKeywordAsync(
@"public class C<T> where T : global::System.Exception $$
{
}");
        }

        [WorkItem(30785, "https://github.com/dotnet/roslyn/issues/30785")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterGenericConstraintStart1()
        {
            await VerifyAbsenceAsync(
@"public class C
{
    void M<T> where T : List<$$
}");
        }

        [WorkItem(30785, "https://github.com/dotnet/roslyn/issues/30785")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterGenericConstraintStart2()
        {
            await VerifyAbsenceAsync(
@"public class C<T> where T : List<$$
{
}");
        }

        [WorkItem(30785, "https://github.com/dotnet/roslyn/issues/30785")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterValidGenericConstraint1()
        {
            await VerifyKeywordAsync(
@"public class C
{
    void M<T> where T : List<int> $$
}");
        }

        [WorkItem(30785, "https://github.com/dotnet/roslyn/issues/30785")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterValidGenericConstraint2()
        {
            await VerifyKeywordAsync(
@"public class C<T> where T : List<int> $$
{
}");
        }

        [WorkItem(30785, "https://github.com/dotnet/roslyn/issues/30785")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterGenericConstraintStartSecondParameter1()
        {
            await VerifyAbsenceAsync(
@"public class C
{
    void M<T> where T : Dictionary<int, $$
}");
        }

        [WorkItem(30785, "https://github.com/dotnet/roslyn/issues/30785")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterGenericConstraintStartSecondParameter2()
        {
            await VerifyAbsenceAsync(
@"public class C<T> where T : Dictionary<int, $$
{
}");
        }

        [WorkItem(30785, "https://github.com/dotnet/roslyn/issues/30785")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterValidGenericConstraint3()
        {
            await VerifyKeywordAsync(
@"public class C
{
    void M<T> where T : Dictionary<int, string> $$
}");
        }

        [WorkItem(30785, "https://github.com/dotnet/roslyn/issues/30785")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterValidGenericConstraint4()
        {
            await VerifyKeywordAsync(
@"public class C<T> where T : Dictionary<int, string> $$
{
}");
        }

        [WorkItem(30785, "https://github.com/dotnet/roslyn/issues/30785")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterDoubleUnclosedGenericConstraint1()
        {
            await VerifyAbsenceAsync(
@"public class C
{
    void M<T> where T : List<List<int>$$
}");
        }

        [WorkItem(30785, "https://github.com/dotnet/roslyn/issues/30785")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterDoubleUnclosedGenericConstraint2()
        {
            await VerifyAbsenceAsync(
@"public class C<T> where T : List<List<int>$$
{
}");
        }

        [WorkItem(30785, "https://github.com/dotnet/roslyn/issues/30785")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterValidGenericConstraint5()
        {
            await VerifyKeywordAsync(
@"public class C
{
    void M<T> where T : List<List<int>> $$
}");
        }

        [WorkItem(30785, "https://github.com/dotnet/roslyn/issues/30785")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterValidGenericConstraint6()
        {
            await VerifyKeywordAsync(
@"public class C<T> where T : List<List<int>> $$
{
}");
        }

        [WorkItem(30785, "https://github.com/dotnet/roslyn/issues/30785")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterTupleInsideGenericConstraintStart1()
        {
            await VerifyAbsenceAsync(
@"public class C
{
    void M<T> where T : List<(int, $$
}");
        }

        [WorkItem(30785, "https://github.com/dotnet/roslyn/issues/30785")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterTupleInsideGenericConstraintStart2()
        {
            await VerifyAbsenceAsync(
@"public class C<T> where T : List<(int, $$
{
}");
        }

        [WorkItem(30785, "https://github.com/dotnet/roslyn/issues/30785")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterTupleClosedInsideGenericConstraintStart1()
        {
            await VerifyAbsenceAsync(
@"public class C
{
    void M<T> where T : List<(int, string)$$
}");
        }

        [WorkItem(30785, "https://github.com/dotnet/roslyn/issues/30785")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestNotAfterTupleClosedInsideGenericConstraintStart2()
        {
            await VerifyAbsenceAsync(
@"public class C<T> where T : List<(int, string)$$
{
}");
        }

        [WorkItem(30785, "https://github.com/dotnet/roslyn/issues/30785")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterValidGenericConstraintWithTuple1()
        {
            await VerifyKeywordAsync(
@"public class C
{
    void M<T> where T : List<(int, string)> $$
}");
        }

        [WorkItem(30785, "https://github.com/dotnet/roslyn/issues/30785")]
        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterValidGenericConstraintWithTuple2()
        {
            await VerifyKeywordAsync(
@"public class C<T> where T : List<(int, string)> $$
{
}");
        }
    }
}
