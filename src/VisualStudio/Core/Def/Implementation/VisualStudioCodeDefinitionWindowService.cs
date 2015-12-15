// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Editor;
using System;
using System.Linq;
using System.Collections.Immutable;
using Microsoft.VisualStudio.Shell.Interop;
using System.Composition;
using System.Runtime.InteropServices;
using SVsServiceProvider = Microsoft.VisualStudio.Shell.SVsServiceProvider;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    [Export(typeof(ICodeDefinitionWindowService)), Shared]
    internal sealed class VisualStudioCodeDefinitionWindowService : ICodeDefinitionWindowService
    {
        private readonly IVsCodeDefView _vsCodeDefView;

        [ImportingConstructor]
        public VisualStudioCodeDefinitionWindowService(SVsServiceProvider serviceProvider)
        {
            _vsCodeDefView = (IVsCodeDefView)serviceProvider.GetService(typeof(SVsCodeDefView));
        }

        public void SetContext(ImmutableArray<CodeDefinitionWindowLocation> locations)
        {
            // If the new context has no location, then just don't update, instead of showing the
            // "No definition selected" page.
            if (locations.Any())
            {
                Marshal.ThrowExceptionForHR(_vsCodeDefView.SetContext(new Context(locations)));
            }
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
                var index = (int)iItem;
                if (index < 0 || index >= _locations.Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(iItem));
                }

                pbstrSymbolName = _locations[index].DisplayName;
                return VSConstants.S_OK;
            }

            int IVsCodeDefViewContext.GetFileName(uint iItem, out string pbstrFilename)
            {
                var index = (int)iItem;
                if (index < 0 || index >= _locations.Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(iItem));
                }

                pbstrFilename = _locations[index].FilePath;
                return VSConstants.S_OK;
            }

            int IVsCodeDefViewContext.GetLine(uint iItem, out uint piLine)
            {
                var index = (int)iItem;
                if (index < 0 || index >= _locations.Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(iItem));
                }

                piLine = (uint)_locations[index].Line;
                return VSConstants.S_OK;
            }

            int IVsCodeDefViewContext.GetCol(uint iItem, out uint piCol)
            {
                var index = (int)iItem;
                if (index < 0 || index >= _locations.Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(iItem));
                }

                piCol = (uint)_locations[index].Character;
                return VSConstants.S_OK;
            }
        }
    }
}
