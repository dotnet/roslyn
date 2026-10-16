// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer;

internal interface IOnDemandProjectLoader : ILspService
{
    /// <summary>
    /// Determines whether the specified document can be loaded on demand.
    /// </summary>
    bool CanLoad(DocumentUri uri);

    /// <summary>
    /// Attempts to load the projects containing the specified document.
    /// </summary>
    Task TryLoadProjectsAsync(DocumentUri uri);

    /// <summary>
    /// Waits for all active on-demand projects to load.
    /// </summary>
    ValueTask WaitForActiveLoadsAsync();
}
