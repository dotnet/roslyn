// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Completion.Providers.ImportCompletion
{
    internal interface IImportCompletionCacheService<TProject, TPortableExecutable> : IWorkspaceService
    {
        // PE references are keyed on assembly path.
        IDictionary<string, TPortableExecutable> PEItemsCache { get; }

        IDictionary<ProjectId, TProject> ProjectItemsCache { get; }
    }
}
