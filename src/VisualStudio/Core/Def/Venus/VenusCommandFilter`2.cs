// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Venus
{
    [Obsolete("This is a compatibility shim for LiveShare and TypeScript; please do not use it.")]
    internal class VenusCommandFilter<TPackage, TLanguageService> : VenusCommandFilter
        where TPackage : AbstractPackage<TPackage, TLanguageService>
        where TLanguageService : AbstractLanguageService<TPackage, TLanguageService>
    {
        [Obsolete("This is a compatibility shim for TypeScript; please do not use it.")]
        public VenusCommandFilter(
            TLanguageService languageService,
            IWpfTextView wpfTextView,
            ICommandHandlerServiceFactory commandHandlerServiceFactory,
            ITextBuffer subjectBuffer,
            IOleCommandTarget nextCommandTarget,
            IVsEditorAdaptersFactoryService editorAdaptersFactoryService)
            : base(wpfTextView, subjectBuffer, nextCommandTarget, languageService.Package.ComponentModel)
        {
        }
    }
}
