// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api;

internal interface IUnitTestingStackTraceServiceAccessor : IWorkspaceService
{
    Task<ImmutableArray<UnitTestingParsedFrameWrapper>> TryParseAsync(string input, Workspace workspace, CancellationToken cancellationToken);
    Task<UnitTestingDefinitionItemWrapper?> TryFindMethodDefinitionAsync(Workspace workspace, UnitTestingParsedFrameWrapper parsedFrame, CancellationToken cancellationToken);
    (Document? document, int lineNumber) GetDocumentAndLine(Workspace workspace, UnitTestingParsedFrameWrapper parsedFrame);
    Task<bool> TryNavigateToAsync(Workspace workspace, UnitTestingDefinitionItemWrapper definitionItem, bool showInPreviewTab, bool activateTab, CancellationToken cancellationToken);
}
