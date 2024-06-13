// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.QuickInfo;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    internal interface ILspHoverResultCreationService : IWorkspaceService
    {
        Task<Hover> CreateHoverAsync(
            Document document, QuickInfoItem info, ClientCapabilities clientCapabilities, CancellationToken cancellationToken);
    }

    [ExportWorkspaceService(typeof(ILspHoverResultCreationService)), Shared]
    internal sealed class DefaultLspHoverResultCreationService : ILspHoverResultCreationService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DefaultLspHoverResultCreationService()
        {
        }

        public Task<Hover> CreateHoverAsync(Document document, QuickInfoItem info, ClientCapabilities clientCapabilities, CancellationToken cancellationToken)
            => CreateDefaultHoverAsync(document, info, clientCapabilities, cancellationToken);

        public static async Task<Hover> CreateDefaultHoverAsync(TextDocument document, QuickInfoItem info, ClientCapabilities clientCapabilities, CancellationToken cancellationToken)
        {
            var clientSupportsMarkdown = clientCapabilities?.TextDocument?.Hover?.ContentFormat?.Contains(MarkupKind.Markdown) == true;

            // Insert line breaks in between sections to ensure we get double spacing between sections.
            var tags = info.Sections
                .SelectMany(section => section.TaggedParts.Add(new TaggedText(TextTags.LineBreak, Environment.NewLine)))
                .ToImmutableArray();

            var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
            var language = document.Project.Language;

            return new Hover
            {
                Range = ProtocolConversions.TextSpanToRange(info.Span, text),
                Contents = ProtocolConversions.GetDocumentationMarkupContent(tags, language, clientSupportsMarkdown),
            };
        }
    }
}
