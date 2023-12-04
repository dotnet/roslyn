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
    public class DefaultKeywordRecommenderTests : KeywordRecommenderTests
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
        public async Task TestNotInPreprocessor1()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                #$$
                """);
        }

        [Fact]
        public async Task TestNotInPreprocessor2()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                #if $$
                """);
        }

        [Fact]
        public async Task TestAfterHash()
        {
            await VerifyKeywordAsync(
@"#line $$");
        }

        [Fact]
        public async Task TestAfterHashAndSpace()
        {
            await VerifyKeywordAsync(
@"# line $$");
        }

        [Fact]
        public async Task TestInEmptyStatement()
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
        public async Task TestAfterSwitch()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                switch (expr) {
                    $$
                """));
        }

        [Fact]
        public async Task TestAfterCase()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                switch (expr) {
                    case 0:
                    $$
                """));
        }

        [Fact]
        public async Task TestAfterDefault()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                switch (expr) {
                    default:
                    $$
                """));
        }

        [Fact]
        public async Task TestAfterOneStatement()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                switch (expr) {
                    default:
                      Console.WriteLine();
                    $$
                """));
        }

        [Fact]
        public async Task TestAfterTwoStatements()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                switch (expr) {
                    default:
                      Console.WriteLine();
                      Console.WriteLine();
                    $$
                """));
        }

        [Fact]
        public async Task TestAfterBlock()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                switch (expr) {
                    default: {
                    }
                    $$
                """));
        }

        [Fact]
        public async Task TestAfterIfElse()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                switch (expr) {
                    default:
                      if (goo) {
                      } else {
                      }
                    $$
                """));
        }

        [Fact]
        public async Task TestAfterIncompleteStatement()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                switch (expr) {
                    default:
                       Console.WriteLine(
                    $$
                """));
        }

        [Fact]
        public async Task TestInsideBlock()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                switch (expr) {
                    default: {
                      $$
                """));
        }

        [Fact]
        public async Task TestAfterCompleteIf()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                switch (expr) {
                    default:
                      if (goo)
                        Console.WriteLine();
                    $$
                """));
        }

        [Fact]
        public async Task TestAfterIncompleteIf()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                switch (expr) {
                    default:
                      if (goo)
                        $$
                """));
        }

        [Fact]
        public async Task TestAfterWhile()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                switch (expr) {
                    default:
                      while (true) {
                      }
                    $$
                """));
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/552717")]
        public async Task TestNotAfterGotoInSwitch()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
                """
                switch (expr) {
                    default:
                      goto $$
                """));
        }

        [Fact]
        public async Task TestNotAfterGotoOutsideSwitch()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"goto $$"));
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

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46283")]
        public async Task TestInTypeParameterConstraint()
        {
            await VerifyKeywordAsync(
                """
                class C
                {
                    void M<T>() where T : $$
                    {
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46283")]
        public async Task TestInTypeParameterConstraint_InOverride()
        {
            await VerifyKeywordAsync(
                """
                class C : Base
                {
                    public override void M<T>() where T : $$
                    {
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/46283")]
        public async Task TestInTypeParameterConstraint_InExplicitInterfaceImplementation()
        {
            await VerifyKeywordAsync(
                """
                class C : I
                {
                    public void I.M<T>() where T : $$
                    {
                    }
                }
                """);
        }

        [Fact]
        public async Task TestAfterLambdaOpenParen()
        {
            await VerifyKeywordAsync(
@"var lam = ($$");
        }

        [Fact]
        public async Task TestAfterLambdaComma()
        {
            await VerifyKeywordAsync(
@"var lam = (int i, $$");
        }

        [Fact]
        public async Task TestLambdaDefaultParameterValue()
        {
            await VerifyKeywordAsync(
@"var lam = (int i = $$");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/36472")]
        public async Task InAmbiguousCast1()
        {
            await VerifyKeywordAsync(
                """
                class C
                {
                    static void Main(string[] args)
                    {
                        (int i, string s) tuple;
                        tuple = ($$)
                        Main(args);
                    }
                }
                """);
        }
    }
}
