// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.BraceCompletion;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.AutomaticCompletion;

internal abstract class AbstractBraceCompletionServiceFactory : IBraceCompletionServiceFactory
{
    private readonly ImmutableArray<IBraceCompletionService> _braceCompletionServices;

    protected AbstractBraceCompletionServiceFactory(
        IEnumerable<Lazy<IBraceCompletionService, LanguageMetadata>> braceCompletionServices,
        string languageName)
    {
        _braceCompletionServices = braceCompletionServices.Where(s => s.Metadata.Language == languageName).SelectAsArray(s => s.Value);
    }

    public IBraceCompletionService? TryGetService(ParsedDocument document, int openingPosition, char openingBrace, CancellationToken cancellationToken)
    {
        foreach (var service in _braceCompletionServices)
        {
            if (service.CanProvideBraceCompletion(openingBrace, openingPosition, document, cancellationToken))
            {
                return service;
            }
        }

        return null;
    }
}
