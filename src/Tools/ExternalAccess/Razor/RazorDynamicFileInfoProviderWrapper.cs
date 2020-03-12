// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor
{
    [Shared]
    [ExportMetadata("Extensions", new string[] { "cshtml", "razor", })]
    [Export(typeof(IDynamicFileInfoProvider))]
    internal sealed class RazorDynamicFileInfoProviderWrapper : IDynamicFileInfoProvider
    {
        private readonly IRazorDynamicFileInfoProvider _innerDynamiFileInfoProvider;

        public event EventHandler<string> Updated;

        [ImportingConstructor]
        public RazorDynamicFileInfoProviderWrapper(
            [Import(AllowDefault = true)] IRazorDynamicFileInfoProvider innerDynamiFileInfoProvider)
        {
            _innerDynamiFileInfoProvider = innerDynamiFileInfoProvider;

            // _innerDynamicFileInfoProvider will be null in the case that the Razor workload is not installed.

            if (_innerDynamiFileInfoProvider != null)
            {
                _innerDynamiFileInfoProvider.Updated += InnerDynamiFileInfoProvider_Updated;
            }
        }

        public async Task<DynamicFileInfo> GetDynamicFileInfoAsync(ProjectId projectId, string projectFilePath, string filePath, CancellationToken cancellationToken)
        {
            if (_innerDynamiFileInfoProvider == null)
            {
                // Razor workload is not installed. Can't build dynamic file infos for any Razor files.
                return null;
            }

            var result = await _innerDynamiFileInfoProvider.GetDynamicFileInfoAsync(projectId, projectFilePath, filePath, cancellationToken).ConfigureAwait(false);
            var serviceProvider = new RazorDocumentServiceProviderWrapper(result.DocumentServiceProvider);
            var dynamicFileInfo = new DynamicFileInfo(result.FilePath, result.SourceCodeKind, result.TextLoader, serviceProvider);

            return dynamicFileInfo;
        }

        public Task RemoveDynamicFileInfoAsync(ProjectId projectId, string projectFilePath, string filePath, CancellationToken cancellationToken)
        {
            if (_innerDynamiFileInfoProvider == null)
            {
                // Razor workload is not installed. Can't remove any dynamic file infos.
                return Task.CompletedTask;
            }

            return _innerDynamiFileInfoProvider.RemoveDynamicFileInfoAsync(projectId, projectFilePath, filePath, cancellationToken);
        }

        private void InnerDynamiFileInfoProvider_Updated(object sender, string e)
        {
            Updated?.Invoke(this, e);
        }
    }
}
