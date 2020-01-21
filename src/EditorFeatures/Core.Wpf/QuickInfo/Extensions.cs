// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.QuickInfo
{
    internal static class Extensions
    {
        /// <summary>
        /// clone content of <paramref name="sourceText"/> to new <see cref="ITextBuffer"/>
        /// with <see cref="ContentTypeNames.RoslynContentType"/>
        /// </summary>
        public static ITextBuffer CreateTextBufferWithRoslynContentType(this SourceText sourceText, Workspace workspace)
        {
            var cloneService = workspace.Services.GetService<ITextBufferCloneService>();
            return cloneService.CloneWithRoslynContentType(sourceText);
        }

        /// <summary>
        /// clone content of <paramref name="sourceText"/> to new <see cref="ITextBuffer"/>
        /// with content type of the document
        /// </summary>
        public static ITextBuffer CloneTextBuffer(this Document document, SourceText sourceText)
        {
            var contentTypeService = document.Project.LanguageServices.GetService<IContentTypeLanguageService>();
            var contentType = contentTypeService.GetDefaultContentType();

            var cloneService = document.Project.Solution.Workspace.Services.GetService<ITextBufferCloneService>();
            return cloneService.Clone(sourceText, contentType);
        }

        /// <summary>
        /// async version of <see cref="CloneTextBuffer(Document, SourceText)"/>
        /// </summary>
        public static async Task<ITextBuffer> CloneTextBufferAsync(this Document document, CancellationToken cancellationToken)
        {
            var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            return CloneTextBuffer(document, sourceText);
        }
    }
}
