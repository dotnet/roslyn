// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests;

/// <summary>
/// Permutation harness for the contextual-keyword modifiers recognized by
/// <see cref="Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax.LanguageParser"/>
/// (<c>partial</c>, <c>async</c>, <c>required</c>, <c>file</c>, ...).
/// <para>
/// The goal of this file is to lock in the property that each contextual-keyword modifier can
/// be parsed in any position of a declaration's modifier list. For modifiers whose ordering is
/// semantically unrestricted, the tests also verify that ordering does not affect binding.
/// </para>
/// <para>
/// The set of contextual modifier kinds is discovered dynamically from
/// <c>LanguageParser.GetModifierExcludingScoped</c>.  Each known kind is registered in
/// <see cref="s_shapes"/> with a declaration template plus a handful of non-contextual companion
/// modifiers that are valid alongside it.  The <see cref="AllContextualModifierKindsCovered"/>
/// canary fires if a new contextual modifier is introduced without a corresponding shape entry.
/// Once the shape is registered the permutation theories below automatically exercise every
/// ordering of the modifier list for the new keyword.
/// </para>
/// </summary>
public sealed class ContextualModifierOrderingTests : CSharpTestBase
{
    private sealed record Shape(
        string Usings,
        string WrapperOpen,
        string WrapperClose,
        string Declaration,
        ImmutableArray<string> CompanionModifiers,
        LanguageVersion LangVersion);

    // For each known contextual-keyword modifier, a declaration template where the modifier is
    // semantically valid, plus the set of non-contextual companion modifiers to interleave with
    // it.  The harness generates every permutation of [contextual keyword + companions] and
    // asserts the resulting declarations parse the contextual keyword as a modifier. Companion
    // modifiers are intentionally drawn from the reserved-keyword set so the tests isolate the
    // contextual keyword's own ordering behavior.
    private static readonly ImmutableDictionary<SyntaxKind, Shape> s_shapes =
        ImmutableDictionary.CreateRange(new KeyValuePair<SyntaxKind, Shape>[]
        {
            KeyValuePair.Create(SyntaxKind.PartialKeyword, new Shape(
                Usings: "",
                WrapperOpen: "",
                WrapperClose: "",
                Declaration: "class C { }",
                CompanionModifiers: ImmutableArray.Create("public", "unsafe"),
                LangVersion: LanguageVersion.Preview)),

            KeyValuePair.Create(SyntaxKind.AsyncKeyword, new Shape(
                Usings: "using System.Threading.Tasks;",
                WrapperOpen: "class Outer {",
                WrapperClose: "}",
                Declaration: "Task M() => Task.CompletedTask;",
                CompanionModifiers: ImmutableArray.Create("public", "static"),
                LangVersion: LanguageVersion.CSharp7)),

            KeyValuePair.Create(SyntaxKind.RequiredKeyword, new Shape(
                Usings: "",
                WrapperOpen: "class Outer {",
                WrapperClose: "}",
                Declaration: "int P { get; set; }",
                CompanionModifiers: ImmutableArray.Create("public", "unsafe"),
                LangVersion: LanguageVersion.CSharp11)),

            KeyValuePair.Create(SyntaxKind.FileKeyword, new Shape(
                Usings: "",
                WrapperOpen: "",
                WrapperClose: "",
                Declaration: "class C { }",
                CompanionModifiers: ImmutableArray.Create("static", "unsafe"),
                LangVersion: LanguageVersion.CSharp11)),

            KeyValuePair.Create(SyntaxKind.ClosedKeyword, new Shape(
                Usings: "",
                WrapperOpen: "",
                WrapperClose: "",
                Declaration: "class C { }",
                CompanionModifiers: ImmutableArray.Create("public", "unsafe"),
                LangVersion: LanguageVersion.Preview)),
        });

    /// <summary>
    /// Discovers every <see cref="SyntaxKind"/> that the parser's modifier dispatch recognizes
    /// through the contextual-keyword path.  An identifier token whose <c>ContextualKind</c>
    /// is one of these values may be promoted to a modifier by the parser.
    /// </summary>
    private static ImmutableArray<SyntaxKind> DiscoverContextualModifierKinds()
    {
        var builder = ImmutableArray.CreateBuilder<SyntaxKind>();
        foreach (SyntaxKind kind in Enum.GetValues<SyntaxKind>())
        {
            if (!SyntaxFacts.IsContextualKeyword(kind))
                continue;

            var asContextual = Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax.LanguageParser
                .GetModifierExcludingScoped(SyntaxKind.IdentifierToken, contextualKind: kind);

            if (asContextual != DeclarationModifiers.None)
                builder.Add(kind);
        }
        return builder.ToImmutable();
    }

    [Fact]
    public void AllContextualModifierKindsCovered()
    {
        var discovered = DiscoverContextualModifierKinds()
            .Select(k => k.ToString())
            .OrderBy(s => s)
            .ToArray();

        var covered = s_shapes.Keys
            .Select(k => k.ToString())
            .OrderBy(s => s)
            .ToArray();

        AssertEx.Equal(
            discovered,
            covered,
            message: "A new contextual-keyword modifier was added to LanguageParser.GetModifierExcludingScoped. " +
                     "Register a shape for it in ContextualModifierOrderingTests.s_shapes (or remove the obsolete entry) " +
                     "so its ordering permutations are exercised.");
    }

    public static TheoryData<SyntaxKind> ContextualModifierKinds()
    {
        var data = new TheoryData<SyntaxKind>();
        foreach (var kind in s_shapes.Keys.OrderBy(k => k.ToString()))
            data.Add(kind);
        return data;
    }

    /// <summary>
    /// For each registered contextual-keyword modifier, parses every permutation of the modifier
    /// list and verifies that the contextual keyword is promoted from an identifier to a modifier
    /// keyword without parser diagnostics.
    /// </summary>
    [Theory, MemberData(nameof(ContextualModifierKinds))]
    public void AllPermutationsParseAsModifiers(SyntaxKind contextualKind)
    {
        var shape = s_shapes[contextualKind];
        var contextualText = SyntaxFacts.GetText(contextualKind);
        var tokens = shape.CompanionModifiers.Append(contextualText).ToImmutableArray();

        foreach (var permutation in Permutations(tokens))
        {
            var modifierList = string.Join(" ", permutation);
            var source = $$"""
                {{shape.Usings}}
                {{shape.WrapperOpen}}
                    {{modifierList}} {{shape.Declaration}}
                {{shape.WrapperClose}}
                """;

            var comp = CreateCompilation(
                source,
                parseOptions: TestOptions.Regular.WithLanguageVersion(shape.LangVersion),
                options: TestOptions.UnsafeReleaseDll);

            comp.GetParseDiagnostics().Verify();
            Assert.Single(comp.SyntaxTrees.Single().GetRoot().DescendantTokens(), token => token.Kind() == contextualKind);
        }
    }

    public static TheoryData<SyntaxKind> OrderIndependentContextualModifierKinds()
    {
        var data = new TheoryData<SyntaxKind>();
        foreach (var kind in s_shapes.Keys.Where(k => k != SyntaxKind.PartialKeyword).OrderBy(k => k.ToString()))
            data.Add(kind);
        return data;
    }

    /// <summary>
    /// For contextual modifiers whose ordering is semantically unrestricted, compiles every
    /// permutation and asserts that the diagnostic IDs are identical.
    /// </summary>
    [Theory, MemberData(nameof(OrderIndependentContextualModifierKinds))]
    public void AllPermutationsBindIdentically(SyntaxKind contextualKind)
    {
        var shape = s_shapes[contextualKind];
        var contextualText = SyntaxFacts.GetText(contextualKind);
        var tokens = shape.CompanionModifiers.Append(contextualText).ToImmutableArray();
        string? baselineIds = null;
        string? baselineSource = null;

        foreach (var permutation in Permutations(tokens))
        {
            var modifierList = string.Join(" ", permutation);
            var source = $$"""
                {{shape.Usings}}
                {{shape.WrapperOpen}}
                    {{modifierList}} {{shape.Declaration}}
                {{shape.WrapperClose}}
                """;
            var comp = CreateCompilation(
                source,
                parseOptions: TestOptions.Regular.WithLanguageVersion(shape.LangVersion),
                options: TestOptions.UnsafeReleaseDll);
            var ids = string.Join(",", comp.GetDiagnostics()
                .Select(d => $"{d.Id}:{d.Severity}")
                .OrderBy(s => s));

            if (baselineIds is null)
            {
                baselineIds = ids;
                baselineSource = source;
            }
            else if (ids != baselineIds)
            {
                Assert.Fail(
                    $"Permutation of modifier '{contextualText}' produced different diagnostics." +
                    $"\n\nBaseline source:\n{baselineSource}\nBaseline diagnostics:\n  {baselineIds}" +
                    $"\n\nDivergent source:\n{source}\nDivergent diagnostics:\n  {ids}");
            }
        }
    }

    private static IEnumerable<ImmutableArray<T>> Permutations<T>(ImmutableArray<T> items)
    {
        if (items.IsEmpty)
        {
            yield return ImmutableArray<T>.Empty;
            yield break;
        }

        var indices = Enumerable.Range(0, items.Length).ToArray();

        do
        {
            var builder = ImmutableArray.CreateBuilder<T>(items.Length);
            foreach (var i in indices)
                builder.Add(items[i]);
            yield return builder.ToImmutable();
        }
        while (NextPermutationLexicographic(indices));
    }

    // Generates permutations in lexicographic order via the classic in-place algorithm.
    private static bool NextPermutationLexicographic(int[] a)
    {
        int i = a.Length - 2;
        while (i >= 0 && a[i] >= a[i + 1])
            i--;

        if (i < 0)
            return false;

        int j = a.Length - 1;
        while (a[j] <= a[i])
            j--;

        (a[i], a[j]) = (a[j], a[i]);
        Array.Reverse(a, i + 1, a.Length - 1 - i);
        return true;
    }
}
