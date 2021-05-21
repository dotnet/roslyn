﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Shared.Utilities
{
    [Export(typeof(IWorkspaceThreadingService))]
    [Shared]
    internal sealed class WorkspaceThreadingService : IWorkspaceThreadingService
    {
        private readonly IThreadingContext _threadingContext;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public WorkspaceThreadingService(IThreadingContext threadingContext)
        {
            _threadingContext = threadingContext;
        }

        public TResult Run<TResult>(Func<Task<TResult>> asyncMethod)
        {
            return _threadingContext.JoinableTaskFactory.Run(asyncMethod);
        }
    }
}
