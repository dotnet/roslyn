// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer;

internal interface IOnDemandProjectLoader : ILspService
{
    Task StartLoadingAsync(DocumentUri uri, ImmutableHashSet<string> workspaceFolders);

    Task WaitForWorkspaceLoadsAsync();
}
