// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    [ExportDynamicFileInfoProvider("cshtml", "vbhtml")]
    [Shared]
    [PartNotDiscoverable]
    internal class TestDynamicFileInfoProviderThatProducesNoFiles : IDynamicFileInfoProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public TestDynamicFileInfoProviderThatProducesNoFiles()
        {
        }

        public event EventHandler<string> Updated;

        public Task<DynamicFileInfo> GetDynamicFileInfoAsync(ProjectId projectId, string projectFilePath, string filePath, CancellationToken cancellationToken)
            => Task.FromResult<DynamicFileInfo>(null);

        public Task RemoveDynamicFileInfoAsync(ProjectId projectId, string projectFilePath, string filePath, CancellationToken cancellationToken)
            => Task.CompletedTask;

        private void OnUpdate()
            => Updated?.Invoke(this, "test");
    }
}
