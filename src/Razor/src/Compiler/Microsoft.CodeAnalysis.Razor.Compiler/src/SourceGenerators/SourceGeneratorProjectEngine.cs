// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor.Compiler.CSharp;
using System;
using System.Diagnostics;
using System.Threading;

namespace Microsoft.NET.Sdk.Razor.SourceGenerators;

internal sealed class SourceGeneratorProjectEngine
{
    private readonly RazorProjectEngine _projectEngine;

    // Phase index boundaries. Identified by explicit phase type lookup at construction time so the
    // SG layer doesn't bake in implicit assumptions about pipeline order (the engine may add or
    // reorder phases as it evolves).

    /// <summary>Inclusive index of <see cref="DefaultRazorDeclCSharpLoweringPhase"/>. Phases at or
    /// before this index produce the unresolved IR and the decl C# text; they are tag-helper-
    /// independent.</summary>
    private readonly int _declLoweringEndIndex = -1;

    /// <summary>Index of <see cref="DefaultRazorTagHelperContextDiscoveryPhase"/>. Cached so the
    /// idempotency check can re-run discovery in isolation without re-lowering.</summary>
    private readonly int _discoveryPhaseIndex = -1;

    /// <summary>Inclusive index of <see cref="DefaultRazorTagHelperRewritePhase"/>. Phases after
    /// this index (optimization + impl C# lowering) consume the resolved IR.</summary>
    private readonly int _tagHelperRewriteEndIndex = -1;

    private readonly IRazorEnginePhase _discoveryPhase;

    private ReadOnlySpan<IRazorEnginePhase> Phases => _projectEngine.Engine.Phases.AsSpan();

    public SourceGeneratorProjectEngine(RazorProjectEngine projectEngine)
    {
        _projectEngine = projectEngine;

        var index = 0;

        foreach (var phase in Phases)
        {
            switch (phase)
            {
                case DefaultRazorDeclCSharpLoweringPhase:
                    _declLoweringEndIndex = index;
                    break;

                case DefaultRazorTagHelperContextDiscoveryPhase:
                    _discoveryPhase = phase;
                    _discoveryPhaseIndex = index;
                    break;

                case DefaultRazorTagHelperRewritePhase:
                    _tagHelperRewriteEndIndex = index;
                    break;
            }

            index++;
        }

        Debug.Assert(_declLoweringEndIndex >= 0);
        Debug.Assert(_discoveryPhase is not null);
        Debug.Assert(_discoveryPhaseIndex >= 0);
        Debug.Assert(_tagHelperRewriteEndIndex >= 0);
        Debug.Assert(_declLoweringEndIndex < _discoveryPhaseIndex);
        Debug.Assert(_discoveryPhaseIndex < _tagHelperRewriteEndIndex);
    }

    /// <summary>
    /// Runs the engine pipeline from parsing through decl C# lowering. After this call, the document
    /// has unresolved IR (tag helper nodes are still represented as
    /// <see cref="Microsoft.AspNetCore.Razor.Language.Intermediate.UnresolvedElementIntermediateNode"/>
    /// and friends) and a fully-written decl <see cref="RazorCSharpDocument"/> accessible via
    /// <see cref="RazorCodeDocumentExtensions.GetDeclCSharpDocument"/>.
    /// </summary>
    /// <remarks>
    /// The returned wrapper carries the source <see cref="RazorProjectItem"/> so that
    /// <see cref="ProcessTagHelpers"/> can rebuild an unresolved IR when tag helpers have changed
    /// materially. See <see cref="SourceGeneratorRazorCodeDocument.SourceItem"/> for the rationale.
    /// </remarks>
    public SourceGeneratorRazorCodeDocument ProcessForDecl(RazorProjectItem projectItem, CancellationToken cancellationToken)
    {
        var codeDocument = CreateAndProcessForDecl(projectItem, cancellationToken);
        return new SourceGeneratorRazorCodeDocument(codeDocument, projectItem);
    }

    private RazorCodeDocument CreateAndProcessForDecl(RazorProjectItem projectItem, CancellationToken cancellationToken)
    {
        var codeDocument = _projectEngine.CreateCodeDocument(projectItem);
        return ExecutePhases(Phases[..(_declLoweringEndIndex + 1)], codeDocument, cancellationToken);
    }

    public SourceGeneratorRazorCodeDocument ProcessTagHelpers(
        SourceGeneratorRazorCodeDocument sgDocument,
        TagHelperCollection tagHelpers,
        bool checkForIdempotency,
        CancellationToken cancellationToken)
    {
        Debug.Assert(sgDocument.CodeDocument.GetSyntaxTree() is not null);

        var codeDocument = sgDocument.CodeDocument;
        var sourceItem = sgDocument.SourceItem;

        if (checkForIdempotency && codeDocument.TryGetTagHelpers(out var previousTagHelpers))
        {
            // Same tag helpers as last time -- nothing changed for this document.
            if (tagHelpers.Equals(previousTagHelpers))
            {
                return sgDocument;
            }

            // Tag helpers changed. Run discovery in isolation to see if the change is material (i.e.
            // if it affects which tag helpers are in scope or which are referenced by this document).
            var previousTagHelpersInScope = codeDocument.GetRequiredTagHelperContext().TagHelpers;
            var previousUsedTagHelpers = codeDocument.GetRequiredReferencedTagHelpers();

            codeDocument = codeDocument.WithTagHelpers(tagHelpers);
            codeDocument = _discoveryPhase.Execute(codeDocument, cancellationToken);

            var newTagHelpersInScope = codeDocument.GetRequiredTagHelperContext().TagHelpers;

            if (!RequiresRewrite(newTagHelpersInScope, previousTagHelpersInScope, previousUsedTagHelpers))
            {
                // Visible tag-helper set didn't change in a way that affects this document.
                return sgDocument;
            }

            // Material change. The IR on the cached codeDocument has already been resolved+rewritten
            // by an earlier ProcessTagHelpers call (or the first ProcessRemaining); we can't re-run
            // resolution against it because the unresolved nodes are gone. Rebuild the unresolved IR
            // by replaying phases 0..decl-lowering from the cached source item, then run
            // discovery+resolution+rewrite against that fresh document.
            Debug.Assert(sourceItem is not null,
                "Cached codeDocument carries previousTagHelpers, which means ProcessForDecl produced it; that should always attach a source item.");

            codeDocument = CreateAndProcessForDecl(sourceItem, cancellationToken);
            codeDocument = codeDocument.WithTagHelpers(tagHelpers);
            codeDocument = ExecutePhases(Phases[_discoveryPhaseIndex..(_tagHelperRewriteEndIndex + 1)], codeDocument, cancellationToken);
        }
        else
        {
            // First run after ProcessForDecl, or caller opted out of idempotency. Run discovery
            // through rewrite against the current document; the IR is still unresolved here.
            codeDocument = codeDocument.WithTagHelpers(tagHelpers);
            codeDocument = ExecutePhases(Phases[_discoveryPhaseIndex..(_tagHelperRewriteEndIndex + 1)], codeDocument, cancellationToken);
        }

        return new SourceGeneratorRazorCodeDocument(codeDocument, sourceItem);
    }

    private static bool RequiresRewrite(
        TagHelperCollection newTagHelpers,
        TagHelperCollection previousTagHelpers,
        TagHelperCollection previousUsedTagHelpers)
    {
        // Check if any new tag helpers were added (that weren't in scope before)
        // Check if any previously used tag helpers were removed (no longer in scope)
        return HasAnyNotIn(newTagHelpers, previousTagHelpers) ||
               HasAnyNotIn(previousUsedTagHelpers, newTagHelpers);
    }

    /// <summary>
    ///  Determines whether the first collection contains any tag helper descriptors that are not present
    ///  in the second collection.
    /// </summary>
    /// <param name="first">The collection to check for unique items.</param>
    /// <param name="second">The collection to compare against.</param>
    /// <returns>
    ///  <see langword="true"/> if <paramref name="first"/> contains any descriptors not present in 
    ///  <paramref name="second"/>; otherwise, <see langword="false"/>.
    /// </returns>
    private static bool HasAnyNotIn(TagHelperCollection first, TagHelperCollection second)
    {
        if (first.IsEmpty)
        {
            return false;
        }

        if (second.IsEmpty)
        {
            return true;
        }

        if (first.Equals(second))
        {
            return false;
        }

        // For each item in the first collection, check if it exists in the second collection
        foreach (var item in first)
        {
            if (!second.Contains(item))
            {
                return true;
            }
        }

        return false;
    }

    public SourceGeneratorRazorCodeDocument ProcessRemaining(SourceGeneratorRazorCodeDocument sgDocument, DefaultUtf8WriteLiteralFeature.Utf8SupportMap utf8SupportMap, CancellationToken cancellationToken)
    {
        var codeDocument = sgDocument.CodeDocument;
        Debug.Assert(codeDocument.GetReferencedTagHelpers() is not null);

        if (_projectEngine.Engine.TryGetFeature<IUtf8WriteLiteralFeature>(out var feature) &&
            feature is DefaultUtf8WriteLiteralFeature defaultFeature)
        {
            defaultFeature.SupportMap = utf8SupportMap;
        }

        codeDocument = ExecutePhases(Phases[(_tagHelperRewriteEndIndex + 1)..], codeDocument, cancellationToken);

        return new SourceGeneratorRazorCodeDocument(codeDocument, sgDocument.SourceItem);
    }

    private static RazorCodeDocument ExecutePhases(ReadOnlySpan<IRazorEnginePhase> phases, RazorCodeDocument codeDocument, CancellationToken cancellationToken)
    {
        var currentDocument = codeDocument;
        foreach (var phase in phases)
        {
            currentDocument = phase.Execute(currentDocument, cancellationToken);
        }
        return currentDocument;
    }
}