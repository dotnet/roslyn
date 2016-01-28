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
using System.Threading.Tasks;
using System;

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
            Tuple<string, TextSpan> foundBlock = null;

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
                    foundBlock = VsLanguageBlock.GetCurrentBlockAsync(snapshot, position.Value, context.CancellationToken).WaitAndGetResult(context.CancellationToken);
                });

            pfBlockAvailable = foundBlock != null ? 1 : 0;
            pbstrDescription = foundBlock?.Item1;

            if (foundBlock != null && ptsBlockSpan != null && ptsBlockSpan.Length >= 1)
            {
                ptsBlockSpan[0] = foundBlock.Item2.ToSnapshotSpan(snapshot).ToVsTextSpan();
            }

            return VSConstants.S_OK;
        }
    }

    internal static class VsLanguageBlock
    {
        public static async Task<Tuple<string, TextSpan>> GetCurrentBlockAsync(
            ITextSnapshot snapshot,
            int position,
            CancellationToken cancellationToken)
        {
            var document = snapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null || !document.SupportsSyntaxTree)
            {
                return null;
            }

            var syntaxFactsService = document.Project.LanguageServices.GetService<ISyntaxFactsService>();
            var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var node = syntaxFactsService.GetContainingMemberDeclaration(syntaxRoot, position, useFullSpan: false);
            if (node == null)
            {
                return null;
            }

            string description = syntaxFactsService.GetDisplayName(node,
                DisplayNameOptions.IncludeMemberKeyword |
                DisplayNameOptions.IncludeParameters |
                DisplayNameOptions.IncludeType |
                DisplayNameOptions.IncludeTypeParameters);

            return Tuple.Create(description, node.Span);
        }
    }
}
