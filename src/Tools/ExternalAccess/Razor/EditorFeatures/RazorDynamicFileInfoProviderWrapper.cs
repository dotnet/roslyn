// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor
{
    [Shared]
    [ExportMetadata("Extensions", new string[] { "cshtml", "razor", })]
    [Export(typeof(IDynamicFileInfoProvider))]
    internal class RazorDynamicFileInfoProviderWrapper : IDynamicFileInfoProvider
    {
        private readonly Lazy<IRazorDynamicFileInfoProvider> _innerDynamicFileInfoProvider;
        private readonly object _attachLock = new object();
        private bool _attached;

        public event EventHandler<string>? Updated;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public RazorDynamicFileInfoProviderWrapper(
            Lazy<IRazorDynamicFileInfoProvider> innerDynamicFileInfoProvider)
        {
            _innerDynamicFileInfoProvider = innerDynamicFileInfoProvider ?? throw new ArgumentNullException(nameof(innerDynamicFileInfoProvider));
        }

        public async Task<DynamicFileInfo?> GetDynamicFileInfoAsync(ProjectId projectId, string? projectFilePath, string filePath, CancellationToken cancellationToken)
        {
            // We lazily attach to the dynamic file info provider in order to ensure that Razor assemblies are not loaded in non-Razor contexts.
            if (!EnsureAttached())
            {
                // Razor workload is not installed. Can't build dynamic file infos for any Razor files.
                return null;
            }

            var result = await _innerDynamicFileInfoProvider.Value.GetDynamicFileInfoAsync(projectId, projectFilePath, filePath, cancellationToken).ConfigureAwait(false);
            // This might not be a file/project Razor is interested in
            if (result is null)
            {
                return null;
            }

            var serviceProvider = new RazorDocumentServiceProviderWrapper(result.DocumentServiceProvider);
            var razorDocumentPropertiesService = result.DocumentServiceProvider.GetService<IRazorDocumentPropertiesService>();
            var designTimeOnly = razorDocumentPropertiesService?.DesignTimeOnly ?? false;
            var dynamicFileInfo = new DynamicFileInfo(result.FilePath, result.SourceCodeKind, result.TextLoader, designTimeOnly, serviceProvider);

            return dynamicFileInfo;
        }

        public Task RemoveDynamicFileInfoAsync(ProjectId projectId, string? projectFilePath, string filePath, CancellationToken cancellationToken)
        {
            if (_innerDynamicFileInfoProvider == null)
            {
                // Razor workload is not installed. Can't remove any dynamic file infos.
                return Task.CompletedTask;
            }

            return _innerDynamicFileInfoProvider.Value.RemoveDynamicFileInfoAsync(projectId, projectFilePath, filePath, cancellationToken);
        }

        private void InnerDynamicFileInfoProvider_Updated(object? sender, string e)
        {
            Updated?.Invoke(this, e);
        }

        private bool EnsureAttached()
        {
            lock (_attachLock)
            {
                if (_attached)
                {
                    return true;
                }

                if (_innerDynamicFileInfoProvider.Value == null)
                {
                    // Razor workload not installed, can't attach.
                    return false;
                }

                _attached = true;
                _innerDynamicFileInfoProvider.Value.Updated += InnerDynamicFileInfoProvider_Updated;

                return true;
            }
        }
    }
}
