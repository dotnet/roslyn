// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.CodeAnalysis.Shared.TestHooks;

namespace Microsoft.CodeAnalysis.Host
{
    /// <summary>
    /// A factory that creates either sequential or parallel task schedulers.
    /// </summary>
    internal interface IWorkspaceAsynchronousOperationListenerProvider : IWorkspaceService
    {
        IAsynchronousOperationListener GetListener();
    }
}
