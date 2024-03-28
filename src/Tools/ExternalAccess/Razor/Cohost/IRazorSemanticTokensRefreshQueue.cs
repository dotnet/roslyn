// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost;

internal interface IRazorSemanticTokensRefreshQueue : ILspService
{
    void Initialize(string clientCapabilitiesString);

    Task TryEnqueueRefreshComputationAsync(Project project, CancellationToken cancellationToken);
}
