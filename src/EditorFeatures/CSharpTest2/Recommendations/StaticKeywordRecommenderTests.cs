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
    public class StaticKeywordRecommenderTests : KeywordRecommenderTests
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

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32174")]
        public async Task TestInEmptyStatement()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"$$"));
        }

        [Fact]
        public async Task TestInCompilationUnit()
        {
            await VerifyKeywordAsync(
@"$$");
        }

        [Fact]
        public async Task TestAfterExtern()
        {
            await VerifyKeywordAsync(
                """
                extern alias Goo;
                $$
                """);
        }

        [Fact]
        public async Task TestAfterUsing()
        {
            await VerifyKeywordAsync(
                """
                using Goo;
                $$
                """);
        }

        [Fact]
        public async Task TestAfterGlobalUsing()
        {
            await VerifyKeywordAsync(
                """
                global using Goo;
                $$
                """);
        }

        [Fact]
        public async Task TestAfterNamespace()
        {
            await VerifyKeywordAsync(
                """
                namespace N {}
                $$
                """);
        }

        [Fact]
        public async Task TestAfterFileScopedNamespace()
        {
            await VerifyKeywordAsync(
                """
                namespace N;
                $$
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66319")]
        public async Task TestFileKeywordInsideNamespace()
        {
            await VerifyKeywordAsync(
                """
                namespace N {
                file $$
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66319")]
        public async Task TestFileKeywordInsideNamespaceBeforeClass()
        {
            await VerifyKeywordAsync(
                """
                namespace N {
                file $$
                class C {}
                }
                """);
        }

        [Fact]
        public async Task TestAfterTypeDeclaration()
        {
            await VerifyKeywordAsync(
                """
                class C {}
                $$
                """);
        }

        [Fact]
        public async Task TestAfterDelegateDeclaration()
        {
            await VerifyKeywordAsync(
                """
                delegate void Goo();
                $$
                """);
        }

        [Fact]
        public async Task TestAfterMethod()
        {
            await VerifyKeywordAsync(
                """
                class C {
                  void Goo() {}
                  $$
                """);
        }

        [Fact]
        public async Task TestAfterField()
        {
            await VerifyKeywordAsync(
                """
                class C {
                  int i;
                  $$
                """);
        }

        [Fact]
        public async Task TestAfterProperty()
        {
            await VerifyKeywordAsync(
                """
                class C {
                  int i { get; }
                  $$
                """);
        }

        [Fact]
        public async Task TestNotBeforeUsing()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Regular,
                """
                $$
                using Goo;
                """);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/9880")]
        public async Task TestNotBeforeUsing_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
                """
                $$
                using Goo;
                """);
        }

        [Fact]
        public async Task TestNotBeforeGlobalUsing()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Regular,
                """
                $$
                global using Goo;
                """);
        }

        [Fact(Skip = "https://github.com/dotnet/roslyn/issues/9880")]
        public async Task TestNotBeforeGlobalUsing_Interactive()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script,
                """
                $$
                global using Goo;
                """);
        }

        [Fact]
        public async Task TestAfterAssemblyAttribute()
        {
            await VerifyKeywordAsync(
                """
                [assembly: goo]
                $$
                """);
        }

        [Fact]
        public async Task TestAfterRootAttribute()
        {
            await VerifyKeywordAsync(
                """
                [goo]
                $$
                """);
        }

        [Fact]
        public async Task TestAfterNestedAttribute()
        {
            await VerifyKeywordAsync(
                """
                class C {
                  [goo]
                  $$
                """);
        }

        // This will be fixed once we have accessibility for members
        [Fact]
        public async Task TestInsideStruct()
        {
            await VerifyKeywordAsync(
                """
                struct S {
                   $$
                """);
        }

        [Fact]
        public async Task TestInsideInterface()
        {
            await VerifyKeywordAsync("""
                interface I {
                   $$
                """);
        }

        [Fact]
        public async Task TestInsideClass()
        {
            await VerifyKeywordAsync(
                """
                class C {
                   $$
                """);
        }

        [Fact]
        public async Task TestNotAfterPartial()
            => await VerifyAbsenceAsync(@"partial $$");

        [Fact]
        public async Task TestNotAfterAbstract()
            => await VerifyAbsenceAsync(@"abstract $$");

        [Fact]
        public async Task TestAfterInternal()
        {
            await VerifyKeywordAsync(
@"internal $$");
        }

        [Fact]
        public async Task TestAfterPublic()
        {
            await VerifyKeywordAsync(
@"public $$");
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/66319")]
        public async Task TestAfterFile()
        {
            await VerifyKeywordAsync(SourceCodeKind.Regular,
@"file $$");
        }

        [Fact]
        public async Task TestNotAfterStaticPublic()
            => await VerifyAbsenceAsync(@"static public $$");

        [Fact]
        public async Task TestNotAfterPublicStatic()
            => await VerifyAbsenceAsync(@"public static $$");

        [Fact]
        public async Task TestNotAfterVirtualPublic()
            => await VerifyAbsenceAsync(@"virtual public $$");

        [Fact]
        public async Task TestAfterPrivate()
        {
            await VerifyKeywordAsync(
@"private $$");
        }

        [Fact]
        public async Task TestAfterProtected()
        {
            await VerifyKeywordAsync(
@"protected $$");
        }

        [Fact]
        public async Task TestNotAfterSealed()
            => await VerifyAbsenceAsync(@"sealed $$");

        [Fact]
        public async Task TestNotAfterStatic()
            => await VerifyAbsenceAsync(@"static $$");

        [Fact]
        public async Task TestNotAfterClass()
            => await VerifyAbsenceAsync(@"class $$");

        [Fact]
        public async Task TestNotAfterDelegate()
            => await VerifyAbsenceAsync(@"delegate $$");

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32214")]
        public async Task TestNotBetweenUsings()
        {
            var source = """
                using Goo;
                $$
                using Bar;
                """;

            await VerifyWorkerAsync(source, absent: true);

            // Recommendation in scripting is not stable. See https://github.com/dotnet/roslyn/issues/32214
            //await VerifyWorkerAsync(source, absent: true, Options.Script);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32214")]
        public async Task TestNotBetweenGlobalUsings_01()
        {
            var source = """
                global using Goo;
                $$
                using Bar;
                """;

            await VerifyWorkerAsync(source, absent: true);

            // Recommendation in scripting is not stable. See https://github.com/dotnet/roslyn/issues/32214
            //await VerifyWorkerAsync(source, absent: true, Options.Script);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32214")]
        public async Task TestNotBetweenGlobalUsings_02()
        {
            var source = """
                global using Goo;
                $$
                global using Bar;
                """;

            await VerifyWorkerAsync(source, absent: true);

            // Recommendation in scripting is not stable. See https://github.com/dotnet/roslyn/issues/32214
            //await VerifyWorkerAsync(source, absent: true, Options.Script);
        }

        [Theory]
        [CombinatorialData]
        public async Task TestNotAfterNestedAbstract([CombinatorialValues("class", "struct", "record", "record struct", "record class")] string declarationKind)
        {
            await VerifyAbsenceAsync(declarationKind + """
                C {
                   abstract $$
                """);
        }

        [Fact]
        public async Task TestAfterNestedAbstractInInterface()
        {
            await VerifyKeywordAsync("""
                interface C {
                    abstract $$
                """);
        }

        [Theory]
        [CombinatorialData]
        public async Task TestNotAfterNestedVirtual([CombinatorialValues("class", "struct", "record", "record struct", "record class")] string declarationKind)
        {
            await VerifyAbsenceAsync(declarationKind + """
                C {
                   virtual $$
                """);
        }

        [Fact]
        public async Task TestAfterNestedVirtualInInterface()
        {
            await VerifyKeywordAsync("""
                interface C {
                    virtual $$
                """);
        }

        [Theory]
        [CombinatorialData]
        public async Task TestNotAfterNestedOverride([CombinatorialValues("class", "struct", "record", "record struct", "record class", "interface")] string declarationKind)
        {
            await VerifyAbsenceAsync(declarationKind + """
                C {
                   override $$
                """);
        }

        [Theory]
        [CombinatorialData]
        public async Task TestNotAfterNestedStatic([CombinatorialValues("class", "struct", "record", "record struct", "record class", "interface")] string declarationKind)
        {
            await VerifyAbsenceAsync(declarationKind + """
                C {
                   static $$
                """);
        }

        [Theory]
        [CombinatorialData]
        public async Task TestNotAfterNestedSealed([CombinatorialValues("class", "struct", "record", "record struct", "record class")] string declarationKind)
        {
            await VerifyAbsenceAsync(declarationKind + """
                C {
                   sealed $$
                """);
        }

        [Fact]
        public async Task TestAfterNestedSealedInInterface()
        {
            await VerifyKeywordAsync("""
                interface C {
                    sealed $$
                """);
        }

        [Fact]
        public async Task TestAfterNestedReadOnly()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    readonly $$
                """);
        }

        [Fact]
        public async Task TestAfterAsync()
        {
            await VerifyKeywordAsync(
                """
                class C {
                    async $$
                """);
        }

        [Fact]
        public async Task TestAfterUsingInCompilationUnit()
        {
            await VerifyKeywordAsync(
@"using $$");
        }

        [Fact]
        public async Task TestAfterGlobalUsingInCompilationUnit()
        {
            await VerifyKeywordAsync(
@"global using $$");
        }

        [Fact]
        public async Task TestNotAfterUsingInMethodBody()
        {
            await VerifyAbsenceAsync(
                """
                class C {
                    void M() {
                        using $$
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/32174")]
        public async Task TestLocalFunction()
            => await VerifyKeywordAsync(AddInsideMethod(@" $$ void local() { }"));

        [Fact]
        public async Task TestInCase()
        {
            await VerifyKeywordAsync(AddInsideMethod("""
                switch (i)
                {
                    case 0:
                        $$
                """));
        }

        [Fact]
        public async Task TestInAssignment()
        {
            await VerifyKeywordAsync(AddInsideMethod("""
                System.Action x = $$
                """));
        }

        [Fact]
        public async Task TestBeforeLambdaInAssignment()
        {
            await VerifyKeywordAsync(AddInsideMethod("""
                System.Action x = $$ (x) => { }
                """));
        }

        [Fact]
        public async Task TestBeforeAnonymousMethodInAssignment()
        {
            await VerifyKeywordAsync(AddInsideMethod("""
                System.Action x = $$ delegate(x) { }
                """));
        }

        [Fact]
        public async Task TestAfterAsyncInAssignment()
        {
            await VerifyKeywordAsync(AddInsideMethod("""
                System.Action x = async $$
                """));
        }

        [Fact]
        public async Task TestBeforeAsyncInAssignment()
        {
            await VerifyKeywordAsync(AddInsideMethod("""
                System.Action x = $$ async
                """));
        }

        [Fact]
        public async Task TestBeforeAsyncLambdaInAssignment()
        {
            await VerifyKeywordAsync(AddInsideMethod("""
                System.Action x = $$ async (x) => { }
                """));
        }

        [Fact]
        public async Task TestAfterAsyncBeforeLambdaInAssignment()
        {
            await VerifyKeywordAsync(AddInsideMethod("""
                System.Action x = async $$ (x) => { }
                """));
        }

        [Fact]
        public async Task TestAfterAsyncLambdaParamInAssignment()
        {
            await VerifyKeywordAsync(AddInsideMethod("""
                System.Action x = async async $$ (x) => { }
                """));
        }

        [Fact]
        public async Task TestInCall()
        {
            await VerifyKeywordAsync(AddInsideMethod("""
                M($$
                """));
        }

        [Fact]
        public async Task TestInIndexer()
        {
            await VerifyKeywordAsync(AddInsideMethod("""
                this[$$
                """));
        }

        [Fact]
        public async Task TestInCallAfterArgumentLabel()
        {
            await VerifyKeywordAsync(AddInsideMethod("""
                M(param: $$
                """));
        }

        [Fact]
        public async Task TestInCallAfterRef()
        {
            await VerifyAbsenceAsync(AddInsideMethod("""
                M(ref $$
                """));
        }

        [Fact]
        public async Task TestInCallAfterIn()
        {
            await VerifyAbsenceAsync(AddInsideMethod("""
                M(in $$
                """));
        }

        [Fact]
        public async Task TestInCallAfterOut()
        {
            await VerifyAbsenceAsync(AddInsideMethod("""
                M(in $$
                """));
        }

        [Fact]
        public async Task TestInAttribute()
        {
            await VerifyAbsenceAsync("""
                class C
                {
                    [$$
                    void M()
                    {
                    }
                }
                """);
        }

        [Fact]
        public async Task TestInAttributeArgument()
        {
            await VerifyAbsenceAsync("""
                class C
                {
                    [Attr($$
                    void M()
                    {
                    }
                }
                """);
        }

        [Fact]
        public async Task TestInFor()
            => await VerifyKeywordAsync(AddInsideMethod(@" for (int i = 0; i < 0; $$) "));

        [Fact]
        public async Task TestAfterUsingKeywordBeforeTopLevelStatement()
        {
            await VerifyKeywordAsync("""
using $$
var i = 1;
""");
        }
    }
}
