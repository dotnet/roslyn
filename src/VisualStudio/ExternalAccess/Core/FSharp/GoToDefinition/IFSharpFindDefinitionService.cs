// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

#if Unified_ExternalAccess
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.ExternalAccess.FSharp.Navigation;

namespace Microsoft.VisualStudio.ExternalAccess.FSharp.GoToDefinition;
#else
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.Navigation;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.GoToDefinition;
#endif

internal interface IFSharpFindDefinitionService
{
    /// <summary>
    /// Finds the definitions for the symbol at the specific position in the document.
    /// </summary>
    Task<ImmutableArray<FSharpNavigableItem>> FindDefinitionsAsync(Document document, int position, CancellationToken cancellationToken);
}
