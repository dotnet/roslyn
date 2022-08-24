// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api
{
    internal interface IVSTypeScriptGoToDefinitionService
    {
        Task<IEnumerable<IVSTypeScriptNavigableItem>?> FindDefinitionsAsync(Document document, int position, CancellationToken cancellationToken);
        bool TryGoToDefinition(Document document, int position, CancellationToken cancellationToken);
    }
}
