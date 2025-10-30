// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.Copilot.SemanticSearch;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.SemanticSearch;

namespace Microsoft.CodeAnalysis.ExternalAccess.Copilot.Internal.SemanticSearch;

[Export(typeof(ICopilotSemanticSearchWindowController)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CopilotSemanticSearchWindowController(Lazy<ISemanticSearchToolWindowController> controller) : ICopilotSemanticSearchWindowController
{
    public Task UpdateQueryAsync(string query, bool activateWindow, bool executeQuery, CancellationToken cancellationToken)
        => controller.Value.UpdateQueryAsync(query, activateWindow, executeQuery, cancellationToken);
}
