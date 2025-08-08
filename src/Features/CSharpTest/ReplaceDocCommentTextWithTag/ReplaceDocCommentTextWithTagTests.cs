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
public sealed class ReplaceDocCommentTextWithTagTests : AbstractCSharpCodeActionTest_NoEditor
{
    protected override CodeRefactoringProvider CreateCodeRefactoringProvider(TestWorkspace workspace, TestParameters parameters)
        => new CSharpReplaceDocCommentTextWithTagCodeRefactoringProvider();

    [Fact]
    public Task TestStartOfKeyword()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestEndOfKeyword()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestEndOfKeyword_NewLineFollowing()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/76548")]
    public Task TestEndOfKeyword_XmlCloseTagFollowing()
        => TestInRegularAndScriptAsync(
            """
            /// <summary>Testing keyword null[||]</summary>
            class C<TKey>
            {
            }
            """,

            """
            /// <summary>Testing keyword <see langword="null"/></summary>
            class C<TKey>
            {
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/76548")]
    public Task TestEndOfKeyword_XmlOpenTagPreceding()
        => TestInRegularAndScriptAsync(
            """
            /// <summary>[||]null is an option.</summary>
            class C<TKey>
            {
            }
            """,

            """
            /// <summary><see langword="null"/> is an option.</summary>
            class C<TKey>
            {
            }
            """);

    [Fact]
    public Task TestSelectedKeyword()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestInsideKeyword()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestNotInsideKeywordIfNonEmptySpan()
        => TestMissingAsync(
            """
            /// TKey must implement the System.IDisposable int[|erf|]ace
            class C<TKey>
            {
            }
            """);

    [Fact]
    public Task TestStartOfFullyQualifiedTypeName_Start()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestStartOfFullyQualifiedTypeName_Mid1()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestStartOfFullyQualifiedTypeName_Mid2()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestStartOfFullyQualifiedTypeName_End()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestStartOfFullyQualifiedTypeName_Selected()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestTypeParameterReference()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestTypeParameterReference_EmptyClassBody()
        => TestInRegularAndScriptAsync(
            """
            /// [||]TKey must implement the System.IDisposable interface.
            class C<TKey>{}
            """,

            """
            /// <typeparamref name="TKey"/> must implement the System.IDisposable interface.
            class C<TKey>{}
            """);

    [Fact]
    public Task TestCanSeeInnerMethod()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestNotOnMispelledName()
        => TestMissingAsync(
            """
            /// Use WriteLine1[||] as a Console.WriteLine replacement
            class C
            {
                void WriteLine<TKey>(TKey value) { }
            }
            """);

    [Fact]
    public Task TestMethodTypeParameterSymbol()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestMethodTypeParameterSymbol_EmptyBody()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestMethodTypeParameterSymbol_ExpressionBody()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestMethodTypeParameter_SemicolonBody()
        => TestInRegularAndScriptAsync(
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

    [Fact]
    public Task TestMethodParameterSymbol()
        => TestInRegularAndScriptAsync(
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
                /// <paramref name="value"/> has type TKey so we don't box primitives.
                void WriteLine<TKey>(TKey value) { }
            }
            """);

    [Fact]
    public Task TestMethodParameterSymbol_EmptyBody()
        => TestInRegularAndScriptAsync(
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
                /// <paramref name="value"/> has type TKey so we don't box primitives.
                void WriteLine<TKey>(TKey value){}
            }
            """);

    [Fact]
    public Task TestMethodParameterSymbol_ExpressionBody()
        => TestInRegularAndScriptAsync(
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
                /// <paramref name="value"/> has type TKey so we don't box primitives.
                object WriteLine<TKey>(TKey value) => null;
            }
            """);

    [Fact]
    public Task TestMethodParameterSymbol_SemicolonBody()
        => TestInRegularAndScriptAsync(
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
                /// <paramref name="value"/> has type TKey so we don't box primitives.
                void WriteLine<TKey>(TKey value);
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/22278")]
    [WorkItem("https://github.com/dotnet/roslyn/issues/31208")]
    public Task TestApplicableKeyword()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/22278")]
    public Task TestInXMLAttribute()
        => TestMissingAsync(
            """
            /// Testing keyword inside <see langword ="nu[||]ll"/>
            class C
            {
                void WriteLine<TKey>(TKey value) { }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/22278")]
    public Task TestInXMLAttribute2()
        => TestMissingAsync(
            """
            /// Testing keyword inside <see langword ="nu[||]ll"
            class C
            {
                void WriteLine<TKey>(TKey value) { }
            }
            """);

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38370")]
    public Task TestBaseKeyword()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/38370")]
    public Task TestThisKeyword()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/31208")]
    public Task TestArbitraryKeyword()
        => TestInRegularAndScriptAsync(
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/31208")]
    public Task TestContextualKeyword()
        => TestInRegularAndScriptAsync(
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
