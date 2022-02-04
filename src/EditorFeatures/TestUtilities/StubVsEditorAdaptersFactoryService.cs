// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using IServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

namespace Microsoft.CodeAnalysis.Editor.UnitTests
{
    [Export(typeof(IVsEditorAdaptersFactoryService))]
    [PartNotDiscoverable]
    internal class StubVsEditorAdaptersFactoryService : IVsEditorAdaptersFactoryService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public StubVsEditorAdaptersFactoryService()
        {
        }

        public IVsCodeWindow CreateVsCodeWindowAdapter(IServiceProvider serviceProvider)
            => throw new NotImplementedException();

        public IVsTextBuffer CreateVsTextBufferAdapter(IServiceProvider serviceProvider)
            => throw new NotImplementedException();

        public IVsTextBuffer CreateVsTextBufferAdapter(IServiceProvider serviceProvider, IContentType contentType)
            => throw new NotImplementedException();

        public IVsTextBuffer CreateVsTextBufferAdapterForSecondaryBuffer(IServiceProvider serviceProvider, ITextBuffer secondaryBuffer)
            => throw new NotImplementedException();

        public IVsTextBufferCoordinator CreateVsTextBufferCoordinatorAdapter()
            => throw new NotImplementedException();

        public IVsTextView CreateVsTextViewAdapter(IServiceProvider serviceProvider)
            => throw new NotImplementedException();

        public IVsTextView CreateVsTextViewAdapter(IServiceProvider serviceProvider, ITextViewRoleSet roles)
            => throw new NotImplementedException();

        public IVsTextBuffer GetBufferAdapter(ITextBuffer textBuffer)
            => throw new NotImplementedException();

        public ITextBuffer GetDataBuffer(IVsTextBuffer bufferAdapter)
            => throw new NotImplementedException();

        public ITextBuffer GetDocumentBuffer(IVsTextBuffer bufferAdapter)
            => throw new NotImplementedException();

        public IVsTextView GetViewAdapter(ITextView textView)
            => throw new NotImplementedException();

        public IWpfTextView GetWpfTextView(IVsTextView viewAdapter)
            => throw new NotImplementedException();

        public IWpfTextViewHost GetWpfTextViewHost(IVsTextView viewAdapter)
            => throw new NotImplementedException();

        public void SetDataBuffer(IVsTextBuffer bufferAdapter, ITextBuffer dataBuffer)
            => throw new NotImplementedException();
    }
}
