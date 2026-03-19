// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.LanguageServices.Implementation.F1Help;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService;

internal abstract partial class AbstractLanguageService<TPackage, TLanguageService> : IVsLanguageContextProvider
{
    public int UpdateLanguageContext(uint dwHint, IVsTextLines pBuffer, Microsoft.VisualStudio.TextManager.Interop.TextSpan[] ptsSelection, object pUC)
    {
        return this.ThreadingContext.JoinableTaskFactory.Run(async () =>
        {
            var editorAdaptersFactoryService = this.Package.ComponentModel.GetService<IVsEditorAdaptersFactoryService>();
            var textBuffer = editorAdaptersFactoryService.GetDataBuffer(pBuffer);
            var context = (IVsUserContext)pUC;

            if (textBuffer == null || context == null)
                return VSConstants.E_UNEXPECTED;

            var document = textBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
                return VSConstants.E_FAIL;

            var start = textBuffer.CurrentSnapshot.GetLineFromLineNumber(ptsSelection[0].iStartLine).Start + ptsSelection[0].iStartIndex;
            var end = textBuffer.CurrentSnapshot.GetLineFromLineNumber(ptsSelection[0].iEndLine).Start + ptsSelection[0].iEndIndex;
            var span = Microsoft.CodeAnalysis.Text.TextSpan.FromBounds(start, end);

            var helpService = document.GetLanguageService<IHelpContextService>();
            if (helpService == null)
                return VSConstants.E_NOTIMPL;

            // VS help is not cancellable.
            var cancellationToken = CancellationToken.None;
            var helpTerm = await helpService.GetHelpTermAsync(
                document, span, cancellationToken).ConfigureAwait(true);

            if (string.IsNullOrWhiteSpace(helpTerm))
                return VSConstants.S_FALSE;

            context.RemoveAttribute("keyword", null);
            context.AddAttribute(VSUSERCONTEXTATTRIBUTEUSAGE.VSUC_Usage_Filter, "devlang", helpService.Language);
            context.AddAttribute(VSUSERCONTEXTATTRIBUTEUSAGE.VSUC_Usage_Filter, "product", helpService.Product);
            context.AddAttribute(VSUSERCONTEXTATTRIBUTEUSAGE.VSUC_Usage_Filter, "product", "VS");
            context.AddAttribute(VSUSERCONTEXTATTRIBUTEUSAGE.VSUC_Usage_LookupF1_CaseSensitive, "keyword", helpTerm);

            return VSConstants.S_OK;
        });
    }
}
