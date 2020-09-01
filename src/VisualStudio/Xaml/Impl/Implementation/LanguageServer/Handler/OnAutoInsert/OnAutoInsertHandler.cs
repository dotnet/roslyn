// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Xaml;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.LanguageServices.Xaml.Features.AutoInsert;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.VisualStudio.LanguageServices.Xaml.LanguageServer.Handler
{
    [Shared]
    [ExportLspMethod(LSP.MSLSPMethods.OnAutoInsertName, StringConstants.XamlLanguageName)]
    internal class OnAutoInsertHandler : AbstractRequestHandler<LSP.DocumentOnAutoInsertParams, LSP.DocumentOnAutoInsertResponseItem[]>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public OnAutoInsertHandler(ILspSolutionProvider solutionProvider) : base(solutionProvider)
        {
        }

        public override async Task<LSP.DocumentOnAutoInsertResponseItem[]> HandleRequestAsync(LSP.DocumentOnAutoInsertParams autoInsertParams, RequestContext context, CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<LSP.DocumentOnAutoInsertResponseItem>.GetInstance(out var response);

            var document = SolutionProvider.GetTextDocument(autoInsertParams.TextDocument, context.ClientName);
            if (document == null)
            {
                return response.ToArray();
            }

            var insertService = document.Project.LanguageServices.GetService<IXamlAutoInsertService>();
            if (insertService == null)
            {
                return response.ToArray();
            }

            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var offset = text.Lines.GetPosition(ProtocolConversions.PositionToLinePosition(autoInsertParams.Position));
            var result = await insertService.GetAutoInsertAsync(document, autoInsertParams.Character[0], offset, cancellationToken).ConfigureAwait(false);
            if (result == null)
            {
                return response.ToArray();
            }

            var insertText = result.TextChange.NewText;
            var insertFormat = LSP.InsertTextFormat.Plaintext;
            if (result.CaretOffset.HasValue)
            {
                insertFormat = LSP.InsertTextFormat.Snippet;
                insertText = insertText?.Insert(result.CaretOffset.Value, "$0");
            }

            response.Add(new LSP.DocumentOnAutoInsertResponseItem
            {
                TextEditFormat = insertFormat,
                TextEdit = new LSP.TextEdit
                {
                    NewText = insertText,
                    Range = ProtocolConversions.TextSpanToRange(result.TextChange.Span, text)
                }
            });

            return response.ToArray();
        }
    }
}
