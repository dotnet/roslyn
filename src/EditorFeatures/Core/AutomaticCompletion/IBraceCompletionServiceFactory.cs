﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.BraceCompletion;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Editor.Implementation.AutomaticCompletion
{
    internal interface IBraceCompletionServiceFactory : ILanguageService
    {
        Task<IBraceCompletionService?> TryGetServiceAsync(Document document, int openingPosition, char openingBrace, CancellationToken cancellationToken);
    }
}
