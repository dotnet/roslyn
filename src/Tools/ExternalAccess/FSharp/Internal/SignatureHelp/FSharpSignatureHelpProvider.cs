// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.SignatureHelp;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.SignatureHelp;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Internal.SignatureHelp
{
    [Shared]
    [ExportSignatureHelpProvider(nameof(FSharpSignatureHelpProvider), LanguageNames.FSharp)]
    internal class FSharpSignatureHelpProvider : ISignatureHelpProvider
    {
        private readonly IFSharpSignatureHelpProvider _provider;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public FSharpSignatureHelpProvider(IFSharpSignatureHelpProvider provider)
        {
            _provider = provider;
        }

        public async Task<SignatureHelpItems> GetItemsAsync(Document document, int position, SignatureHelpTriggerInfo triggerInfo, CancellationToken cancellationToken)
        {
            var mappedTriggerReason = FSharpSignatureHelpTriggerReasonHelpers.ConvertFrom(triggerInfo.TriggerReason);
            var mappedTriggerInfo = new FSharpSignatureHelpTriggerInfo(mappedTriggerReason, triggerInfo.TriggerCharacter);
            var mappedSignatureHelpItems = await _provider.GetItemsAsync(document, position, mappedTriggerInfo, cancellationToken).ConfigureAwait(false);

            if (mappedSignatureHelpItems != null)
            {
                return new SignatureHelpItems(
                    mappedSignatureHelpItems.Items?.Select(x =>
                        new SignatureHelpItem(
                            x.IsVariadic,
                            x.DocumentationFactory,
                            x.PrefixDisplayParts,
                            x.SeparatorDisplayParts,
                            x.SuffixDisplayParts,
                            x.Parameters.Select(y =>
                                new SignatureHelpParameter(
                                    y.Name,
                                    y.IsOptional,
                                    y.DocumentationFactory,
                                    y.DisplayParts,
                                    y.PrefixDisplayParts,
                                    y.SuffixDisplayParts,
                                    y.SelectedDisplayParts)).ToList(),
                            x.DescriptionParts)).ToList(),
                    mappedSignatureHelpItems.ApplicableSpan,
                    mappedSignatureHelpItems.ArgumentIndex,
                    mappedSignatureHelpItems.ArgumentCount,
                    mappedSignatureHelpItems.ArgumentName,
                    mappedSignatureHelpItems.SelectedItemIndex);
            }
            else
            {
                return null;
            }
        }

        public bool IsRetriggerCharacter(char ch)
        {
            return _provider.IsRetriggerCharacter(ch);
        }

        public bool IsTriggerCharacter(char ch)
        {
            return _provider.IsTriggerCharacter(ch);
        }
    }
}
