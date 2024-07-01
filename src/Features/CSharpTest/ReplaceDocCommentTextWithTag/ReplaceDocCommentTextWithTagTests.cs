// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.ReplaceDocCommentTextWithTag;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.ReplaceDocCommentTextWithTag;

[Trait(Traits.Feature, Traits.Features.CodeActionsReplaceDocCommentTextWithTag)]
public class ReplaceDocCommentTextWithTagTests : AbstractCSharpCodeActionTest_NoEditor
{
    protected override CodeRefactoringProvider CreateCodeRefactoringProvider(TestWorkspace workspace, TestParameters parameters)
        => new CSharpReplaceDocCommentTextWithTagCodeRefactoringProvider();

    [Fact]
    public async Task TestStartOfKeyword()
    {
        await TestInRegularAndScriptAsync(
            """
            /// Testing keyword [||]null.
            class C<TKey>
            {
            }
            """,

            """
            /// Testing keyword <see langword="null"/>.
            class C<TKey>
            {
            }
            """);
    }

    [Fact]
    public async Task TestEndOfKeyword()
    {
        await TestInRegularAndScriptAsync(
            """
            /// Testing keyword abstract[||].
            class C<TKey>
            {
            }
            """,

            """
            /// Testing keyword <see langword="abstract"/>.
            class C<TKey>
            {
            }
            """);
    }

    [Fact]
    public async Task TestEndOfKeyword_NewLineFollowing()
    {
        await TestInRegularAndScriptAsync(
            """
            /// Testing keyword static[||]
            class C<TKey>
            {
            }
            """,

            """
            /// Testing keyword <see langword="static"/>
            class C<TKey>
            {
            }
            """);
    }

    [Fact]
    public async Task TestSelectedKeyword()
    {
        await TestInRegularAndScriptAsync(
            """
            /// Testing keyword [|abstract|].
            class C<TKey>
            {
            }
            """,

            """
            /// Testing keyword <see langword="abstract"/>.
            class C<TKey>
            {
            }
            """);
    }

    [Fact]
    public async Task TestInsideKeyword()
    {
        await TestInRegularAndScriptAsync(
            """
            /// Testing keyword asy[||]nc.
            class C<TKey>
            {
            }
            """,

            """
            /// Testing keyword <see langword="async"/>.
            class C<TKey>
            {
            }
            """);
    }

    [Fact]
    public async Task TestNotInsideKeywordIfNonEmptySpan()
    {
        await TestMissingAsync(
            """
            /// TKey must implement the System.IDisposable int[|erf|]ace
            class C<TKey>
            {
            }
            """);
    }

    [Fact]
    public async Task TestStartOfFullyQualifiedTypeName_Start()
    {
        await TestInRegularAndScriptAsync(
            """
            /// TKey must implement the [||]System.IDisposable interface.
            class C<TKey>
            {
            }
            """,

            """
            /// TKey must implement the <see cref="System.IDisposable"/> interface.
            class C<TKey>
            {
            }
            """);
    }

    [Fact]
    public async Task TestStartOfFullyQualifiedTypeName_Mid1()
    {
        await TestInRegularAndScriptAsync(
            """
            /// TKey must implement the System[||].IDisposable interface.
            class C<TKey>
            {
            }
            """,

            """
            /// TKey must implement the <see cref="System.IDisposable"/> interface.
            class C<TKey>
            {
            }
            """);
    }

    [Fact]
    public async Task TestStartOfFullyQualifiedTypeName_Mid2()
    {
        await TestInRegularAndScriptAsync(
            """
            /// TKey must implement the System.[||]IDisposable interface.
            class C<TKey>
            {
            }
            """,

            """
            /// TKey must implement the <see cref="System.IDisposable"/> interface.
            class C<TKey>
            {
            }
            """);
    }

    [Fact]
    public async Task TestStartOfFullyQualifiedTypeName_End()
    {
        await TestInRegularAndScriptAsync(
            """
            /// TKey must implement the System.IDisposable[||] interface.
            class C<TKey>
            {
            }
            """,

            """
            /// TKey must implement the <see cref="System.IDisposable"/> interface.
            class C<TKey>
            {
            }
            """);
    }

    [Fact]
    public async Task TestStartOfFullyQualifiedTypeName_Selected()
    {
        await TestInRegularAndScriptAsync(
            """
            /// TKey must implement the [|System.IDisposable|] interface.
            class C<TKey>
            {
            }
            """,

            """
            /// TKey must implement the <see cref="System.IDisposable"/> interface.
            class C<TKey>
            {
            }
            """);
    }

    [Fact]
    public async Task TestTypeParameterReference()
    {
        await TestInRegularAndScriptAsync(
            """
            /// [||]TKey must implement the System.IDisposable interface.
            class C<TKey>
            {
            }
            """,

            """
            /// <typeparamref name="TKey"/> must implement the System.IDisposable interface.
            class C<TKey>
            {
            }
            """);
    }

    [Fact]
    public async Task TestTypeParameterReference_EmptyClassBody()
    {
        await TestInRegularAndScriptAsync(
            """
            /// [||]TKey must implement the System.IDisposable interface.
            class C<TKey>{}
            """,

            """
            /// <typeparamref name="TKey"/> must implement the System.IDisposable interface.
            class C<TKey>{}
            """);
    }

    [Fact]
    public async Task TestCanSeeInnerMethod()
    {
        await TestInRegularAndScriptAsync(
            """
            /// Use WriteLine[||] as a Console.WriteLine replacement
            class C
            {
                void WriteLine<TKey>(TKey value) { }
            }
            """,

            """
            /// Use <see cref="WriteLine"/> as a Console.WriteLine replacement
            class C
            {
                void WriteLine<TKey>(TKey value) { }
            }
            """);
    }

    [Fact]
    public async Task TestNotOnMispelledName()
    {
        await TestMissingAsync(
            """
            /// Use WriteLine1[||] as a Console.WriteLine replacement
            class C
            {
                void WriteLine<TKey>(TKey value) { }
            }
            """);
    }

    [Fact]
    public async Task TestMethodTypeParameterSymbol()
    {
        await TestInRegularAndScriptAsync(
            """
            class C
            {
                /// value has type TKey[||] so we don't box primitives.
                void WriteLine<TKey>(TKey value) { }
            }
            """,

            """
            class C
            {
                /// value has type <typeparamref name="TKey"/> so we don't box primitives.
                void WriteLine<TKey>(TKey value) { }
            }
            """);
    }

    [Fact]
    public async Task TestMethodTypeParameterSymbol_EmptyBody()
    {
        await TestInRegularAndScriptAsync(
            """
            class C
            {
                /// value has type TKey[||] so we don't box primitives.
                void WriteLine<TKey>(TKey value){}
            }
            """,

            """
            class C
            {
                /// value has type <typeparamref name="TKey"/> so we don't box primitives.
                void WriteLine<TKey>(TKey value){}
            }
            """);
    }

    [Fact]
    public async Task TestMethodTypeParameterSymbol_ExpressionBody()
    {
        await TestInRegularAndScriptAsync(
            """
            class C
            {
                /// value has type TKey[||] so we don't box primitives.
                object WriteLine<TKey>(TKey value) => null;
            }
            """,

            """
            class C
            {
                /// value has type <typeparamref name="TKey"/> so we don't box primitives.
                object WriteLine<TKey>(TKey value) => null;
            }
            """);
    }

    [Fact]
    public async Task TestMethodTypeParameter_SemicolonBody()
    {
        await TestInRegularAndScriptAsync(
            """
            class C
            {
                /// value has type TKey[||] so we don't box primitives.
                void WriteLine<TKey>(TKey value);
            }
            """,

            """
            class C
            {
                /// value has type <typeparamref name="TKey"/> so we don't box primitives.
                void WriteLine<TKey>(TKey value);
            }
            """);
    }

    [Fact]
    public async Task TestMethodParameterSymbol()
    {
        await TestInRegularAndScriptAsync(
            """
            class C
            {
                /// value[||] has type TKey so we don't box primitives.
                void WriteLine<TKey>(TKey value) { }
            }
            """,

            """
            class C
            {
                /// <see langword="value"/> has type TKey so we don't box primitives.
                void WriteLine<TKey>(TKey value) { }
            }
            """);
    }

    [Fact]
    public async Task TestMethodParameterSymbol_EmptyBody()
    {
        await TestInRegularAndScriptAsync(
            """
            class C
            {
                /// value[||] has type TKey so we don't box primitives.
                void WriteLine<TKey>(TKey value){}
            }
            """,

            """
            class C
            {
                /// <see langword="value"/> has type TKey so we don't box primitives.
                void WriteLine<TKey>(TKey value){}
            }
            """);
    }

    [Fact]
    public async Task TestMethodParameterSymbol_ExpressionBody()
    {
        await TestInRegularAndScriptAsync(
            """
            class C
            {
                /// value[||] has type TKey so we don't box primitives.
                object WriteLine<TKey>(TKey value) => null;
            }
            """,

            """
            class C
            {
                /// <see langword="value"/> has type TKey so we don't box primitives.
                object WriteLine<TKey>(TKey value) => null;
            }
            """);
    }

    [Fact]
    public async Task TestMethodParameterSymbol_SemicolonBody()
    {
        await TestInRegularAndScriptAsync(
            """
            class C
            {
                /// value[||] has type TKey so we don't box primitives.
                void WriteLine<TKey>(TKey value);
            }
            """,

            """
            class C
            {
                /// <see langword="value"/> has type TKey so we don't box primitives.
                void WriteLine<TKey>(TKey value);
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/22278")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/31208")]
    public async Task TestApplicableKeyword()
    {
        await TestInRegularAndScript1Async(
            """
            /// Testing keyword interfa[||]ce.
            class C<TKey>
            {
            }
            """,
            """
            /// Testing keyword <see langword="interface"/>.
            class C<TKey>
            {
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/22278")]
    public async Task TestInXMLAttribute()
    {
        await TestMissingAsync(
            """
            /// Testing keyword inside <see langword ="nu[||]ll"/>
            class C
            {
                void WriteLine<TKey>(TKey value) { }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/22278")]
    public async Task TestInXMLAttribute2()
    {
        await TestMissingAsync(
            """
            /// Testing keyword inside <see langword ="nu[||]ll"
            class C
            {
                void WriteLine<TKey>(TKey value) { }
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38370")]
    public async Task TestBaseKeyword()
    {
        await TestInRegularAndScriptAsync(
            """
            /// Testing keyword [||]base.
            class C<TKey>
            {
            }
            """,

            """
            /// Testing keyword <see langword="base"/>.
            class C<TKey>
            {
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38370")]
    public async Task TestThisKeyword()
    {
        await TestInRegularAndScriptAsync(
            """
            /// Testing keyword [||]this.
            class C<TKey>
            {
            }
            """,

            """
            /// Testing keyword <see langword="this"/>.
            class C<TKey>
            {
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/31208")]
    public async Task TestArbitraryKeyword()
    {
        await TestInRegularAndScriptAsync(
            """
            /// Testing keyword [||]delegate.
            class C<TKey>
            {
            }
            """,

            """
            /// Testing keyword <see langword="delegate"/>.
            class C<TKey>
            {
            }
            """);
    }

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/31208")]
    public async Task TestContextualKeyword()
    {
        await TestInRegularAndScriptAsync(
            """
            /// Testing keyword [||]yield.
            class C<TKey>
            {
            }
            """,

            """
            /// Testing keyword <see langword="yield"/>.
            class C<TKey>
            {
            }
            """);
    }
}
