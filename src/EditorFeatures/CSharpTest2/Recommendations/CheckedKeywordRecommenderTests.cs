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
    public class CheckedKeywordRecommenderTests : KeywordRecommenderTests
    {
        [Fact]
        public async Task TestAtRoot_Interactive()
        {
            await VerifyKeywordAsync(SourceCodeKind.Script,
@"$$");
        }

        [Fact]
        public async Task TestAfterClass_Interactive()
        {
            await VerifyKeywordAsync(SourceCodeKind.Script,
                """
                class C { }
                $$
                """);
        }

        [Fact]
        public async Task TestAfterGlobalStatement_Interactive()
        {
            await VerifyKeywordAsync(SourceCodeKind.Script,
                """
                System.Console.WriteLine();
                $$
                """);
        }

        [Fact]
        public async Task TestAfterGlobalVariableDeclaration_Interactive()
        {
            await VerifyKeywordAsync(SourceCodeKind.Script,
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
        public async Task TestEmptyStatement()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"$$"));
        }

        [Fact]
        public async Task TestInExpression()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var q = $$"));
        }

        [Fact]
        public async Task TestNotInPreProcessor()
        {
            await VerifyAbsenceAsync(
@"#if a || $$");
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538804")]
        public async Task TestNotInTypeOf()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"typeof($$"));
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538804")]
        public async Task TestNotInDefault()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"default($$"));
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538804")]
        public async Task TestNotInSizeOf()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"sizeof($$"));
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544219")]
        public async Task TestNotInObjectInitializerMemberContext()
        {
            await VerifyAbsenceAsync("""
                class C
                {
                    public int x, y;
                    void M()
                    {
                        var c = new C { x = 2, y = 3, $$
                """);
        }

        [Fact]
        public async Task TestAfterRefExpression()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"ref int x = ref $$"));
        }

        [Fact]
        public async Task TestNotAfterImplicitOperator_01()
        {
            await VerifyAbsenceAsync(
                """
                class Goo {
                    public static implicit operator $$
                """);
        }

        [Fact]
        public async Task TestNotAfterImplicitOperator_02()
        {
            await VerifyAbsenceAsync(
                """
                class Goo {
                    public static implicit operator $$ int (Goo x){}
                """);
        }

        [Fact]
        public async Task TestAfterExplicitOperator_01()
        {
            await VerifyKeywordAsync(
                """
                class Goo {
                    public static explicit operator $$
                """);
        }

        [Fact]
        public async Task TestAfterExplicitOperator_02()
        {
            await VerifyKeywordAsync(
                """
                class Goo {
                    public static explicit /* some comment */ operator $$
                """);
        }

        [Fact]
        public async Task TestAfterExplicitOperator_03()
        {
            await VerifyKeywordAsync(
                """
                class Goo {
                    public static explicit operator $$ int (Goo x){}
                """);
        }

        [Fact]
        public async Task TestAfterOperator_01()
        {
            await VerifyKeywordAsync(
                """
                class Goo { 
                    public static int operator $$
                """);
        }

        [Fact]
        public async Task TestAfterOperator_02()
        {
            await VerifyKeywordAsync(
                """
                class Goo { 
                    public static int /* some comment */ operator $$
                """);
        }

        [Fact]
        public async Task TestAfterOperator_03()
        {
            await VerifyKeywordAsync(
                """
                class Goo { 
                    public static int operator $$ -(Goo x){}
                """);
        }

        [Fact]
        public async Task TestAfterOperator_04()
        {
            await VerifyKeywordAsync(
                """
                class Goo { 
                    public static int operator $$ -(Goo x, Goo y){}
                """);
        }

        [Fact]
        public async Task TestNotAfterOperator()
        {
            await VerifyAbsenceAsync(
@"operator $$");
        }

        [Fact]
        public async Task TestNotAfterImplicitOperator_ExplicitImplementation_01()
        {
            await VerifyAbsenceAsync(
                """
                class Goo {
                    public static implicit I1.operator $$
                """);
        }

        [Fact]
        public async Task TestNotAfterImplicitOperator_ExplicitImplementation_02()
        {
            await VerifyAbsenceAsync(
                """
                class Goo {
                    public static implicit I1.operator $$ int (Goo x){}
                """);
        }

        [Fact]
        public async Task TestAfterExplicitOperator_ExplicitImplementation_01()
        {
            await VerifyKeywordAsync(
                """
                class Goo {
                    public static explicit I1.operator $$
                """);
        }

        [Fact]
        public async Task TestAfterExplicitOperator_ExplicitImplementation_02()
        {
            await VerifyKeywordAsync(
                """
                class Goo {
                    public static explicit /* some comment */ I1.operator $$
                """);
        }

        [Fact]
        public async Task TestAfterExplicitOperator_ExplicitImplementation_03()
        {
            await VerifyKeywordAsync(
                """
                class Goo {
                    public static explicit I1. /* some comment */ operator $$
                """);
        }

        [Fact]
        public async Task TestAfterExplicitOperator_ExplicitImplementation_04()
        {
            await VerifyKeywordAsync(
                """
                class Goo {
                    public static explicit I1.operator $$ int (Goo x){}
                """);
        }

        [Fact]
        public async Task TestAfterOperator_ExplicitImplementation_01()
        {
            await VerifyKeywordAsync(
                """
                class Goo { 
                    public static int I1.operator $$
                """);
        }

        [Fact]
        public async Task TestAfterOperator_ExplicitImplementation_02()
        {
            await VerifyKeywordAsync(
                """
                class Goo { 
                    public static int /* some comment */ I1.operator $$
                """);
        }

        [Fact]
        public async Task TestAfterOperator_ExplicitImplementation_03()
        {
            await VerifyKeywordAsync(
                """
                class Goo { 
                    public static int I1. /* some comment */ operator $$
                """);
        }

        [Fact]
        public async Task TestAfterOperator_ExplicitImplementation_04()
        {
            await VerifyKeywordAsync(
                """
                class Goo { 
                    public static int I1.operator $$ -(Goo x){}
                """);
        }

        [Fact]
        public async Task TestAfterOperator_ExplicitImplementation_05()
        {
            await VerifyKeywordAsync(
                """
                class Goo { 
                    public static int I1.operator $$ -(Goo x, Goo y){}
                """);
        }
    }
}
