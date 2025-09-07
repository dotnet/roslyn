// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Snippets;
using Microsoft.VisualStudio.LanguageServices.Implementation.Snippets;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Snippets;

[ExportLanguageService(typeof(ISnippetInfoService), LanguageNames.CSharp), Shared]
internal class CSharpSnippetInfoService : AbstractSnippetInfoService
{
    // #region and #endregion when appears in the completion list as snippets
    // we should format the snippet on commit. 
    private readonly ISet<string> _formatTriggeringSnippets = new HashSet<string>(new string[] { "#region", "#endregion" });

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpSnippetInfoService(
        IThreadingContext threadingContext,
        SVsServiceProvider serviceProvider,
        IAsynchronousOperationListenerProvider listenerProvider)
        : base(threadingContext, (IAsyncServiceProvider)serviceProvider, Guids.CSharpLanguageServiceId, listenerProvider)
    {
    }

    public override bool ShouldFormatSnippet(SnippetInfo snippetInfo)
        => _formatTriggeringSnippets.Contains(snippetInfo.Shortcut);
}
