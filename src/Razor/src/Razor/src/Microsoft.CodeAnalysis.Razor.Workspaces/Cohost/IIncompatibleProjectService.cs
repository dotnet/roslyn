// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer.Handler;

namespace Microsoft.CodeAnalysis.Razor.Cohost;

internal interface IIncompatibleProjectService
{
    Task HandleMissingDocumentAsync(TextDocumentIdentifier? textDocumentIdentifier, RequestContext context, CancellationToken cancellationToken);
}
