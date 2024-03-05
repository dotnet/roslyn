// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Completion.Providers;

internal interface IImportCompletionCacheService<TProject, TPortableExecutable> : IWorkspaceService
{
    // PE references are keyed on assembly path.
    IDictionary<string, TPortableExecutable> PEItemsCache { get; }

    IDictionary<ProjectId, TProject> ProjectItemsCache { get; }

    AsyncBatchingWorkQueue<Project> WorkQueue { get; }
}
