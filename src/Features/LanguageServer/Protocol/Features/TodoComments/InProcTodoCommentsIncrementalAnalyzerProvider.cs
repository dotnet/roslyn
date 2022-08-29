// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.SolutionCrawler;

namespace Microsoft.CodeAnalysis.TodoComments
{
    /// <remarks>Note: this is explicitly <b>not</b> exported.  We don't want the workspace
    /// to automatically load this.  Instead, VS waits until it is ready
    /// and then calls into the service to tell it to start analyzing the solution.  At that point we'll get
    /// created and added to the solution crawler.
    /// </remarks>
    internal sealed class InProcTodoCommentsIncrementalAnalyzerProvider : IIncrementalAnalyzerProvider
    {
        private readonly TodoCommentsListener _listener;

        public InProcTodoCommentsIncrementalAnalyzerProvider(TodoCommentsListener listener)
            => _listener = listener;

        public IIncrementalAnalyzer CreateIncrementalAnalyzer(Workspace workspace)
            => new InProcTodoCommentsIncrementalAnalyzer(_listener);
    }
}
