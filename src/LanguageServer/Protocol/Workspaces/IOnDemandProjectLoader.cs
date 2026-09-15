// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer;

internal interface IOnDemandProjectLoader : ILspService
{
    Task StartLoadingAsync(DocumentUri uri);

    /// <summary>
    /// Captures current discovery and project-load operations. Await the returned task outside serialized request
    /// dispatch; awaiting this method only captures the snapshot.
    /// </summary>
    ValueTask<Task> GetWorkspaceLoadTaskAsync();
}
