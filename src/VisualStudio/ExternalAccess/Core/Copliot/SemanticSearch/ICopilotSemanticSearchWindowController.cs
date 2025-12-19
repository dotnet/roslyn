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

internal interface ICopilotSemanticSearchWindowController
{
    /// <summary>
    /// Updates the query in Semantic Search window editor.
    /// </summary>
    /// <param name="activateWindow">True to show the window and set focus.</param>
    /// <param name="executeQuery">True to trigger execution.</param>
    Task UpdateQueryAsync(string query, bool activateWindow, bool executeQuery, CancellationToken cancellationToken);
}
