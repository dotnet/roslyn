// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.AspNetCore.Razor.Language;

/// <summary>
/// Classifies the user-authored class-level children of a Razor component (the contents
/// of @code blocks and similar) into per-member decl/impl routing decisions, so that
/// markup-bearing user methods are routed to the impl half where tag-helper resolution
/// has happened and surface members stay in the decl half visible to cross-page
/// tag-helper discovery.
/// </summary>
/// <remarks>
/// <para>
/// The @code contents arrive at this point as a flat sequence of <see
/// cref="CSharpCodeIntermediateNode"/> and markup IR nodes interleaved as siblings of the
/// generated render method. The Razor parser doesn't try to parse user @code semantically
/// -- it just hands the text through as raw chunks. To make per-member routing decisions
/// we (a) concatenate the chunks into a single C# class-body text with placeholder
/// identifiers standing in for the markup nodes, (b) feed the result through Roslyn's
/// C# parser, (c) classify each <see cref="MemberDeclarationSyntax"/> as surface or impl
/// based on its attributes and shape, and (d) map each IR chunk back to the member whose
/// byte range covers it. When a CSharpCode chunk's text straddles multiple members with
/// different routing decisions (e.g. fields immediately followed by a markup-bearing
/// surface property in the same parser-emitted text block), the chunk is split textually
/// at the member boundaries so each slice routes independently.
/// </para>
/// <para>
/// The chunker classifies every user @code chunk into one of three buckets. Non-markup
/// members stay in decl. Non-surface markup-bearing members (e.g. private markup helper
/// methods) route to impl via <see cref="ChunkTarget.ImplOnly"/>. Surface properties
/// whose body contains markup (e.g. <c>[Parameter] public RenderFragment Foo => @&lt;p/&gt;;</c>)
/// are rewritten via the helper-delegation pattern: the decl half emits a stubbed
/// property that delegates to a synthesized partial method, and the impl half emits the
/// partial method definition wrapping the original markup body. This keeps the surface
/// declaration visible to cross-page tag-helper discovery via the decl document while
/// still running resolved markup at runtime via impl. The pattern recognizes
/// <c>RenderFragment</c>, <c>RenderFragment&lt;T&gt;</c>, <c>Func&lt;T, RenderFragment&gt;</c>,
/// and aliased forms, and handles markup in a getter-like body (expression body,
/// initializer) via a parameterless synth as well as markup in a <c>set</c>/<c>init</c>
/// accessor via a <c>partial void</c> synth taking the incoming <c>value</c>. A property
/// carrying markup in more than one accessor (e.g. a markup getter <em>and</em> a markup
/// setter) mints one synth per markup-bearing accessor and rewrites the property so each
/// accessor delegates to its own synth.
/// </para>
/// <para>
/// The transform only applies to surface <em>properties</em> whose markup fits a
/// RenderFragment shape. Every other surface member with markup is left untransformed and
/// routed to <see cref="ChunkTarget.DeclOnly"/>: it stays in decl exactly as the user wrote
/// it. This isn't a graceful degradation of valid code; the only programs that reach this
/// path are already invalid. Markup (a <c>RenderTreeBuilder</c> lambda) can only bind to a
/// RenderFragment-shaped delegate, so a non-RenderFragment-typed markup property is a C#
/// error (CS8917); and the surface attributes (<c>[Parameter]</c> etc.) all target
/// properties, so a surface-attributed <em>field</em> is a Blazor error (CS0592) whatever
/// its markup. Emitting such a member verbatim lets the C# compiler report the exact same
/// diagnostic it would for the unsplit document -- the split neither fixes nor worsens
/// invalid input, so there's no reason to try to transform it.
/// </para>
/// </remarks>
internal static class ClassBodySplitter
{
    // The decl and impl C# lowering phases each split the SAME primary class, with inputs
    // (render method, usings) derived from the same document, so the resulting plan is
    // identical. The plan is also resolution-independent. Memoize it on the class node's
    // identity so the second phase reuses the first's work instead of re-parsing -- keyed
    // weakly so it's released when the document's IR is collected. Split() itself stays a
    // pure, uncached function (direct callers, e.g. unit tests, are unaffected); only the
    // phases go through GetOrCreateSplitPlan.
    private static readonly ConditionalWeakTable<ClassDeclarationIntermediateNode, ClassBodySplitPlan> s_planCache = new();

    /// <summary>
    /// Memoized form of <see cref="Split"/> keyed on <paramref name="primaryClass"/>. Both
    /// lowering phases split the same class with the same derived inputs, so this lets the
    /// second phase reuse the first's plan rather than re-parsing the @code.
    /// </summary>
    public static ClassBodySplitPlan GetOrCreateSplitPlan(
        ClassDeclarationIntermediateNode primaryClass,
        MethodDeclarationIntermediateNode renderMethod,
        IReadOnlyList<UsingDirectiveIntermediateNode>? usingDirectives = null)
    {
        // GetValue atomically returns the cached plan or invokes the callback once on a miss.
        // (AddOrUpdate isn't available on netstandard2.0.)
        return s_planCache.GetValue(primaryClass, _ => Split(primaryClass, renderMethod, usingDirectives));
    }

    /// <summary>
    /// Build a <see cref="ClassBodySplitPlan"/> for the given component's primary class
    /// body. Always succeeds — when the body has no user @code content or cannot be
    /// parsed reliably, returns a plan that routes everything to DeclOnly (preserving
    /// the existing single-file behavior).
    /// </summary>
    public static ClassBodySplitPlan Split(
        ClassDeclarationIntermediateNode primaryClass,
        MethodDeclarationIntermediateNode renderMethod,
        IReadOnlyList<UsingDirectiveIntermediateNode>? usingDirectives = null)
    {
        if (primaryClass is null) throw new ArgumentNullException(nameof(primaryClass));
        if (renderMethod is null) throw new ArgumentNullException(nameof(renderMethod));

        // Build a textual alias map from @using directives that contain `Alias = Type`.
        // The decl phase runs before semantic analysis, so we can't ask Roslyn what an
        // identifier resolves to; instead we read the user-authored @using directives
        // straight off the IR. This lets `TryBuildHelperSynth` recognize aliased
        // RenderFragment shapes (e.g. `@using RF = Microsoft.AspNetCore.Components.RenderFragment`)
        // so they route through the helper-synth instead of falling into the
        // unresolved-markup decl-emission path.
        var aliases = BuildAliasMap(usingDirectives);

        // Step 1: collect the user-@code IR children (everything that isn't the render
        // method, isn't a compiler-synthesized helper, isn't a structured declaration
        // Razor generated, isn't a directive surface node like @inject).
        using var userChildren = new PooledArrayBuilder<IntermediateNode>();
        foreach (var child in primaryClass.Children)
        {
            if (child == renderMethod || child.IsSynthesizedHelper)
            {
                continue;
            }

            if (!IsRoutableUserCodeChunk(child))
            {
                continue;
            }

            userChildren.Add(child);
        }

        if (userChildren.Count == 0)
        {
            // No user @code content to classify — return empty plan.
            return new ClassBodySplitPlan(
                ImmutableArray<RoutedChunk>.Empty,
                ImmutableArray<HelperSynth>.Empty);
        }

        var userChildrenArray = userChildren.ToImmutable();

        // Fast path: if the @code contains no markup at all, there is nothing to route to
        // impl -- every member stays in decl -- so skip the parse entirely. Markup always
        // arrives as a distinct IR node: either a non-CSharpCode child, or (the unexpected
        // nested case Preprocess guards against) a non-token child inside a CSharpCode node.
        // So "no markup node" implies "no markup" with no false negatives, and the result is
        // exactly what the parse path produces for markup-free @code (every chunk DeclOnly).
        // Most @code is pure C#, so this avoids a Roslyn class-parse for the common case --
        // twice per component (once in each lowering phase) on the source-generator path.
        if (!ContainsMarkupNode(userChildrenArray))
        {
            return BuildDeclOnlyPlan(userChildrenArray);
        }

        // Step 2: preprocess. Concatenate CSharpCode token text and replace markup
        // children with placeholder identifiers. Record the byte range of each input
        // child in the cleaned text so we can map syntax members back to IR chunks.
        var preprocessed = Preprocess(userChildrenArray);

        // Step 3: parse the cleaned text as a class body. We wrap in a `class __Shim`
        // so that members at the top level (properties, fields, etc.) parse the way
        // they would inside a class. A trailing newline keeps any user preprocessor
        // directives on a fresh line.
        var classText = "class __Shim {\n" + preprocessed.CleanText + "\n}\n";
        var tree = CSharpSyntaxTree.ParseText(classText);
        var diagnostics = tree.GetDiagnostics();

        // If parsing produced an error, or we can't find the shim class (defensive),
        // route every chunk to DeclOnly. Unparseable @code isn't something to transform:
        // emit the user text verbatim in decl and let the C# compiler report the real
        // error, exactly as it would for the unsplit document.
        var shimClass = tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault();
        if (shimClass is null || diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
        {
            return BuildDeclOnlyPlan(userChildrenArray);
        }

        // Step 4: classify each member. We only look at members directly inside the
        // shim -- nested types aren't recursed into because the classifier is uniform
        // for nested members (they're all impl-only). Helper-synth descriptors for
        // surface properties with markup bodies are collected here.
        var existingNames = CollectUserMemberNames(shimClass);
        using var synthsBuilder = new PooledArrayBuilder<HelperSynth>();
        ref var synthsRef = ref synthsBuilder.AsRef();
        var memberClassifications = ClassifyMembers(shimClass, existingNames, ref synthsRef, aliases);

        // Step 5: map IR chunks to members in source order, splitting CSharpCode chunks
        // that straddle members with different routing targets. The split slices are
        // synthetic chunks (CSharpCode wrappers around a single text token); the
        // original chunks remain unmodified.
        var routedChunks = BuildRoutedChunks(
            userChildrenArray,
            preprocessed.ChildRanges,
            memberClassifications,
            shimBodyOffset: "class __Shim {\n".Length);

        return new ClassBodySplitPlan(routedChunks, synthsBuilder.ToImmutable());
    }

    /// <summary>
    /// Build a plan that routes all chunks to DeclOnly (no split, no synths). Used
    /// when parsing fails or the input can't be classified reliably.
    /// </summary>
    private static ClassBodySplitPlan BuildDeclOnlyPlan(ImmutableArray<IntermediateNode> userChildren)
    {
        using var routedChunks = new PooledArrayBuilder<RoutedChunk>(userChildren.Length);
        foreach (var child in userChildren)
        {
            routedChunks.Add(new RoutedChunk(child, ChunkTarget.DeclOnly));
        }
        return new ClassBodySplitPlan(routedChunks.ToImmutableAndClear(), ImmutableArray<HelperSynth>.Empty);
    }

    /// <summary>
    /// True if any chunk represents markup. Markup always arrives as a distinct IR node:
    /// either a non-CSharpCode child, or -- the unexpected nested form <see cref="Preprocess"/>
    /// also treats as markup -- a non-<see cref="IntermediateToken"/> child inside a
    /// CSharpCode node. A CSharpCode node holding only tokens is raw C# text with no markup.
    /// This matches exactly what <see cref="Preprocess"/> would emit a placeholder for, so a
    /// false result reliably means "no markup anywhere" -- no false negatives.
    /// </summary>
    private static bool ContainsMarkupNode(ImmutableArray<IntermediateNode> userChildren)
    {
        foreach (var child in userChildren)
        {
            if (child is not CSharpCodeIntermediateNode csharpCode)
            {
                return true;
            }
            foreach (var grandchild in csharpCode.Children)
            {
                if (grandchild is not IntermediateToken)
                {
                    return true;
                }
            }
        }
        return false;
    }

    // --------- Preprocess --------------------------------------------------------------

    private sealed record PreprocessResult(
        string CleanText,
        ImmutableArray<(int Start, int End)> ChildRanges);

    private static PreprocessResult Preprocess(ImmutableArray<IntermediateNode> children)
    {
        using var _ = StringBuilderPool.GetPooledObject(out var sb);
        using var ranges = new PooledArrayBuilder<(int, int)>(children.Length);
        var markupCounter = 0;

        foreach (var child in children)
        {
            var start = sb.Length;

            if (child is CSharpCodeIntermediateNode csharpCode)
            {
                // Concatenate all token content.
                foreach (var tokenNode in csharpCode.Children)
                {
                    if (tokenNode is IntermediateToken token)
                    {
                        sb.Append(token.Content);
                    }
                    // Anything else inside a CSharpCode node is unexpected (e.g. nested
                    // markup). Treat it as a markup placeholder -- safer than dropping.
                    else
                    {
                        AppendMarkupPlaceholder(sb, markupCounter++);
                    }
                }
            }
            else
            {
                // Non-CSharpCode child: treat as a markup chunk regardless of its
                // specific node kind. The classifier only needs to know "markup here";
                // it doesn't depend on the markup's content.
                AppendMarkupPlaceholder(sb, markupCounter++);
            }

            ranges.Add((start, sb.Length));
        }

        return new PreprocessResult(sb.ToString(), ranges.ToImmutableAndClear());
    }

    /// <summary>
    /// Emits a placeholder identifier (starting with <see cref="MarkupPlaceholderPrefix"/>)
    /// that occupies the syntactic slot of a markup chunk and stays visible to
    /// <see cref="ContainsMarkupPlaceholder"/>. The shape depends on what immediately
    /// precedes it in the buffer:
    /// <list type="bullet">
    /// <item><description>After <c>=&gt;</c> or <c>=</c> (expression position, e.g.
    /// <c>[Parameter] X =&gt; @&lt;p/&gt;;</c>): emit a bare identifier, which parses
    /// as an identifier expression and can be terminated by the user's trailing
    /// <c>;</c>.</description></item>
    /// <item><description>Anywhere else (statement position inside a method body,
    /// e.g. <c>void M() { &lt;p/&gt; }</c>): emit <c>identifier();</c>, a complete
    /// expression statement that parses standalone.</description></item>
    /// </list>
    /// The placeholder is only used by the parse-step shim and never reaches Roslyn's
    /// semantic phase, so the unresolved identifier doesn't matter.
    /// </summary>
    private static void AppendMarkupPlaceholder(StringBuilder sb, int index)
    {
        // Decide expression vs statement BEFORE appending anything; the check inspects
        // the trailing user-code characters that precede the placeholder.
        var inExpressionPosition = IsExpressionPosition(sb);
        sb.Append(MarkupPlaceholderPrefix);
        sb.Append(index);
        sb.Append("__");
        if (!inExpressionPosition)
        {
            sb.Append("();");
        }
    }

    private const string MarkupPlaceholderPrefix = "__razor_markup_";

    private static bool IsExpressionPosition(StringBuilder sb)
    {
        // Find the last significant (non-trivia) character, skipping trailing whitespace
        // and C# comments. An inline comment between an expression-position token and the
        // markup placeholder (`return /* note */ @<p/>` or `=> /* x */ @<p/>`) must not
        // make the last char look like `/`, and a `//` or `*/` INSIDE a string literal
        // (`"http://x"`, `"a*/b"`) must not be mistaken for a comment -- so this scans
        // forward tracking lexical state rather than guessing backward.
        var i = FindLastCodePosition(sb);
        if (i < 0)
        {
            return false;
        }
        // `=>` (lambda or expression body) puts us in expression position.
        if (sb[i] == '>' && i > 0 && sb[i - 1] == '=')
        {
            return true;
        }
        // Bare `=` (initializer / assignment) puts us in expression position.
        // Filter out comparison operators (`==`, `!=`, `<=`, `>=`).
        if (sb[i] == '=')
        {
            if (i == 0)
            {
                return true;
            }
            var prev = sb[i - 1];
            return prev != '=' && prev != '!' && prev != '<' && prev != '>';
        }
        // `(` -- parenthesized expression, argument list head, parameter-list lambda.
        // The C# parser accepts an expression directly after these. Note this means
        // we emit a bare identifier in argument position too (e.g. `Foo(@<p/>)`), which
        // also parses correctly as a single-arg call.
        if (sb[i] == '(')
        {
            return true;
        }
        // `,` -- continues an argument list / element list; the next slot is an
        // expression.
        if (sb[i] == ',')
        {
            return true;
        }
        // `?` -- conditional ternary's true-branch position (`cond ? @<p/> : ...`)
        // OR null-coalescing's right-operand position (`x ?? @<p/>`). Both want a
        // bare expression. Filter out the null-conditional access (`x?.y`), but the
        // placeholder is never inserted between `?` and `.` because there's never a
        // markup chunk in that position -- so a simple `?` -> expression mapping is
        // safe.
        if (sb[i] == '?')
        {
            return true;
        }
        // `:` -- conditional ternary's false-branch position (`cond ? a : @<p/>`).
        // We must exclude switch-case labels (`case 1:`, `default:`) and labelled
        // statements, which look like statement positions and need their bare
        // identifier shape kept intact. Walk back to the previous statement
        // boundary (start-of-line / `;` / `{` / `}`) and check whether `case `
        // or `default` appears in that span.
        if (sb[i] == ':')
        {
            return !IsLabelContext(sb, i);
        }
        // `return KEYWORD` -- a return-statement's expression position. Without
        // this check, a statement-bodied getter `{ get { return @<p/>; } }` would
        // get a statement-shaped placeholder (`__razor_markup_0__();`) appended
        // BEFORE the user's `;`, producing two statements in the parsed accessor
        // body instead of one and defeating the accessor-extraction synth.
        if (char.IsLetter(sb[i]) || sb[i] == '_')
        {
            var wordEnd = i;
            while (i >= 0 && (char.IsLetterOrDigit(sb[i]) || sb[i] == '_'))
            {
                i--;
            }
            var word = sb.ToString(i + 1, wordEnd - i);
            if (word == "return")
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Return the index of the last significant (non-trivia) character in
    /// <paramref name="sb"/> -- the last character that is real code, ignoring trailing
    /// whitespace and C# comments. Characters inside string/char literals count as code,
    /// so a <c>//</c> in <c>"http://x"</c> or a <c>*/</c> in <c>"a*/b"</c> is NOT mistaken
    /// for a comment. Returns -1 if the buffer is empty or all trivia.
    /// </summary>
    /// <remarks>
    /// Scans forward once, tracking lexical state: a backward scan can't reliably tell a
    /// <c>//</c> inside a string from a real line comment without first knowing the string
    /// boundaries, which themselves require a forward pass. Regular, verbatim (<c>@"..."</c>),
    /// and char literals are handled; raw (<c>"""..."""</c>) and the brace interior of
    /// interpolated strings are treated best-effort, matching the simplifications elsewhere
    /// in this file. The buffer is the user @code text (small) and this runs once per markup
    /// placeholder, so the linear rescan is not a hot path.
    /// </remarks>
    private static int FindLastCodePosition(StringBuilder sb)
    {
        var n = sb.Length;
        var lastCode = -1;
        var i = 0;
        while (i < n)
        {
            var c = sb[i];

            // Line comment `// ... \n` -- trivia to end of line.
            if (c == '/' && i + 1 < n && sb[i + 1] == '/')
            {
                i += 2;
                while (i < n && sb[i] != '\n')
                {
                    i++;
                }
                continue;
            }
            // Block comment `/* ... */` -- trivia. An unterminated block comment runs to
            // the end of the buffer.
            if (c == '/' && i + 1 < n && sb[i + 1] == '*')
            {
                i += 2;
                while (i + 1 < n && !(sb[i] == '*' && sb[i + 1] == '/'))
                {
                    i++;
                }
                i += 2;
                continue;
            }
            // Verbatim string `@"..."` where `""` is an escaped quote. (`@` before a
            // non-quote is a verbatim identifier, handled as ordinary code below.)
            if (c == '@' && i + 1 < n && sb[i + 1] == '"')
            {
                lastCode = i;
                i += 2;
                while (i < n)
                {
                    if (sb[i] == '"')
                    {
                        if (i + 1 < n && sb[i + 1] == '"')
                        {
                            i += 2;
                            continue;
                        }
                        break;
                    }
                    i++;
                }
                if (i < n)
                {
                    lastCode = i;
                    i++;
                }
                continue;
            }
            // Regular (and best-effort interpolated) string `"..."` with `\` escapes.
            if (c == '"')
            {
                lastCode = i;
                i++;
                while (i < n)
                {
                    if (sb[i] == '\\')
                    {
                        i += 2;
                        continue;
                    }
                    if (sb[i] == '"')
                    {
                        break;
                    }
                    i++;
                }
                if (i < n)
                {
                    lastCode = i;
                    i++;
                }
                continue;
            }
            // Char literal `'x'` / `'\n'` with `\` escapes.
            if (c == '\'')
            {
                lastCode = i;
                i++;
                while (i < n)
                {
                    if (sb[i] == '\\')
                    {
                        i += 2;
                        continue;
                    }
                    if (sb[i] == '\'')
                    {
                        break;
                    }
                    i++;
                }
                if (i < n)
                {
                    lastCode = i;
                    i++;
                }
                continue;
            }
            // Ordinary code: a non-whitespace character is significant.
            if (!char.IsWhiteSpace(c))
            {
                lastCode = i;
            }
            i++;
        }
        return lastCode;
    }

    /// <summary>
    /// Disambiguate `:` -- is it a switch-case label / labelled-statement marker
    /// (statement context) or a ternary's false-branch separator (expression
    /// context)? Walks back from the `:` position to the previous statement
    /// boundary (start-of-buffer, `;`, `{`, `}`, or `\n`) and checks whether
    /// `case` or `default` appears as a keyword in that span. Pattern-matching
    /// labels (`case string s when ...:`) also start with `case`, so the same
    /// scan catches them. The boundary scan and the keyword search both ignore
    /// content inside string/char literals and comments so e.g.
    /// <c>x == "case" ? a : b</c> doesn't false-positive.
    /// </summary>
    private static bool IsLabelContext(StringBuilder sb, int colonPos)
    {
        // Walk back to the previous statement boundary, then scan that span
        // forward for the `case` / `default` keyword. Both passes skip strings
        // and comments.
        var boundary = -1;
        for (var i = colonPos - 1; i >= 0;)
        {
            // Block comment ends here: `*/` -- skip to matching `/*`.
            if (i >= 1 && sb[i] == '/' && sb[i - 1] == '*')
            {
                var j = i - 2;
                while (j >= 1 && !(sb[j - 1] == '/' && sb[j] == '*'))
                {
                    j--;
                }
                i = j - 2;
                continue;
            }
            // String literal ends here: `"` -- skip to matching opening `"`
            // (treating `\"` as escaped). We DO NOT handle `@"..."` verbatim
            // strings or `$"..."` interpolated strings comprehensively; both
            // simplifications can produce occasional false positives, but the
            // common cases (regular string with a `case` substring) are
            // handled.
            if (sb[i] == '"')
            {
                var j = i - 1;
                while (j >= 0)
                {
                    if (sb[j] == '"' && (j == 0 || sb[j - 1] != '\\'))
                    {
                        break;
                    }
                    j--;
                }
                i = j - 1;
                continue;
            }
            // Char literal: `'x'` or `'\n'` -- mirror of string.
            if (sb[i] == '\'')
            {
                var j = i - 1;
                while (j >= 0)
                {
                    if (sb[j] == '\'' && (j == 0 || sb[j - 1] != '\\'))
                    {
                        break;
                    }
                    j--;
                }
                i = j - 1;
                continue;
            }
            var ch = sb[i];
            if (ch == ';' || ch == '{' || ch == '}' || ch == '\n')
            {
                boundary = i;
                break;
            }
            i--;
        }
        var spanStart = boundary + 1;
        var spanLen = colonPos - spanStart;
        if (spanLen < 4)
        {
            return false;
        }
        // Build a normalized span text with strings/comments stripped before
        // the keyword check.
        var span = StripStringsAndComments(sb, spanStart, spanLen);
        return ContainsKeyword(span, "case") || ContainsKeyword(span, "default");
    }

    /// <summary>
    /// Return a copy of <paramref name="sb"/>'s range [<paramref name="start"/>,
    /// <paramref name="start"/>+<paramref name="length"/>) with strings, char
    /// literals, line comments, and block comments replaced by spaces. Used by
    /// keyword-presence checks so e.g. <c>"case"</c> as a string literal doesn't
    /// false-positive as a <c>case</c> label.
    /// </summary>
    private static string StripStringsAndComments(StringBuilder sb, int start, int length)
    {
        using var _ = StringBuilderPool.GetPooledObject(out var result);
        var end = start + length;
        for (var i = start; i < end; i++)
        {
            var ch = sb[i];
            // Block comment `/* ... */`
            if (ch == '/' && i + 1 < end && sb[i + 1] == '*')
            {
                result.Append("  ");
                i += 2;
                while (i + 1 < end && !(sb[i] == '*' && sb[i + 1] == '/'))
                {
                    result.Append(' ');
                    i++;
                }
                if (i + 1 < end)
                {
                    result.Append("  ");
                    i++; // for-loop's i++ handles the second character
                }
                continue;
            }
            // Line comment `// ... \n`
            if (ch == '/' && i + 1 < end && sb[i + 1] == '/')
            {
                while (i < end && sb[i] != '\n')
                {
                    result.Append(' ');
                    i++;
                }
                if (i < end)
                {
                    result.Append(sb[i]);
                }
                continue;
            }
            // String literal `"..."` (does not handle verbatim/interpolated
            // strings comprehensively; rare in user @code anyway).
            if (ch == '"')
            {
                result.Append(' ');
                i++;
                while (i < end && sb[i] != '"')
                {
                    if (sb[i] == '\\' && i + 1 < end)
                    {
                        result.Append("  ");
                        i += 2;
                        continue;
                    }
                    result.Append(' ');
                    i++;
                }
                if (i < end)
                {
                    result.Append(' ');
                }
                continue;
            }
            // Char literal `'x'` / `'\n'`.
            if (ch == '\'')
            {
                result.Append(' ');
                i++;
                while (i < end && sb[i] != '\'')
                {
                    if (sb[i] == '\\' && i + 1 < end)
                    {
                        result.Append("  ");
                        i += 2;
                        continue;
                    }
                    result.Append(' ');
                    i++;
                }
                if (i < end)
                {
                    result.Append(' ');
                }
                continue;
            }
            result.Append(ch);
        }
        return result.ToString();
    }

    private static bool ContainsKeyword(string text, string keyword)
    {
        var idx = 0;
        while ((idx = text.IndexOf(keyword, idx, StringComparison.Ordinal)) >= 0)
        {
            // Match only if at a word boundary on both sides.
            var before = idx == 0 || !IsIdentifierChar(text[idx - 1]);
            var afterIdx = idx + keyword.Length;
            var after = afterIdx == text.Length || !IsIdentifierChar(text[afterIdx]);
            if (before && after)
            {
                return true;
            }
            idx++;
        }
        return false;
    }

    private static bool IsIdentifierChar(char c) => char.IsLetterOrDigit(c) || c == '_';

    // --------- Classify ----------------------------------------------------------------

    private static readonly ImmutableHashSet<string> SurfaceAttributeNames =
        ImmutableHashSet.Create(
            StringComparer.Ordinal,
            "Parameter", "ParameterAttribute",
            "CascadingParameter", "CascadingParameterAttribute",
            "SupplyParameterFromQuery", "SupplyParameterFromQueryAttribute",
            "SupplyParameterFromForm", "SupplyParameterFromFormAttribute",
            "Inject", "InjectAttribute",
            "EditorRequired", "EditorRequiredAttribute");

    private static ImmutableArray<MemberClassification> ClassifyMembers(
        ClassDeclarationSyntax shim,
        ImmutableHashSet<string> existingNames,
        ref PooledArrayBuilder<HelperSynth> synths,
        IReadOnlyDictionary<string, string> aliases)
    {
        using var result = new PooledArrayBuilder<MemberClassification>();
        foreach (var member in shim.Members)
        {
            var (kind, memberSynths, bodyShimSpans) = ClassifyOne(member, existingNames, aliases);
            foreach (var synth in memberSynths)
            {
                synths.Add(synth);
                // Keep each helper name reserved so a later NeedsHelper member (or a
                // sibling accessor's synth on the same property) doesn't collide.
                existingNames = existingNames.Add(synth.SynthMethodName);
            }
            // ClassifyOne returns one body span per synth method (the syntactic body
            // each synth's impl wraps). Chunks inside a span route to NeedsHelperBody
            // (the synth's captured content); chunks outside every span route to
            // NeedsHelperOmit (replaced by the synth stubs in decl).
            result.Add(new MemberClassification(member.FullSpan, kind, bodyShimSpans));
        }
        return result.ToImmutable();
    }

    private static (MemberKind Kind, ImmutableArray<HelperSynth> Synths, ImmutableArray<TextSpan> BodySpans) ClassifyOne(
        MemberDeclarationSyntax member,
        ImmutableHashSet<string> existingNames,
        IReadOnlyDictionary<string, string> aliases)
    {
        var containsMarkup = ContainsMarkupPlaceholder(member);
        if (!containsMarkup)
        {
            // Members without markup work equally well in either half and stay in decl
            // to keep the baseline diff for non-markup @code shapes minimal.
            return (MemberKind.DeclOnly, [], []);
        }

        // From here on the member has markup somewhere in its body.
        // A surface PROPERTY whose body contains markup is the tricky case: the property
        // must stay visible in decl (so other pages can see it via tag-helper discovery)
        // but its body needs resolved tag helpers (only available in impl). The
        // helper-delegation pattern handles this when the property's shape fits: decl gets
        // a stubbed property that delegates to a synthesized partial method; impl gets the
        // partial-method definition wrapping the user's body. Aliased type names (e.g.
        // `@using RF = RenderFragment`) are resolved via the document's alias map before
        // shape detection.
        //
        // Anything else with a surface attribute is left untransformed (DeclOnly, emitted
        // verbatim in decl), because the only inputs that reach it don't compile: a markup
        // property of a non-RenderFragment type is a C# error (CS8917 -- a RenderTreeBuilder
        // lambda can't bind to it), and the surface attributes (`[Parameter]` etc.) all
        // target properties, so a surface-attributed field is a Blazor error (CS0592)
        // regardless of its markup. Emitting such a member as-written lets the compiler
        // report the same diagnostic it would for the unsplit document -- we don't
        // transform invalid code.
        if (HasSurfaceAttribute(member))
        {
            if (member is PropertyDeclarationSyntax property
                && TryBuildHelperSynth(property, existingNames, aliases, out var synths, out var bodySpans))
            {
                return (MemberKind.NeedsHelper, synths, bodySpans);
            }
            return (MemberKind.DeclOnly, [], []);
        }

        // Non-surface markup-bearing members (private helper methods etc.) move to
        // impl as-is so their markup lowering sees resolved tag helpers.
        return (MemberKind.ImplOnly, [], []);
    }

    /// <summary>
    /// Build a helper-synth descriptor for a surface property whose body contains
    /// markup. Supports two shapes:
    /// <list type="bullet">
    ///   <item>Expression-bodied properties (<c>=&gt;</c>) of <c>RenderFragment</c> or
    ///     <c>RenderFragment&lt;T&gt;</c>: decl delegates via an instance partial method.</item>
    ///   <item>Auto-properties with markup-bearing initializers (<c>= ...;</c>) of the
    ///     same types: decl delegates via a <em>static</em> partial method. A field
    ///     initializer can reference a static method (no implicit <c>this</c> capture)
    ///     -- and any user markup inside an initializer already cannot reference
    ///     instance state (CS0236), so a static body is semantically lossless.</item>
    /// </list>
    /// Returns false when the property's shape isn't one the delegation pattern targets.
    /// In valid code that means there's no markup to move; the shapes that have markup but
    /// aren't RenderFragment-typed only occur in invalid code (a RenderTreeBuilder lambda
    /// can't bind to a non-RenderFragment type -- CS8917), so a false return routes the
    /// member to DeclOnly and lets the compiler report that error unchanged.
    /// </summary>
    private static bool TryBuildHelperSynth(
        PropertyDeclarationSyntax property,
        ImmutableHashSet<string> existingNames,
        IReadOnlyDictionary<string, string> aliases,
        out ImmutableArray<HelperSynth> synths,
        out ImmutableArray<TextSpan> bodySpans)
    {
        synths = [];
        bodySpans = [];

        // Locate every markup-bearing body site in the property. Empty means no markup
        // (or a shape FindBodySites doesn't recognise). A property can carry markup in
        // more than one accessor; each site gets its own synth method.
        var sites = FindBodySites(property);
        if (sites.IsEmpty)
        {
            return false;
        }

        // The delegation pattern targets RenderFragment-shaped properties (RenderFragment,
        // RenderFragment<T>, Func<T, RenderFragment>, and aliased forms). A markup body on
        // any other type doesn't compile (CS8917), so declining here leaves the invalid
        // member untransformed rather than reshaping code the compiler will reject anyway.
        var typeText = property.Type.ToString();
        if (!TryGetRenderFragmentShape(typeText, aliases))
        {
            return false;
        }

        var propertyName = property.Identifier.Text;
        // Strip any nullable-annotation suffix: generated code isn't in an explicit
        // #nullable context, so a `RenderFragment?` synth type would trigger CS8669.
        var synthTypeText = typeText.TrimEnd('?').TrimEnd();

        // Build one synth per site, reserving each method name as we go so a property
        // with markup in both a getter and a setter mints two distinct names.
        var names = existingNames;
        using var siteSynths = new PooledArrayBuilder<(string Name, string Signature, string SynthCall, string Open, string Close, TextSpan Span)>();
        foreach (var site in sites)
        {
            if (!TryBuildSiteSynth(site, propertyName, synthTypeText, ref names,
                    out var name, out var signature, out var synthCall, out var open, out var close))
            {
                return false;
            }
            siteSynths.Add((name, signature, synthCall, open, close, site.SiteSpan));
        }

        // The decl property delegates each markup body to its synth call.
        using var synthCallsBuilder = new PooledArrayBuilder<string>();
        for (var i = 0; i < siteSynths.Count; i++)
        {
            synthCallsBuilder.Add(siteSynths[i].SynthCall);
        }
        var declPropertyText = BuildTransformedPropertyText(property, sites, synthCallsBuilder.ToImmutable());

        // Only the first synth carries the decl source (the shared rewritten property
        // plus every synth declaration); the rest carry none so the property and
        // declarations aren't emitted more than once. Each synth keeps its own impl
        // wrapper for the body run the impl phase pairs it with.
        using var _ = StringBuilderPool.GetPooledObject(out var declSb);
        declSb.Append(declPropertyText).Append('\n');
        for (var i = 0; i < siteSynths.Count; i++)
        {
            declSb.Append(siteSynths[i].Signature).Append(";\n");
        }
        var declSource = declSb.ToString();

        using var synthBuilder = new PooledArrayBuilder<HelperSynth>();
        using var spanBuilder = new PooledArrayBuilder<TextSpan>();
        for (var i = 0; i < siteSynths.Count; i++)
        {
            synthBuilder.Add(new HelperSynth(
                SynthMethodName: siteSynths[i].Name,
                SynthDeclSource: i == 0 ? declSource : "",
                SynthImplOpenSource: siteSynths[i].Open,
                SynthImplCloseSource: siteSynths[i].Close));
            spanBuilder.Add(siteSynths[i].Span);
        }

        synths = synthBuilder.ToImmutable();
        bodySpans = spanBuilder.ToImmutable();
        return true;
    }

    /// <summary>
    /// Build the synth method name, declaration signature, decl-side call, and impl
    /// wrapper for a single markup body site. A set/init accessor body is void and reads
    /// <c>value</c>, so its synth returns void and takes a <c>value</c> parameter; other
    /// body sites use a parameterless synth returning the property type. An initializer
    /// site must be STATIC because field initializers can't reference instance members
    /// (CS0236).
    /// </summary>
    private static bool TryBuildSiteSynth(
        BodySite site,
        string propertyName,
        string synthTypeText,
        ref ImmutableHashSet<string> names,
        out string name,
        out string signature,
        out string synthCall,
        out string implPrefix,
        out string implSuffix)
    {
        var isSetter = site.MarkupAccessor is { } acc && acc.Keyword.Text is "set" or "init";
        var staticModifier = site.Kind == BodySiteKind.PropertyInitializer ? "static " : "";
        var suffix = isSetter ? "Set" : "Body";
        name = PickFreshName($"__razor_synth_{propertyName}{suffix}", names);
        names = names.Add(name);
        // `partial void Synth(T value)` (setter) is a classic partial method; the getter
        // shape `partial T Synth()` is an extended partial method (C# 9+) needing explicit
        // accessibility.
        synthCall = isSetter ? $"{name}(value)" : $"{name}()";
        signature = isSetter
            ? $"private partial void {name}({synthTypeText} value)"
            : $"private {staticModifier}partial {synthTypeText} {name}()";
        switch (site.Kind)
        {
            case BodySiteKind.PropertyExpressionBody:
            case BodySiteKind.AccessorExpressionBody:
            case BodySiteKind.PropertyInitializer:
                // Wrap as an expression-bodied partial method with explicit parens around
                // the user's expression to neutralise operator-precedence pitfalls (e.g.
                // `expr ?? @<p/>` parsing the lambda arrow against the wrong operand).
                implPrefix = signature + " => (";
                implSuffix = ");\n";
                return true;
            case BodySiteKind.AccessorStatementBody:
                // Chunks contain the full block including braces, so just prefix the signature.
                implPrefix = signature + "\n";
                implSuffix = "\n";
                return true;
            default:
                implPrefix = implSuffix = "";
                return false;
        }
    }

    /// <summary>
    /// Build the new property declaration text by replacing each markup-bearing body
    /// site with its <paramref name="synthCalls"/> expression. Preserves the rest of the
    /// property structure exactly (attributes, modifiers, type, name, other accessors,
    /// accessor-level attributes, etc.) by walking the user's syntax tree and only
    /// mutating the markup-bearing sites.
    /// </summary>
    private static string BuildTransformedPropertyText(
        PropertyDeclarationSyntax property,
        ImmutableArray<BodySite> sites,
        ImmutableArray<string> synthCalls)
    {
        // A property-level expression body is always the sole site.
        if (sites[0].Kind == BodySiteKind.PropertyExpressionBody)
        {
            // Replace `=> EXPR;` with `=> synthCall;`.
            return BuildPropertyTextWithReplacedExpressionBody(property, synthCalls[0]);
        }

        // A lone initializer (auto-property `{ get; set; } = @<p/>;`) keeps the accessor
        // list verbatim and replaces just the initializer.
        if (sites.Length == 1 && sites[0].Kind == BodySiteKind.PropertyInitializer)
        {
            return BuildPropertyTextWithReplacedInitializer(property, synthCalls[0]);
        }

        // One or more accessor bodies, optionally alongside a markup initializer (the
        // C# `field`-keyword case where a bodied accessor and an initializer coexist).
        // Replace each mapped accessor body, and the initializer if it has its own synth.
        // Unmapped accessors and a markup-free initializer emit verbatim.
        string? initializerCall = null;
        using (DictionaryPool<AccessorDeclarationSyntax, string>.GetPooledObject(out var callByAccessor))
        {
            for (var i = 0; i < sites.Length; i++)
            {
                if (sites[i].Kind == BodySiteKind.PropertyInitializer)
                {
                    initializerCall = synthCalls[i];
                }
                else
                {
                    callByAccessor[sites[i].MarkupAccessor!] = synthCalls[i];
                }
            }
            return BuildPropertyTextWithReplacedAccessorBodies(property, callByAccessor, initializerCall);
        }
    }

    private static string BuildPropertyTextWithReplacedExpressionBody(
        PropertyDeclarationSyntax property,
        string synthCall)
    {
        // Reuse user's attribute lists, modifiers, type, identifier verbatim;
        // emit a new expression body with the synth call.
        using var _ = StringBuilderPool.GetPooledObject(out var sb);
        WritePropertyHeader(sb, property);
        sb.Append(" => ").Append(synthCall).Append(';');
        return sb.ToString();
    }

    private static string BuildPropertyTextWithReplacedInitializer(
        PropertyDeclarationSyntax property,
        string synthCall)
    {
        using var _ = StringBuilderPool.GetPooledObject(out var sb);
        WritePropertyHeader(sb, property);
        sb.Append(' ');
        // Preserve user's accessor list verbatim -- this is critical for `init`,
        // `private set`, etc. A property with an initializer always has an accessor
        // list (an expression-bodied property cannot also have an initializer).
        sb.Append(property.AccessorList!.NormalizeWhitespace().ToFullString());
        sb.Append(" = ").Append(synthCall).Append(';');
        return sb.ToString();
    }

    private static string BuildPropertyTextWithReplacedAccessorBodies(
        PropertyDeclarationSyntax property,
        IReadOnlyDictionary<AccessorDeclarationSyntax, string> synthCallByAccessor,
        string? initializerCall = null)
    {
        using var _ = StringBuilderPool.GetPooledObject(out var sb);
        WritePropertyHeader(sb, property);
        sb.Append(" {\n");
        foreach (var accessor in property.AccessorList!.Accessors)
        {
            // Preserve accessor attributes (e.g. [get: ...]) and modifiers
            // (e.g. private, internal, protected on accessor).
            if (accessor.AttributeLists.Count > 0)
            {
                foreach (var a in accessor.AttributeLists)
                {
                    sb.Append("    ").Append(a.NormalizeWhitespace().ToFullString().Trim()).Append('\n');
                }
            }
            sb.Append("    ");
            if (accessor.Modifiers.Count > 0)
            {
                sb.Append(string.Join(" ", accessor.Modifiers.Select(m => m.Text))).Append(' ');
            }
            sb.Append(accessor.Keyword.Text);
            if (synthCallByAccessor.TryGetValue(accessor, out var synthCall))
            {
                sb.Append(" => ").Append(synthCall).Append(";\n");
            }
            else if (accessor.ExpressionBody is { } ebody)
            {
                sb.Append(' ').Append(ebody.NormalizeWhitespace().ToFullString()).Append(";\n");
            }
            else if (accessor.Body is { } block)
            {
                sb.Append(' ').Append(block.NormalizeWhitespace().ToFullString()).Append('\n');
            }
            else
            {
                // Auto-accessor (`get;` or `set;` or `init;`).
                sb.Append(";\n");
            }
        }
        sb.Append("}");
        // A `field`-keyword property can carry markup in both an accessor and the
        // initializer; when the initializer has its own synth, emit the call, otherwise
        // emit the user's initializer verbatim.
        if (initializerCall is not null)
        {
            sb.Append(" = ").Append(initializerCall).Append(';');
        }
        else if (property.Initializer is { Value: { } initValue })
        {
            sb.Append(" = ").Append(initValue.NormalizeWhitespace().ToFullString()).Append(';');
        }
        return sb.ToString();
    }

    private static void WritePropertyHeader(StringBuilder sb, PropertyDeclarationSyntax property)
    {
        // Attribute targets (e.g. `[get: ...]` / `[field: ...]`) and modifiers
        // (`required`, `unsafe`, `new`, `virtual`, `override`, `partial`, etc.) are
        // preserved with their original text.
        AppendAttributesAndModifiers(sb, property.AttributeLists, property.Modifiers);
        sb.Append(property.Type.ToString());
        if (property.ExplicitInterfaceSpecifier is { } eis)
        {
            sb.Append(' ').Append(eis.Name.ToString()).Append('.');
        }
        else
        {
            sb.Append(' ');
        }
        sb.Append(property.Identifier.Text);
    }

    private static void AppendAttributesAndModifiers(
        StringBuilder sb,
        SyntaxList<AttributeListSyntax> attributeLists,
        SyntaxTokenList modifiers)
    {
        foreach (var attrList in attributeLists)
        {
            sb.Append(attrList.NormalizeWhitespace().ToFullString().Trim()).Append(' ');
        }
        foreach (var mod in modifiers)
        {
            sb.Append(mod.Text).Append(' ');
        }
    }

    /// <summary>
    /// True if the textual type is <c>RenderFragment</c> or <c>RenderFragment&lt;T&gt;</c>
    /// (with or without namespace qualification and nullable annotation). The check is
    /// textual on purpose -- we don't have a semantic model at this phase. <paramref
    /// name="aliases"/> resolves <c>@using ALIAS = TYPE</c> declarations so aliased
    /// forms (e.g. <c>using RF = RenderFragment</c> + <c>public RF Foo => @&lt;p/&gt;;</c>)
    /// also match. Types that still don't resolve to a RenderFragment shape fall through
    /// to DeclOnly.
    /// </summary>
    private static bool TryGetRenderFragmentShape(
        string typeText,
        IReadOnlyDictionary<string, string> aliases)
    {
        var trimmed = typeText.TrimEnd('?').Trim();

        // Single-level alias resolution covers `@using RF = RenderFragment` and
        // `@using RFS = RenderFragment<string>` patterns. Chained aliases (e.g.
        // `using A = X; using B = A;`) are not real-world: C# requires the
        // right-hand side of a using-alias to be a namespace or type reference
        // expressed in `qualified-alias-member-name` syntax, and `A` (another
        // alias in the same scope) doesn't qualify -- CS0246. So we don't follow
        // chains here either.
        if (aliases.TryGetValue(trimmed, out var resolved))
        {
            trimmed = resolved.TrimEnd('?').Trim();
        }

        // Non-generic shape.
        if (trimmed == "RenderFragment" || trimmed.EndsWith(".RenderFragment", StringComparison.Ordinal))
        {
            return true;
        }

        // Generic shape: find the matching `>` for the first `<` (balanced for nested
        // generics like `RenderFragment<List<int>>`). The substring before `<` must
        // textually resolve to `RenderFragment`. Also accept `Func<T, RenderFragment>`
        // since that's the underlying delegate shape of `RenderFragment<T>`; some
        // libraries write templates that way (or use the synonym to be explicit).
        var lt = trimmed.IndexOf('<');
        if (lt < 0)
        {
            return false;
        }
        var head = trimmed.Substring(0, lt).TrimEnd();
        if (head != "RenderFragment" && !head.EndsWith(".RenderFragment", StringComparison.Ordinal))
        {
            // `Func<T, RenderFragment>` / `Func<T, *.RenderFragment>` -- one-argument
            // Func returning RenderFragment. Other arity / return types fall through.
            if (head == "Func" || head.EndsWith(".Func", StringComparison.Ordinal))
            {
                return IsFuncRenderFragmentShape(trimmed, lt, aliases);
            }
            return false;
        }
        // The type ends with the matching `>` -- if there's trailing text beyond it the
        // type isn't shaped like `RenderFragment<...>` and the synth can't safely use it.
        var depth = 1;
        for (var i = lt + 1; i < trimmed.Length; i++)
        {
            if (trimmed[i] == '<')
            {
                depth++;
            }
            else if (trimmed[i] == '>')
            {
                depth--;
                if (depth == 0)
                {
                    if (i != trimmed.Length - 1)
                    {
                        return false;
                    }
                    var arg = trimmed.Substring(lt + 1, i - lt - 1).Trim();
                    return arg.Length > 0;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Recognize <c>Func&lt;T, RenderFragment&gt;</c> (or any qualified
    /// <c>*.Func</c> / <c>*.RenderFragment</c> form), the underlying delegate shape
    /// of <c>RenderFragment&lt;T&gt;</c>. Multi-argument <c>Func</c> (e.g.
    /// <c>Func&lt;T, U, RenderFragment&gt;</c>) and non-RenderFragment return
    /// types fall through. The return-type arg is run through the alias map
    /// so <c>Func&lt;T, RF&gt;</c> with <c>@using RF = RenderFragment</c>
    /// also matches.
    /// </summary>
    private static bool IsFuncRenderFragmentShape(
        string trimmed,
        int lt,
        IReadOnlyDictionary<string, string> aliases)
    {
        // Walk through the angle-bracketed arg list, splitting at top-level commas.
        // We track BOTH angle bracket depth AND parenthesis depth so commas inside
        // a tuple type (e.g. `Func<(int,string),RenderFragment>`) aren't mistaken
        // for argument separators.
        var angleDepth = 1;
        var parenDepth = 0;
        var argStart = lt + 1;
        using var _ = ListPool<string>.GetPooledObject(out var args);
        var close = -1;
        for (var i = lt + 1; i < trimmed.Length; i++)
        {
            var ch = trimmed[i];
            if (ch == '<') angleDepth++;
            else if (ch == '>')
            {
                angleDepth--;
                if (angleDepth == 0 && parenDepth == 0)
                {
                    args.Add(trimmed.Substring(argStart, i - argStart).Trim());
                    close = i;
                    break;
                }
            }
            else if (ch == '(') parenDepth++;
            else if (ch == ')') parenDepth--;
            else if (ch == ',' && angleDepth == 1 && parenDepth == 0)
            {
                args.Add(trimmed.Substring(argStart, i - argStart).Trim());
                argStart = i + 1;
            }
        }
        // Must have exactly two args (T, RenderFragment) and `>` must end the type.
        if (close < 0 || close != trimmed.Length - 1 || args.Count != 2)
        {
            return false;
        }
        var ret = args[1];
        // Apply alias resolution to the return type so `Func<T, RF>` with
        // `@using RF = RenderFragment` matches.
        if (aliases.TryGetValue(ret, out var resolved))
        {
            ret = resolved.Trim();
        }
        if (ret != "RenderFragment" && !ret.EndsWith(".RenderFragment", StringComparison.Ordinal))
        {
            return false;
        }
        return args[0].Length > 0;
    }

    /// <summary>
    /// Parse <c>@using</c> directive content for alias declarations of the form
    /// <c>ALIAS = TYPE</c> and return a map keyed by alias name. The <see
    /// cref="UsingDirectiveIntermediateNode.Content"/> string is the text the user
    /// wrote between <c>@using</c> and the trailing <c>;</c>, with any wrapping
    /// trivia already stripped (e.g. <c>"RF = Microsoft.AspNetCore.Components.RenderFragment"</c>
    /// or <c>"global System.Text"</c> -- which has no <c>=</c> and is skipped).
    /// We deliberately don't try to chase chained aliases or evaluate
    /// <c>global::</c> prefixes; <see cref="TryGetRenderFragmentShape"/> trims those
    /// the same way it trims the literal type text.
    /// </summary>
    private static IReadOnlyDictionary<string, string> BuildAliasMap(
        IReadOnlyList<UsingDirectiveIntermediateNode>? usingDirectives)
    {
        if (usingDirectives is null || usingDirectives.Count == 0)
        {
            return EmptyAliases;
        }

        Dictionary<string, string>? map = null;
        foreach (var directive in usingDirectives)
        {
            var content = directive.Content;
            if (string.IsNullOrEmpty(content))
            {
                continue;
            }

            // Strip any `static` prefix (alias declarations can't be `using static`,
            // but skipping it is harmless for non-alias directives that share this
            // parsing branch).
            var span = content.AsSpan().Trim();
            const string staticPrefix = "static ";
            if (span.StartsWith(staticPrefix.AsSpan(), StringComparison.Ordinal))
            {
                continue;
            }

            var equalsIdx = span.IndexOf('=');
            if (equalsIdx <= 0)
            {
                continue;
            }

            var aliasName = span.Slice(0, equalsIdx).Trim().ToString();
            var resolvedType = span.Slice(equalsIdx + 1).Trim().TrimEnd(';').Trim().ToString();
            if (aliasName.Length == 0 || resolvedType.Length == 0)
            {
                continue;
            }

            map ??= new Dictionary<string, string>(StringComparer.Ordinal);
            // Last write wins -- Razor doesn't enforce alias uniqueness; if the user
            // somehow declared the same alias twice the C# compiler would complain
            // anyway.
            map[aliasName] = resolvedType;
        }

        return (IReadOnlyDictionary<string, string>?)map ?? EmptyAliases;
    }

    private static readonly IReadOnlyDictionary<string, string> EmptyAliases =
        new Dictionary<string, string>(0);

    /// <summary>
    /// Identify every markup-bearing body site within a property, in source order.
    /// Each site records its kind (initializer, expression-body, accessor expression-body,
    /// or accessor block body), the span of the SHIM text it covers (used by chunk routing
    /// to identify which chunks become the synth's wrapped body), and -- when the markup
    /// lives inside a specific accessor -- a reference to that accessor so the synth's
    /// emitted decl can preserve the other accessors verbatim.
    /// </summary>
    /// <remarks>
    /// Each potential site is tested for markup independently; a site without markup is not
    /// returned. A property can carry markup in more than one place at once -- multiple
    /// accessors (a computed getter plus a fallback setter), or, with the C# <c>field</c>
    /// keyword, a bodied accessor AND an initializer (<c>{ get =&gt; field ?? @&lt;p/&gt;; }
    /// = @&lt;div/&gt;;</c>). Accessor sites precede the initializer site because the
    /// accessor list is textually ahead of <c>= initializer</c>; together with the
    /// accessor declaration order this yields the source order the impl phase pairs synths
    /// against. A multi-statement getter returns <see cref="BodySiteKind.AccessorStatementBody"/>
    /// with a span covering the entire block (braces included) so the synth wraps the whole
    /// body, preserving preceding statements / locals / control flow.
    /// </remarks>
    private static ImmutableArray<BodySite> FindBodySites(PropertyDeclarationSyntax property)
    {
        // A property-level expression body (`Foo => EXPR`) is mutually exclusive with an
        // accessor list and an initializer, so it is the sole site when present.
        if (property.ExpressionBody is { Expression: { } exprBody })
        {
            return ContainsMarkupPlaceholder(exprBody)
                ? [new BodySite(BodySiteKind.PropertyExpressionBody, exprBody.FullSpan, MarkupAccessor: null)]
                : [];
        }

        using var sites = new PooledArrayBuilder<BodySite>();

        // Accessor bodies first -- the accessor list precedes any `= initializer` in source
        // order. Each markup-bearing accessor gets its own site / synth.
        if (property.AccessorList is { Accessors: var accessors })
        {
            foreach (var accessor in accessors)
            {
                if (!ContainsMarkupPlaceholder(accessor))
                {
                    continue;
                }
                if (accessor.ExpressionBody is { Expression: { } accExpr })
                {
                    sites.Add(new BodySite(BodySiteKind.AccessorExpressionBody, accExpr.FullSpan, accessor));
                }
                else if (accessor.Body is { } block)
                {
                    // Cover the entire block (including braces) so the synth's impl
                    // wraps the whole accessor body -- preserving any preceding
                    // statements and local-variable declarations the markup references.
                    sites.Add(new BodySite(BodySiteKind.AccessorStatementBody, block.FullSpan, accessor));
                }
            }
        }

        // Then the initializer, if its value contains markup. Checking it independently
        // (rather than returning early on any initializer) is what lets a `field`-keyword
        // property with markup in BOTH an accessor and the initializer route both bodies.
        if (property.Initializer is { Value: { } initValue } && ContainsMarkupPlaceholder(initValue))
        {
            sites.Add(new BodySite(BodySiteKind.PropertyInitializer, initValue.FullSpan, MarkupAccessor: null));
        }

        return sites.ToImmutable();
    }

    private enum BodySiteKind
    {
        PropertyExpressionBody,
        PropertyInitializer,
        AccessorExpressionBody,
        AccessorStatementBody,
    }

    private sealed record BodySite(
        BodySiteKind Kind,
        TextSpan SiteSpan,
        AccessorDeclarationSyntax? MarkupAccessor);

    private static string PickFreshName(string baseName, ImmutableHashSet<string> existingNames)
    {
        if (!existingNames.Contains(baseName))
        {
            return baseName;
        }
        for (var i = 1; ; i++)
        {
            var candidate = baseName + "_" + i;
            if (!existingNames.Contains(candidate))
            {
                return candidate;
            }
        }
    }

    /// <summary>
    /// Collect every top-level member name declared in the parsed shim. Used to pick
    /// helper-synth names that can't collide with anything the user wrote.
    /// </summary>
    private static ImmutableHashSet<string> CollectUserMemberNames(ClassDeclarationSyntax shim)
    {
        var builder = ImmutableHashSet.CreateBuilder<string>(StringComparer.Ordinal);
        foreach (var member in shim.Members)
        {
            switch (member)
            {
                case PropertyDeclarationSyntax p:
                    builder.Add(p.Identifier.Text);
                    break;
                case MethodDeclarationSyntax m:
                    builder.Add(m.Identifier.Text);
                    break;
                case FieldDeclarationSyntax f:
                    foreach (var v in f.Declaration.Variables)
                    {
                        builder.Add(v.Identifier.Text);
                    }
                    break;
                case EventDeclarationSyntax e:
                    builder.Add(e.Identifier.Text);
                    break;
                case EventFieldDeclarationSyntax ef:
                    foreach (var v in ef.Declaration.Variables)
                    {
                        builder.Add(v.Identifier.Text);
                    }
                    break;
                case BaseTypeDeclarationSyntax t:
                    builder.Add(t.Identifier.Text);
                    break;
                case DelegateDeclarationSyntax d:
                    builder.Add(d.Identifier.Text);
                    break;
            }
        }
        return builder.ToImmutable();
    }

    private static bool ContainsMarkupPlaceholder(SyntaxNode node)
    {
        foreach (var token in node.DescendantTokens())
        {
            if (token.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.IdentifierToken)
                && token.ValueText.StartsWith(MarkupPlaceholderPrefix, StringComparison.Ordinal))
            {
                return true;
            }
        }
        return false;
    }

    private static bool HasSurfaceAttribute(MemberDeclarationSyntax m)
    {
        foreach (var list in m.AttributeLists)
        {
            foreach (var attr in list.Attributes)
            {
                var name = attr.Name switch
                {
                    IdentifierNameSyntax id => id.Identifier.Text,
                    QualifiedNameSyntax q => q.Right.Identifier.Text,
                    _ => null
                };
                if (name is not null && SurfaceAttributeNames.Contains(name))
                {
                    return true;
                }
            }
        }
        return false;
    }

    // --------- Map IR chunks to members ------------------------------------------------

    private enum MemberKind { DeclOnly, ImplOnly, NeedsHelper }

    /// <summary>
    /// A classified user @code member. <see cref="ShimSpan"/> is the full member span
    /// in shim coordinates (used to attribute chunks to a member). For
    /// <see cref="MemberKind.NeedsHelper"/> members, <see cref="BodyShimSpans"/> holds the
    /// syntactic body span of each markup-bearing site (one per synth method -- usually a
    /// single expression body or initializer, but a property with markup in multiple
    /// accessors has one span per accessor). Chunks inside a body span become the
    /// matching synth's captured body (<see cref="ChunkTarget.NeedsHelperBody"/>); chunks
    /// outside every span are the leading attribute/type/`=>` and trailing `;` text the
    /// synth stubs replace (<see cref="ChunkTarget.NeedsHelperOmit"/>).
    /// </summary>
    private sealed record MemberClassification(
        TextSpan ShimSpan,
        MemberKind Kind,
        ImmutableArray<TextSpan> BodyShimSpans);

    /// <summary>
    /// Walk the user IR chunks in source order, producing per-chunk routing decisions.
    /// Each CSharpCode chunk is sub-divided at member boundaries and (for NeedsHelper
    /// members) at the body span boundary, so each emitted slice routes to a single
    /// target. Non-CSharpCode chunks (Template etc.) are treated as opaque single
    /// units. Chunks whose slices all share the same target keep their original IR
    /// node (preserving source mapping); chunks with mixed targets are emitted as
    /// synthetic CSharpCode wrappers around the sliced text.
    /// </summary>
    private static ImmutableArray<RoutedChunk> BuildRoutedChunks(
        ImmutableArray<IntermediateNode> userChildren,
        ImmutableArray<(int Start, int End)> childRanges,
        ImmutableArray<MemberClassification> members,
        int shimBodyOffset)
    {
        using var result = new PooledArrayBuilder<RoutedChunk>();

        for (var i = 0; i < userChildren.Length; i++)
        {
            var chunk = userChildren[i];
            var (cleanStart, cleanEnd) = childRanges[i];
            var shimStart = cleanStart + shimBodyOffset;
            var shimEnd = cleanEnd + shimBodyOffset;

            // Non-CSharpCode chunks (Template, MarkupElement, etc.) can't be sliced
            // textually -- the chunk's clean-text contribution is a placeholder; the
            // real content is IR-level. Route the whole thing based on its midpoint's
            // enclosing member.
            if (chunk is not CSharpCodeIntermediateNode csharpChunk)
            {
                var midpoint = (shimStart + shimEnd) / 2;
                var coveringMember = FindCoveringMember(members, midpoint);
                var target = ComputeTarget(coveringMember, midpoint);
                result.Add(new RoutedChunk(chunk, target));
                continue;
            }

            var slices = SliceChunk(shimStart, shimEnd, members);

            // If every slice routes to the same target, emit the original chunk to
            // preserve source mapping.
            if (slices.Length == 1 && slices[0].Start == shimStart && slices[0].End == shimEnd)
            {
                result.Add(new RoutedChunk(chunk, slices[0].Target));
                continue;
            }

            // Multi-slice case: synthesize per-slice CSharpCode wrappers. Source
            // mapping is lost for each slice -- only happens when the parser bundled
            // text from multiple members or split a NeedsHelper member into prefix /
            // body / suffix in the same chunk.
            var fullText = ConcatTokenText(csharpChunk);
            foreach (var slice in slices)
            {
                var off = slice.Start - shimStart;
                var len = slice.End - slice.Start;
                if (len <= 0)
                {
                    continue;
                }
                var sliceText = fullText.Substring(off, len);
                var syntheticChunk = IntermediateNodeFactory.CSharpCode(sliceText);
                result.Add(new RoutedChunk(syntheticChunk, slice.Target));
            }
        }

        return result.ToImmutable();
    }

    /// <summary>
    /// Find the member covering a given shim position. Member spans are in source
    /// order and may abut (one member's End equals the next's Start); the first match
    /// wins.
    /// </summary>
    private static MemberClassification? FindCoveringMember(
        ImmutableArray<MemberClassification> members,
        int shimPosition)
    {
        foreach (var member in members)
        {
            if (member.ShimSpan.Start <= shimPosition && shimPosition < member.ShimSpan.End)
            {
                return member;
            }
        }
        return null;
    }

    /// <summary>
    /// Compute the routing target for a position within a chunk, given the member
    /// covering that position. NeedsHelper members distinguish positions inside a body
    /// span (NeedsHelperBody -- captured by the synth's wrapper) from positions outside
    /// (NeedsHelperOmit -- replaced by the synth's stub in decl). This is keyed purely on
    /// position, so markup (Template) chunks and CSharpCode chunks are treated alike: both
    /// carry a shim position, and a markup chunk whose position falls outside every body
    /// span (which only arises for invalid input) routes to Omit rather than mis-claiming
    /// a synth from the impl phase's pairing queue.
    /// </summary>
    private static ChunkTarget ComputeTarget(
        MemberClassification? member,
        int shimPosition)
    {
        if (member is null)
        {
            return ChunkTarget.DeclOnly;
        }
        switch (member.Kind)
        {
            case MemberKind.DeclOnly:
                return ChunkTarget.DeclOnly;
            case MemberKind.ImplOnly:
                return ChunkTarget.ImplOnly;
            case MemberKind.NeedsHelper:
                foreach (var body in member.BodyShimSpans)
                {
                    if (body.Start <= shimPosition && shimPosition < body.End)
                    {
                        return ChunkTarget.NeedsHelperBody;
                    }
                }
                return ChunkTarget.NeedsHelperOmit;
            default:
                return ChunkTarget.DeclOnly;
        }
    }

    /// <summary>
    /// Carve a CSharpCode chunk's shim-text range into per-target slices. Boundaries
    /// are taken from member starts/ends and NeedsHelper body starts/ends that fall
    /// inside the chunk; adjacent slices that share a target are collapsed.
    /// </summary>
    private static ImmutableArray<(int Start, int End, ChunkTarget Target)> SliceChunk(
        int chunkShimStart,
        int chunkShimEnd,
        ImmutableArray<MemberClassification> members)
    {
        var boundaries = new SortedSet<int> { chunkShimStart, chunkShimEnd };
        foreach (var member in members)
        {
            if (member.ShimSpan.Start > chunkShimStart && member.ShimSpan.Start < chunkShimEnd)
            {
                boundaries.Add(member.ShimSpan.Start);
            }
            if (member.ShimSpan.End > chunkShimStart && member.ShimSpan.End < chunkShimEnd)
            {
                boundaries.Add(member.ShimSpan.End);
            }
            if (member.Kind == MemberKind.NeedsHelper)
            {
                foreach (var body in member.BodyShimSpans)
                {
                    if (body.Start > chunkShimStart && body.Start < chunkShimEnd)
                    {
                        boundaries.Add(body.Start);
                    }
                    if (body.End > chunkShimStart && body.End < chunkShimEnd)
                    {
                        boundaries.Add(body.End);
                    }
                }
            }
        }

        var positions = boundaries.ToArray();
        using var result = new PooledArrayBuilder<(int Start, int End, ChunkTarget Target)>();
        ref var resultRef = ref result.AsRef();
        for (var i = 0; i < positions.Length - 1; i++)
        {
            var sliceStart = positions[i];
            var sliceEnd = positions[i + 1];
            if (sliceEnd <= sliceStart)
            {
                continue;
            }
            // Probe at the slice's midpoint so a boundary that exactly equals a span's
            // edge is unambiguous (member spans are half-open [start, end)).
            var probe = sliceStart + (sliceEnd - sliceStart) / 2;
            var covering = FindCoveringMember(members, probe);
            var target = ComputeTarget(covering, probe);

            // Collapse adjacent slices that share a target.
            if (resultRef.Count > 0)
            {
                var last = resultRef[resultRef.Count - 1];
                if (last.End == sliceStart && last.Target == target)
                {
                    resultRef[resultRef.Count - 1] = (last.Start, sliceEnd, target);
                    continue;
                }
            }
            resultRef.Add((sliceStart, sliceEnd, target));
        }
        return resultRef.ToImmutable();
    }

    private static string ConcatTokenText(CSharpCodeIntermediateNode chunk)
    {
        using var _ = StringBuilderPool.GetPooledObject(out var sb);
        foreach (var child in chunk.Children)
        {
            if (child is IntermediateToken token)
            {
                sb.Append(token.Content);
            }
        }
        return sb.ToString();
    }

    /// <summary>
    /// True if this child is user-authored @code content the splitter should classify
    /// (raw user C# text or any markup/expression node that lives at class scope inside
    /// an @code block). Returns false for things that aren't user @code: Razor-generated
    /// structured declarations (MethodDeclaration / FieldDeclaration / PropertyDeclaration
    /// / ClassDeclaration for nested helper classes), directive-introduced nodes
    /// (ComponentInjectIntermediateNode for @inject, etc.), and anything else the
    /// existing decl/impl routing already knows how to handle.
    /// </summary>
    /// <remarks>
    /// The splitter only routes chunks for which this returns true. A non-routable node
    /// that nonetheless emits a class-body member -- notably the @inject property node --
    /// is not represented in the resulting plan's RoutedChunks. Callers building the decl
    /// document must therefore preserve every primary-class child for which this returns
    /// false (excluding the render method and synthesized helpers), or those members are
    /// silently dropped from both halves of the partial class.
    /// </remarks>
    internal static bool IsRoutableUserCodeChunk(IntermediateNode child)
    {
        // Razor-generated structured declarations are already routed by the existing
        // decl/impl logic and shouldn't go through the chunker.
        if (child is MethodDeclarationIntermediateNode
            or FieldDeclarationIntermediateNode
            or PropertyDeclarationIntermediateNode
            or ClassDeclarationIntermediateNode)
        {
            return false;
        }

        // Directive-introduced surface nodes (@inject etc.) aren't part of user @code
        // text; they're surface metadata that other phases place into the class.
        var typeName = child.GetType().Name;
        if (typeName.StartsWith("Component", StringComparison.Ordinal) && typeName.EndsWith("DirectiveIntermediateNode", StringComparison.Ordinal))
        {
            return false;
        }
        if (typeName is "ComponentInjectIntermediateNode")
        {
            return false;
        }

        // Everything else -- CSharpCode, MarkupElement, MarkupBlock, HtmlContent,
        // CSharpExpression, UnresolvedElement, Component, Template, etc. -- represents
        // user @code content that the chunker should classify and route. Template nodes
        // in particular are produced by `@<p/>` expression-position markup (e.g. the
        // body of `[Parameter] RenderFragment X => @<p/>;`).
        return true;
    }
}

/// <summary>
/// Routing decision for a single IR chunk of user-@code content.
/// </summary>
internal enum ChunkTarget
{
    /// <summary>Emit only in decl (and omit from impl).</summary>
    DeclOnly,

    /// <summary>Emit only in impl (and omit from decl).</summary>
    ImplOnly,

    /// <summary>
    /// Belongs to a NeedsHelper member: omit from BOTH halves' verbatim emission.
    /// The decl half emits a synthesized stub in its place (from the plan's
    /// <see cref="ClassBodySplitPlan.HelperSynths"/>); the impl half emits a
    /// synthesized partial-method wrapper around the member's markup body.
    /// </summary>
    NeedsHelperOmit,

    /// <summary>
    /// The markup body of a NeedsHelper member: omit from decl, emit in impl wrapped
    /// in a synthesized partial method body so the existing impl-side lowering writer
    /// emits resolved markup calls inside the synth method.
    /// </summary>
    NeedsHelperBody,
}

/// <summary>
/// Description of a single helper-delegation rewrite for a surface property whose body
/// contains markup. Decl emits the replacement property (delegating to a synthesized
/// partial method) plus the partial-method declaration; impl emits the partial-method
/// definition wrapping the member's markup body.
/// </summary>
internal sealed record HelperSynth(
    string SynthMethodName,
    string SynthDeclSource,
    string SynthImplOpenSource,
    string SynthImplCloseSource);

/// <summary>
/// The output of <see cref="ClassBodySplitter.Split"/>: an ordered list of routing
/// decisions for every user-@code IR chunk in the primary class body, plus the helper-
/// delegation rewrites for any surface property with a markup-bearing body.
/// </summary>
/// <remarks>
/// <para>
/// Each entry in <see cref="RoutedChunks"/> is an (IntermediateNode, ChunkTarget) pair.
/// The chunk is either an original child of the primary class (when no splitting was
/// required) or a synthetic <see cref="CSharpCodeIntermediateNode"/> wrapping a slice of
/// a larger chunk's text (when the original spanned multiple members with different
/// routing decisions). Entries are in source order; consumers should emit them in that
/// order.
/// </para>
/// <para>
/// Children of <c>primaryClass</c> that are not part of the user @code surface -- the
/// render method, compiler-synthesized helpers (<see cref="IntermediateNode.IsSynthesizedHelper"/>),
/// and structured declarations the Razor IR built itself -- do NOT appear in
/// <see cref="RoutedChunks"/>; consumers continue handling those via the original
/// child iteration.
/// </para>
/// </remarks>
internal sealed class ClassBodySplitPlan
{
    public ImmutableArray<RoutedChunk> RoutedChunks { get; }

    /// <summary>
    /// Helper-delegation rewrites to emit in addition to the per-chunk routing. Each entry
    /// describes a surface property whose body contains markup: decl gets the synth stub,
    /// impl gets the wrapper open/close text bracketing the member's markup body.
    /// </summary>
    public ImmutableArray<HelperSynth> HelperSynths { get; }

    internal ClassBodySplitPlan(
        ImmutableArray<RoutedChunk> routedChunks,
        ImmutableArray<HelperSynth> helperSynths)
    {
        RoutedChunks = routedChunks;
        HelperSynths = helperSynths;
    }
}

/// <summary>
/// A single (chunk, target) routing entry from <see cref="ClassBodySplitPlan.RoutedChunks"/>.
/// </summary>
internal readonly record struct RoutedChunk(
    IntermediateNode Chunk,
    ChunkTarget Target);
