// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.Navigation;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.GoToDefinition;

internal interface IFSharpFindDefinitionService
{
    /// <summary>
    /// Finds the definitions for the symbol at the specific position in the document.
    /// </summary>
    Task<ImmutableArray<FSharpNavigableItem>> FindDefinitionsAsync(Document document, int position, CancellationToken cancellationToken);
}
