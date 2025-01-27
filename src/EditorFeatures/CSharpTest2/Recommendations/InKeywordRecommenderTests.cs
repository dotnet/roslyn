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
    public class InKeywordRecommenderTests : KeywordRecommenderTests
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
        public async Task TestNotAfterFrom()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"var q = from $$"));
        }

        [Fact]
        public async Task TestAfterFromIdentifier()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var q = from x $$"));
        }

        [Fact]
        public async Task TestAfterFromAndTypeAndIdentifier()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var q = from int x $$"));
        }

        [Fact]
        public async Task TestNotAfterJoin()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
                """
                var q = from x in y
                          join $$
                """));
        }

        [Fact]
        public async Task TestAfterJoinIdentifier()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                var q = from x in y
                          join z $$
                """));
        }

        [Fact]
        public async Task TestAfterJoinAndTypeAndIdentifier()
        {
            await VerifyKeywordAsync(AddInsideMethod(
                """
                var q = from x in y
                          join int z $$
                """));
        }

        [Fact]
        public async Task TestAfterJoinNotAfterIn()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
                """
                var q = from x in y
                          join z in $$
                """));
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544158")]
        public async Task TestNotAfterJoinPredefinedType()
        {
            await VerifyAbsenceAsync(
                """
                using System;
                using System.Linq;
                class C {
                    void M()
                    {
                        var q = from x in y
                                join int $$
                """);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544158")]
        public async Task TestNotAfterJoinType()
        {
            await VerifyAbsenceAsync(
                """
                using System;
                using System.Linq;
                class C {
                    void M()
                    {
                        var q = from x in y
                                join Int32 $$
                """);
        }

        [Fact]
        public async Task TestInForEach()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"foreach (var v $$"));
        }

        [Fact]
        public async Task TestInForEach1()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"foreach (var v $$ c"));
        }

        [Fact]
        public async Task TestInForEach2()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"foreach (var v $$ c"));
        }

        [Fact]
        public async Task TestNotInForEach()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"foreach ($$"));
        }

        [Fact]
        public async Task TestNotInForEach1()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"foreach (var $$"));
        }

        [Fact]
        public async Task TestNotInForEach2()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"foreach (var v in $$"));
        }

        [Fact]
        public async Task TestNotInForEach3()
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"foreach (var v in c $$"));
        }

        [Fact]
        public async Task TestInterfaceTypeVarianceAfterAngle()
        {
            await VerifyKeywordAsync(
@"interface IGoo<$$");
        }

        [Fact]
        public async Task TestInterfaceTypeVarianceNotAfterIn()
        {
            await VerifyAbsenceAsync(
@"interface IGoo<in $$");
        }

        [Fact]
        public async Task TestInterfaceTypeVarianceAfterComma()
        {
            await VerifyKeywordAsync(
@"interface IGoo<Goo, $$");
        }

        [Fact]
        public async Task TestInterfaceTypeVarianceAfterAttribute()
        {
            await VerifyKeywordAsync(
@"interface IGoo<[Goo]$$");
        }

        [Fact]
        public async Task TestDelegateTypeVarianceAfterAngle()
        {
            await VerifyKeywordAsync(
@"delegate void D<$$");
        }

        [Fact]
        public async Task TestDelegateTypeVarianceAfterComma()
        {
            await VerifyKeywordAsync(
@"delegate void D<Goo, $$");
        }

        [Fact]
        public async Task TestDelegateTypeVarianceAfterAttribute()
        {
            await VerifyKeywordAsync(
@"delegate void D<[Goo]$$");
        }

        [Fact]
        public async Task TestNotInClassTypeVarianceAfterAngle()
        {
            await VerifyAbsenceAsync(
@"class IGoo<$$");
        }

        [Fact]
        public async Task TestNotInStructTypeVarianceAfterAngle()
        {
            await VerifyAbsenceAsync(
@"struct IGoo<$$");
        }

        [Fact]
        public async Task TestNotInBaseListAfterAngle()
        {
            await VerifyAbsenceAsync(
@"interface IGoo : Bar<$$");
        }

        [Fact]
        public async Task TestNotInGenericMethod()
        {
            await VerifyAbsenceAsync(
                """
                interface IGoo {
                    void Goo<$$
                """);
        }

        [Fact]
        public async Task TestFrom2()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var q2 = from int x $$ ((IEnumerable)src))"));
        }

        [Fact]
        public async Task TestFrom3()
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"var q2 = from x $$ ((IEnumerable)src))"));
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544158")]
        public async Task TestNotAfterFromPredefinedType()
        {
            await VerifyAbsenceAsync(
                """
                using System;
                using System.Linq;
                class C {
                    void M()
                    {
                        var q = from int $$
                """);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544158")]
        public async Task TestNotAfterFromType()
        {
            await VerifyAbsenceAsync(
                """
                using System;
                using System.Linq;
                class C {
                    void M()
                    {
                        var q = from Int32 $$
                """);
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [Fact]
        public async Task TestInAsParameterModifierInMethods()
        {
            await VerifyKeywordAsync("""
                class Program
                {
                    public static void Test($$ p) { }
                }
                """);
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [Fact]
        public async Task TestInAsParameterModifierInSecondParameter()
        {
            await VerifyKeywordAsync("""
                class Program
                {
                    public static void Test(int p1, $$ p2) { }
                }
                """);
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [Fact]

        public async Task TestInAsParameterModifierInDelegates()
        {
            await VerifyKeywordAsync("""
                public delegate int Delegate($$ int p);
                """);
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [Fact]
        public async Task TestInAsParameterModifierInLocalFunctions()
        {
            await VerifyKeywordAsync("""
                class Program
                {
                    public static void Test()
                    {
                        void localFunc($$ int p) { }
                    }
                }
                """);
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [Fact]
        public async Task TestInAsParameterModifierInLambdaExpressions()
        {
            await VerifyKeywordAsync("""
                public delegate int Delegate(in int p);

                class Program
                {
                    public static void Test()
                    {
                        Delegate lambda = ($$ int p) => p;
                    }
                }
                """);
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [Fact]
        public async Task TestInAsParameterModifierInAnonymousMethods()
        {
            await VerifyKeywordAsync("""
                public delegate int Delegate(in int p);

                class Program
                {
                    public static void Test()
                    {
                        Delegate anonymousDelegate = delegate ($$ int p) { return p; };
                    }
                }
                """);
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [Fact]
        public async Task TestInAsModifierInMethodReturnTypes()
        {
            await VerifyAbsenceAsync("""
                class Program
                {
                    public $$ int Test()
                    {
                        return ref x;
                    }
                }
                """);
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [Fact]
        public async Task TestInAsModifierInGlobalMemberDeclaration()
        {
            await VerifyAbsenceAsync(SourceCodeKind.Script, """
                public $$
                """);
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [Fact]
        public async Task TestInAsModifierInDelegateReturnType()
        {
            await VerifyAbsenceAsync("""
                public delegate $$ int Delegate();

                class Program
                {
                }
                """);
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [Fact]
        public async Task TestInAsModifierInMemberDeclaration()
        {
            await VerifyAbsenceAsync("""
                class Program
                {
                    public $$ int Test { get; set; }
                }
                """);
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [Fact]
        public async Task TestInMethodFirstArgumentModifier()
        {
            await VerifyKeywordAsync("""
                class C {
                    void M() {
                        Call($$
                """);
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [Fact]
        public async Task TestInMethodSecondArgumentModifier()
        {
            await VerifyKeywordAsync("""
                class C {
                    void M(object arg1) {
                        Call(arg1, $$
                """);
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [Fact]
        public async Task TestInBaseCallFirstArgumentModifier()
        {
            await VerifyKeywordAsync("""
                class C {
                    public C() : base($$
                """);
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [Fact]
        public async Task TestInBaseCallSecondArgumentModifier()
        {
            await VerifyKeywordAsync("""
                class C {
                    public C(object arg1) : base(arg1, $$
                """);
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [Fact]
        public async Task TestInThisCallFirstArgumentModifier()
        {
            await VerifyKeywordAsync("""
                class C {
                    public C() : this($$
                """);
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [Fact]
        public async Task TestInThisCallSecondArgumentModifier()
        {
            await VerifyKeywordAsync("""
                class C {
                    public C(object arg1) : this(arg1, $$
                """);
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/24079")]
        public async Task TestInAsParameterModifierInConversionOperators()
        {
            await VerifyKeywordAsync("""
                class Program
                {
                    public static explicit operator double($$) { }
                }
                """);
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/24079")]
        public async Task TestInAsParameterModifierInBinaryOperators()
        {
            await VerifyKeywordAsync("""
                class Program
                {
                    public static Program operator +($$) { }
                }
                """);
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [Fact]
        public async Task TestInConstructorCallFirstArgumentModifier()
        {
            await VerifyKeywordAsync("""
                class C {
                    void M() {
                        new MyType($$
                """);
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [Fact]
        public async Task TestInConstructorSecondArgumentModifier()
        {
            await VerifyKeywordAsync("""
                class C {
                    void M(object arg1) {
                        new MyType(arg1, $$
                """);
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [Fact]
        public async Task TestInMethodFirstNamedArgumentModifier()
        {
            await VerifyKeywordAsync("""
                class C {
                    void M() {
                        Call(a: $$
                """);
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [Fact]
        public async Task TestInMethodSecondNamedArgumentModifier()
        {
            await VerifyKeywordAsync("""
                class C {
                    void M(object arg1) {
                        Call(a: arg1, b: $$
                """);
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [Fact]
        public async Task TestInBaseCallFirstNamedArgumentModifier()
        {
            await VerifyKeywordAsync("""
                class C {
                    public C() : base(a: $$
                """);
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [Fact]
        public async Task TestInBaseCallSecondNamedArgumentModifier()
        {
            await VerifyKeywordAsync("""
                class C {
                    public C(object arg1) : base(a: arg1, b: $$
                """);
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [Fact]
        public async Task TestInThisCallFirstNamedArgumentModifier()
        {
            await VerifyKeywordAsync("""
                class C {
                    public C() : this(a: $$
                """);
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [Fact]
        public async Task TestInThisCallSecondNamedArgumentModifier()
        {
            await VerifyKeywordAsync("""
                class C {
                    public C(object arg1) : this(a: arg1, b: $$
                """);
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [Fact]
        public async Task TestInConstructorCallFirstNamedArgumentModifier()
        {
            await VerifyKeywordAsync("""
                class C {
                    void M() {
                        new MyType(a: $$
                """);
        }

        [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
        [Fact]
        public async Task TestInConstructorSecondNamedArgumentModifier()
        {
            await VerifyKeywordAsync("""
                class C {
                    void M(object arg1) {
                        new MyType(a: arg1, b: $$
                """);
        }

        [Fact]
        public async Task TestExtensionMethods_FirstParameter()
        {
            await VerifyKeywordAsync(
                """
                static class Extensions {
                    static void Extension($$
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30339")]
        public async Task TestExtensionMethods_FirstParameter_AfterThisKeyword()
        {
            await VerifyKeywordAsync(
                """
                static class Extensions {
                    static void Extension(this $$
                """);
        }

        [Fact]
        public async Task TestExtensionMethods_SecondParameter()
        {
            await VerifyKeywordAsync(
                """
                static class Extensions {
                    static void Extension(this int i, $$
                """);
        }

        [Fact]
        public async Task TestExtensionMethods_SecondParameter_AfterThisKeyword()
        {
            await VerifyAbsenceAsync(
                """
                static class Extensions {
                    static void Extension(this int i, this $$
                """);
        }

        [Fact]
        public async Task TestExtensionMethods_FirstParameter_NonStaticClass()
        {
            await VerifyKeywordAsync(
                """
                class Extensions {
                    static void Extension($$
                """);
        }

        [Fact]
        public async Task TestExtensionMethods_FirstParameter_AfterThisKeyword_NonStaticClass()
        {
            await VerifyAbsenceAsync(
                """
                class Extensions {
                    static void Extension(this $$
                """);
        }

        [Fact]
        public async Task TestExtensionMethods_SecondParameter_NonStaticClass()
        {
            await VerifyKeywordAsync(
                """
                class Extensions {
                    static void Extension(this int i, $$
                """);
        }

        [Fact]
        public async Task TestExtensionMethods_SecondParameter_AfterThisKeyword_NonStaticClass()
        {
            await VerifyAbsenceAsync(
                """
                class Extensions {
                    static void Extension(this int i, this $$
                """);
        }

        [Fact]
        public async Task TestExtensionMethods_FirstParameter_NonStaticMethod()
        {
            await VerifyKeywordAsync(
                """
                static class Extensions {
                    void Extension($$
                """);
        }

        [Fact]
        public async Task TestExtensionMethods_FirstParameter_AfterThisKeyword_NonStaticMethod()
        {
            await VerifyAbsenceAsync(
                """
                static class Extensions {
                    void Extension(this $$
                """);
        }

        [Fact]
        public async Task TestExtensionMethods_SecondParameter_NonStaticMethod()
        {
            await VerifyKeywordAsync(
                """
                static class Extensions {
                    void Extension(this int i, $$
                """);
        }

        [Fact]
        public async Task TestExtensionMethods_SecondParameter_AfterThisKeyword_NonStaticMethod()
        {
            await VerifyAbsenceAsync(
                """
                static class Extensions {
                    void Extension(this int i, this $$
                """);
        }

        [Fact]
        public async Task TestInFunctionPointerTypeNoExistingModifiers()
        {
            await VerifyKeywordAsync("""
                class C
                {
                    delegate*<$$
                """);
        }

        [Theory]
        [InlineData("in")]
        [InlineData("out")]
        [InlineData("ref")]
        [InlineData("ref readonly")]
        public async Task TestNotInFunctionPointerTypeExistingModifiers(string modifier)
        {
            await VerifyAbsenceAsync($@"
class C
{{
    delegate*<{modifier} $$");
        }

        [Fact]
        public async Task TestInParameterAfterScoped()
        {
            await VerifyKeywordAsync("""
                class C
                {
                    void M(scoped $$)
                }
                """);
        }

        [Fact]
        public async Task TestInParameterAfterThisScoped()
        {
            await VerifyKeywordAsync("""
                static class C
                {
                    static void M(this scoped $$)
                }
                """);
        }

        [Fact]
        public async Task TestInAnonymousMethodParameterAfterScoped()
        {
            await VerifyKeywordAsync("""
                class C
                {
                    void M()
                    {
                        var x = delegate (scoped $$) { };
                    }
                }
                """);
        }
    }
}
