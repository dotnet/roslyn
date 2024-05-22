// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Shared.Utilities;

[Export(typeof(IWorkspaceThreadingService))]
[Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class WorkspaceThreadingService(IThreadingContext threadingContext) : IWorkspaceThreadingService
{
    private readonly IThreadingContext _threadingContext = threadingContext;

    public bool IsOnMainThread => _threadingContext.JoinableTaskContext.IsOnMainThread;

    public TResult Run<TResult>(Func<Task<TResult>> asyncMethod)
    {
        return _threadingContext.JoinableTaskFactory.Run(asyncMethod);
    }
}
