// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Snippets;
using Microsoft.VisualStudio.LanguageServices.Implementation.Snippets;
using Microsoft.VisualStudio.Shell;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Snippets
{
    // HACK: The Export attribute (as ISnippetInfoService) is used by EditorTestApp to create this
    // SnippetInfoService on the UI thread.
    [Export(typeof(ISnippetInfoService))]
    [ExportLanguageService(typeof(ISnippetInfoService), LanguageNames.CSharp), Shared]
    internal class CSharpSnippetInfoService : AbstractSnippetInfoService
    {
        // #region and #endregion when appears in the completion list as snippets
        // we should format the snippet on commit. 
        private ISet<string> _formatTriggeringSnippets = new HashSet<string>(new string[] { "#region", "#endregion" });
        [ImportingConstructor]
        public CSharpSnippetInfoService(
            SVsServiceProvider serviceProvider,
            IAsynchronousOperationListenerProvider listenerProvider)
            : base(serviceProvider, Guids.CSharpLanguageServiceId, listenerProvider)
        {
        }

        public override bool ShouldFormatSnippet(SnippetInfo snippetInfo)
        {
            return _formatTriggeringSnippets.Contains(snippetInfo.Shortcut);
        }
    }
}
