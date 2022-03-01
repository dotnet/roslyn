// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.SignatureHelp;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.Text.Adornments;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    [ExportRoslynLanguagesLspRequestHandlerProvider(typeof(SignatureHelpHandler)), Shared]
    [Method(LSP.Methods.TextDocumentSignatureHelpName)]
    internal class SignatureHelpHandler : AbstractStatelessRequestHandler<LSP.TextDocumentPositionParams, LSP.SignatureHelp?>
    {
        private readonly IEnumerable<Lazy<ISignatureHelpProvider, OrderableLanguageMetadata>> _allProviders;
        private readonly IGlobalOptionService _globalOptions;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public SignatureHelpHandler(
            [ImportMany] IEnumerable<Lazy<ISignatureHelpProvider, OrderableLanguageMetadata>> allProviders,
            IGlobalOptionService globalOptions)
        {
            _allProviders = allProviders;
            _globalOptions = globalOptions;
        }

        public override bool MutatesSolutionState => false;
        public override bool RequiresLSPSolution => true;

        public override LSP.TextDocumentIdentifier? GetTextDocumentIdentifier(LSP.TextDocumentPositionParams request) => request.TextDocument;

        public override async Task<LSP.SignatureHelp?> HandleRequestAsync(LSP.TextDocumentPositionParams request, RequestContext context, CancellationToken cancellationToken)
        {
            var document = context.Document;
            if (document == null)
                return null;

            var position = await document.GetPositionFromLinePositionAsync(ProtocolConversions.PositionToLinePosition(request.Position), cancellationToken).ConfigureAwait(false);

            var providers = _allProviders.Where(p => p.Metadata.Language == document.Project.Language);
            var triggerInfo = new SignatureHelpTriggerInfo(SignatureHelpTriggerReason.InvokeSignatureHelpCommand);
            var options = _globalOptions.GetSignatureHelpOptions(document.Project.Language);

            foreach (var provider in providers)
            {
                var items = await provider.Value.GetItemsAsync(document, position, triggerInfo, options, cancellationToken).ConfigureAwait(false);

                if (items != null)
                {
                    var sigInfos = new ArrayBuilder<LSP.SignatureInformation>();

                    foreach (var item in items.Items)
                    {
                        LSP.SignatureInformation sigInfo;
                        if (context.ClientCapabilities?.HasVisualStudioLspCapability() == true)
                        {
                            sigInfo = new LSP.VSInternalSignatureInformation
                            {
                                ColorizedLabel = GetSignatureClassifiedText(item)
                            };
                        }
                        else
                        {
                            sigInfo = new LSP.SignatureInformation();
                        }

                        sigInfo.Label = GetSignatureText(item);
                        sigInfo.Documentation = new LSP.MarkupContent { Kind = LSP.MarkupKind.PlainText, Value = item.DocumentationFactory(cancellationToken).GetFullText() };
                        sigInfo.Parameters = item.Parameters.Select(p => new LSP.ParameterInformation
                        {
                            Label = p.Name,
                            Documentation = new LSP.MarkupContent { Kind = LSP.MarkupKind.PlainText, Value = p.DocumentationFactory(cancellationToken).GetFullText() }
                        }).ToArray();
                        sigInfos.Add(sigInfo);
                    }

                    var sigHelp = new LSP.SignatureHelp
                    {
                        ActiveSignature = GetActiveSignature(items),
                        ActiveParameter = items.ArgumentIndex,
                        Signatures = sigInfos.ToArrayAndFree()
                    };

                    return sigHelp;
                }
            }

            return null;
        }

        private static int GetActiveSignature(SignatureHelpItems items)
        {
            if (items.SelectedItemIndex.HasValue)
            {
                return items.SelectedItemIndex.Value;
            }

            // From Roslyn's doc comments for SelectedItemIndex:
            //   If this is null, then the controller will pick the first item that has enough arguments
            //   to be viable based on what argument position the user is currently inside of.
            // However, the LSP spec expects the language server to make this decision.
            // So implement the logic of picking a signature that has enough arguments here.

            var matchingSignature = items.Items.FirstOrDefault(sig => sig.Parameters.Length > items.ArgumentIndex);
            return matchingSignature != null ? items.Items.IndexOf(matchingSignature) : 0;
        }

        /// <summary>
        /// The <see cref="SignatureHelpItem"/> contains a prefix, parameters separated by a
        /// separator and a suffix. Parameters themselves have a prefix, display and suffix.
        /// Concatenate them all to get the text.
        /// </summary>
        private static string GetSignatureText(SignatureHelpItem item)
        {
            var sb = new StringBuilder();

            sb.Append(item.PrefixDisplayParts.GetFullText());

            var separators = item.SeparatorDisplayParts.GetFullText();
            for (var i = 0; i < item.Parameters.Length; i++)
            {
                var param = item.Parameters[i];

                if (i > 0)
                {
                    sb.Append(separators);
                }

                sb.Append(param.PrefixDisplayParts.GetFullText());
                sb.Append(param.DisplayParts.GetFullText());
                sb.Append(param.SuffixDisplayParts.GetFullText());
            }

            sb.Append(item.SuffixDisplayParts.GetFullText());
            sb.Append(item.DescriptionParts.GetFullText());

            return sb.ToString();
        }
        private static ClassifiedTextElement GetSignatureClassifiedText(SignatureHelpItem item)
        {
            var taggedTexts = new ArrayBuilder<TaggedText>();

            taggedTexts.AddRange(item.PrefixDisplayParts);

            var separators = item.SeparatorDisplayParts;
            for (var i = 0; i < item.Parameters.Length; i++)
            {
                var param = item.Parameters[i];

                if (i > 0)
                {
                    taggedTexts.AddRange(separators);
                }

                taggedTexts.AddRange(param.PrefixDisplayParts);
                taggedTexts.AddRange(param.DisplayParts);
                taggedTexts.AddRange(param.SuffixDisplayParts);
            }

            taggedTexts.AddRange(item.SuffixDisplayParts);
            taggedTexts.AddRange(item.DescriptionParts);

            return new ClassifiedTextElement(taggedTexts.ToArrayAndFree().Select(part => new ClassifiedTextRun(part.Tag.ToClassificationTypeName(), part.Text)));
        }
    }
}
