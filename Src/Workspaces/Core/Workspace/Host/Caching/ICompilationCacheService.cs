// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Host
{
    internal interface ICompilationCacheService : IWorkspaceService
    {
        /// <summary>
        /// compilation cache for main branch
        /// </summary>
        ICompilationCache Primary { get; }

        /// <summary>
        /// compilation cache for all other branches which could include in progress main branch compilation
        /// </summary>
        ICompilationCache Secondary { get; }

        /// <summary>
        /// clear compilation caches belong to this workspace
        /// </summary>
        void Clear();
    }
}
