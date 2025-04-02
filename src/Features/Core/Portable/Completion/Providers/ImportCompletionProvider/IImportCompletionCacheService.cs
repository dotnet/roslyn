// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Threading;

namespace Microsoft.CodeAnalysis.Completion.Providers;

internal interface IImportCompletionCacheService<TProject, TPortableExecutable> : IWorkspaceService
{
    AsyncBatchingWorkQueue<Project> WorkQueue { get; }
}
