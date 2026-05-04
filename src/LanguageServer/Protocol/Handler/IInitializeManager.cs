// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler;

internal interface IInitializeManager : ILspService
{
    ClientCapabilities GetClientCapabilities();

    ClientCapabilities? TryGetClientCapabilities();

    InitializeParams? TryGetInitializeParams();

    /// <summary>Expected to be non-default after the Initialize event.</summary>
    ImmutableArray<string> GetRequiredWorkspaceFolderPaths();

    void SetInitializeParams(InitializeParams initializeParams);
}
