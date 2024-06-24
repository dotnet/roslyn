// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    [Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public class WhereKeywordRecommenderTests : KeywordRecommenderTests
    {
        [Fact]
        public async Task TestNotAtRoot_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
@"$$");
        }

        [Fact]
        public async Task TestNotAfterClass_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
                """
                class C { }
                $$
                """);
        }

        [Fact]
        public async Task TestNotAfterGlobalStatement_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
                """
                System.Console.WriteLine();
                $$
                """);
        }

        [Fact]
        public async Task TestNotAfterGlobalVariableDeclaration_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
                """
                int i = 0;
                $$
                """);
        }

        [Fact]
        public async Task TestNotInUsingAlias()
        {
            await VerifyAbsenceAsync(
@"using Goo = $$");
        }

        [Fact]
        public async Task TestNotInGlobalUsingAlias()
        {
            await VerifyAbsenceAsync(
@"global using Goo = $$");
        }

        [Fact]
        public async Task TestNotInEmptyStatement()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"$$"));
        }

        [Fact]
        public async Task TestNewClause()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                var q = from x in y
                          $$
                """));
        }

        [Fact]
        public async Task TestAfterPreviousClause()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                var v = from x in y
                          where x > y
                          $$
                """));
        }

        [Fact]
        public async Task TestAfterPreviousContinuationClause()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                var v = from x in y
                          group x by y into g
                          $$
                """));
        }

        [Fact]
        public async Task TestNotAtEndOfPreviousClause()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"var q = from x in y$$"));
        }

        [Fact]
        public async Task TestBetweenClauses()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                var q = from x in y
                          $$
                          from z in w
                """));
        }

        [Fact]
        public async Task TestNotAfterWhere()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
                """
                var q = from x in y
                          where $$
                          from z in w
                """));
        }

        [Fact]
        public async Task TestNotAfterClass()
        {
            await VerifyAbsenceAsync(
@"class C $$");
        }

        [Fact]
        public async Task TestAfterGenericClass()
        {
            await VerifyKeywordAsync(
@"class C<T> $$");
        }

        [Fact]
        public async Task TestNotAfterClassBaseList()
        {
            await VerifyAbsenceAsync(
@"class C : IGoo $$");
        }

        [Fact]
        public async Task TestAfterGenericClassBaseList()
        {
            await VerifyKeywordAsync(
@"class C<T> : IGoo $$");
        }

        [Fact]
        public async Task TestNotAfterDelegate()
        {
            await VerifyAbsenceAsync(
@"delegate void D() $$");
        }

        [Fact]
        public async Task TestAfterGenericDelegate()
        {
            await VerifyKeywordAsync(
@"delegate void D<T>() $$");
        }

        [Fact]
        public async Task TestAfterPreviousClassConstraint()
        {
            await VerifyKeywordAsync(
@"class C<T> where T : class $$");
        }

        [Fact]
        public async Task TestAfterPreviousStructConstraint()
        {
            await VerifyKeywordAsync(
@"class C<T> where T : struct $$");
        }

        [Fact]
        public async Task TestAfterPreviousNewConstraint()
        {
            await VerifyKeywordAsync(
@"class C<T> where T : new() $$");
        }

        [Fact]
        public async Task TestAfterPreviousAllowsRefStructConstraint_01()
        {
            await VerifyKeywordAsync(
@"class C<T> where T : allows ref struct $$");
        }

        [Fact]
        public async Task TestAfterPreviousAllowsRefStructConstraint_02()
        {
            await VerifyKeywordAsync(
@"class C { void M<T>() where T : allows ref struct $$");
        }

        [Fact]
        public async Task TestAfterPreviousConstraint()
        {
            await VerifyKeywordAsync(
@"class C<T> where T : IList<T> $$");
        }

        [Fact]
        public async Task TestAfterPreviousDelegateClassConstraint()
        {
            await VerifyKeywordAsync(
@"delegate void D<T>() where T : class $$");
        }

        [Fact]
        public async Task TestAfterPreviousDelegateStructConstraint()
        {
            await VerifyKeywordAsync(
@"delegate void D<T>() where T : struct $$");
        }

        [Fact]
        public async Task TestAfterPreviousDelegateNewConstraint()
        {
            await VerifyKeywordAsync(
@"delegate void D<T>() where T : new() $$");
        }

        [Fact]
        public async Task TestAfterPreviousDelegateConstraint()
        {
            await VerifyKeywordAsync(
@"delegate void D<T>() where T : IList<T> $$");
        }

        [Fact]
        public async Task TestNotAfterMethod()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    void D() $$
                """);
        }

        [Fact]
        public async Task TestAfterGenericMethod()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    void D<T>() $$
                """);
        }

        [Fact]
        public async Task TestAfterPreviousMethodClassConstraint()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    void D<T>() where T : class $$
                """);
        }

        [Fact]
        public async Task TestAfterPreviousMethodStructConstraint()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    void D<T>() where T : struct $$
                """);
        }

        [Fact]
        public async Task TestAfterPreviousMethodNewConstraint()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    void D<T>() where T : new() $$
                """);
        }

        [Fact]
        public async Task TestAfterPreviousMethodConstraint()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    void D<T>() where T : IList<T> $$
                """);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/550715")]
        public async Task TestNotAfterWhereTypeConstraint()
        {
            await VerifyAbsenceAsync(
                """
                public class Goo<T> : System.Object where $$
                {
                }
                """);
        }

        [Fact]
        public async Task TestNotAfterWhereWhere()
        {
            await VerifyAbsenceAsync(
                """
                public class Goo<T> : System.Object where where $$
                {
                }
                """);
        }

        [Fact]
        public async Task TestNotAfterWhereWhereWhere()
        {
            await VerifyAbsenceAsync(
                """
                public class Goo<T> : System.Object where where where $$
                {
                }
                """);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/550720")]
        public async Task TestNoWhereAfterDot()
        {
            await VerifyAbsenceAsync(
                """
                public class Goo<where> : System.$$
                {
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30785")]
        public async Task TestNotAfterDot1()
        {
            await VerifyAbsenceAsync(
                """
                public class C
                {
                    void M<T> where T : System.$$
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30785")]
        public async Task TestNotAfterDot2()
        {
            await VerifyAbsenceAsync(
                """
                public class C<T> where T : System.$$
                {
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30785")]
        public async Task TestAfterValidConstraint1()
        {
            await VerifyKeywordAsync(
                """
                public class C
                {
                    void M<T> where T : System.Exception $$
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30785")]
        public async Task TestAfterValidConstraint2()
        {
            await VerifyKeywordAsync(
                """
                public class C<T> where T : System.Exception $$
                {
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30785")]
        public async Task TestNotAfterGlobal1()
        {
            await VerifyAbsenceAsync(
                """
                public class C
                {
                    void M<T> where T : global::$$
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30785")]
        public async Task TestNotAfterGlobal2()
        {
            await VerifyAbsenceAsync(
                """
                public class C<T> where T : global::$$
                {
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30785")]
        public async Task TestAfterValidConstraint3()
        {
            await VerifyKeywordAsync(
                """
                public class C
                {
                    void M<T> where T : global::System.Exception $$
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30785")]
        public async Task TestAfterValidConstraint4()
        {
            await VerifyKeywordAsync(
                """
                public class C<T> where T : global::System.Exception $$
                {
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30785")]
        public async Task TestNotAfterGenericConstraintStart1()
        {
            await VerifyAbsenceAsync(
                """
                public class C
                {
                    void M<T> where T : List<$$
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30785")]
        public async Task TestNotAfterGenericConstraintStart2()
        {
            await VerifyAbsenceAsync(
                """
                public class C<T> where T : List<$$
                {
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30785")]
        public async Task TestAfterValidGenericConstraint1()
        {
            await VerifyKeywordAsync(
                """
                public class C
                {
                    void M<T> where T : List<int> $$
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30785")]
        public async Task TestAfterValidGenericConstraint2()
        {
            await VerifyKeywordAsync(
                """
                public class C<T> where T : List<int> $$
                {
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30785")]
        public async Task TestNotAfterGenericConstraintStartSecondParameter1()
        {
            await VerifyAbsenceAsync(
                """
                public class C
                {
                    void M<T> where T : Dictionary<int, $$
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30785")]
        public async Task TestNotAfterGenericConstraintStartSecondParameter2()
        {
            await VerifyAbsenceAsync(
                """
                public class C<T> where T : Dictionary<int, $$
                {
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30785")]
        public async Task TestAfterValidGenericConstraint3()
        {
            await VerifyKeywordAsync(
                """
                public class C
                {
                    void M<T> where T : Dictionary<int, string> $$
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30785")]
        public async Task TestAfterValidGenericConstraint4()
        {
            await VerifyKeywordAsync(
                """
                public class C<T> where T : Dictionary<int, string> $$
                {
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30785")]
        public async Task TestNotAfterDoubleUnclosedGenericConstraint1()
        {
            await VerifyAbsenceAsync(
                """
                public class C
                {
                    void M<T> where T : List<List<int>$$
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30785")]
        public async Task TestNotAfterDoubleUnclosedGenericConstraint2()
        {
            await VerifyAbsenceAsync(
                """
                public class C<T> where T : List<List<int>$$
                {
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30785")]
        public async Task TestAfterValidGenericConstraint5()
        {
            await VerifyKeywordAsync(
                """
                public class C
                {
                    void M<T> where T : List<List<int>> $$
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30785")]
        public async Task TestAfterValidGenericConstraint6()
        {
            await VerifyKeywordAsync(
                """
                public class C<T> where T : List<List<int>> $$
                {
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30785")]
        public async Task TestNotAfterTupleInsideGenericConstraintStart1()
        {
            await VerifyAbsenceAsync(
                """
                public class C
                {
                    void M<T> where T : List<(int, $$
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30785")]
        public async Task TestNotAfterTupleInsideGenericConstraintStart2()
        {
            await VerifyAbsenceAsync(
                """
                public class C<T> where T : List<(int, $$
                {
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30785")]
        public async Task TestNotAfterTupleClosedInsideGenericConstraintStart1()
        {
            await VerifyAbsenceAsync(
                """
                public class C
                {
                    void M<T> where T : List<(int, string)$$
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30785")]
        public async Task TestNotAfterTupleClosedInsideGenericConstraintStart2()
        {
            await VerifyAbsenceAsync(
                """
                public class C<T> where T : List<(int, string)$$
                {
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30785")]
        public async Task TestAfterValidGenericConstraintWithTuple1()
        {
            await VerifyKeywordAsync(
                """
                public class C
                {
                    void M<T> where T : List<(int, string)> $$
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30785")]
        public async Task TestAfterValidGenericConstraintWithTuple2()
        {
            await VerifyKeywordAsync(
                """
                public class C<T> where T : List<(int, string)> $$
                {
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72821")]
        public async Task TestNotAfterLocalFunction()
        {
            await VerifyAbsenceAsync(
                """
                class C
                {
                    void M<T>()
                    {
                        void Inner() $$
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72821")]
        public async Task TestAfterGenericLocalFunction()
        {
            await VerifyKeywordAsync(
                """
                class C
                {
                    void M<T>()
                    {
                        void Inner<T1>() $$
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/72821")]
        public async Task TestAfterFirstValidConstraintInGenericLocalFunction()
        {
            await VerifyKeywordAsync(
                """
                class C
                {
                    void M<T>()
                    {
                        void Inner<T1, T2>()
                            where T1 : C
                            $$
                """);
        }

        [Fact]
        public async Task TestNotInExtensionForType()
        {
            await VerifyAbsenceAsync(
                """
                implicit extension E for $$
                """);
        }

        [Fact]
        public async Task TestExtensionConstraints1()
        {
            await VerifyAbsenceAsync(
                """
                implicit extension E $$
                """);
        }

        [Fact]
        public async Task TestExtensionConstraints2()
        {
            await VerifyKeywordAsync(
                """
                implicit extension E<T> $$
                """);
        }

        [Fact]
        public async Task TestExtensionConstraints3()
        {
            await VerifyAbsenceAsync(
                """
                implicit extension E(int i) $$
                """);
        }

        [Fact]
        public async Task TestExtensionConstraints4()
        {
            await VerifyAbsenceAsync(
                """
                implicit extension E<T>(int i) $$
                """);
        }

        [Fact]
        public async Task TestExtensionConstraints5()
        {
            await VerifyAbsenceAsync(
                """
                implicit extension E : X $$
                """);
        }

        [Fact]
        public async Task TestExtensionConstraints6()
        {
            await VerifyKeywordAsync(
                """
                implicit extension E<T> : X $$
                """);
        }

        [Fact]
        public async Task TestExtensionConstraints7()
        {
            await VerifyAbsenceAsync(
                """
                implicit extension E : X, Y $$
                """);
        }

        [Fact]
        public async Task TestExtensionConstraints8()
        {
            await VerifyKeywordAsync(
                """
                implicit extension E<T> : X, Y $$
                """);
        }

        [Fact]
        public async Task TestExtensionConstraints9()
        {
            await VerifyAbsenceAsync(
                """
                implicit extension E : X<int> $$
                """);
        }

        [Fact]
        public async Task TestExtensionConstraints10()
        {
            await VerifyKeywordAsync(
                """
                implicit extension E<T> : X<int> $$
                """);
        }
    }
}
