// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeDefinitionWindow;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeDefinitionWindow
{
    [Export(typeof(ICodeDefinitionWindowService)), Shared]
    internal sealed class VisualStudioCodeDefinitionWindowService : ICodeDefinitionWindowService
    {
        private readonly IAsyncServiceProvider _asyncServiceProvider;
        private readonly IThreadingContext _threadingContext;

        [DisallowNull]
        private IVsCodeDefView? _lazyCodeDefView;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioCodeDefinitionWindowService(SVsServiceProvider asyncServiceProvider, IThreadingContext threadingContext)
        {
            _asyncServiceProvider = (IAsyncServiceProvider)asyncServiceProvider;
            _threadingContext = threadingContext;
        }

        private async Task<IVsCodeDefView> GetVsCodeDefViewAsync()
        {
            if (_lazyCodeDefView is not null)
            {
                return _lazyCodeDefView;
            }

            _lazyCodeDefView = await _asyncServiceProvider.GetServiceAsync<SVsCodeDefView, IVsCodeDefView>(_threadingContext.JoinableTaskFactory).ConfigureAwait(true);

            return _lazyCodeDefView;
        }

        public async Task<bool> IsWindowOpenAsync(CancellationToken cancellationToken)
        {
            var vsCodeDefView = await GetVsCodeDefViewAsync().ConfigureAwait(true);

            // Switch to the UI thread before using the IVsCodeDefView service
            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            // IsVisible returns S_FALSE if it's not visible
            return vsCodeDefView.IsVisible() == VSConstants.S_OK;
        }

        public async Task SetContextAsync(ImmutableArray<CodeDefinitionWindowLocation> locations, CancellationToken cancellationToken)
        {
            // If the new context has no location, then just don't update, instead of showing the
            // "No definition selected" page.
            if (!locations.Any())
            {
                return;
            }

            var vsCodeDefView = await GetVsCodeDefViewAsync().ConfigureAwait(true);

            // Switch to the UI thread before using the IVsCodeDefView service
            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            Marshal.ThrowExceptionForHR(vsCodeDefView.SetContext(new Context(locations)));
        }

        private class Context : IVsCodeDefViewContext
        {
            private readonly ImmutableArray<CodeDefinitionWindowLocation> _locations;

            public Context(ImmutableArray<CodeDefinitionWindowLocation> locations)
            {
                _locations = locations;
            }

            int IVsCodeDefViewContext.GetCount(out uint pcItems)
            {
                pcItems = (uint)_locations.Length;
                return VSConstants.S_OK;
            }

            int IVsCodeDefViewContext.GetSymbolName(uint iItem, out string pbstrSymbolName)
            {
                pbstrSymbolName = GetItem(iItem).DisplayName;
                return VSConstants.S_OK;
            }

            int IVsCodeDefViewContext.GetFileName(uint iItem, out string pbstrFilename)
            {
                pbstrFilename = GetItem(iItem).FilePath;
                return VSConstants.S_OK;
            }

            int IVsCodeDefViewContext.GetLine(uint iItem, out uint piLine)
            {
                piLine = (uint)GetItem(iItem).Position.Line;
                return VSConstants.S_OK;
            }

            int IVsCodeDefViewContext.GetCol(uint iItem, out uint piCol)
            {
                piCol = (uint)GetItem(iItem).Position.Character;
                return VSConstants.S_OK;
            }

            private CodeDefinitionWindowLocation GetItem(uint iItem)
            {
                var index = (int)iItem;
                if (index < 0 || index >= _locations.Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(iItem));
                }

                return _locations[index];
            }
        }
    }
}
