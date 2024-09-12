// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.EmbeddedLanguages.StackFrame;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.StackTraceExplorer;

internal interface IStackTraceExplorerService : IWorkspaceService
{
    /// <summary>
    /// If the <paramref name="frame"/> has file information, attempts to map it to existing documents
    /// in a solution. Looks for an exact filepath match first, then defaults to 
    /// a best guess.
    /// </summary>
    (Document? document, int line) GetDocumentAndLine(Solution solution, ParsedFrame frame);
    Task<DefinitionItem?> TryFindDefinitionAsync(Solution solution, ParsedFrame frame, StackFrameSymbolPart symbolPart, CancellationToken cancellationToken);
}

internal interface IRemoteStackTraceExplorerService
{
    ValueTask<SerializableDefinitionItem?> TryFindDefinitionAsync(Checksum solutionChecksum, string frameString, StackFrameSymbolPart symbolPart, CancellationToken cancellationToken);
}
