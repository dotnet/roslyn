// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.Copilot.ContextProviders;

internal interface ICopilotMethodCallersService : ILanguageService
{
    Task<ISymbol?> GetContainingMethodSymbolAsync(Document document, int position, CancellationToken cancellationToken);
    Task GetMethodCallersAsync(Document document, ISymbol symbol, Func<Document, TextSpan, CancellationToken, ValueTask> reporter, CancellationToken cancellationToken);
}
