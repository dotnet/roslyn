// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindUsages;

[ExportWorkspaceService(typeof(IExternalDefinitionItemProvider)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class DefaultExternalDefinitionItemProvider() : IExternalDefinitionItemProvider
{
    /// <summary>
    /// Provides an extension point that allows for other workspace layers to add additional
    /// results to the results found by the FindReferences engine.
    /// </summary>
    public async Task<DefinitionItem?> GetThirdPartyDefinitionItemAsync(Solution solution, DefinitionItem definitionItem, CancellationToken cancellationToken)
        => null;
}
