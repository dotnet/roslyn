// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.SolutionCrawler;

internal abstract class AbstractUnitTestingDocumentDifferenceService : IUnitTestingDocumentDifferenceService
{
    public UnitTestingDocumentDifferenceResult? GetDifference(Document oldDocument, Document newDocument, CancellationToken cancellationToken)
    {
        try
        {
            var syntaxFactsService = newDocument.Project.Services.GetService<ISyntaxFactsService>();
            if (syntaxFactsService == null)
            {
                // somehow, we can't get the service. without it, there is nothing we can do.
                return new UnitTestingDocumentDifferenceResult(UnitTestingInvocationReasons.DocumentChanged);
            }
            // this is based on the implementation detail where opened documents use strong references
            // to tree and text rather than recoverable versions.
            if (!oldDocument.TryGetText(out var oldText) ||
                !newDocument.TryGetText(out var newText))
            {
                // no cheap way to determine top level changes. assumes top level has changed
                return new UnitTestingDocumentDifferenceResult(UnitTestingInvocationReasons.DocumentChanged);
            }
            // quick check whether two tree versions are same
            if (oldDocument.TryGetSyntaxVersion(out var oldVersion) &&
                newDocument.TryGetSyntaxVersion(out var newVersion) &&
                oldVersion.Equals(newVersion))
            {
                // nothing has changed. don't do anything.
                // this could happen if a document is opened/closed without any buffer change
                return null;
            }

            var range = newText.GetEncompassingTextChangeRange(oldText);
            if (range == default)
            {
                // nothing has changed. don't do anything
                return null;
            }

            return new UnitTestingDocumentDifferenceResult(UnitTestingInvocationReasons.DocumentChanged);
        }
        catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken))
        {
            throw ExceptionUtilities.Unreachable();
        }
    }
}
