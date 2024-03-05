// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Shared.TestHooks;

namespace Microsoft.CodeAnalysis.Host;

/// <summary>
/// Workspace service that provides <see cref="IAsynchronousOperationListener"/> instance.
/// </summary>
internal interface IWorkspaceAsynchronousOperationListenerProvider : IWorkspaceService
{
    IAsynchronousOperationListener GetListener();
}
