// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

#nullable enable

namespace Microsoft.VisualStudio.Extensibility.Testing
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;
    using global::Xunit;
    using Microsoft.VisualStudio.Threading;

    /// <summary>
    /// Provides access to helpers for common integration test functionality.
    /// </summary>
    public sealed class TestServices
    {
        private TestServices(JoinableTaskFactory joinableTaskFactory)
        {
            JoinableTaskFactory = joinableTaskFactory;

            SolutionExplorer = new global::Microsoft.VisualStudio.Extensibility.Testing.SolutionExplorerInProcess(this);
            Shell = new global::Microsoft.VisualStudio.Extensibility.Testing.ShellInProcess(this);
        }

        /// <summary>
        /// Gets the <see cref="Threading.JoinableTaskFactory"/> for use in integration tests.
        /// </summary>
        public JoinableTaskFactory JoinableTaskFactory { get; }

        internal global::Microsoft.VisualStudio.Extensibility.Testing.SolutionExplorerInProcess SolutionExplorer { get; }
        internal global::Microsoft.VisualStudio.Extensibility.Testing.ShellInProcess Shell { get; }

        internal static async Task<TestServices> CreateAsync(JoinableTaskFactory joinableTaskFactory)
        {
            var services = new TestServices(joinableTaskFactory);
            await services.InitializeAsync();
            return services;
        }

        private async Task InitializeAsync()
        {
            await ((IAsyncLifetime)SolutionExplorer).InitializeAsync();
            await ((IAsyncLifetime)Shell).InitializeAsync();
        }
    }
}
