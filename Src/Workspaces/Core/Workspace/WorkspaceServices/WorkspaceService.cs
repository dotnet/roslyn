// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.WorkspaceServices
{
    /// <summary>
    /// A service for accessing other workspace specific services.
    /// </summary>
    internal static partial class WorkspaceService
    {
        public static IWorkspaceServiceProvider GetProvider(Workspace workspace)
        {
            return workspace.GetWorkspaceServicesInternal();
        }

        public static TService GetService<TService>(Workspace workspace) where TService : class, IWorkspaceService
        {
            return workspace.GetWorkspaceServicesInternal().GetService<TService>();
        }
    }
}
