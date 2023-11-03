// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    [Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
    public class BaseKeywordRecommenderTests : KeywordRecommenderTests
    {
        [Fact]
        public async Task TestNotAtRoot_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
@"$$");
        }

        [Fact]
        public async Task TestNotInTopLevelMethod()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
                """
                void Goo()
                {
                    $$
                }
                """);
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
        public async Task TestNotAfterGlobalStatement()
        {
            await VerifyAbsenceAsync(
                """
                System.Console.WriteLine();
                $$
                """, options: CSharp9ParseOptions);
        }

        [Fact]
        public async Task TestNotAfterGlobalVariableDeclaration()
        {
            await VerifyAbsenceAsync(
                """
                int i = 0;
                $$
                """, options: CSharp9ParseOptions);
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
        public async Task TestInClassConstructorInitializer()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    public C() : $$
                """);
        }

        [Fact]
        public async Task TestInRecordConstructorInitializer()
        {
            // The recommender doesn't work in record in script
            // Tracked by https://github.com/dotnet/roslyn/issues/44865
            await VerifyWorkerAsync("""
                record C {
                    public C() : $$
                """, absent: false, options: TestOptions.RegularPreview);
        }

        [Fact]
        public async Task TestNotInStaticClassConstructorInitializer()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    static C() : $$
                """);
        }

        [Fact]
        public async Task TestNotInStructConstructorInitializer()
        {
            await VerifyAbsenceAsync(
                """
                struct C {
                    public C() : $$
                """);
        }

        [Fact]
        public async Task TestAfterCast()
        {
            await VerifyKeywordAsync(
                """
                struct C {
                    new internal ErrorCode Code { get { return (ErrorCode)$$
                """);
        }

        [Fact]
        public async Task TestInEmptyMethod()
        {
            await VerifyKeywordAsync(
                SourceCodeKind.Regular,
                AddInsideMethod(
@"$$"));
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538264")]
        public async Task TestNotInEnumMemberInitializer1()
        {
            await VerifyAbsenceAsync(
                """
                enum E {
                    a = $$
                }
                """);
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

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/16335")]
        public async Task InExpressionBodyAccessor()
        {
            await VerifyKeywordAsync("""
                class B
                {
                    public virtual int T { get => bas$$ }
                }
                """);
        }

        [Fact]
        public async Task TestAfterRefExpression()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"ref int x = ref $$"));
        }
    }
}
