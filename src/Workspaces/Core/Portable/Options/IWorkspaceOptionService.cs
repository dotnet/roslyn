// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

namespace Microsoft.CodeAnalysis.Options
{
    /// <summary>
    /// Interface used for exposing functionality from the option service that we don't want to 
    /// ever be public.
    /// </summary>
    internal interface IWorkspaceOptionService : IOptionService
    {
        void OnWorkspaceDisposed(Workspace workspace);
    }
}
