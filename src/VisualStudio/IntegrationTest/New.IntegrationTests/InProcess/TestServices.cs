// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;

namespace Roslyn.VisualStudio.IntegrationTests.InProcess
{
    internal class TestServices
    {
        protected TestServices(JoinableTaskFactory joinableTaskFactory)
        {
            JoinableTaskFactory = joinableTaskFactory;

            Editor = new EditorInProcess(this);
            ErrorList = new ErrorListInProcess(this);
            SolutionExplorer = new SolutionExplorerInProcess(this);
        }

        public JoinableTaskFactory JoinableTaskFactory { get; }

        public EditorInProcess Editor { get; }

        public ErrorListInProcess ErrorList { get; }

        public SolutionExplorerInProcess SolutionExplorer { get; }

        internal static async Task<TestServices> CreateAsync(JoinableTaskFactory joinableTaskFactory)
        {
            var services = new TestServices(joinableTaskFactory);
            await services.InitializeAsync();
            return services;
        }

        protected virtual Task InitializeAsync()
        {
            return Task.CompletedTask;
        }
    }
}
