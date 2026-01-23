// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.SemanticSearch;

#if Unified_ExternalAccess
using Microsoft.VisualStudio.ExternalAccess.Copilot.SemanticSearch;

namespace Microsoft.VisualStudio.ExternalAccess.Copilot.Internal.SemanticSearch;
#else
using Microsoft.CodeAnalysis.ExternalAccess.Copilot.SemanticSearch;

namespace Microsoft.CodeAnalysis.ExternalAccess.Copilot.Internal.SemanticSearch;
#endif

[Export(typeof(ICopilotSemanticSearchPresenterController)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CopilotSemanticSearchPresenterController(Lazy<ISemanticSearchPresenterController> controller) : ICopilotSemanticSearchPresenterController
{
    public Task ExecuteQueryAsync(string query, CancellationToken cancellationToken)
        => controller.Value.ExecuteQueryAsync(query, cancellationToken);
}
