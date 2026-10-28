// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.AspNetCore.Razor.Utilities;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.Formatting;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Razor.Telemetry;
using Microsoft.CodeAnalysis.Razor.Workspaces;
using Microsoft.CodeAnalysis.Razor.Workspaces.Settings;
using Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;
using Microsoft.CodeAnalysis.Text;
using RoslynSyntaxNode = Microsoft.CodeAnalysis.SyntaxNode;

namespace Microsoft.CodeAnalysis.Remote.Razor.DocumentMapping;

[Export(typeof(IRazorEditService)), Shared]
[method: ImportingConstructor]
internal sealed partial class RazorEditService(
    IDocumentMappingService documentMappingService,
    IClientSettingsManager clientSettingsManager,
    RemoteSnapshotManager snapshotManager,
    ITelemetryReporter telemetryReporter) : IRazorEditService
{
    private readonly record struct GeneratedDocumentChanges(ImmutableArray<RazorTextChange> Changes, SyntaxTree SyntaxTree, bool DeclarationDocument);

    private readonly IDocumentMappingService _documentMappingService = documentMappingService;
    private readonly IClientSettingsManager _clientSettingsManager = clientSettingsManager;
    private readonly RemoteSnapshotManager _snapshotManager = snapshotManager;
    private readonly ITelemetryReporter _telemetryReporter = telemetryReporter;

    public async Task<ImmutableArray<RazorTextChange>> MapCSharpEditsAsync(
        ImmutableArray<RazorTextChange> textChanges,
        RemoteDocumentSnapshot snapshot,
        bool declarationDocument,
        bool includeCSharpLanguageFeatureEdits,
        Func<RazorTextChange, bool>? directlyMappedEditFilter,
        CancellationToken cancellationToken)
    {
        var codeDocument = await snapshot.GetGeneratedOutputAsync(cancellationToken).ConfigureAwait(false);
        var originalCSharpSyntaxTree = await snapshot.GetCSharpSyntaxTreeAsync(declarationDocument, cancellationToken).ConfigureAwait(false);

        return await MapCSharpEditsAsync(
            snapshot.TextDocument,
            [new(textChanges, originalCSharpSyntaxTree, declarationDocument)],
            codeDocument,
            includeCSharpLanguageFeatureEdits,
            directlyMappedEditFilter,
            cancellationToken).ConfigureAwait(false);
    }

    private async Task<ImmutableArray<RazorTextChange>> MapCSharpEditsAsync(
        TextDocument razorDocument,
        ImmutableArray<GeneratedDocumentChanges> documentChanges,
        RazorCodeDocument codeDocument,
        bool includeCSharpLanguageFeatureEdits,
        Func<RazorTextChange, bool>? directlyMappedEditFilter,
        CancellationToken cancellationToken)
    {
        using var edits = new PooledArrayBuilder<RazorTextChange>();
        using var addedUsings = new PooledArrayBuilder<string>();
        using var removedUsings = new PooledArrayBuilder<string>();

        foreach (var (textChanges, originalCSharpSyntaxTree, declarationDocument) in documentChanges)
        {
            AddDirectlyMappedEdits(ref edits.AsRef(), declarationDocument, textChanges, codeDocument, directlyMappedEditFilter, cancellationToken, out var skippedEdits);
            if (!includeCSharpLanguageFeatureEdits || skippedEdits.Length == 0)
            {
                continue;
            }

            var originalCSharpSourceText = await originalCSharpSyntaxTree.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var originalCSharpSyntaxRoot = await originalCSharpSyntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);

            // This temporary C# text contains only skipped edits, not the complete requested change.
            // That's enough to find added or removed language features outside the mapped regions.
            // In particular, don't reintroduce edits rejected by the caller's directly-mapped edit filter.
            var newCSharpSourceText = originalCSharpSourceText.WithChanges(skippedEdits.Select(static c => c.ToTextChange()));
            var newCSharpSyntaxTree = originalCSharpSyntaxTree.WithChangedText(newCSharpSourceText);
            var newCSharpSyntaxRoot = await newCSharpSyntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);

            // Collect using changes from each generated document so they can be applied together below.
            var oldUsings = UsingDirectiveHelper.FindUsingDirectiveStrings(originalCSharpSyntaxRoot, originalCSharpSourceText);
            var newUsings = UsingDirectiveHelper.FindUsingDirectiveStrings(newCSharpSyntaxRoot, newCSharpSourceText);
            addedUsings.AddRange(Delta.Compute(oldUsings, newUsings));
            removedUsings.AddRange(Delta.Compute(newUsings, oldUsings));

            var clientSettings = _clientSettingsManager.GetClientSettings();
            var options = razorDocument.GetRazorFormattingOptions(clientSettings, cancellationToken);
            AddCSharpLanguageFeatureChanges(ref edits.AsRef(), codeDocument, originalCSharpSyntaxRoot, originalCSharpSourceText, newCSharpSyntaxRoot, newCSharpSourceText, options);
        }

        // The impl and decl documents can produce identical edits to a using directive. Remove
        // duplicates before applying them to its text, since SourceText.WithChanges rejects overlapping edits.
        var mappedEdits = NormalizeEdits(edits.ToImmutableOrderedByAndClear(static e => e.Span.Start), cancellationToken);
        edits.AddRange(mappedEdits);
        AddUsingsChanges(
            ref edits.AsRef(),
            codeDocument,
            addedUsings.ToImmutable().Distinct(),
            removedUsings.ToImmutable().Distinct(),
            cancellationToken);

        // Normalize again to account for the using-block replacements added above.
        return NormalizeEdits(edits.ToImmutableOrderedByAndClear(static e => e.Span.Start), cancellationToken);
    }

    private static void AddCSharpLanguageFeatureChanges(
        ref PooledArrayBuilder<RazorTextChange> edits,
        RazorCodeDocument codeDocument,
        RoslynSyntaxNode originalCSharpSyntaxRoot,
        SourceText originalCSharpSourceText,
        RoslynSyntaxNode newCSharpSyntaxRoot,
        SourceText newCSharpSourceText,
        RazorFormattingOptions options)
    {
        var oldMembers = FindMembers(originalCSharpSyntaxRoot, originalCSharpSourceText, codeDocument.FileKind);
        var newMembers = FindMembers(newCSharpSyntaxRoot, newCSharpSourceText, codeDocument.FileKind);
        var addedMembers = Delta.Compute(oldMembers, newMembers);

        AddMemberChanges(ref edits, codeDocument, addedMembers, options);
    }

    /// <summary>
    /// Go through edits and make sure a few things are true:
    ///
    /// <list type="number">
    /// <item>
    ///  No edit is added twice. This can happen if a rename happens.
    /// </item>
    /// <item>
    ///  No edit overlaps with another edit. If they do throw to capture logs but choose the first
    ///  edit to at least not completely fail. It's possible this will need to be tweaked later.
    /// </item>
    /// </list>
    /// </summary>
    private ImmutableArray<RazorTextChange> NormalizeEdits(ImmutableArray<RazorTextChange> changes, CancellationToken cancellationToken)
    {
        // Ensure that the changes are sorted by start position otherwise
        // the normalization logic will not work.
        Debug.Assert(changes.SequenceEqual(changes.OrderBy(static c => c.Span.Start)));

        using var normalizedChanges = new PooledArrayBuilder<RazorTextChange>(changes.Length);
        var remaining = changes.AsSpan();

        var droppedEdits = 0;
        while (remaining is not [])
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (remaining is [var edit, var nextEdit, ..])
            {
                var editSpan = edit.Span.ToTextSpan();
                var nextEditSpan = nextEdit.Span.ToTextSpan();

                if (editSpan == nextEditSpan)
                {
                    normalizedChanges.Add(nextEdit);
                    remaining = remaining[1..];

                    if (edit.NewText != nextEdit.NewText)
                    {
                        droppedEdits++;
                    }
                }
                else if (StrictlyContains(editSpan, nextEditSpan))
                {
                    // Cases where there was a removal and addition on the same
                    // line err to taking the addition. This can happen in the
                    // case of a namespace rename
                    if (editSpan.Start == nextEditSpan.Start)
                    {
                        if (string.IsNullOrEmpty(edit.NewText) && !string.IsNullOrEmpty(nextEdit.NewText))
                        {
                            // Don't count this as a dropped edit, it is expected
                            // in the case of a rename
                            normalizedChanges.Add(new RazorTextChange()
                            {
                                Span = edit.Span,
                                NewText = nextEdit.NewText
                            });
                            remaining = remaining[1..];
                        }
                        else
                        {
                            normalizedChanges.Add(edit);
                            remaining = remaining[1..];
                            droppedEdits++;
                        }
                    }
                    else
                    {
                        normalizedChanges.Add(edit);

                        remaining = remaining[1..];
                        droppedEdits++;
                    }
                }
                else if (StrictlyContains(nextEditSpan, editSpan))
                {
                    // Add the edit that is contained in the other edit
                    // and skip the next edit.
                    normalizedChanges.Add(nextEdit);
                    remaining = remaining[1..];
                    if (edit.NewText != nextEdit.NewText)
                    {
                        droppedEdits++;
                    }
                }
                else
                {
                    normalizedChanges.Add(edit);
                }
            }
            else
            {
                normalizedChanges.Add(remaining[0]);
            }

            remaining = remaining[1..];
        }

        if (droppedEdits > 0)
        {
            _telemetryReporter.ReportFault(
                new DroppedEditsException(),
                "Potentially dropped edits when trying to map",
                new Property("droppedEditCount", droppedEdits));
        }

        if (normalizedChanges.Count == changes.Length)
        {
            return changes;
        }

        return normalizedChanges.ToImmutable();
    }

    /// <summary>
    /// Checks whether <paramref name="outer"/> truly contains <paramref name="inner"/>,
    /// excluding the case where <paramref name="inner"/> is a zero-width insertion that sits
    /// exactly at the end of <paramref name="outer"/>. Roslyn's <see cref="TextSpan.Contains(TextSpan)"/>
    /// treats an empty span at the end boundary as contained (e.g. [24,24) inside [23,24)),
    /// but for edit normalization purposes those spans are adjacent, not overlapping, and both
    /// edits should be preserved.
    /// </summary>
    private static bool StrictlyContains(TextSpan outer, TextSpan inner)
        => outer.Contains(inner) && !(inner.IsEmpty && inner.Start == outer.End);

    private RazorTextChange? TryGetMappedEdit(
        RazorCSharpDocument csharpDocument,
        RazorTextChange change)
    {
        var spanStart = change.Span.Start;
        var spanEnd = spanStart + change.Span.Length;
        var newText = change.NewText ?? "";

        var csharpSourceText = csharpDocument.Text;

        // Deliberately doing a naive check to avoid telemetry for truly bad data
        if (spanStart <= 0 || spanStart >= csharpSourceText.Length || spanEnd <= 0 || spanEnd >= csharpSourceText.Length)
        {
            return null;
        }

        var startLine = csharpSourceText.Lines.GetLineFromPosition(spanStart).LineNumber;
        var endLine = csharpSourceText.Lines.GetLineFromPosition(spanEnd).LineNumber;

        var mappedStart = _documentMappingService.TryMapToRazorDocumentPosition(csharpDocument, spanStart, out _, out var hostStartIndex);
        var mappedEnd = _documentMappingService.TryMapToRazorDocumentPosition(csharpDocument, spanEnd, out _, out var hostEndIndex);

        // Ideal case, both start and end can be mapped so just return a mapped edit
        if (mappedStart && mappedEnd)
        {
            return new RazorTextChange()
            {
                Span = RazorTextSpan.FromBounds(hostStartIndex, hostEndIndex),
                NewText = newText
            };
        }

        // The opposite case of the above: for the last line of a code block, the C# formatter might
        // return an edit that starts within our mapping, but ends after. In those cases, when the edit
        // spans multiple lines we just take the first line and try to use that.
        // For example given `@{ var x= 4; }`, running the "Remove unused variable" code action will remove the whole
        // line, so the end position won't map.
        if (mappedStart && !mappedEnd && startLine != endLine)
        {
            // Construct a theoretical edit that is just for the first line of the edit that the C# formatter
            // gave us, and see if we can map that.
            if (!csharpSourceText.TryGetAbsoluteIndex(startLine, csharpSourceText.Lines[startLine].Span.Length, out var endIndex))
            {
                return null;
            }

            if (_documentMappingService.TryMapToRazorDocumentPosition(csharpDocument, endIndex, out _, out hostEndIndex))
            {
                // If there's a newline in the new text, only take the part before it
                var firstNewLine = newText.IndexOfAny(['\n', '\r']);
                return new RazorTextChange()
                {
                    Span = RazorTextSpan.FromBounds(hostStartIndex, hostEndIndex),
                    NewText = firstNewLine >= 0
                        ? newText[..firstNewLine]
                        : newText
                };
            }
        }

        return null;
    }

    /// <summary>
    /// Map edits directly to the Razor document, including identifier edits within using aliases.
    /// Edits that don't map are skipped, and other using directive changes are handled separately
    /// by <see cref="AddUsingsChanges"/>. The original unmappable C# edits are returned unchanged via
    /// <paramref name="skippedEdits"/>.
    /// </summary>
    private void AddDirectlyMappedEdits(
        ref PooledArrayBuilder<RazorTextChange> edits,
        bool declarationDocument,
        ImmutableArray<RazorTextChange> csharpEdits,
        RazorCodeDocument codeDocument,
        Func<RazorTextChange, bool>? directlyMappedEditFilter,
        CancellationToken cancellationToken,
        out ImmutableArray<RazorTextChange> skippedEdits)
    {
        var root = codeDocument.GetRequiredSyntaxRoot();
        var csharpDocument = codeDocument.GetRequiredCSharpDocument(declarationDocument);
        using var skipped = new PooledArrayBuilder<RazorTextChange>();

        foreach (var csharpEdit in csharpEdits)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // First try to map the edit directly from the generated C# document to the Razor document, as that means it can be applied
            // directly. There is some special handling in here for edits where only one end can be mapped, but in general if we can't
            // directly map the edit then we skip it and handle it later with more complex processing.
            if (TryGetMappedEdit(csharpDocument, csharpEdit) is not { } mappedEdit)
            {
                skipped.Add(csharpEdit);
                continue;
            }

            if (directlyMappedEditFilter?.Invoke(mappedEdit) == false)
            {
                continue;
            }

            var mappedSpan = mappedEdit.Span.ToTextSpan();
            var node = root.FindNode(mappedSpan, getInnermostNodeForTie: true);
            if (node is null)
            {
                skipped.Add(csharpEdit);
                continue;
            }

            if (RazorSyntaxFacts.IsInUsingDirective(node))
            {
                // Normally, skip using edits here so AddUsingsChanges can compare the old and new C# imports
                // and add or remove the corresponding Razor @using directives. Mapping the raw C# edits could
                // insert C# syntax such as semicolons, or remove only the mapped text and leave a stray '@'.
                //
                // In an @using alias, map single-identifier edits directly, whether they change the alias
                // name or a namespace/type identifier in its target. This keeps edits from both generated
                // documents tied to their original Razor spans before the using block is rendered, and
                // preserves formatting and unrelated directives for identifier-only renames.
                var isIdentifierEditInUsingAlias =
                    node.FirstAncestorOrSelf<CSharpStatementLiteralSyntax>()?.ChunkGenerator is AddImportChunkGenerator import &&
                    import.ParsedNamespace.Contains('=') &&
                    CSharp.SyntaxFacts.IsValidIdentifier(mappedEdit.NewText) &&
                    CSharp.SyntaxFacts.IsValidIdentifier(codeDocument.Source.Text.ToString(mappedSpan));
                if (!isIdentifierEditInUsingAlias)
                {
                    skipped.Add(csharpEdit);
                    continue;
                }
            }

            edits.Add(mappedEdit);

            if (node is BaseMarkupStartTagSyntax startTagSyntax &&
                startTagSyntax.GetEndTag() is { } endTag)
            {
                // We are changing a start tag, and so we have a matching end tag. We have to translate the edit over there too
                // as we only map the start tag, but if they got out of sync that would be bad.
                edits.Add(new RazorTextChange()
                {
                    Span = new RazorTextSpan()
                    {
                        Start = mappedSpan.Start + (endTag.Name.SpanStart - startTagSyntax.Name.SpanStart),
                        Length = mappedSpan.Length
                    },
                    NewText = mappedEdit.NewText
                });
            }
        }

        skippedEdits = skipped.ToImmutable();
    }

    private sealed class DroppedEditsException : Exception
    {
    }

    internal TestAccessor GetTestAccessor() => new(this);

    internal readonly struct TestAccessor(RazorEditService instance)
    {
        public Task<ImmutableArray<RazorTextChange>> MapCSharpEditsAsync(
            TextDocument razorDocument,
            ImmutableArray<RazorTextChange> textChanges,
            RazorCodeDocument codeDocument,
            SyntaxTree originalCSharpSyntaxTree,
            bool declarationDocument,
            bool includeCSharpLanguageFeatureEdits,
            Func<RazorTextChange, bool>? directlyMappedEditFilter,
            CancellationToken cancellationToken)
        {
            return instance.MapCSharpEditsAsync(
                razorDocument,
                [new(textChanges, originalCSharpSyntaxTree, declarationDocument)],
                codeDocument,
                includeCSharpLanguageFeatureEdits,
                directlyMappedEditFilter,
                cancellationToken);
        }
    }
}
