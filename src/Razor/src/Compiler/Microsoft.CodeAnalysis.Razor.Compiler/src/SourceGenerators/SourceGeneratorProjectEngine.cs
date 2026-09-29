// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Microsoft.CodeAnalysis.Razor.Compiler.CSharp;
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

        return new SourceGeneratorRazorCodeDocument(codeDocument);
    }

    /// <summary>
    ///  Runs tag-helper discovery, resolution and rewrite for a document. The generator calls this twice per
    ///  document: first with <paramref name="checkForIdempotency"/> <see langword="false"/> to do the initial
    ///  pass, then again with <see langword="true"/>. The second call is an incremental gate -- its inputs
    ///  include the project-wide tag-helper set, so it re-runs whenever that set changes, but it returns the
    ///  document unchanged unless the change actually affects this document (a used descriptor was added or
    ///  removed). Only then does it replay resolution. The first call's inputs deliberately exclude the
    ///  tag-helper set, so it re-runs only when the document itself changes.
    /// </summary>
    public SourceGeneratorRazorCodeDocument ProcessTagHelpers(
        SourceGeneratorRazorCodeDocument sgDocument, 
        TagHelperCollection tagHelpers, 
        bool checkForIdempotency, 
        CancellationToken cancellationToken)
    {
        Debug.Assert(sgDocument.CodeDocument.GetSyntaxTree() is not null);

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

            codeDocument = ResolveTagHelpers(codeDocument);

            return new SourceGeneratorRazorCodeDocument(codeDocument);
        }

        codeDocument = codeDocument.WithTagHelpers(tagHelpers);
        codeDocument = _discoveryPhase.Execute(codeDocument, cancellationToken);
        codeDocument = ResolveTagHelpers(codeDocument);

        return new SourceGeneratorRazorCodeDocument(codeDocument);

        RazorCodeDocument ResolveTagHelpers(RazorCodeDocument codeDocument)
        {
            // Capture the unresolved node on the first pass (discovery doesn't mutate it), then resolve a
            // clone of it every time. Resolution binds tag helpers by mutating the IR in place, so cloning
            // keeps the stored node pristine for later replays -- which start from it instead of re-lowering.
            var unresolvedDocumentNode = codeDocument.GetUnresolvedDocumentNode();
            if (unresolvedDocumentNode is null)
            {
                unresolvedDocumentNode = codeDocument.GetRequiredDocumentNode();
                codeDocument = codeDocument.WithUnresolvedDocumentNode(unresolvedDocumentNode);
            }

            codeDocument = codeDocument.WithDocumentNode((DocumentIntermediateNode)unresolvedDocumentNode.Clone());
            return ExecutePhases(Phases[(_discoveryPhaseIndex + 1)..(_rewritePhaseIndex + 1)], codeDocument, cancellationToken);
        }
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