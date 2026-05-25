// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.UseCollectionInitializer;

/// <summary>
/// A single fold candidate produced by the use-object-initializer / use-collection-initializer
/// analyzers. Carries the syntactic statement that would be folded into the resulting
/// initializer plus a discriminator indicating which kind of element initializer the fixer
/// must synthesize. Designed as the union of data both today's IDE0017 and IDE0028 fold paths
/// produce, so a single match type can flow through the eventually-unified pipeline (this is
/// Pass 1 of the IDE0017+IDE0028 unification — the per-language fixers continue to consume
/// only the subset of match kinds their analyzer produces today; later passes merge them).
/// </summary>
/// <remarks>
/// Field population by kind:
/// <list type="bullet">
/// <item><description><see cref="InitializerMatchKind.MemberInitializer"/>: <see cref="Node"/>
/// is an assignment expression-statement like <c>x.Member = value;</c> or any compound form when
/// the language supports compound assignment in initializers (csharplang-approved feature). The
/// fixer recovers the LHS member-access and the RHS by re-inspecting the assignment.</description></item>
/// <item><description><see cref="InitializerMatchKind.AddInvocation"/>: <see cref="Node"/>
/// is an expression-statement like <c>x.Add(value);</c>. Used by both IDE0028's pure-collection
/// fold and (since PR 5 of the mixed object/collection initializer feature, csharplang#10185)
/// IDE0017's mixed-init fold. The fixer recovers the argument(s) from the invocation.
/// <see cref="UseSpread"/> is set when the call was actually <c>AddRange(...)</c> and the
/// synthesized element should be a spread (<c>..value</c>) in a collection expression.
/// <see cref="UseKeyValue"/> is set when the call has two arguments that map to a dictionary
/// indexer and the collection-expression target should emit a <c>k:v</c> element.</description></item>
/// <item><description><see cref="InitializerMatchKind.IndexAssignment"/>: <see cref="Node"/>
/// is an indexer assignment like <c>x[k] = v;</c>. Synthesized as an implicit-element-access
/// initializer. <see cref="UseKeyValue"/> is set when the collection-expression target supports
/// <c>k:v</c> elements.</description></item>
/// <item><description><see cref="InitializerMatchKind.ForEach"/>: <see cref="Node"/> is a
/// <c>foreach</c> loop whose body is a single <c>x.Add(item)</c> invocation. Collection-expression
/// target only; always emitted as a spread (<see cref="UseSpread"/> implicitly true). Initializer
/// targets never produce this kind because <c>foreach</c> can't be folded into <c>{…}</c>.</description></item>
/// <item><description><see cref="InitializerMatchKind.ConstructorArgument"/>: <see cref="Node"/>
/// is an argument or argument-list of the original object creation; this kind is only emitted as
/// a pre-match by IDE0028's collection-expression branch when the constructor argument can be
/// spread (or dropped) into the resulting <c>[ … ]</c>.</description></item>
/// </list>
/// The <typeparamref name="TNode"/> parameter intentionally allows any <see cref="SyntaxNode"/>
/// rather than constraining to statements: IDE0028's collection-expression pre-matches wrap
/// arguments and argument-lists, neither of which is a statement. Per-kind contracts above
/// pin which subtype each kind actually carries.
/// </remarks>
internal readonly record struct InitializerMatch<TNode>(
    TNode Node,
    InitializerMatchKind Kind,
    bool UseSpread = false,
    bool UseCast = false,
    bool UseKeyValue = false)
    where TNode : SyntaxNode;

/// <summary>
/// Discriminator for <see cref="InitializerMatch{TStatementSyntax}"/>. See the comment on that
/// type for the per-kind data-population contract.
/// </summary>
internal enum InitializerMatchKind
{
    /// <summary><c>x.Member = value;</c> or any compound form (when language-supported).</summary>
    MemberInitializer,

    /// <summary><c>x.Add(value);</c> or <c>x.AddRange(values);</c> — see <see cref="InitializerMatch{TStatementSyntax}.UseSpread"/>.</summary>
    AddInvocation,

    /// <summary><c>x[k] = v;</c></summary>
    IndexAssignment,

    /// <summary><c>foreach (var item in xs) x.Add(item);</c> — collection-expression target only.</summary>
    ForEach,

    /// <summary>
    /// Argument or argument-list of the original <c>new C(...)</c> creation; only ever emitted
    /// as a pre-match by IDE0028's collection-expression branch when the constructor argument
    /// can be lifted into the resulting collection expression (as a spread, a <c>with(args)</c>
    /// argument, or dropped entirely for a capacity argument). Initializer targets never use
    /// this kind.
    /// </summary>
    ConstructorArgument,
}
