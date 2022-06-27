// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Shared.Utilities
{
    /// <summary>
    /// An optional interface which allows an environment to customize the behavior for synchronous methods that need to
    /// block on the result of an asynchronous invocation. An implementation of this is provided in the MEF catalog when
    /// applicable.
    /// </summary>
    /// <remarks>
    /// <para>For Visual Studio, Microsoft.VisualStudio.Threading provides the JoinableTaskFactory.Run method, which is
    /// the expected way to invoke an asynchronous method from a synchronous entry point and block on its completion.
    /// Other environments may choose to use this or any other strategy, or omit an implementation of this interface to
    /// allow callers to simply use <see cref="Task.Wait()"/>.</para>
    ///
    /// <para>New code is expected to use fully-asynchronous programming where possible. In cases where external APIs
    /// restrict ability to be asynchronous, this service allows Roslyn to adhere to environmental policies related to
    /// joining asynchronous work.</para>
    /// </remarks>
    internal interface IWorkspaceThreadingService
    {
        TResult Run<TResult>(Func<Task<TResult>> asyncMethod);
    }

    internal interface IWorkspaceThreadingServiceProvider : IWorkspaceService
    {
        IWorkspaceThreadingService Service { get; }
    }
}
