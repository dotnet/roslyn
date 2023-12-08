// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.ExternalAccess.OmniSharp.Options;
using Microsoft.CodeAnalysis.Formatting;

namespace Microsoft.CodeAnalysis.ExternalAccess.OmniSharp.DocumentationComments
{
    internal readonly struct OmniSharpDocumentationCommentOptionsWrapper
    {
        internal readonly DocumentationCommentOptions UnderlyingObject;

        internal OmniSharpDocumentationCommentOptionsWrapper(DocumentationCommentOptions underlyingObject)
            => UnderlyingObject = underlyingObject;

        public OmniSharpDocumentationCommentOptionsWrapper(
            bool autoXmlDocCommentGeneration,
            OmniSharpLineFormattingOptions lineFormattingOptions)
            : this(new DocumentationCommentOptions()
            {
                LineFormatting = new LineFormattingOptions()
                {
                    UseTabs = lineFormattingOptions.UseTabs,
                    TabSize = lineFormattingOptions.TabSize,
                    IndentationSize = lineFormattingOptions.IndentationSize,
                    NewLine = lineFormattingOptions.NewLine,
                },
                AutoXmlDocCommentGeneration = autoXmlDocCommentGeneration
            })
        {
        }

        public static async ValueTask<OmniSharpDocumentationCommentOptionsWrapper> FromDocumentAsync(
            Document document,
            bool autoXmlDocCommentGeneration,
            CancellationToken cancellationToken)
        {
            var formattingOptions = await document.GetSyntaxFormattingOptionsAsync(CodeActionOptions.DefaultProvider, cancellationToken).ConfigureAwait(false);

            return new(new() { LineFormatting = formattingOptions.LineFormatting, AutoXmlDocCommentGeneration = autoXmlDocCommentGeneration });
        }
    }
}
