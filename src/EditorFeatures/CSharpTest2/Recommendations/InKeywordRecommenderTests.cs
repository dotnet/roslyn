// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations;

[Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
public sealed class InKeywordRecommenderTests : KeywordRecommenderTests
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
    public Task TestNotAfterFrom()
        => VerifyAbsenceAsync(AddInsideMethod(
@"var q = from $$"));

    [Fact]
    public Task TestAfterFromIdentifier()
        => VerifyKeywordAsync(AddInsideMethod(
@"var q = from x $$"));

    [Fact]
    public Task TestAfterFromAndTypeAndIdentifier()
        => VerifyKeywordAsync(AddInsideMethod(
@"var q = from int x $$"));

    [Fact]
    public Task TestNotAfterJoin()
        => VerifyAbsenceAsync(AddInsideMethod(
            """
            var q = from x in y
                      join $$
            """));

    [Fact]
    public Task TestAfterJoinIdentifier()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            var q = from x in y
                      join z $$
            """));

    [Fact]
    public Task TestAfterJoinAndTypeAndIdentifier()
        => VerifyKeywordAsync(AddInsideMethod(
            """
            var q = from x in y
                      join int z $$
            """));

    [Fact]
    public Task TestAfterJoinNotAfterIn()
        => VerifyAbsenceAsync(AddInsideMethod(
            """
            var q = from x in y
                      join z in $$
            """));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544158")]
    public Task TestNotAfterJoinPredefinedType()
        => VerifyAbsenceAsync(
            """
            using System;
            using System.Linq;
            class C {
                void M()
                {
                    var q = from x in y
                            join int $$
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544158")]
    public Task TestNotAfterJoinType()
        => VerifyAbsenceAsync(
            """
            using System;
            using System.Linq;
            class C {
                void M()
                {
                    var q = from x in y
                            join Int32 $$
            """);

    [Fact]
    public Task TestInForEach()
        => VerifyKeywordAsync(AddInsideMethod(
@"foreach (var v $$"));

    [Fact]
    public Task TestInForEach1()
        => VerifyKeywordAsync(AddInsideMethod(
@"foreach (var v $$ c"));

    [Fact]
    public Task TestInForEach2()
        => VerifyKeywordAsync(AddInsideMethod(
@"foreach (var v $$ c"));

    [Fact]
    public Task TestNotInForEach()
        => VerifyAbsenceAsync(AddInsideMethod(
@"foreach ($$"));

    [Fact]
    public Task TestNotInForEach1()
        => VerifyAbsenceAsync(AddInsideMethod(
@"foreach (var $$"));

    [Fact]
    public Task TestNotInForEach2()
        => VerifyAbsenceAsync(AddInsideMethod(
@"foreach (var v in $$"));

    [Fact]
    public Task TestNotInForEach3()
        => VerifyAbsenceAsync(AddInsideMethod(
@"foreach (var v in c $$"));

    [Fact]
    public Task TestInterfaceTypeVarianceAfterAngle()
        => VerifyKeywordAsync(
@"interface IGoo<$$");

    [Fact]
    public Task TestInterfaceTypeVarianceNotAfterIn()
        => VerifyAbsenceAsync(
@"interface IGoo<in $$");

    [Fact]
    public Task TestInterfaceTypeVarianceAfterComma()
        => VerifyKeywordAsync(
@"interface IGoo<Goo, $$");

    [Fact]
    public Task TestInterfaceTypeVarianceAfterAttribute()
        => VerifyKeywordAsync(
@"interface IGoo<[Goo]$$");

    [Fact]
    public Task TestDelegateTypeVarianceAfterAngle()
        => VerifyKeywordAsync(
@"delegate void D<$$");

    [Fact]
    public Task TestDelegateTypeVarianceAfterComma()
        => VerifyKeywordAsync(
@"delegate void D<Goo, $$");

    [Fact]
    public Task TestDelegateTypeVarianceAfterAttribute()
        => VerifyKeywordAsync(
@"delegate void D<[Goo]$$");

    [Fact]
    public Task TestNotInClassTypeVarianceAfterAngle()
        => VerifyAbsenceAsync(
@"class IGoo<$$");

    [Fact]
    public Task TestNotInStructTypeVarianceAfterAngle()
        => VerifyAbsenceAsync(
@"struct IGoo<$$");

    [Fact]
    public Task TestNotInBaseListAfterAngle()
        => VerifyAbsenceAsync(
@"interface IGoo : Bar<$$");

    [Fact]
    public Task TestNotInGenericMethod()
        => VerifyAbsenceAsync(
            """
            interface IGoo {
                void Goo<$$
            """);

    [Fact]
    public Task TestFrom2()
        => VerifyKeywordAsync(AddInsideMethod(
@"var q2 = from int x $$ ((IEnumerable)src))"));

    [Fact]
    public Task TestFrom3()
        => VerifyKeywordAsync(AddInsideMethod(
@"var q2 = from x $$ ((IEnumerable)src))"));

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544158")]
    public Task TestNotAfterFromPredefinedType()
        => VerifyAbsenceAsync(
            """
            using System;
            using System.Linq;
            class C {
                void M()
                {
                    var q = from int $$
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/544158")]
    public Task TestNotAfterFromType()
        => VerifyAbsenceAsync(
            """
            using System;
            using System.Linq;
            class C {
                void M()
                {
                    var q = from Int32 $$
            """);

    [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
    [Fact]
    public Task TestInAsParameterModifierInMethods()
        => VerifyKeywordAsync("""
            class Program
            {
                public static void Test($$ p) { }
            }
            """);

    [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
    [Fact]
    public Task TestInAsParameterModifierInSecondParameter()
        => VerifyKeywordAsync("""
            class Program
            {
                public static void Test(int p1, $$ p2) { }
            }
            """);

    [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
    [Fact]

    public Task TestInAsParameterModifierInDelegates()
        => VerifyKeywordAsync("""
            public delegate int Delegate($$ int p);
            """);

    [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
    [Fact]
    public Task TestInAsParameterModifierInLocalFunctions()
        => VerifyKeywordAsync("""
            class Program
            {
                public static void Test()
                {
                    void localFunc($$ int p) { }
                }
            }
            """);

    [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
    [Fact]
    public Task TestInAsParameterModifierInLambdaExpressions()
        => VerifyKeywordAsync("""
            public delegate int Delegate(in int p);

            class Program
            {
                public static void Test()
                {
                    Delegate lambda = ($$ int p) => p;
                }
            }
            """);

    [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
    [Fact]
    public Task TestInAsParameterModifierInAnonymousMethods()
        => VerifyKeywordAsync("""
            public delegate int Delegate(in int p);

            class Program
            {
                public static void Test()
                {
                    Delegate anonymousDelegate = delegate ($$ int p) { return p; };
                }
            }
            """);

    [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
    [Fact]
    public Task TestInAsModifierInMethodReturnTypes()
        => VerifyAbsenceAsync("""
            class Program
            {
                public $$ int Test()
                {
                    return ref x;
                }
            }
            """);

    [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
    [Fact]
    public Task TestInAsModifierInGlobalMemberDeclaration()
        => VerifyAbsenceAsync(SourceCodeKind.Script, """
            public $$
            """);

    [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
    [Fact]
    public Task TestInAsModifierInDelegateReturnType()
        => VerifyAbsenceAsync("""
            public delegate $$ int Delegate();

            class Program
            {
            }
            """);

    [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
    [Fact]
    public Task TestInAsModifierInMemberDeclaration()
        => VerifyAbsenceAsync("""
            class Program
            {
                public $$ int Test { get; set; }
            }
            """);

    [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
    [Fact]
    public Task TestInMethodFirstArgumentModifier()
        => VerifyKeywordAsync("""
            class C {
                void M() {
                    Call($$
            """);

    [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
    [Fact]
    public Task TestInMethodSecondArgumentModifier()
        => VerifyKeywordAsync("""
            class C {
                void M(object arg1) {
                    Call(arg1, $$
            """);

    [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
    [Fact]
    public Task TestInBaseCallFirstArgumentModifier()
        => VerifyKeywordAsync("""
            class C {
                public C() : base($$
            """);

    [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
    [Fact]
    public Task TestInBaseCallSecondArgumentModifier()
        => VerifyKeywordAsync("""
            class C {
                public C(object arg1) : base(arg1, $$
            """);

    [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
    [Fact]
    public Task TestInThisCallFirstArgumentModifier()
        => VerifyKeywordAsync("""
            class C {
                public C() : this($$
            """);

    [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
    [Fact]
    public Task TestInThisCallSecondArgumentModifier()
        => VerifyKeywordAsync("""
            class C {
                public C(object arg1) : this(arg1, $$
            """);

    [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/24079")]
    public Task TestInAsParameterModifierInConversionOperators()
        => VerifyKeywordAsync("""
            class Program
            {
                public static explicit operator double($$) { }
            }
            """);

    [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/24079")]
    public Task TestInAsParameterModifierInBinaryOperators()
        => VerifyKeywordAsync("""
            class Program
            {
                public static Program operator +($$) { }
            }
            """);

    [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
    [Fact]
    public Task TestInConstructorCallFirstArgumentModifier()
        => VerifyKeywordAsync("""
            class C {
                void M() {
                    new MyType($$
            """);

    [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
    [Fact]
    public Task TestInConstructorSecondArgumentModifier()
        => VerifyKeywordAsync("""
            class C {
                void M(object arg1) {
                    new MyType(arg1, $$
            """);

    [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
    [Fact]
    public Task TestInMethodFirstNamedArgumentModifier()
        => VerifyKeywordAsync("""
            class C {
                void M() {
                    Call(a: $$
            """);

    [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
    [Fact]
    public Task TestInMethodSecondNamedArgumentModifier()
        => VerifyKeywordAsync("""
            class C {
                void M(object arg1) {
                    Call(a: arg1, b: $$
            """);

    [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
    [Fact]
    public Task TestInBaseCallFirstNamedArgumentModifier()
        => VerifyKeywordAsync("""
            class C {
                public C() : base(a: $$
            """);

    [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
    [Fact]
    public Task TestInBaseCallSecondNamedArgumentModifier()
        => VerifyKeywordAsync("""
            class C {
                public C(object arg1) : base(a: arg1, b: $$
            """);

    [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
    [Fact]
    public Task TestInThisCallFirstNamedArgumentModifier()
        => VerifyKeywordAsync("""
            class C {
                public C() : this(a: $$
            """);

    [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
    [Fact]
    public Task TestInThisCallSecondNamedArgumentModifier()
        => VerifyKeywordAsync("""
            class C {
                public C(object arg1) : this(a: arg1, b: $$
            """);

    [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
    [Fact]
    public Task TestInConstructorCallFirstNamedArgumentModifier()
        => VerifyKeywordAsync("""
            class C {
                void M() {
                    new MyType(a: $$
            """);

    [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
    [Fact]
    public Task TestInConstructorSecondNamedArgumentModifier()
        => VerifyKeywordAsync("""
            class C {
                void M(object arg1) {
                    new MyType(a: arg1, b: $$
            """);

    [Fact]
    public Task TestExtensionMethods_FirstParameter()
        => VerifyKeywordAsync(
            """
            static class Extensions {
                static void Extension($$
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/30339")]
    public Task TestExtensionMethods_FirstParameter_AfterThisKeyword()
        => VerifyKeywordAsync(
            """
            static class Extensions {
                static void Extension(this $$
            """);

    [Fact]
    public Task TestExtensionMethods_SecondParameter()
        => VerifyKeywordAsync(
            """
            static class Extensions {
                static void Extension(this int i, $$
            """);

    [Fact]
    public Task TestExtensionMethods_SecondParameter_AfterThisKeyword()
        => VerifyAbsenceAsync(
            """
            static class Extensions {
                static void Extension(this int i, this $$
            """);

    [Fact]
    public Task TestExtensionMethods_FirstParameter_NonStaticClass()
        => VerifyKeywordAsync(
            """
            class Extensions {
                static void Extension($$
            """);

    [Fact]
    public Task TestExtensionMethods_FirstParameter_AfterThisKeyword_NonStaticClass()
        => VerifyAbsenceAsync(
            """
            class Extensions {
                static void Extension(this $$
            """);

    [Fact]
    public Task TestExtensionMethods_SecondParameter_NonStaticClass()
        => VerifyKeywordAsync(
            """
            class Extensions {
                static void Extension(this int i, $$
            """);

    [Fact]
    public Task TestExtensionMethods_SecondParameter_AfterThisKeyword_NonStaticClass()
        => VerifyAbsenceAsync(
            """
            class Extensions {
                static void Extension(this int i, this $$
            """);

    [Fact]
    public Task TestExtensionMethods_FirstParameter_NonStaticMethod()
        => VerifyKeywordAsync(
            """
            static class Extensions {
                void Extension($$
            """);

    [Fact]
    public Task TestExtensionMethods_FirstParameter_AfterThisKeyword_NonStaticMethod()
        => VerifyAbsenceAsync(
            """
            static class Extensions {
                void Extension(this $$
            """);

    [Fact]
    public Task TestExtensionMethods_SecondParameter_NonStaticMethod()
        => VerifyKeywordAsync(
            """
            static class Extensions {
                void Extension(this int i, $$
            """);

    [Fact]
    public Task TestExtensionMethods_SecondParameter_AfterThisKeyword_NonStaticMethod()
        => VerifyAbsenceAsync(
            """
            static class Extensions {
                void Extension(this int i, this $$
            """);

    [Fact]
    public Task TestInFunctionPointerTypeNoExistingModifiers()
        => VerifyKeywordAsync("""
            class C
            {
                delegate*<$$
            """);

    [Theory]
    [InlineData("in")]
    [InlineData("out")]
    [InlineData("ref")]
    [InlineData("ref readonly")]
    public Task TestNotInFunctionPointerTypeExistingModifiers(string modifier)
        => VerifyAbsenceAsync($$"""
            class C
            {
                delegate*<{{modifier}} $$
            """);

    [Fact]
    public Task TestInParameterAfterScoped()
        => VerifyKeywordAsync("""
            class C
            {
                void M(scoped $$)
            }
            """);

    [Fact]
    public Task TestInParameterAfterThisScoped()
        => VerifyKeywordAsync("""
            static class C
            {
                static void M(this scoped $$)
            }
            """);

    [Fact]
    public Task TestInAnonymousMethodParameterAfterScoped()
        => VerifyKeywordAsync("""
            class C
            {
                void M()
                {
                    var x = delegate (scoped $$) { };
                }
            }
            """);
}
