// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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