// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations
{
    public abstract class SpecialTypeKeywordRecommenderTests : KeywordRecommenderTests
    {
        [Fact]
        public async Task TestAtRoot()
        {
            await VerifyKeywordAsync("$$", options: CSharp9ParseOptions);
        }

        [Fact]
        public async Task TestAtRoot_Interactive()
        {
            await VerifyKeywordAsync(SourceCodeKind.Script, "$$");
        }

        [Fact]
        public async Task TestAfterClass_Interactive()
        {
            await VerifyKeywordAsync(SourceCodeKind.Script, """
                class C { }
                $$
                """);
        }

        [Fact]
        public async Task TestAfterGlobalStatement()
        {
            await VerifyKeywordAsync("""
                System.Console.WriteLine();
                $$
                """, options: CSharp9ParseOptions);
        }

        [Fact]
        public async Task TestAfterGlobalStatement_Interactive()
        {
            await VerifyKeywordAsync(SourceCodeKind.Script, """
                System.Console.WriteLine();
                $$
                """);
        }

        [Fact]
        public async Task TestAfterGlobalVariableDeclaration()
        {
            await VerifyKeywordAsync("""
                int i = 0;
                $$
                """, options: CSharp9ParseOptions);
        }

        [Fact]
        public async Task TestAfterGlobalVariableDeclaration_Interactive()
        {
            await VerifyKeywordAsync(SourceCodeKind.Script, """
                int i = 0;
                $$
                """);
        }

        [Fact]
        public async Task TestNotInPreprocessor1()
        {
            await VerifyAbsenceAsync("#$$");
        }

        [Fact]
        public async Task TestNotInPreprocessor2()
        {
            await VerifyAbsenceAsync("#if $$");
        }

        [Fact]
        public async Task TestNotInUsingAlias()
        {
            await VerifyAbsenceAsync("using Goo = $$");
        }

        [Fact]
        public async Task TestNotInGlobalUsingAlias()
        {
            await VerifyAbsenceAsync("global using Goo = $$");
        }

        [Fact]
        public async Task TestInDelegateReturnType()
        {
            await VerifyKeywordAsync("public delegate $$");
        }

        [Fact]
        public async Task TestAfterConstInMemberContext()
        {
            await VerifyKeywordAsync("""
                class C {
                    const $$
                """);
        }

        [Theory]
        [CombinatorialData]
        public async Task TestAfterConstInStatementContext(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
                "const $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory]
        [CombinatorialData]
        public async Task TestAfterConstLocalDeclaration(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
                "const $$ int local;", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Fact]
        public async Task TestAfterRefInMemberContext()
        {
            await VerifyKeywordAsync("""
                class C {
                    ref $$
                """);
        }

        [Theory]
        [CombinatorialData]
        public async Task TestAfterRefInStatementContext(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
                "ref $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory]
        [CombinatorialData]
        public async Task TestAfterRefLocalDeclaration(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
                "ref $$ int local;", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory]
        [CombinatorialData]
        public async Task TestAfterRefLocalFunction(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
                "ref $$ int Function();", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory]
        [CombinatorialData]
        public async Task TestAfterRefExpression(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
                "ref int x = ref $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Fact]
        public async Task TestAfterRefReadonlyInMemberContext()
        {
            await VerifyKeywordAsync("""
                class C {
                    ref readonly $$
                """);
        }

        [Theory]
        [CombinatorialData]
        public async Task TestAfterRefReadonlyInStatementContext(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
                "ref readonly $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory]
        [CombinatorialData]
        public async Task TestAfterRefReadonlyLocalDeclaration(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
                "ref readonly $$ int local;", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory]
        [CombinatorialData]
        public async Task TestAfterRefReadonlyLocalFunction(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
                "ref readonly $$ int Function();", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Fact]
        public async Task TestInEmptyStatement()
        {
            await VerifyKeywordAsync(AddInsideMethod("$$"));
        }

        [Theory]
        [CombinatorialData]
        public async Task TestInCastType(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
                "var str = (($$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory]
        [CombinatorialData]
        public async Task TestInCastType2(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
                "var str = (($$)items) as string;", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Fact]
        public async Task TestInCastType3()
        {
            await VerifyKeywordAsync(AddInsideMethod("return (LeafSegment)(object)$$"));
        }

        [Fact]
        public async Task TestInCheckedCast()
        {
            await VerifyKeywordAsync(AddInsideMethod("return checked(($$"));
        }

        [Fact]
        public async Task TestInUncheckedCast()
        {
            await VerifyKeywordAsync(AddInsideMethod("return unchecked(($$"));
        }

        [Fact, WorkItem(543819, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543819")]
        public async Task TestInChecked()
        {
            await VerifyKeywordAsync(AddInsideMethod("var a = checked($$"));
        }

        [Fact, WorkItem(543819, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543819")]
        public async Task TestInUnchecked()
        {
            await VerifyKeywordAsync(AddInsideMethod("var a = unchecked($$"));
        }

        [Theory]
        [CombinatorialData]
        public async Task TestInGenericType1(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
                "IList<$$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory]
        [CombinatorialData]
        public async Task TestInGenericType2(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
                "IList<int,$$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory]
        [CombinatorialData]
        public async Task TestInGenericType3(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
                "IList<int[],$$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory]
        [CombinatorialData]
        public async Task TestInGenericType4(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
                "IList<IGoo<int?,byte*>,$$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Fact]
        public async Task TestNotInGenericClassDecl()
        {
            await VerifyAbsenceAsync("class CL<$$");
        }

        [Fact]
        public async Task TestNotInGenericClassDeclList()
        {
            await VerifyAbsenceAsync("class CL<T, $$");
        }

        [Fact]
        public async Task TestNotInGenericStructDecl()
        {
            await VerifyAbsenceAsync("struct S<$$");
        }

        [Fact]
        public async Task TestNotInGenericStructDeclList()
        {
            await VerifyAbsenceAsync("struct S<T, $$");
        }

        [Fact]
        public async Task TestNotInGenericInterfaceDecl()
        {
            await VerifyAbsenceAsync("interface S<$$");
        }

        [Fact]
        public async Task TestNotInGenericInterfaceDeclList()
        {
            await VerifyAbsenceAsync("interface S<T, $$");
        }

        [Fact]
        public async Task TestInGenericDelegateDecl()
        {
            await VerifyAbsenceAsync("delegate void Del<$$");
        }

        [Fact]
        public async Task TestNotInGenericDelegateDeclList()
        {
            await VerifyAbsenceAsync("delegate void Del<T, $$");
        }

        [Fact]
        public async Task TestNotInGenericMethodDecl()
        {
            await VerifyAbsenceAsync("class C { void Method<$$");
        }

        [Fact]
        public async Task TestNotInGenericMethodDeclList()
        {
            await VerifyAbsenceAsync("class C { void Method<T, $$");
        }

        [Fact]
        public async Task TestNotInBaseList()
        {
            await VerifyAbsenceAsync("class C : $$");
        }

        [Fact]
        public async Task TestInGenericType_InBaseList()
        {
            await VerifyKeywordAsync("class C : IList<$$");
        }

        [Theory]
        [CombinatorialData]
        public async Task TestAfterIs(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
                "var v = goo is $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory]
        [CombinatorialData]
        public async Task TestAfterAs(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
                "var v = goo as $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Fact]
        public async Task TestAfterMethod()
        {
            await VerifyKeywordAsync("""
                class C {
                  void Goo() {}
                  $$
                """);
        }

        [Fact]
        public async Task TestAfterField()
        {
            await VerifyKeywordAsync("""
                class C {
                  int i;
                  $$
                """);
        }

        [Fact]
        public async Task TestAfterProperty()
        {
            await VerifyKeywordAsync("""
                class C {
                  int i { get; }
                  $$
                """);
        }

        [Fact]
        public async Task TestAfterNestedAttribute()
        {
            await VerifyKeywordAsync("""
                class C {
                  [goo]
                  $$
                """);
        }

        [Fact]
        public async Task TestAfterStatementAttribute()
        {
            await VerifyKeywordAsync(AddInsideMethod("[Goo] $$"));
        }

        [Fact]
        public async Task TestAfterTopLevelMemberDeclaration()
        {
            await VerifyKeywordAsync(SourceCodeKind.Script, """
                void Goo()
                {
                }

                $$
                """);
        }

        [Fact]
        public async Task TestAfterNestedType()
        {
            await VerifyKeywordAsync("""
                class SimpleParsedExpression {
                    enum CommandType
                    {
                        Command_MAX,
                    };
                    $$[] commandNames
                """);
        }

        [Fact]
        public async Task TestInsideStruct()
        {
            await VerifyKeywordAsync("""
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
            await VerifyKeywordAsync("""
                class C {
                   $$
                """);
        }

        [Fact]
        public async Task TestNotAfterPartial()
        {
            await VerifyAbsenceAsync("partial $$");
        }

        [Fact]
        public async Task TestNotAfterNestedPartial()
        {
            await VerifyAbsenceAsync("""
                class C {
                    partial $$
                """);
        }

        [Fact]
        public async Task TestAfterNestedAbstract()
        {
            await VerifyKeywordAsync("""
                class C {
                    abstract $$
                """);
        }

        [Fact]
        public async Task TestAfterNestedInternal()
        {
            await VerifyKeywordAsync("""
                class C {
                    internal $$
                """);
        }

        [Fact]
        public async Task TestAfterNestedPublic()
        {
            await VerifyKeywordAsync("""
                class C {
                    public $$
                """);
        }

        [Fact]
        public async Task TestAfterPublicKeyword_Interactive()
        {
            await VerifyKeywordAsync(SourceCodeKind.Script, "public $$");
        }

        [Fact]
        public async Task TestAfterNestedPrivate()
        {
            await VerifyKeywordAsync("""
                class C {
                   private $$
                """);
        }

        [Fact]
        public async Task TestAfterNestedProtected()
        {
            await VerifyKeywordAsync("""
                class C {
                    protected $$
                """);
        }

        [Fact]
        public async Task TestAfterNestedSealed()
        {
            await VerifyKeywordAsync("""
                class C {
                    sealed $$
                """);
        }

        [Fact]
        public async Task TestAfterNestedStatic()
        {
            await VerifyKeywordAsync("""
                class C {
                    static $$
                """);
        }

        [Fact]
        public async Task TestAfterStaticKeyword_Interactive()
        {
            await VerifyKeywordAsync(SourceCodeKind.Script, "static $$");
        }

        [Fact]
        public async Task TestAfterVirtua()
        {
            await VerifyKeywordAsync("""
                class C {
                    virtual $$
                """);
        }

        [Fact]
        public async Task TestAfterNestedStaticPublic()
        {
            await VerifyKeywordAsync("""
                class C {
                    static public $$
                """);
        }

        [Fact]
        public async Task TestAfterNestedPublicStatic()
        {
            await VerifyKeywordAsync("""
                class C {
                    public static $$
                """);
        }

        [Fact]
        public async Task TestAfterVirtualPublic()
        {
            await VerifyKeywordAsync("""
                class C {
                    virtual public $$
                """);
        }

        [Theory]
        [CombinatorialData]
        public async Task TestInForVariableDeclaration(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
                "for ($$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory]
        [CombinatorialData]
        public async Task TestInForCondition(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
                "for (;$$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory]
        [CombinatorialData]
        public async Task TestInForIncrementor(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
                "for (;;$$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory]
        [CombinatorialData]
        public async Task TestInForeachVariableDeclaration(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
                "foreach ($$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory]
        [CombinatorialData]
        public async Task TestInUsingVariableDeclaration(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
                "using ($$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory]
        [CombinatorialData]
        public async Task TestInFromVariableDeclaration(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
                "var q = from $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory]
        [CombinatorialData]
        public async Task TestInJoinVariableDeclaration(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod("""
                var q = from a in b 
                        join $$
                """, topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Fact]
        public async Task TestAfterMethodOpenParen()
        {
            await VerifyKeywordAsync("""
                class C {
                    void Goo($$
                """);
        }

        [Fact]
        public async Task TestAfterMethodComma()
        {
            await VerifyKeywordAsync("""
                class C {
                    void Goo(int i, $$
                """);
        }

        [Fact]
        public async Task TestAfterMethodAttribute()
        {
            await VerifyKeywordAsync("""
                class C {
                    void Goo(int i, [Goo]$$
                """);
        }

        [Fact]
        public async Task TestAfterConstructorOpenParen()
        {
            await VerifyKeywordAsync("""
                class C {
                    public C($$
                """);
        }

        [Fact]
        public async Task TestAfterConstructorComma()
        {
            await VerifyKeywordAsync("""
                class C {
                    public C(int i, $$
                """);
        }

        [Fact]
        public async Task TestAfterConstructorAttribute()
        {
            await VerifyKeywordAsync("""
                class C {
                    public C(int i, [Goo]$$
                """);
        }

        [Fact]
        public async Task TestAfterIndexerBracket()
        {
            await VerifyKeywordAsync("""
                class C {
                    int this[$$
                """);
        }

        [Fact]
        public async Task TestAfterIndexerBracketComma()
        {
            await VerifyKeywordAsync("""
                class C {
                    int this[int i, $$
                """);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
        public async Task TestAfterDelegateOpenParen()
        {
            await VerifyKeywordAsync("delegate void D($$");
        }

        [Fact]
        public async Task TestAfterDelegateComma()
        {
            await VerifyKeywordAsync("delegate void D(int i, $$");
        }

        [Fact]
        public async Task TestAfterDelegateAttribute()
        {
            await VerifyKeywordAsync("delegate void D(int i, [Goo]$$");
        }

        [Fact]
        public async Task TestNotAfterPointerDecl()
        {
            await VerifyAbsenceAsync(AddInsideMethod("int* $$"));
        }

        [Fact]
        public async Task TestNotAfterNullableDecl()
        {
            await VerifyAbsenceAsync(AddInsideMethod("int? $$"));
        }

        [Fact]
        public async Task TestAfterThis()
        {
            await VerifyKeywordAsync("""
                static class C {
                     public static void Goo(this $$
                """);
        }

        [Fact]
        public async Task TestAfterRef()
        {
            await VerifyKeywordAsync("""
                class C {
                     void Goo(ref $$
                """);
        }

        [Fact]
        public async Task TestAfterOut()
        {
            await VerifyKeywordAsync("""
                class C {
                     void Goo(out $$
                """);
        }

        [Fact]
        public async Task TestAfterOut2()
        {
            await VerifyKeywordAsync("""
                class C {
                    private static void RoundToFloat(double d, out $$
                """);
        }

        [Fact]
        public async Task TestAfterOut3()
        {
            await VerifyKeywordAsync("""
                class C {
                    private static void RoundToFloat(double d, out $$ float f)
                """);
        }

        [Fact]
        public async Task TestAfterLambdaRef()
        {
            await VerifyKeywordAsync("""
                class C {
                     void Goo() {
                          System.Func<int, int> f = (ref $$
                """);
        }

        [Fact]
        public async Task TestAfterLambdaOut()
        {
            await VerifyKeywordAsync("""
                class C {
                     void Goo() {
                          System.Func<int, int> f = (out $$
                """);
        }

        [Fact]
        public async Task TestAfterParams()
        {
            await VerifyKeywordAsync("""
                class C {
                     void Goo(params $$
                """);
        }

        [Fact]
        public async Task TestInImplicitOperator()
        {
            await VerifyKeywordAsync("""
                class C {
                     public static implicit operator $$
                """);
        }

        [Fact]
        public async Task TestInExplicitOperator()
        {
            await VerifyKeywordAsync("""
                class C {
                     public static explicit operator $$
                """);
        }

        [Theory]
        [CombinatorialData]
        public async Task TestAfterNewInExpression(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
                "new $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, WorkItem(538804, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538804")]
        [CombinatorialData]
        public async Task TestInTypeOf(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod("typeof($$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory, WorkItem(538804, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/538804")]
        [CombinatorialData]
        public async Task TestInDefault(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod("default($$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Fact, WorkItem(544219, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544219")]
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

        [Fact, WorkItem(546938, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546938")]
        public async Task TestInCrefContext()
        {
            await VerifyKeywordAsync("""
                class Program
                {
                    /// <see cref="$$">
                    static void Main(string[] args)
                    {

                    }
                }
                """);
        }

        [Fact, WorkItem(546955, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546955")]
        public async Task TestInCrefContextNotAfterDot()
        {
            await VerifyAbsenceAsync("""
                /// <see cref="System.$$" />
                class C { }
                """);
        }

        [Fact, WorkItem(1468, "https://github.com/dotnet/roslyn/issues/1468")]
        public async Task TestNotInCrefTypeParameter()
        {
            await VerifyAbsenceAsync("""
                using System;
                /// <see cref="List{$$}" />
                class C { }
                """);
        }

        [Fact]
        public async Task Preselection()
        {
            await VerifyKeywordAsync($$"""
                class Program
                {
                    static void Main(string[] args)
                    {
                        Helper($$)
                    }
                    static void Helper({{KeywordText}} x) { }
                }
                """);
        }

        [Fact, WorkItem(14127, "https://github.com/dotnet/roslyn/issues/14127")]
        public async Task TestInTupleWithinType()
        {
            await VerifyKeywordAsync("""
                class Program
                {
                    ($$
                }
                """);
        }

        [Theory, WorkItem(14127, "https://github.com/dotnet/roslyn/issues/14127")]
        [CombinatorialData]
        public async Task TestInTupleWithinMember(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
                "($$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Fact]
        public async Task TestNotInDeclarationDeconstruction()
        {
            await VerifyAbsenceAsync(AddInsideMethod("var (x, $$) = (0, 0);"));
        }

        [Fact]
        public async Task TestInMixedDeclarationAndAssignmentInDeconstruction()
        {
            await VerifyKeywordAsync(AddInsideMethod("(x, $$) = (0, 0);"));
        }

        [Fact]
        public async Task TestInFunctionPointerType()
        {
            await VerifyKeywordAsync("""
                class C
                {
                    delegate*<$$
                """);
        }

        [Fact]
        public async Task TestInFunctionPointerTypeAfterComma()
        {
            await VerifyKeywordAsync("""
                class C
                {
                    delegate*<int, $$
                """);
        }

        [Fact]
        public async Task TestInFunctionPointerTypeAfterModifier()
        {
            await VerifyKeywordAsync("""
                class C
                {
                    delegate*<ref $$
                """);
        }

        [Fact]
        public async Task TestNotAfterDelegateAsterisk()
        {
            await VerifyAbsenceAsync("""
                class C
                {
                    delegate*$$
                """);
        }

        [Fact, WorkItem(60341, "https://github.com/dotnet/roslyn/issues/60341")]
        public async Task TestNotAfterAsync()
        {
            await VerifyAbsenceAsync(@"class c { async $$ }");
        }

        [Fact, WorkItem(60341, "https://github.com/dotnet/roslyn/issues/60341")]
        public async Task TestNotAfterAsyncAsType()
        {
            await VerifyAbsenceAsync(@"class c { async async $$ }");
        }

        [Theory, WorkItem(53585, "https://github.com/dotnet/roslyn/issues/53585")]
        [ClassData(typeof(TheoryDataKeywordsIndicatingLocalFunctionWithoutAsync))]
        public async Task TestAfterKeywordIndicatingLocalFunctionWithoutAsync(string keyword)
        {
            await VerifyKeywordAsync(AddInsideMethod($"{keyword} $$"));
        }

        [Theory, WorkItem(60341, "https://github.com/dotnet/roslyn/issues/60341")]
        [ClassData(typeof(TheoryDataKeywordsIndicatingLocalFunctionWithAsync))]
        public async Task TestNotAfterKeywordIndicatingLocalFunctionWithAsync(string keyword)
        {
            await VerifyAbsenceAsync(AddInsideMethod($"{keyword} $$"));
        }

        [Fact, WorkItem(64585, "https://github.com/dotnet/roslyn/issues/64585")]
        public async Task TestAfterRequired()
        {
            await VerifyKeywordAsync("""
                class C
                {
                    required $$
                }
                """);
        }
    }
}
