// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language;
using System;
using System.Diagnostics;
using System.Threading;

namespace Microsoft.NET.Sdk.Razor.SourceGenerators;

internal sealed class SourceGeneratorProjectEngine
{
    private readonly RazorProjectEngine _projectEngine;

    private readonly IRazorEnginePhase _discoveryPhase;
    private readonly int _discoveryPhaseIndex = -1;
    private readonly int _rewritePhaseIndex = -1;

    private ReadOnlySpan<IRazorEnginePhase> Phases => _projectEngine.Engine.Phases.AsSpan();

    public SourceGeneratorProjectEngine(RazorProjectEngine projectEngine)
    {
        _projectEngine = projectEngine;

        var index = 0;

        foreach (var phase in Phases)
        {
            if (_discoveryPhaseIndex >= 0 && _rewritePhaseIndex >= 0)
            {
                break;
            }

            switch (phase)
            {
                case DefaultRazorTagHelperContextDiscoveryPhase:
                    _discoveryPhase = phase;
                    _discoveryPhaseIndex = index;
                    break;

                case DefaultRazorTagHelperRewritePhase:
                    _rewritePhaseIndex = index;
                    break;
            }

            index++;
        }

        Debug.Assert(_discoveryPhase is not null);
        Debug.Assert(_discoveryPhaseIndex >= 0);
        Debug.Assert(_rewritePhaseIndex >= 0);
        Debug.Assert(_discoveryPhaseIndex < _rewritePhaseIndex);
    }

    public SourceGeneratorRazorCodeDocument ProcessInitialParse(RazorProjectItem projectItem, CancellationToken cancellationToken)
    {
        var codeDocument = _projectEngine.CreateCodeDocument(projectItem);

        codeDocument = ExecutePhases(Phases[.._discoveryPhaseIndex], codeDocument, cancellationToken);

        // By this point, DefaultRazorParsingPhase has set the canonical syntax tree (_syntaxTree)
        // so that discovery and subsequent phases can read it via GetSyntaxTree().
        return new SourceGeneratorRazorCodeDocument(codeDocument);
    }

    public SourceGeneratorRazorCodeDocument ProcessTagHelpers(
        SourceGeneratorRazorCodeDocument sgDocument, 
        TagHelperCollection tagHelpers, 
        bool checkForIdempotency, 
        CancellationToken cancellationToken)
    {
        Debug.Assert(sgDocument.CodeDocument.GetSyntaxTree() is not null);

        int startIndex = _discoveryPhaseIndex;
        var codeDocument = sgDocument.CodeDocument;

        if (checkForIdempotency && codeDocument.TryGetTagHelpers(out var previousTagHelpers))
        {
            // compare the tag helpers with the ones the document last used
            if (tagHelpers.Equals(previousTagHelpers))
            {
                // tag helpers are the same, nothing to do!
                return sgDocument;
            }

            // tag helpers have changed, figure out if we need to re-write
            var previousTagHelpersInScope = codeDocument.GetRequiredTagHelperContext().TagHelpers;
            var previousUsedTagHelpers = codeDocument.GetRequiredReferencedTagHelpers();

            // re-run discovery to figure out which tag helpers are now in scope for this document
            codeDocument = codeDocument.WithTagHelpers(tagHelpers);
            codeDocument = _discoveryPhase.Execute(codeDocument, cancellationToken);

            var newTagHelpersInScope = codeDocument.GetRequiredTagHelperContext().TagHelpers;

            // Check if any new tag helpers were added or ones we previously used were removed
            if (!RequiresRewrite(newTagHelpersInScope, previousTagHelpersInScope, previousUsedTagHelpers))
            {
                // No newly visible tag helpers, and any that got removed weren't used by this document anyway
                return sgDocument;
            }

            // We need to re-process the document with the updated tag helpers, but can skip discovery
            // as we just performed it. We must re-run lowering (not just rewrite) because the resolution
            // phase resolves unresolved nodes based on tag helpers and mutates the IR in place.
            startIndex = _discoveryPhaseIndex + 1;
        }
        else
        {
            codeDocument = codeDocument.WithTagHelpers(tagHelpers);
        }

        codeDocument = ExecutePhases(Phases[startIndex..(_rewritePhaseIndex + 1)], codeDocument, cancellationToken);

        return new SourceGeneratorRazorCodeDocument(codeDocument);
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

    public SourceGeneratorRazorCodeDocument ProcessRemaining(SourceGeneratorRazorCodeDocument sgDocument, CancellationToken cancellationToken)
    {
        var codeDocument = sgDocument.CodeDocument;
        Debug.Assert(codeDocument.GetReferencedTagHelpers() is not null);

        codeDocument = ExecutePhases(Phases[(_rewritePhaseIndex + 1)..], codeDocument, cancellationToken);

        return new SourceGeneratorRazorCodeDocument(codeDocument);
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