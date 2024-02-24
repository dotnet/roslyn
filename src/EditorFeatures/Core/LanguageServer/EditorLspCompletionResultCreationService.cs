// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Completion;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Core.Imaging;
using Microsoft.VisualStudio.Imaging;
using Roslyn.Text.Adornments;
using Roslyn.Utilities;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer
{
    [ExportWorkspaceService(typeof(ILspCompletionResultCreationService), ServiceLayer.Editor), Shared]
    internal sealed class EditorLspCompletionResultCreationService : AbstractLspCompletionResultCreationService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public EditorLspCompletionResultCreationService()
        {
        }

        protected override async Task<LSP.CompletionItem> CreateItemAndPopulateTextEditAsync(
            Document document,
            SourceText documentText,
            bool snippetsSupported,
            bool itemDefaultsSupported,
            TextSpan defaultSpan,
            string typedText,
            CompletionItem item,
            LSP.CompletionItemKind kind,
            CompletionService completionService,
            CancellationToken cancellationToken)
        {
            var lspItem = new LSP.VSInternalCompletionItem
            {
                Label = item.GetEntireDisplayText(),
            };

            var imageId = item.Tags.GetFirstGlyph().GetImageId();
            if (imageId != GetCompletionImageId(kind))
            {
                // Only populate the icon if it differs from the default for the kind. The icon information
                // is quite verbose in the serialized json.
                lspItem.Icon = new ImageElement(imageId.ToLSPImageId());
            }

            // Complex text edits (e.g. override and partial method completions) are always populated in the
            // resolve handler, so we leave both TextEdit and InsertText unpopulated in these cases.
            if (item.IsComplexTextEdit)
            {
                lspItem.VsResolveTextEditOnCommit = true;

                // Razor C# is currently the only language client that supports LSP.InsertTextFormat.Snippet.
                // We can enable it for regular C# once LSP is used for local completion.
                if (snippetsSupported)
                    lspItem.InsertTextFormat = LSP.InsertTextFormat.Snippet;
            }
            else
            {
                await GetChangeAndPopulateSimpleTextEditAsync(
                    document,
                    documentText,
                    itemDefaultsSupported,
                    defaultSpan,
                    item,
                    lspItem,
                    completionService,
                    cancellationToken).ConfigureAwait(false);
            }

            return lspItem;
        }

        public override async Task<LSP.CompletionItem> ResolveAsync(
            LSP.CompletionItem lspItem,
            CompletionItem roslynItem,
            LSP.TextDocumentIdentifier textDocumentIdentifier,
            Document document,
            CompletionCapabilityHelper capabilityHelper,
            CompletionService completionService,
            CompletionOptions completionOptions,
            SymbolDescriptionOptions symbolDescriptionOptions,
            CancellationToken cancellationToken)
        {
            var description = await completionService.GetDescriptionAsync(document, roslynItem, completionOptions, symbolDescriptionOptions, cancellationToken).ConfigureAwait(false)!;
            if (description != null)
            {
                if (capabilityHelper.SupportVSInternalClientCapabilities)
                {
                    var vsCompletionItem = (LSP.VSInternalCompletionItem)lspItem;
                    vsCompletionItem.Description = new ClassifiedTextElement(description.TaggedParts
                        .Select(tp => new ClassifiedTextRun(tp.Tag.ToClassificationTypeName(), tp.Text)));
                }
                else
                {
                    lspItem.Documentation = ProtocolConversions.GetDocumentationMarkupContent(description.TaggedParts, document, capabilityHelper.SupportsMarkdownDocumentation);
                }
            }

            // We compute the TextEdit resolves for complex text edits (e.g. override and partial
            // method completions) here. Lazily resolving TextEdits is technically a violation of
            // the LSP spec, but is currently supported by the VS client anyway. Once the VS client
            // adheres to the spec, this logic will need to change and VS will need to provide
            // official support for TextEdit resolution in some form.
            if (roslynItem.IsComplexTextEdit)
            {
                Contract.ThrowIfTrue(lspItem.InsertText != null);
                Contract.ThrowIfTrue(lspItem.TextEdit != null);

                var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
                var (edit, _, _) = await GenerateComplexTextEditAsync(
                    document, completionService, roslynItem, capabilityHelper.SupportSnippets, insertNewPositionPlaceholder: true, cancellationToken).ConfigureAwait(false);

                lspItem.TextEdit = edit;
            }

            return lspItem;
        }

        private static ImageId GetCompletionImageId(LSP.CompletionItemKind kind)
        {
            var id = kind switch
            {
                LSP.CompletionItemKind.Text => KnownImageIds.TextElement,
                LSP.CompletionItemKind.Method => KnownImageIds.MethodPublic,
                LSP.CompletionItemKind.Function => KnownImageIds.MethodPublic,
                LSP.CompletionItemKind.Constructor => KnownImageIds.ClassPublic,
                LSP.CompletionItemKind.Field => KnownImageIds.FieldPrivate,
                LSP.CompletionItemKind.Variable => KnownImageIds.LocalVariable,
                LSP.CompletionItemKind.Class => KnownImageIds.ClassPublic,
                LSP.CompletionItemKind.Interface => KnownImageIds.InterfacePublic,
                LSP.CompletionItemKind.Module => KnownImageIds.ModulePublic,
                LSP.CompletionItemKind.Property => KnownImageIds.PropertyPublic,
                LSP.CompletionItemKind.Unit => KnownImageIds.Numeric,
                LSP.CompletionItemKind.Value => KnownImageIds.ValueType,
                LSP.CompletionItemKind.Enum => KnownImageIds.EnumerationPublic,
                LSP.CompletionItemKind.Keyword => KnownImageIds.IntellisenseKeyword,
                LSP.CompletionItemKind.Snippet => KnownImageIds.Snippet,
                LSP.CompletionItemKind.Color => KnownImageIds.ColorPalette,
                LSP.CompletionItemKind.File => KnownImageIds.TextFile,
                LSP.CompletionItemKind.Reference => KnownImageIds.Reference,
                LSP.CompletionItemKind.Folder => KnownImageIds.FolderClosed,
                LSP.CompletionItemKind.EnumMember => KnownImageIds.EnumerationItemPublic,
                LSP.CompletionItemKind.Constant => KnownImageIds.ConstantPublic,
                LSP.CompletionItemKind.Struct => KnownImageIds.ValueTypePublic, // We're using ValueTypePublic instead of StructurePublic because that's what language partners have been using for structs
                LSP.CompletionItemKind.Event => KnownImageIds.EventPublic,
                LSP.CompletionItemKind.Operator => KnownImageIds.Operator,
                LSP.CompletionItemKind.TypeParameter => KnownImageIds.Type,
                LSP.CompletionItemKind.Macro => KnownImageIds.MacroPublic,
                LSP.CompletionItemKind.Namespace => KnownImageIds.Namespace,
                LSP.CompletionItemKind.Template => KnownImageIds.Template,
                LSP.CompletionItemKind.TypeDefinition => KnownImageIds.TypeDefinition,
                LSP.CompletionItemKind.Union => KnownImageIds.Union,
                LSP.CompletionItemKind.Delegate => KnownImageIds.Delegate,
                LSP.CompletionItemKind.TagHelper => KnownImageIds.XMLAttribute,
                LSP.CompletionItemKind.ExtensionMethod => KnownImageIds.ExtensionMethod,
                LSP.CompletionItemKind.Element => KnownImageIds.MarkupTag,
                LSP.CompletionItemKind.LocalResource => KnownImageIds.LocalResources,
                LSP.CompletionItemKind.SystemResource => KnownImageIds.SystemResources,
                LSP.CompletionItemKind.CloseElement => KnownImageIds.HTMLEndTag,
                _ => KnownImageIds.TextElement, // Gracefully handle missing and unknown kinds
            };

            return new ImageId(KnownImageIds.ImageCatalogGuid, id);
        }
    }
}
