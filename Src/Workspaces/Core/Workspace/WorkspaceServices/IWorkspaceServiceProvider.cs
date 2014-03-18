// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.WorkspaceServices
{
    internal interface IWorkspaceServiceProvider
    {
        string Kind { get; }
        IWorkspaceServiceProviderFactory Factory { get; }
        TWorkspaceService GetService<TWorkspaceService>() where TWorkspaceService : IWorkspaceService;

        IEnumerable<Lazy<T>> GetServiceExtensions<T>() where T : class;
        IEnumerable<Lazy<TExtension, TMetadata>> GetServiceExtensions<TExtension, TMetadata>() where TExtension : class;
    }
}