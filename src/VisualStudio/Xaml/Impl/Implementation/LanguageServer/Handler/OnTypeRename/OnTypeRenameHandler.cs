// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Xaml;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.LanguageServices.Xaml.Features.TypeRename;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Xaml.LanguageServer.Handler
{
    [ExportXamlLspRequestHandlerProvider(typeof(OnTypeRenameHandler)), Shared]
    [Method(Methods.TextDocumentLinkedEditingRangeName)]
    internal class OnTypeRenameHandler : AbstractStatelessRequestHandler<LinkedEditingRangeParams, LinkedEditingRanges?>
    {
        // From https://www.w3.org/TR/xml/#NT-NameStartChar
        // Notes:
        //     \u10000-\uEFFFF isn't included as .NET regular expressions only allow 4 chars after \u.
        //     The : shouldn't really be used as start character either so included it in the name char pattern.
        //     We want to allow complete removal and replacement of names so we need to make the start char
        //     optional in the name char pattern.

        // NameStartChar ::= ":" | [A-Z] | "_" | [a-z] | [#xC0-#xD6] | [#xD8-#xF6] | [#xF8-#x2FF] | [#x370-#x37D] | [#x37F-#x1FFF] | [#x200C-#x200D] | [#x2070-#x218F] | [#x2C00-#x2FEF] | [#x3001-#xD7FF] | [#xF900-#xFDCF] | [#xFDF0-#xFFFD] | [#x10000-#xEFFFF]
        private const string NameStartCharPattern = "A-Z_a-z" +
                                                    "\\u00C0-\\u00D6" +
                                                    "\\u00D8-\\u00F6" +
                                                    "\\u00F8-\\u02FF" +
                                                    "\\u0370-\\u037D" +
                                                    "\\u037F-\\u1FFF" +
                                                    "\\u200C-\\u200D" +
                                                    "\\u2070-\\u218F" +
                                                    "\\u2C00-\\u2FEF" +
                                                    "\\u3001-\\uD7FF" +
                                                    "\\uF900-\\uFDCF" +
                                                    "\\uFDF0-\\uFFFD";

        // NameChar ::= NameStartChar | "-" | "." | [0-9] | #xB7 | [#x0300-#x036F] | [#x203F-#x2040]
        private const string NameCharPattern = NameStartCharPattern +
                                                    ":\\-.0-9" +
                                                    "\\u00B7" +
                                                    "\\u0300-\\u036F" +
                                                    "\\u203F-\\u2040";

        // Name ::= NameStartChar (NameChar)*
        internal const string NamePattern = $"[{NameStartCharPattern}]?[{NameCharPattern}]*";

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public OnTypeRenameHandler()
        {
        }

        public override bool MutatesSolutionState => false;
        public override bool RequiresLSPSolution => true;

        public override TextDocumentIdentifier? GetTextDocumentIdentifier(LinkedEditingRangeParams request) => request.TextDocument;

        public override async Task<LinkedEditingRanges?> HandleRequestAsync(LinkedEditingRangeParams request, RequestContext context, CancellationToken cancellationToken)
        {
            var document = context.Document;
            if (document == null)
            {
                return null;
            }

            var renameService = document.Project.LanguageServices.GetService<IXamlTypeRenameService>();
            if (renameService == null)
            {
                return null;
            }

            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var offset = text.Lines.GetPosition(ProtocolConversions.PositionToLinePosition(request.Position));

            var result = await renameService.GetTypeRenameAsync(document, offset, cancellationToken).ConfigureAwait(false);
            if (result == null)
            {
                return null;
            }

            Contract.ThrowIfTrue(result.Ranges.IsDefault);

            return new LinkedEditingRanges
            {
                Ranges = result.Ranges.Select(s => ProtocolConversions.TextSpanToRange(s, text)).ToArray(),
                WordPattern = result.WordPattern
            };
        }
    }
}
