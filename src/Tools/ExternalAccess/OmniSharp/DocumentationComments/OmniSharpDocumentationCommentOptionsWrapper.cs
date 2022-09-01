// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.DocumentationComments;

namespace Microsoft.CodeAnalysis.ExternalAccess.OmniSharp.DocumentationComments
{
    internal readonly struct OmniSharpDocumentationCommentOptionsWrapper
    {
        internal readonly DocumentationCommentOptions UnderlyingObject;

        internal OmniSharpDocumentationCommentOptionsWrapper(DocumentationCommentOptions underlyingObject)
            => UnderlyingObject = underlyingObject;

        public OmniSharpDocumentationCommentOptionsWrapper(
            bool autoXmlDocCommentGeneration,
            int tabSize,
            bool useTabs,
            string newLine)
            : this(new(autoXmlDocCommentGeneration, tabSize, useTabs, newLine))
        {
        }

        public static async ValueTask<OmniSharpDocumentationCommentOptionsWrapper> FromDocumentAsync(
            Document document,
            bool autoXmlDocCommentGeneration,
            CancellationToken cancellationToken)
        {
            var documentOptions = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
            return new(DocumentationCommentOptions.From(documentOptions) with { AutoXmlDocCommentGeneration = autoXmlDocCommentGeneration });
        }
    }
}
