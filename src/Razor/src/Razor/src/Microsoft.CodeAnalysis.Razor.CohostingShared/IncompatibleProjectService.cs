// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using Microsoft.AspNetCore.Razor;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.Cohost;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

[Export(typeof(IIncompatibleProjectService))]
[method: ImportingConstructor]
internal sealed class IncompatibleProjectService(IIncompatibleProjectNotifier incompatibleProjectNotifier) : IIncompatibleProjectService
{
    private readonly IIncompatibleProjectNotifier _incompatibleProjectNotifier = incompatibleProjectNotifier;

    private ImmutableHashSet<ProjectId> _incompatibleProjectIds = [];

    public void HandleMissingDocument(RazorTextDocumentIdentifier? textDocumentIdentifier, RazorCohostRequestContext context)
    {
        if (context.Solution is null)
        {
            // If the solution is null, we have no idea what is going on, so err on the side of ignoring this request
            // and not annoying the user.
            return;
        }

        if (textDocumentIdentifier is not { Uri: { } uri })
        {
            // Can't do anything without a uri
            return;
        }

        // We know that the textDocumentIdentifier doesn't map to a document in the solution, or we wouldn't be here,
        // but we don't want to notify the user for each file, so we try to find the project that contains the file
        // through other means.

        var filePath = uri.GetDocumentFilePath();
        var filePathSpan = filePath.AsSpan();
        foreach (var project in context.Solution.Projects)
        {
            if (project.FilePath is null)
            {
                continue;
            }

            if (filePathSpan.StartsWith(PathUtilities.GetDirectoryName(project.FilePath.AsSpan()), PathUtilities.OSSpecificPathComparison))
            {
                if (ImmutableInterlocked.Update(ref _incompatibleProjectIds, static (set, id) => set.Add(id), project.Id))
                {
                    _incompatibleProjectNotifier.NotifyMissingDocument(project, filePath);
                }

                break;
            }
        }

        // If we couldn't find a candidate project, then this could be a misc file or linked file from somewhere, but we'll err on the side of not reporting
        // it. In future we could consider a separate hashset for these, so we report once per file.
    }
}
