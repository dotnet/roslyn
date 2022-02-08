// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    internal abstract partial class AbstractOleCommandTarget : IOleCommandTarget
    {
        /// <summary>
        /// This is set only during Exec. Currently, this is required to disambiguate the editor calls to
        /// <see cref="IVsTextViewFilter.GetPairExtents(int, int, TextSpan[])"/> between GotoBrace and GotoBraceExt commands.
        /// </summary>
        protected uint CurrentlyExecutingCommand { get; private set; }

        public AbstractOleCommandTarget(
            IWpfTextView wpfTextView,
            IComponentModel componentModel)
        {
            Contract.ThrowIfNull(wpfTextView);
            Contract.ThrowIfNull(componentModel);

            WpfTextView = wpfTextView;
            ComponentModel = componentModel;
        }

        public IComponentModel ComponentModel { get; }

        public IVsEditorAdaptersFactoryService EditorAdaptersFactory
        {
            get { return ComponentModel.GetService<IVsEditorAdaptersFactoryService>(); }
        }

        /// <summary>
        /// The IWpfTextView that this command filter is attached to.
        /// </summary>
        public IWpfTextView WpfTextView { get; }

        /// <summary>
        /// The next command target in the chain. This is set by the derived implementation of this
        /// class.
        /// </summary>
        [DisallowNull]
        protected internal IOleCommandTarget? NextCommandTarget { get; set; }

        internal AbstractOleCommandTarget AttachToVsTextView()
        {
            var vsTextView = EditorAdaptersFactory.GetViewAdapter(WpfTextView);
            Contract.ThrowIfNull(vsTextView);

            // Add command filter to IVsTextView. If something goes wrong, throw.
            var returnValue = vsTextView.AddCommandFilter(this, out var nextCommandTarget);
            Marshal.ThrowExceptionForHR(returnValue);
            Contract.ThrowIfNull(nextCommandTarget);

            NextCommandTarget = nextCommandTarget;

            return this;
        }

        protected virtual ITextBuffer? GetSubjectBufferContainingCaret()
            => WpfTextView.GetBufferContainingCaret();

        protected virtual ITextView ConvertTextView()
            => WpfTextView;
    }
}
