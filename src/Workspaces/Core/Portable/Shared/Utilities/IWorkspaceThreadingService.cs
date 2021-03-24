// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Shared.Utilities
{
    /// <summary>
    /// An optional interface which allows an environment to customize the behavior for synchronous methods that need to
    /// block on the result of an asynchronous invocation. An implementation of this is provided in the MEF catalog when
    /// applicable.
    /// </summary>
    internal interface IWorkspaceThreadingService
    {
        TResult Run<TResult>(Func<Task<TResult>> asyncMethod);
    }
}
