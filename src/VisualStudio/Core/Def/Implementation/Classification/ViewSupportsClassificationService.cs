// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Implementation.Classification;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.LanguageServices.Utilities;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Classification
{
    [Export(typeof(IViewSupportsClassificationService))]
    internal class ViewSupportsClassificationService : IViewSupportsClassificationService
    {
        private readonly IVsEditorAdaptersFactoryService _editorAdaptersFactoryService;
        private readonly SVsServiceProvider _serviceProvider;

        [ImportingConstructor]
        public ViewSupportsClassificationService(
            ITextBufferAssociatedViewService viewService,
            IVsEditorAdaptersFactoryService editorAdaptersFactoryService, 
            SVsServiceProvider serviceProvider)
        {
            _editorAdaptersFactoryService = editorAdaptersFactoryService;
            _serviceProvider = serviceProvider;
        }

        public bool CanClassifyViews(IEnumerable<ITextView> views)
        {
            var vsTextViews = views.Select(view => _editorAdaptersFactoryService.GetViewAdapter(view)).WhereNotNull();
            return !vsTextViews.ContainsImmediateWindow((IVsUIShell)_serviceProvider.GetService(typeof(SVsUIShell)), _editorAdaptersFactoryService);
        }
    }
}
