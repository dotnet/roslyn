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
    public class AsyncKeywordRecommenderTests : KeywordRecommenderTests
    {
        [Fact]
        public async Task TestMethodDeclaration1()
        {
            await VerifyKeywordAsync("""
                class C
                {
                    $$
                }
                """);
        }

        [Fact]
        public async Task TestMethodDeclaration2()
        {
            await VerifyKeywordAsync("""
                class C
                {
                    public $$
                }
                """);
        }

        [Fact]
        public async Task TestMethodDeclaration3()
        {
            await VerifyKeywordAsync("""
                class C
                {
                    $$ public void goo() { }
                }
                """);
        }

        [Fact]
        public async Task TestMethodDeclarationAsyncAfterCursor()
        {
            await VerifyKeywordAsync("""
                class C
                {
                    public $$ async void goo() { }
                }
                """);
        }

        [Fact]
        public async Task TestInsideInterface()
        {
            await VerifyKeywordAsync("""
                interface C
                {
                    $$
                }
                """);
        }

        [Fact]
        public async Task TestMethodDeclarationInGlobalStatement1()
        {
            const string text = @"$$";
            await VerifyKeywordAsync(SourceCodeKind.Script, text);
        }

        [Fact]
        public async Task TestMethodDeclarationInGlobalStatement2()
        {
            const string text = @"public $$";
            await VerifyKeywordAsync(SourceCodeKind.Script, text);
        }

        [Fact]
        public async Task TestExpressionContext()
        {
            await VerifyKeywordAsync("""
                class C
                {
                    void goo()
                    {
                        goo($$
                    }
                }
                """);
        }

        [Fact]
        public async Task TestNotInParameter()
        {
            await VerifyAbsenceAsync("""
                class C
                {
                    void goo($$)
                    {
                    }
                }
                """);
        }

        [Fact]
        public async Task TestBeforeLambda()
        {
            await VerifyKeywordAsync("""
                class Program
                {
                    static void Main(string[] args)
                    {
                        var z =  $$ () => 2;
                    }
                }
                """);
        }

        [Fact]
        public async Task TestBeforeStaticLambda()
        {
            await VerifyKeywordAsync("""
                class Program
                {
                    static void Main(string[] args)
                    {
                        var z =  $$ static () => 2;
                    }
                }
                """);
        }

        [Fact]
        public async Task TestAfterStaticInLambda()
        {
            await VerifyKeywordAsync("""
                class Program
                {
                    static void Main(string[] args)
                    {
                        var z =  static $$ () => 2;
                    }
                }
                """);
        }

        [Fact]
        public async Task TestAfterStaticInExpression()
        {
            await VerifyKeywordAsync("""
                class Program
                {
                    static void Main(string[] args)
                    {
                        var z = static $$
                    }
                }
                """);
        }

        [Fact]
        public async Task TestAfterDuplicateStaticInExpression()
        {
            await VerifyKeywordAsync("""
                class Program
                {
                    static void Main(string[] args)
                    {
                        var z = static static $$
                    }
                }
                """);
        }

        [Fact]
        public async Task TestAfterStaticAsyncInExpression()
        {
            await VerifyAbsenceAsync("""
                class Program
                {
                    static void Main(string[] args)
                    {
                        var z = static async $$
                    }
                }
                """);
        }

        [Fact]
        public async Task TestAfterAsyncStaticInExpression()
        {
            await VerifyAbsenceAsync("""
                class Program
                {
                    static void Main(string[] args)
                    {
                        var z = async static $$
                    }
                }
                """);
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
        public async Task TestBeforeStaticInExpression()
        {
            await VerifyKeywordAsync("""
                class Program
                {
                    static void Main(string[] args)
                    {
                        var z = $$ static
                    }
                }
                """);
        }

        [Fact]
        public async Task TestNotIfAlreadyAsyncInLambda()
        {
            await VerifyAbsenceAsync("""
                class Program
                {
                    static void Main(string[] args)
                    {
                        var z = async $$ () => 2;
                    }
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/60340")]
        public async Task TestNotIfAlreadyAsyncBeforeOtherMember()
        {
            await VerifyAbsenceAsync("""
                class Program
                {
                    async $$    

                    public void M() {}
                }
                """);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/60340")]
        public async Task TestNotIfAlreadyAsyncAsLastMember()
        {
            await VerifyAbsenceAsync("""
                class Program
                {
                    async $$
                }
                """);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578061")]
        public async Task TestNotInNamespace()
        {
            await VerifyAbsenceAsync("""
                namespace Goo
                {
                    $$
                }
                """);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578069")]
        public async Task TestNotAfterPartialInNamespace()
        {
            await VerifyAbsenceAsync("""
                namespace Goo
                {
                    partial $$
                }
                """);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578750")]
        public async Task TestNotAfterPartialInClass()
        {
            await VerifyAbsenceAsync("""
                class Goo
                {
                    partial $$
                }
                """);
        }

        [Fact, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/578750")]
        public async Task TestAfterAttribute()
        {
            await VerifyKeywordAsync("""
                class Goo
                {
                    [Attr] $$
                }
                """);
        }

        [Theory]
        [CombinatorialData]
        [WorkItem("https://github.com/dotnet/roslyn/issues/8616")]
        [CompilerTrait(CompilerFeature.LocalFunctions)]
        public async Task TestLocalFunction(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"$$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory]
        [CombinatorialData]
        [WorkItem("https://github.com/dotnet/roslyn/issues/14525")]
        [CompilerTrait(CompilerFeature.LocalFunctions)]
        public async Task TestLocalFunction2(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"unsafe $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory]
        [CombinatorialData]
        [WorkItem("https://github.com/dotnet/roslyn/issues/14525")]
        [CompilerTrait(CompilerFeature.LocalFunctions)]
        public async Task TestLocalFunction3(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"unsafe $$ void L() { }", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory]
        [CombinatorialData]
        [WorkItem("https://github.com/dotnet/roslyn/issues/8616")]
        [CompilerTrait(CompilerFeature.LocalFunctions)]
        public async Task TestLocalFunction4(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"$$ void L() { }", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/8616")]
        [CompilerTrait(CompilerFeature.LocalFunctions)]
        public async Task TestLocalFunction5()
        {
            await VerifyKeywordAsync("""
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
        }

        [Theory]
        [CombinatorialData]
        [WorkItem("https://github.com/dotnet/roslyn/issues/8616")]
        [CompilerTrait(CompilerFeature.LocalFunctions)]
        public async Task TestLocalFunction6(bool topLevelStatement)
        {
            await VerifyAbsenceAsync(AddInsideMethod(
@"int $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }

        [Theory]
        [CombinatorialData]
        [WorkItem("https://github.com/dotnet/roslyn/issues/8616")]
        [CompilerTrait(CompilerFeature.LocalFunctions)]
        public async Task TestLocalFunction7(bool topLevelStatement)
        {
            await VerifyKeywordAsync(AddInsideMethod(
@"static $$", topLevelStatement: topLevelStatement), options: CSharp9ParseOptions);
        }
    }
}
