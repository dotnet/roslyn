// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations;

[Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
public sealed class WhereKeywordRecommenderTests : KeywordRecommenderTests
{
    [Fact]
    public Task TestNotAtRoot_Interactive()
        => VerifyAbsenceAsync(SourceCodeKind.Script,
@"$$");

    [Fact]
    public Task TestNotAfterClass_Interactive()
        => VerifyAbsenceAsync(SourceCodeKind.Script,
            """
            class C { }
            $$
            """);

    [Fact]
    public Task TestNotAfterGlobalStatement_Interactive()
        => VerifyAbsenceAsync(SourceCodeKind.Script,
            """
            System.Console.WriteLine();
            $$
            """);

    [Fact]
    public Task TestNotAfterGlobalVariableDeclaration_Interactive()
        => VerifyAbsenceAsync(SourceCodeKind.Script,
            """
            int i = 0;
            $$
            """);

    [Fact]
    public Task TestNotInUsingAlias()
        => VerifyAbsenceAsync(
@"using Goo = $$");

    [Fact]
    public Task TestNotInGlobalUsingAlias()
        => VerifyAbsenceAsync(
@"global using Goo = $$");

    [Fact]
    public Task TestNotInEmptyStatement()
        => VerifyAbsenceAsync(AddInsideMethod(
@"$$"));

    [Fact]
    public Task TestNewClause()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            var q = from x in y
                      $$
            """));

    [Fact]
    public Task TestAfterPreviousClause()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            var v = from x in y
                      where x > y
                      $$
            """));

    [Fact]
    public Task TestAfterPreviousContinuationClause()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            var v = from x in y
                      group x by y into g
                      $$
            """));

    [Fact]
    public Task TestNotAtEndOfPreviousClause()
        => VerifyAbsenceAsync(AddInsideMethod(
@"var q = from x in y$$"));

    [Fact]
    public Task TestBetweenClauses()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            var q = from x in y
                      $$
                      from z in w
            """));

    [Fact]
    public Task TestNotAfterWhere()
        => VerifyAbsenceAsync(AddInsideMethod(
            """
            var q = from x in y
                      where $$
                      from z in w
            """));

    [Fact]
    public Task TestNotAfterClass()
        => VerifyAbsenceAsync(
@"class C $$");

    [Fact]
    public Task TestAfterGenericClass()
        => VerifyKeywordAsync(
@"class C<T> $$");

    [Fact]
    public Task TestNotAfterClassBaseList()
        => VerifyAbsenceAsync(
@"class C : IGoo $$");

    [Fact]
    public Task TestAfterGenericClassBaseList()
        => VerifyKeywordAsync(
@"class C<T> : IGoo $$");

    [Fact]
    public Task TestNotAfterDelegate()
        => VerifyAbsenceAsync(
@"delegate void D() $$");

    [Fact]
    public Task TestAfterGenericDelegate()
        => VerifyKeywordAsync(
@"delegate void D<T>() $$");

    [Fact]
    public Task TestAfterPreviousClassConstraint()
        => VerifyKeywordAsync(
@"class C<T> where T : class $$");

    [Fact]
    public Task TestAfterPreviousStructConstraint()
        => VerifyKeywordAsync(
@"class C<T> where T : struct $$");

    [Fact]
    public Task TestAfterPreviousNewConstraint()
        => VerifyKeywordAsync(
@"class C<T> where T : new() $$");

    [Fact]
    public Task TestAfterPreviousAllowsRefStructConstraint_01()
        => VerifyKeywordAsync(
@"class C<T> where T : allows ref struct $$");

    [Fact]
    public Task TestAfterPreviousAllowsRefStructConstraint_02()
        => VerifyKeywordAsync(
@"class C { void M<T>() where T : allows ref struct $$");

    [Fact]
    public Task TestAfterPreviousConstraint()
        => VerifyKeywordAsync(
@"class C<T> where T : IList<T> $$");

    [Fact]
    public Task TestAfterPreviousDelegateClassConstraint()
        => VerifyKeywordAsync(
@"delegate void D<T>() where T : class $$");

    [Fact]
    public Task TestAfterPreviousDelegateStructConstraint()
        => VerifyKeywordAsync(
@"delegate void D<T>() where T : struct $$");

    [Fact]
    public Task TestAfterPreviousDelegateNewConstraint()
        => VerifyKeywordAsync(
@"delegate void D<T>() where T : new() $$");

    [Fact]
    public Task TestAfterPreviousDelegateConstraint()
        => VerifyKeywordAsync(
@"delegate void D<T>() where T : IList<T> $$");

    [Fact]
    public Task TestNotAfterMethod()
        => VerifyAbsenceAsync(
            """
            class C {
                void D() $$
            """);

    [Fact]
    public Task TestAfterGenericMethod()
        => VerifyKeywordAsync(
            """
            class C {
                void D<T>() $$
            """);

    [Fact]
    public Task TestAfterPreviousMethodClassConstraint()
        => VerifyKeywordAsync(
            """
            class C {
                void D<T>() where T : class $$
            """);

    [Fact]
    public Task TestAfterPreviousMethodStructConstraint()
        => VerifyKeywordAsync(
            """
            class C {
                void D<T>() where T : struct $$
            """);

    [Fact]
    public Task TestAfterPreviousMethodNewConstraint()
        => VerifyKeywordAsync(
            """
            class C {
                void D<T>() where T : new() $$
            """);

    [Fact]
    public Task TestAfterPreviousMethodConstraint()
        => VerifyKeywordAsync(
            """
            class C {
                void D<T>() where T : IList<T> $$
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/550715")]
    public Task TestNotAfterWhereTypeConstraint()
        => VerifyAbsenceAsync(
            """
            public class Goo<T> : System.Object where $$
            {
            }
            """);

    [Fact]
    public Task TestNotAfterWhereWhere()
        => VerifyAbsenceAsync(
            """
            public class Goo<T> : System.Object where where $$
            {
            }
            """);

    [Fact]
    public Task TestNotAfterWhereWhereWhere()
        => VerifyAbsenceAsync(
            """
            public class Goo<T> : System.Object where where where $$
            {
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/550720")]
    public Task TestNoWhereAfterDot()
        => VerifyAbsenceAsync(
            """
            public class Goo<where> : System.$$
            {
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30785")]
    public Task TestNotAfterDot1()
        => VerifyAbsenceAsync(
            """
            public class C
            {
                void M<T> where T : System.$$
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30785")]
    public Task TestNotAfterDot2()
        => VerifyAbsenceAsync(
            """
            public class C<T> where T : System.$$
            {
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30785")]
    public Task TestAfterValidConstraint1()
        => VerifyKeywordAsync(
            """
            public class C
            {
                void M<T> where T : System.Exception $$
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30785")]
    public Task TestAfterValidConstraint2()
        => VerifyKeywordAsync(
            """
            public class C<T> where T : System.Exception $$
            {
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30785")]
    public Task TestNotAfterGlobal1()
        => VerifyAbsenceAsync(
            """
            public class C
            {
                void M<T> where T : global::$$
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30785")]
    public Task TestNotAfterGlobal2()
        => VerifyAbsenceAsync(
            """
            public class C<T> where T : global::$$
            {
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30785")]
    public Task TestAfterValidConstraint3()
        => VerifyKeywordAsync(
            """
            public class C
            {
                void M<T> where T : global::System.Exception $$
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30785")]
    public Task TestAfterValidConstraint4()
        => VerifyKeywordAsync(
            """
            public class C<T> where T : global::System.Exception $$
            {
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30785")]
    public Task TestNotAfterGenericConstraintStart1()
        => VerifyAbsenceAsync(
            """
            public class C
            {
                void M<T> where T : List<$$
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30785")]
    public Task TestNotAfterGenericConstraintStart2()
        => VerifyAbsenceAsync(
            """
            public class C<T> where T : List<$$
            {
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30785")]
    public Task TestAfterValidGenericConstraint1()
        => VerifyKeywordAsync(
            """
            public class C
            {
                void M<T> where T : List<int> $$
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30785")]
    public Task TestAfterValidGenericConstraint2()
        => VerifyKeywordAsync(
            """
            public class C<T> where T : List<int> $$
            {
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30785")]
    public Task TestNotAfterGenericConstraintStartSecondParameter1()
        => VerifyAbsenceAsync(
            """
            public class C
            {
                void M<T> where T : Dictionary<int, $$
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30785")]
    public Task TestNotAfterGenericConstraintStartSecondParameter2()
        => VerifyAbsenceAsync(
            """
            public class C<T> where T : Dictionary<int, $$
            {
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30785")]
    public Task TestAfterValidGenericConstraint3()
        => VerifyKeywordAsync(
            """
            public class C
            {
                void M<T> where T : Dictionary<int, string> $$
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30785")]
    public Task TestAfterValidGenericConstraint4()
        => VerifyKeywordAsync(
            """
            public class C<T> where T : Dictionary<int, string> $$
            {
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30785")]
    public Task TestNotAfterDoubleUnclosedGenericConstraint1()
        => VerifyAbsenceAsync(
            """
            public class C
            {
                void M<T> where T : List<List<int>$$
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30785")]
    public Task TestNotAfterDoubleUnclosedGenericConstraint2()
        => VerifyAbsenceAsync(
            """
            public class C<T> where T : List<List<int>$$
            {
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30785")]
    public Task TestAfterValidGenericConstraint5()
        => VerifyKeywordAsync(
            """
            public class C
            {
                void M<T> where T : List<List<int>> $$
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30785")]
    public Task TestAfterValidGenericConstraint6()
        => VerifyKeywordAsync(
            """
            public class C<T> where T : List<List<int>> $$
            {
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30785")]
    public Task TestNotAfterTupleInsideGenericConstraintStart1()
        => VerifyAbsenceAsync(
            """
            public class C
            {
                void M<T> where T : List<(int, $$
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30785")]
    public Task TestNotAfterTupleInsideGenericConstraintStart2()
        => VerifyAbsenceAsync(
            """
            public class C<T> where T : List<(int, $$
            {
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30785")]
    public Task TestNotAfterTupleClosedInsideGenericConstraintStart1()
        => VerifyAbsenceAsync(
            """
            public class C
            {
                void M<T> where T : List<(int, string)$$
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30785")]
    public Task TestNotAfterTupleClosedInsideGenericConstraintStart2()
        => VerifyAbsenceAsync(
            """
            public class C<T> where T : List<(int, string)$$
            {
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30785")]
    public Task TestAfterValidGenericConstraintWithTuple1()
        => VerifyKeywordAsync(
            """
            public class C
            {
                void M<T> where T : List<(int, string)> $$
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30785")]
    public Task TestAfterValidGenericConstraintWithTuple2()
        => VerifyKeywordAsync(
            """
            public class C<T> where T : List<(int, string)> $$
            {
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72821")]
    public Task TestNotAfterLocalFunction()
        => VerifyAbsenceAsync(
            """
            class C
            {
                void M<T>()
                {
                    void Inner() $$
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72821")]
    public Task TestAfterGenericLocalFunction()
        => VerifyKeywordAsync(
            """
            class C
            {
                void M<T>()
                {
                    void Inner<T1>() $$
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72821")]
    public Task TestAfterFirstValidConstraintInGenericLocalFunction()
        => VerifyKeywordAsync(
            """
            class C
            {
                void M<T>()
                {
                    void Inner<T1, T2>()
                        where T1 : C
                        $$
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/80636")]
    public Task TestAfterExtensionWithTypeParameterAndCompleteParameterList()
        => VerifyKeywordAsync(
            """
            public static class C
            {
                extension<T>(T value) $$
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/80636")]
    public Task TestAfterExtensionWithMultipleTypeParametersAndCompleteParameterList()
        => VerifyKeywordAsync(
            """
            public static class C
            {
                extension<T1, T2>(T1 value1, T2 value2) $$
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/80636")]
    public Task TestAfterExtensionWithTypeParameterConstraint()
        => VerifyKeywordAsync(
            """
            public static class C
            {
                extension<T>(T value) where T : class $$
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/80636")]
    public Task TestNotAfterExtensionWithTypeParameterWithoutParameterList()
        => VerifyAbsenceAsync(
            """
            public static class C
            {
                extension<T> $$
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/80636")]
    public Task TestNotAfterExtensionWithoutTypeParameter()
        => VerifyAbsenceAsync(
            """
            public static class C
            {
                extension $$
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/80636")]
    public Task TestNotAfterExtensionWithoutTypeParameterAndParameterList()
        => VerifyAbsenceAsync(
            """
            public static class C
            {
                extension() $$
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/80636")]
    public Task TestNotAfterExtensionWithIncompleteParameterList()
        => VerifyAbsenceAsync(
            """
            public static class C
            {
                extension( $$
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/80636")]
    public Task TestNotAfterExtensionWithTypeParameterAndIncompleteParameterList()
        => VerifyAbsenceAsync(
            """
            public static class C
            {
                extension<T>( $$
            """);
}
