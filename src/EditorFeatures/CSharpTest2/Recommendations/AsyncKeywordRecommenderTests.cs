// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Recommendations;

[Trait(Traits.Feature, Traits.Features.KeywordRecommending)]
public sealed class AsyncKeywordRecommenderTests : KeywordRecommenderTests
{
    [Fact]
    public Task TestMethodDeclaration1()
        => VerifyKeywordAsync("""
            class C
            {
                $$
            }
            """);

    [Fact]
    public Task TestMethodDeclaration2()
        => VerifyKeywordAsync("""
            class C
            {
                public $$
            }
            """);

    [Fact]
    public Task TestMethodDeclaration3()
        => VerifyKeywordAsync("""
            class C
            {
                $$ public void goo() { }
            }
            """);

    [Fact]
    public Task TestMethodDeclarationAsyncAfterCursor()
        => VerifyKeywordAsync("""
            class C
            {
                public $$ async void goo() { }
            }
            """);

    [Fact]
    public Task TestInsideInterface()
        => VerifyKeywordAsync("""
            interface C
            {
                $$
            }
            """);

    [Fact]
    public Task TestMethodDeclarationInGlobalStatement1()
        => VerifyKeywordAsync(SourceCodeKind.Script, @"$$");

    [Fact]
    public Task TestMethodDeclarationInGlobalStatement2()
        => VerifyKeywordAsync(SourceCodeKind.Script, @"public $$");

    [Fact]
    public Task TestExpressionContext()
        => VerifyKeywordAsync("""
            class C
            {
                void goo()
                {
                    goo($$
                }
            }
            """);

    [Fact]
    public Task TestNotInParameter()
        => VerifyAbsenceAsync("""
            class C
            {
                void goo($$)
                {
                }
            }
            """);

    [Fact]
    public Task TestBeforeLambda()
        => VerifyKeywordAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    var z =  $$ () => 2;
                }
            }
            """);

    [Fact]
    public Task TestBeforeStaticLambda()
        => VerifyKeywordAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    var z =  $$ static () => 2;
                }
            }
            """);

    [Fact]
    public Task TestAfterStaticInLambda()
        => VerifyKeywordAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    var z =  static $$ () => 2;
                }
            }
            """);

    [Fact]
    public Task TestAfterStaticInExpression()
        => VerifyKeywordAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    var z = static $$
                }
            }
            """);

    [Fact]
    public Task TestAfterDuplicateStaticInExpression()
        => VerifyKeywordAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    var z = static static $$
                }
            }
            """);

    [Fact]
    public Task TestAfterStaticAsyncInExpression()
        => VerifyAbsenceAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    var z = static async $$
                }
            }
            """);

    [Fact]
    public Task TestAfterAsyncStaticInExpression()
        => VerifyAbsenceAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    var z = async static $$
                }
            }
            """);

    [Fact]
    public Task TestInAttribute()
        => VerifyAbsenceAsync("""
            class C
            {
                [$$
                void M()
                {
                }
            }
            """);

    [Fact]
    public Task TestInAttributeArgument()
        => VerifyAbsenceAsync("""
            class C
            {
                [Attr($$
                void M()
                {
                }
            }
            """);

    [Fact]
    public Task TestBeforeStaticInExpression()
        => VerifyKeywordAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    var z = $$ static
                }
            }
            """);

    [Fact]
    public Task TestNotIfAlreadyAsyncInLambda()
        => VerifyAbsenceAsync("""
            class Program
            {
                static void Main(string[] args)
                {
                    var z = async $$ () => 2;
                }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/60340")]
    public Task TestNotIfAlreadyAsyncBeforeOtherMember()
        => VerifyAbsenceAsync("""
            class Program
            {
                async $$    

                public void M() {}
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/60340")]
    public Task TestNotIfAlreadyAsyncAsLastMember()
        => VerifyAbsenceAsync("""
            class Program
            {
                async $$
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578061")]
    public Task TestNotInNamespace()
        => VerifyAbsenceAsync("""
            namespace Goo
            {
                $$
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578069")]
    public Task TestNotAfterPartialInNamespace()
        => VerifyAbsenceAsync("""
            namespace Goo
            {
                partial $$
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578750")]
    public Task TestNotAfterPartialInClass()
        => VerifyAbsenceAsync("""
            class Goo
            {
                partial $$
            }
            """);

    [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578750")]
    public Task TestAfterAttribute()
        => VerifyKeywordAsync("""
            class Goo
            {
                [Attr] $$
            }
            """);

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/8616")]
    [CompilerTrait(CompilerFeature.LocalFunctions)]
    public Task TestLocalFunction(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"$$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/14525")]
    [CompilerTrait(CompilerFeature.LocalFunctions)]
    public Task TestLocalFunction2(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"unsafe $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/14525")]
    [CompilerTrait(CompilerFeature.LocalFunctions)]
    public Task TestLocalFunction3(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"unsafe $$ void L() { }", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/8616")]
    [CompilerTrait(CompilerFeature.LocalFunctions)]
    public Task TestLocalFunction4(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"$$ void L() { }", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/8616")]
    [CompilerTrait(CompilerFeature.LocalFunctions)]
    public Task TestLocalFunction5()
        => VerifyKeywordAsync("""
            class Goo
            {
                public void M(Action<int> a)
                {
                    M(async () =>
                    {
                        $$
                    });
                }
            }
            """);

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/8616")]
    [CompilerTrait(CompilerFeature.LocalFunctions)]
    public Task TestLocalFunction6(bool topLevelStatement)
        => VerifyAbsenceAsync(AddInsideMethod(
@"int $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Theory, CombinatorialData]
    [WorkItem("https://github.com/dotnet/roslyn/issues/8616")]
    [CompilerTrait(CompilerFeature.LocalFunctions)]
    public Task TestLocalFunction7(bool topLevelStatement)
        => VerifyKeywordAsync(AddInsideMethod(
@"static $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);

    [Fact]
    public Task TestWithinExtension()
        => VerifyKeywordAsync(
            """
            static class C
            {
                extension(string s)
                {
                    $$
                }
            }
            """, CSharpNextParseOptions);
}
