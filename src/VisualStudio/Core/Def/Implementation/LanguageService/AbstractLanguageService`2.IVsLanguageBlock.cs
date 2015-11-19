// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.LanguageServices.Implementation.Extensions;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

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
            var foundBlock = false;
            string description = null;
            var span = default(TextSpan);

            var snapshot = this.EditorAdaptersFactoryService.GetDataBuffer(pTextLines).CurrentSnapshot;
            var position = snapshot?.TryGetPosition(iCurrentLine, iCurrentChar);
            if (position == null)
            {
                pbstrDescription = null;
                pfBlockAvailable = 0;
                return VSConstants.S_OK;
            }

            var waitIndicator = this.Package.ComponentModel.GetService<IWaitIndicator>();
            waitIndicator.Wait(
                ServicesVSResources.CurrentBlock,
                ServicesVSResources.DeterminingCurrentBlock,
                allowCancel: true,
                action: context =>
                {
                    foundBlock = VsLanguageBlock.GetCurrentBlock(snapshot, position.Value, context.CancellationToken, ref description, ref span);
                });

            pfBlockAvailable = foundBlock ? 1 : 0;
            pbstrDescription = description;

            if (ptsBlockSpan != null && ptsBlockSpan.Length >= 1)
            {
                ptsBlockSpan[0] = span.ToSnapshotSpan(snapshot).ToVsTextSpan();
            }
            return VSConstants.S_OK;
        }
    }

    internal static class VsLanguageBlock
    {
        public static bool GetCurrentBlock(
            ITextSnapshot snapshot,
            int position,
            CancellationToken cancellationToken,
            ref string description,
            ref TextSpan span)
        {
            var document = snapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null || !document.SupportsSyntaxTree)
            {
                return false;
            }

            var syntaxFactsService = document.Project.LanguageServices.GetService<ISyntaxFactsService>();
            var syntaxRoot = document.GetSyntaxRootAsync(cancellationToken).WaitAndGetResult(cancellationToken);
            var node = syntaxFactsService.GetContainingMemberDeclaration(syntaxRoot, position, useFullSpan: false);
            if (node == null)
            {
                return false;
            }

            description = syntaxFactsService.GetDisplayName(node,
                DisplayNameOptions.IncludeMemberKeyword |
                DisplayNameOptions.IncludeParameters |
                DisplayNameOptions.IncludeType |
                DisplayNameOptions.IncludeTypeParameters);
            span = node.Span;
            return true;
        }
    }
}
