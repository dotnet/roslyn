// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

#if Unified_ExternalAccess
namespace Microsoft.VisualStudio.ExternalAccess.Copilot.SemanticSearch;
#else
namespace Microsoft.CodeAnalysis.ExternalAccess.Copilot.SemanticSearch;
#endif

internal interface ICopilotSemanticSearchPresenterController
{
    /// <summary>
    /// Executes semantic seqech <paramref name="query"/> and presents the results in Find Results window.
    /// </summary>
    Task ExecuteQueryAsync(string query, CancellationToken cancellationToken);
}
