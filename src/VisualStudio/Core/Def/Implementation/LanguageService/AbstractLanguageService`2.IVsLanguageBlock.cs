// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.LanguageServices.Implementation.Extensions;
using Microsoft.VisualStudio.Text;

using IVsLanguageBlock = Microsoft.VisualStudio.TextManager.Interop.IVsLanguageBlock;
using IVsTextLines = Microsoft.VisualStudio.TextManager.Interop.IVsTextLines;
using VsTextSpan = Microsoft.VisualStudio.TextManager.Interop.TextSpan;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService
{
    internal abstract partial class AbstractLanguageService<TPackage, TLanguageService>
        : IVsLanguageBlock
    {
        public int GetCurrentBlock(
            IVsTextLines pTextLines,
            int iCurrentLine,
            int iCurrentChar,
            VsTextSpan[] ptsBlockSpan,
            out string pbstrDescription,
            out int pfBlockAvailable)
        {
            var snapshot = this.EditorAdaptersFactoryService.GetDataBuffer(pTextLines).CurrentSnapshot;
            var position = snapshot?.TryGetPosition(iCurrentLine, iCurrentChar);
            if (position == null)
            {
                pbstrDescription = null;
                pfBlockAvailable = 0;
                return VSConstants.S_OK;
            }

            (string description, TextSpan span)? foundBlock = null;

            var waitIndicator = this.Package.ComponentModel.GetService<IWaitIndicator>();
            waitIndicator.Wait(
                ServicesVSResources.Current_block,
                ServicesVSResources.Determining_current_block,
                allowCancel: true,
                action: context =>
                {
                    foundBlock = VsLanguageBlock.GetCurrentBlock(snapshot, position.Value, context.CancellationToken);
                });

            pfBlockAvailable = foundBlock != null ? 1 : 0;
            pbstrDescription = foundBlock?.description;

            if (foundBlock != null && ptsBlockSpan != null && ptsBlockSpan.Length >= 1)
            {
                ptsBlockSpan[0] = foundBlock.Value.span.ToSnapshotSpan(snapshot).ToVsTextSpan();
            }

            return VSConstants.S_OK;
        }
    }

    internal static class VsLanguageBlock
    {
        public static (string description, TextSpan span)? GetCurrentBlock(
            ITextSnapshot snapshot,
            int position,
            CancellationToken cancellationToken)
        {
            var document = snapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null || !document.SupportsSyntaxTree)
            {
                return null;
            }

            var syntaxFactsService = document.GetLanguageService<ISyntaxFactsService>();
            var syntaxRoot = document.GetSyntaxRootSynchronously(cancellationToken);
            var node = syntaxFactsService.GetContainingMemberDeclaration(syntaxRoot, position, useFullSpan: false);
            if (node == null)
            {
                return null;
            }

            var description = syntaxFactsService.GetDisplayName(node,
                DisplayNameOptions.IncludeMemberKeyword |
                DisplayNameOptions.IncludeParameters |
                DisplayNameOptions.IncludeType |
                DisplayNameOptions.IncludeTypeParameters);

            return (description, node.Span);
        }
    }
}
