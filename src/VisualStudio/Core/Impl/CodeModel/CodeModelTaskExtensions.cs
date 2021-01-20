// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel
{
    internal static class CodeModelTaskExtensions
    {
        /// <summary>
        /// Easy extension for running a task synchronously for CodeModel code.
        /// </summary>
        /// <remarks>
        /// CodeModel only exists in Visual Studio, and is required to be a synchronous API. Since it may
        /// now call into other processes, which then could call back to the UI thread in VS, we need a non
        /// UI blocking way to handle this behavior. This forces the use of JTF as the way to do asynchronous
        /// work synchronously in the CodeModel layer. It is not advised that this is general purpose to all
        /// of Roslyn editor layer.
        /// </remarks>
        public static T WaitAndGetResult_CodeModel<T>(this Task<T> task, IThreadingContext threadingContext)
            // Make sure to use async/await in the lambda so that JTF can correctly
            // create a JoinableTask to wrap the Task<T> passed in
            => threadingContext.JoinableTaskFactory.Run(async () => await task);
    }
}
