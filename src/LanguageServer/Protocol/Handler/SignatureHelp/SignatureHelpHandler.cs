// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.SignatureHelp;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Text.Adornments;
using Roslyn.Utilities;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler;

[ExportCSharpVisualBasicStatelessLspService(typeof(SignatureHelpHandler)), Shared]
[Method(LSP.Methods.TextDocumentSignatureHelpName)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class SignatureHelpHandler(SignatureHelpService signatureHelpService) : ILspServiceDocumentRequestHandler<LSP.TextDocumentPositionParams, LSP.SignatureHelp?>
{
    public bool MutatesSolutionState => false;
    public bool RequiresLSPSolution => true;

    public LSP.TextDocumentIdentifier GetTextDocumentIdentifier(LSP.TextDocumentPositionParams request) => request.TextDocument;

    public Task<LSP.SignatureHelp?> HandleRequestAsync(LSP.TextDocumentPositionParams request, RequestContext context, CancellationToken cancellationToken)
    {
        var document = context.Document;
        if (document == null)
            return SpecializedTasks.Null<LSP.SignatureHelp>();

        var supportsVisualStudioExtensions = context.GetRequiredClientCapabilities().HasVisualStudioLspCapability();
        var linePosition = ProtocolConversions.PositionToLinePosition(request.Position);
        return GetSignatureHelpAsync(signatureHelpService, document, linePosition, supportsVisualStudioExtensions, cancellationToken);
    }

    internal static async Task<LSP.SignatureHelp?> GetSignatureHelpAsync(SignatureHelpService signatureHelpService, Document document, LinePosition linePosition, bool supportsVisualStudioExtensions, CancellationToken cancellationToken)
    {
        var position = await document.GetPositionFromLinePositionAsync(linePosition, cancellationToken).ConfigureAwait(false);
        var triggerInfo = new SignatureHelpTriggerInfo(SignatureHelpTriggerReason.InvokeSignatureHelpCommand);

        var (_, sigItems) = await signatureHelpService.GetSignatureHelpAsync(document, position, triggerInfo, cancellationToken).ConfigureAwait(false);
        if (sigItems is null)
        {
            return null;
        }
        using var _ = ArrayBuilder<LSP.SignatureInformation>.GetInstance(out var sigInfos);
        foreach (var item in sigItems.Items)
        {
            LSP.SignatureInformation sigInfo;
            if (supportsVisualStudioExtensions)
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
            sigInfo.Parameters = [.. item.Parameters.Select(p => new LSP.ParameterInformation
            {
                Label = p.Name,
                Documentation = new LSP.MarkupContent { Kind = LSP.MarkupKind.PlainText, Value = p.DocumentationFactory(cancellationToken).GetFullText() }
            })];
            sigInfos.Add(sigInfo);
        }

        var sigHelp = new LSP.SignatureHelp
        {
            ActiveSignature = GetActiveSignature(sigItems),
            ActiveParameter = sigItems.SemanticParameterIndex,
            Signatures = sigInfos.ToArray()
        };

        return sigHelp;
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

        var matchingSignature = items.Items.FirstOrDefault(
            sig => sig.Parameters.Length > items.SemanticParameterIndex);
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
        using var _ = ArrayBuilder<TaggedText>.GetInstance(out var taggedTexts);

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

        return new ClassifiedTextElement(taggedTexts.ToArray().Select(part => new ClassifiedTextRun(part.Tag.ToClassificationTypeName(), part.Text)));
    }
}
