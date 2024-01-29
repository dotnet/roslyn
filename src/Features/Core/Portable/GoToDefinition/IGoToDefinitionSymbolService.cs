// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.GoToDefinition;

internal interface IGoToDefinitionSymbolService : ILanguageService
{
    Task<(ISymbol?, Project, TextSpan)> GetSymbolProjectAndBoundSpanAsync(Document document, int position, CancellationToken cancellationToken);

    /// <summary>
    /// If the position is on a control flow keyword (continue, break, yield, return , etc), returns the relevant position in the corresponding control flow statement.
    /// Otherwise, returns null.
    /// </summary>
    Task<(int? targetPosition, TextSpan tokenSpan)> GetTargetIfControlFlowAsync(Document document, int position, CancellationToken cancellationToken);
}
