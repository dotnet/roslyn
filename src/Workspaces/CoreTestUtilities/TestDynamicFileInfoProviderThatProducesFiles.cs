// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.Test.Utilities;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    [ExportDynamicFileInfoProvider("cshtml", "vbhtml")]
    [Shared]
    [PartNotDiscoverable]
    internal class TestDynamicFileInfoProviderThatProducesFiles : IDynamicFileInfoProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public TestDynamicFileInfoProviderThatProducesFiles()
        {
        }

        event EventHandler<string> IDynamicFileInfoProvider.Updated { add { } remove { } }

        public Task<DynamicFileInfo> GetDynamicFileInfoAsync(ProjectId projectId, string projectFilePath, string filePath, CancellationToken cancellationToken)
        {
            return Task.FromResult(new DynamicFileInfo(
                filePath + ".fromdynamicfile",
                SourceCodeKind.Regular,
                new TestTextLoader(GetDynamicFileText(filePath)),
                designTimeOnly: false,
                new TestDocumentServiceProvider()));
        }

        public static string GetDynamicFileText(string filePath)
        {
            if (filePath.EndsWith(".cshtml"))
            {
                return "// dynamic file from " + filePath;
            }
            else
            {
                return "' dynamic file from " + filePath;
            }
        }

        public Task RemoveDynamicFileInfoAsync(ProjectId projectId, string projectFilePath, string filePath, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
}
