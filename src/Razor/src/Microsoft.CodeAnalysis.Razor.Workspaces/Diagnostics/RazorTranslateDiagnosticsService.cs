// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost;
using Microsoft.CodeAnalysis.Razor.DocumentMapping;
using Microsoft.CodeAnalysis.Razor.Logging;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.Diagnostics;

using RazorDiagnosticFactory = AspNetCore.Razor.Language.RazorDiagnosticFactory;
using SyntaxNode = AspNetCore.Razor.Language.Syntax.SyntaxNode;

/// <summary>
/// Contains several methods for mapping and filtering Razor and C# diagnostics. It allows for
/// translating code diagnostics from one representation into another, such as from C# to Razor.
/// </summary>
internal class RazorTranslateDiagnosticsService(IDocumentMappingService documentMappingService, ILoggerFactory loggerFactory)
{
    private readonly IDocumentMappingService _documentMappingService = documentMappingService;
    private readonly ILogger _logger = loggerFactory.GetOrCreateLogger<RazorTranslateDiagnosticsService>();

    /// <summary>
    ///  Translates code diagnostics from one representation into another.
    /// </summary>
    /// <param name="diagnosticKind">
    ///  The <see cref="RazorLanguageKind"/> of the <see cref="Diagnostic"/> objects
    ///  included in <paramref name="diagnostics"/>.
    /// </param>
    /// <param name="diagnostics">
    ///  An array of <see cref="Diagnostic"/> objects to translate.
    /// </param>
    /// <param name="documentSnapshot">
    ///  The <see cref="IDocumentSnapshot"/> for the code document associated with the diagnostics.
    /// </param>
    /// <param name="cancellationToken">A token that can be checked to cancel work.</param>
    /// <returns>An array of translated diagnostics</returns>
    internal async Task<LspDiagnostic[]> TranslateAsync(
        RazorLanguageKind diagnosticKind,
        LspDiagnostic[] diagnostics,
        IDocumentSnapshot documentSnapshot,
        CancellationToken cancellationToken)
    {
        var codeDocument = await documentSnapshot.GetGeneratedOutputAsync(cancellationToken).ConfigureAwait(false);

        var filteredDiagnostics = diagnosticKind == RazorLanguageKind.CSharp
            ? FilterCSharpDiagnostics(diagnostics, codeDocument)
            : FilterHTMLDiagnostics(diagnostics, codeDocument);
        if (filteredDiagnostics.Length == 0)
        {
            _logger.LogDebug($"No diagnostics remaining after filtering.");
            return [];
        }

        _logger.LogDebug($"{filteredDiagnostics.Length}/{diagnostics.Length} diagnostics remain after filtering {diagnosticKind}.");

        var mappedDiagnostics = MapDiagnostics(
            diagnosticKind,
            filteredDiagnostics,
            documentSnapshot,
            codeDocument);

        return mappedDiagnostics;
    }

    private static LspDiagnostic[] FilterHTMLDiagnostics(
        LspDiagnostic[] unmappedDiagnostics,
        RazorCodeDocument codeDocument)
    {
        var syntaxTree = codeDocument.GetRequiredTagHelperRewrittenSyntaxTree();
        var sourceText = codeDocument.Source.Text;

        using var _ = DictionaryPool<TextSpan, bool>.GetPooledObject(out var processedAttributes);

        var filteredDiagnostics = unmappedDiagnostics
            .Where(d =>
                !InRazorComment(d, sourceText, syntaxTree) &&
                !InCSharpLiteral(d, sourceText, syntaxTree) &&
                !InAttributeContainingCSharp(d, sourceText, syntaxTree, processedAttributes) &&
                !AppliesToTagHelperTagName(d, sourceText, syntaxTree) &&
                !ShouldFilterHtmlDiagnosticBasedOnErrorCode(d, sourceText, syntaxTree))
            .ToArray();

        return filteredDiagnostics;
    }

    internal LspDiagnostic[] MapDiagnostics(
        RazorLanguageKind languageKind,
        LspDiagnostic[] diagnostics,
        IDocumentSnapshot documentSnapshot,
        RazorCodeDocument codeDocument)
    {
        var projects = RazorDiagnosticHelper.GetProjectInformation(documentSnapshot);
        using var mappedDiagnostics = new PooledArrayBuilder<LspDiagnostic>();

        foreach (var diagnostic in diagnostics)
        {
            // C# requests don't map directly to where they are in the document.
            if (languageKind == RazorLanguageKind.CSharp)
            {
                if (!TryGetOriginalDiagnosticRange(diagnostic, codeDocument, out var originalRange))
                {
                    continue;
                }

                diagnostic.Range = originalRange;
            }

            if (diagnostic is VSDiagnostic vsDiagnostic)
            {
                // We're the ones reporting the diagnostic, and it shows up as coming from our filename (not the generated one), so
                // the project info should be consistent too
                vsDiagnostic.Projects = projects;
            }

            mappedDiagnostics.Add(diagnostic);
        }

        return mappedDiagnostics.ToArray();
    }

    private static bool InRazorComment(
        LspDiagnostic d,
        SourceText sourceText,
        RazorSyntaxTree syntaxTree)
    {
        // If the diagnostic is within a Razor comment block, we don't want to show it.
        // Razor comments are not part of the Html document, so diagnostics within them stem from misinterpretation
        // of the "~" and comments that are generated by the compiler.
        return d.Range is not null &&
            syntaxTree.Root.FindNode(sourceText.GetTextSpan(d.Range), getInnermostNodeForTie: true) is RazorCommentBlockSyntax;
    }

    private static bool InCSharpLiteral(
        LspDiagnostic d,
        SourceText sourceText,
        RazorSyntaxTree syntaxTree)
    {
        if (d.Range is null)
        {
            return false;
        }

        var owner = syntaxTree.Root.FindNode(sourceText.GetTextSpan(d.Range), getInnermostNodeForTie: true);
        if (IsCsharpKind(owner))
        {
            return true;
        }

        if (owner is CSharpImplicitExpressionSyntax implicitExpressionSyntax &&
            implicitExpressionSyntax.Body is CSharpImplicitExpressionBodySyntax bodySyntax &&
            bodySyntax.CSharpCode is CSharpCodeBlockSyntax codeBlock)
        {
            return codeBlock.Children.Count == 1
                && IsCsharpKind(codeBlock.Children[0]);
        }

        return false;

        static bool IsCsharpKind([NotNullWhen(true)] SyntaxNode? node)
            => node?.Kind is SyntaxKind.CSharpExpressionLiteral
                or SyntaxKind.CSharpStatementLiteral
                or SyntaxKind.CSharpEphemeralTextLiteral;
    }

    private static bool AppliesToTagHelperTagName(LspDiagnostic diagnostic, SourceText sourceText, RazorSyntaxTree syntaxTree)
    {
        // Goal of this method is to filter diagnostics that touch TagHelper tag names. Reason being is TagHelpers can output anything. Meaning
        // If you have a TagHelper like:
        //
        // <Input>
        // </Input>
        //
        // HTML would see this as an error because the input element can't have a body; however, a TagHelper could respect this in a totally valid
        // way.

        if (diagnostic.Range is null)
        {
            return false;
        }

        var owner = syntaxTree.FindInnermostNode(sourceText, diagnostic.Range.End);

        var startOrEndTag = owner?.FirstAncestorOrSelf<RazorSyntaxNode>(static n => n is MarkupTagHelperStartTagSyntax || n is MarkupTagHelperEndTagSyntax);
        if (startOrEndTag is null)
        {
            return false;
        }

        var tagName = startOrEndTag is MarkupTagHelperStartTagSyntax startTag ? startTag.Name : ((MarkupTagHelperEndTagSyntax)startOrEndTag).Name;
        var tagNameRange = tagName.GetRange(syntaxTree.Source);

        if (!tagNameRange.IntersectsOrTouches(diagnostic.Range))
        {
            // The diagnostic doesn't touch the tag name
            return false;
        }

        // Diagnostic is touching the start or end tag name range
        return true;
    }

    private static bool ShouldFilterHtmlDiagnosticBasedOnErrorCode(LspDiagnostic diagnostic, SourceText sourceText, RazorSyntaxTree syntaxTree)
    {
        if (!diagnostic.Code.HasValue)
        {
            return false;
        }

        diagnostic.Code.Value.TryGetSecond(out var str);

        return str switch
        {
            CSSErrorCodes.UnrecognizedBlockType => IsEscapedAtSign(diagnostic, sourceText),
            CSSErrorCodes.MissingOpeningBrace or
            CSSErrorCodes.MissingClassNameAfterDot or
            CSSErrorCodes.MissingSelectorAfterCombinator or
            CSSErrorCodes.MissingPropertyName or
            CSSErrorCodes.MissingPropertyValue or
            CSSErrorCodes.MissingSelectorBeforeCombinatorCode => IsAtCSharpTransitionInStyleBlock(diagnostic, sourceText, syntaxTree),
            HtmlErrorCodes.UnexpectedEndTagErrorCode => IsHtmlWithBangAndMatchingTags(diagnostic, sourceText, syntaxTree),
            HtmlErrorCodes.InvalidNestingErrorCode => IsAnyFilteredInvalidNestingError(diagnostic, sourceText, syntaxTree),
            HtmlErrorCodes.MissingEndTagErrorCode => syntaxTree.Options.FileKind.IsComponent(), // Redundant with RZ9980 in Components
            HtmlErrorCodes.TooFewElementsErrorCode => IsAnyFilteredTooFewElementsError(diagnostic, sourceText, syntaxTree),
            _ => false,
        };

        static bool IsEscapedAtSign(LspDiagnostic diagnostic, SourceText sourceText)
        {
            // Filters out "Unrecognized block type" errors in CSS, which occur with something like this:
            //
            // <style>
            //     @@font - face
            //     {
            //         // contents
            //     }
            // </style>
            //
            // The "@@" tells Razor that the user wants an "@" in the final Html, but the design time document
            // for the Html has to line up with the source Razor file, so that doesn't happen in the IDE. When
            // CSS gets the two "@"s, it raises the "Unrecognized block type" error.

            if (!sourceText.TryGetAbsoluteIndex(diagnostic.Range.Start, out var absoluteIndex))
            {
                return false;
            }

            // It's much easier to just check the source text directly, rather than try to understand all of the
            // possible shapes of the syntax tree here. We assume that since the diagnostics we're filtering out
            // came from the CSS server, it's a CSS block.
            return absoluteIndex > 0 &&
                sourceText[absoluteIndex] == '@' &&
                sourceText[absoluteIndex - 1] == '@';
        }

        static bool IsAtCSharpTransitionInStyleBlock(LspDiagnostic diagnostic, SourceText sourceText, RazorSyntaxTree syntaxTree)
        {
            if (!sourceText.TryGetAbsoluteIndex(diagnostic.Range.Start, out var absoluteIndex))
            {
                return false;
            }

            // Skip past non-newline whitespace to find the first interesting node
            while (sourceText[absoluteIndex] is ' ' or '\t')
            {
                absoluteIndex++;
                if (absoluteIndex == sourceText.Length)
                {
                    return false;
                }
            }

            var owner = syntaxTree.Root.FindInnermostNode(absoluteIndex);

            // If we're not at an @ to transition to C#, then we don't want to filter this diagnostic
            if (owner is not CSharpTransitionSyntax)
            {
                return false;
            }

            return owner.FirstAncestorOrSelf<BaseMarkupElementSyntax>(static n => n.StartTag?.Name.Content == "style") is not null;
        }

        // Ideally this would be solved instead by not emitting the "!" at the HTML backing file,
        // but we don't currently have a system to accomplish that
        static bool IsAnyFilteredTooFewElementsError(LspDiagnostic diagnostic, SourceText sourceText, RazorSyntaxTree syntaxTree)
        {
            var owner = syntaxTree.FindInnermostNode(sourceText, diagnostic.Range.Start);
            if (owner is null)
            {
                return false;
            }

            var element = owner.FirstAncestorOrSelf<MarkupElementSyntax>();
            if (element is null)
            {
                return false;
            }

            if (element.StartTag?.Name.Content != "html")
            {
                return false;
            }

            var bodyElement = element
                .ChildNodes()
                .OfType<MarkupElementSyntax>()
                .SingleOrDefault(static element => element.StartTag?.Name.Content == "body");

            return bodyElement is not null &&
                   bodyElement.StartTag?.Bang is not null;
        }

        // Ideally this would be solved instead by not emitting the "!" at the HTML backing file,
        // but we don't currently have a system to accomplish that
        static bool IsHtmlWithBangAndMatchingTags(LspDiagnostic diagnostic, SourceText sourceText, RazorSyntaxTree syntaxTree)
        {
            var owner = syntaxTree.FindInnermostNode(sourceText, diagnostic.Range.Start);
            if (owner is null)
            {
                return false;
            }

            var element = owner.FirstAncestorOrSelf<MarkupElementSyntax>();
            var startNode = element?.StartTag;
            var endNode = element?.EndTag;

            if (startNode is null || endNode is null)
            {
                // We only care about tags with a start and an end because we want to exclude diagnostics from their children
                return false;
            }

            var haveBang = startNode.Bang.IsValid() && endNode.Bang.IsValid();
            var namesEquivalent = startNode.Name.Content == endNode.Name.Content;

            return haveBang && namesEquivalent;
        }

        static bool IsAnyFilteredInvalidNestingError(LspDiagnostic diagnostic, SourceText sourceText, RazorSyntaxTree syntaxTree)
            => IsInvalidNestingWarningWithinComponent(diagnostic, sourceText, syntaxTree) ||
               IsInvalidNestingFromBody(diagnostic, sourceText, syntaxTree);

        static bool IsInvalidNestingWarningWithinComponent(LspDiagnostic diagnostic, SourceText sourceText, RazorSyntaxTree syntaxTree)
        {
            var owner = syntaxTree.FindInnermostNode(sourceText, diagnostic.Range.Start);
            if (owner is null)
            {
                return false;
            }

            var taghelperNode = owner.FirstAncestorOrSelf<MarkupTagHelperElementSyntax>();

            return taghelperNode is not null;
        }

        // Ideally this would be solved instead by not emitting the "!" at the HTML backing file,
        // but we don't currently have a system to accomplish that
        static bool IsInvalidNestingFromBody(LspDiagnostic diagnostic, SourceText sourceText, RazorSyntaxTree syntaxTree)
        {
            var owner = syntaxTree.FindInnermostNode(sourceText, diagnostic.Range.Start);
            if (owner is null)
            {
                return false;
            }

            var body = owner.FirstAncestorOrSelf<MarkupElementSyntax>(static n => n.StartTag?.Name.Content.Equals("body", StringComparison.Ordinal) == true);

            if (ReferenceEquals(body, owner))
            {
                return false;
            }

            if (diagnostic.Message is null)
            {
                return false;
            }

            return diagnostic.Message.EndsWith("cannot be nested inside element 'html'.") && body?.StartTag?.Bang is not null;
        }
    }

    private static bool InAttributeContainingCSharp(
        LspDiagnostic diagnostic,
        SourceText sourceText,
        RazorSyntaxTree syntaxTree,
        Dictionary<TextSpan, bool> processedAttributes)
    {
        // Examine the _end_ of the diagnostic to see if we're at the
        // start of an (im/ex)plicit expression. Looking at the start
        // of the diagnostic isn't sufficient.
        if (diagnostic.Range is null)
        {
            return false;
        }

        if (!sourceText.TryGetAbsoluteIndex(diagnostic.Range.End, out var absoluteIndex))
        {
            return false;
        }

        var owner = syntaxTree.Root.FindInnermostNode(absoluteIndex);
        if (owner is null)
        {
            return false;
        }

        // If the owner is the close quote of an attribute value, then we need to move to the previous position, as
        // the closing quote is actually owned by the whole attribute node. This will put us in the actual attribute
        // value node for sure, and as long as the diagnostic range isn't zero-width, shouldn't affect semantics.
        if (absoluteIndex > 0 &&
            diagnostic.Range.Start != diagnostic.Range.End &&
            owner is MarkupTextLiteralSyntax { LiteralTokens: [{ Content: "\"" or "'" }], Parent: MarkupTagHelperAttributeSyntax or MarkupAttributeBlockSyntax })
        {
            owner = syntaxTree.Root.FindInnermostNode(absoluteIndex - 1);
            if (owner is null)
            {
                return false;
            }
        }

        var markupAttributeValue = owner.FirstAncestorOrSelf<RazorSyntaxNode>(static n =>
            (n.Parent is MarkupAttributeBlockSyntax block && n == block.Value) ||
            n is MarkupTagHelperAttributeValueSyntax or MarkupMiscAttributeContentSyntax);

        if (markupAttributeValue is not null)
        {
            if (!processedAttributes.TryGetValue(markupAttributeValue.Span, out var shouldFilterDiagnostic))
            {
                // If a component attribute is spread across multiple lines, it's not valid Html so the Html server can't be expected to reason
                // about the contents correctly
                shouldFilterDiagnostic = markupAttributeValue is MarkupTagHelperAttributeValueSyntax &&
                    markupAttributeValue.GetLinePositionSpan(syntaxTree.Source).SpansMultipleLines();

                // Similarly, if the attribute value contains non-markup, the Html could report false positives
                shouldFilterDiagnostic |= CheckIfAttributeContainsNonMarkupNodes(markupAttributeValue);
                processedAttributes.Add(markupAttributeValue.Span, shouldFilterDiagnostic);
            }

            return shouldFilterDiagnostic;
        }

        return false;

        static bool CheckIfAttributeContainsNonMarkupNodes(RazorSyntaxNode attributeNode)
        {
            return attributeNode.DescendantNodes().Any(IsNotMarkupOrCommentNode);
        }

        static bool IsNotMarkupOrCommentNode(SyntaxNode node)
        {
            return !(node is
                MarkupBlockSyntax or
                MarkupSyntaxNode or
                GenericBlockSyntax or
                RazorCommentBlockSyntax);
        }
    }

    private LspDiagnostic[] FilterCSharpDiagnostics(LspDiagnostic[] diagnostics, RazorCodeDocument codeDocument)
    {
        using var filteredDiagnostics = new PooledArrayBuilder<LspDiagnostic>();

        foreach (var diagnostic in diagnostics)
        {
            if (diagnostic.Code is not { } code ||
                !code.TryGetSecond(out var str) ||
                str is null)
            {
                filteredDiagnostics.Add(diagnostic);
                continue;
            }

            if (str switch
            {
                "CS1525" => ShouldIgnoreCS1525(diagnostic, codeDocument),
                Constants.DiagnosticIds.IDE0005_gen => IsUsingDirectiveUsed(diagnostic, codeDocument),
                // This diagnostics is produced by Roslyn to help its Remove Usings code fixer, so is irrelevant to us
                Constants.DiagnosticIds.RemoveUnnecessaryImportsFixable => true,
                _ => false
            })
            {
                continue;
            }

            filteredDiagnostics.Add(diagnostic);
        }

        return filteredDiagnostics.ToArrayAndClear();

        bool ShouldIgnoreCS1525(LspDiagnostic diagnostic, RazorCodeDocument codeDocument)
        {
            if (CheckIfDocumentHasRazorDiagnostic(codeDocument, RazorDiagnosticFactory.TagHelper_EmptyBoundAttribute.Id) &&
                TryGetOriginalDiagnosticRange(diagnostic, codeDocument, out var originalRange) &&
                originalRange.IsUndefined())
            {
                // Empty attribute values will take the following form in the generated C# document:
                // __o = Microsoft.AspNetCore.Components.EventCallback.Factory.Create<Microsoft.AspNetCore.Components.Web.ProgressEventArgs>(this, );
                // The trailing `)` with no value preceding it, will lead to a C# error which doesn't make sense within the razor file.
                // The empty attribute value is not directly mappable to Razor, hence we check if the diagnostic has an undefined range.
                // Note; Error RZ2008 informs the user that the empty attribute value is not allowed.
                // https://github.com/dotnet/aspnetcore/issues/30480
                return true;
            }

            return false;
        }
    }

    private bool IsUsingDirectiveUsed(LspDiagnostic diagnostic, RazorCodeDocument codeDocument)
    {
        // In imports files, all usings are considered used
        if (codeDocument.IsImportsFile())
        {
            return true;
        }

        // In legacy files, using directives don't affect tag helper discovery so they're all "unused" to us.
        if (codeDocument.FileKind.IsLegacy())
        {
            return false;
        }

        // Roslyn reports any usings that aren't used by user code for us. Some of these usings might be
        // used for component tags though, which are always fully qualified by the Razor compiler, so we
        // have to check if the using was actually used by component binding, if so, we need to keep the
        // diagnostic. Conveniently, this means we don't need to worry about actually reporting our own
        // unused diagnostics, so it's worth it.
        var syntaxTree = codeDocument.GetRequiredTagHelperRewrittenSyntaxTree();
        if (TryGetOriginalDiagnosticRange(diagnostic, codeDocument, out var originalRange) &&
            syntaxTree.FindInnermostNode(codeDocument.Source.Text, originalRange.Start) is { Parent.Parent: RazorUsingDirectiveSyntax usingDirectiveSyntax })
        {
            return codeDocument.IsDirectiveUsed(usingDirectiveSyntax);
        }

        return true;
    }

    private static bool CheckIfDocumentHasRazorDiagnostic(RazorCodeDocument codeDocument, string razorDiagnosticCode)
    {
        return codeDocument.GetRequiredTagHelperRewrittenSyntaxTree().Diagnostics.Any(razorDiagnosticCode, static (d, code) => d.Id == code);
    }

    private bool TryGetOriginalDiagnosticRange(LspDiagnostic diagnostic, RazorCodeDocument codeDocument, [NotNullWhen(true)] out LspRange? originalRange)
    {
        if (!_documentMappingService.TryMapToRazorDocumentRange(
            codeDocument.GetRequiredCSharpDocument(),
            diagnostic.Range,
            MappingBehavior.Inferred,
            out originalRange))
        {
            // Couldn't remap the range correctly.
            // If this is error it's worth at least logging so we know if there's an issue
            // for mapping when a user reports not seeing an error they thought they should
            if (diagnostic.Severity == LspDiagnosticSeverity.Error)
            {
                _logger.LogWarning($"Dropping diagnostic {diagnostic.Code}:{diagnostic.Message} at csharp range {diagnostic.Range}");
            }

            return false;
        }

        return true;
    }
}
