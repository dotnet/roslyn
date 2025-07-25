// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.QuickInfo;

internal static class Extensions
{
    extension(SourceText sourceText)
    {
        /// <summary>
        /// clone content of <paramref name="sourceText"/> to new <see cref="ITextBuffer"/>
        /// with <see cref="ContentTypeNames.RoslynContentType"/>
        /// </summary>
        public ITextBuffer CreateTextBufferWithRoslynContentType(Workspace workspace)
        {
            var cloneServices = workspace.Services.SolutionServices.ExportProvider.GetExports<ITextBufferCloneService>();
            foreach (var cloneService in cloneServices)
                return cloneService.Value.CloneWithRoslynContentType(sourceText);

            throw ExceptionUtilities.Unreachable();
        }
    }

    extension(Document document)
    {
        /// <summary>
        /// clone content of <paramref name="sourceText"/> to new <see cref="ITextBuffer"/>
        /// with content type of the document
        /// </summary>
        public ITextBuffer CloneTextBuffer(SourceText sourceText)
        {
            var contentTypeService = document.Project.Services.GetService<IContentTypeLanguageService>();
            var contentType = contentTypeService.GetDefaultContentType();

            var cloneServices = document.Project.Solution.Services.ExportProvider.GetExports<ITextBufferCloneService>();
            foreach (var cloneService in cloneServices)
                return cloneService.Value.Clone(sourceText, contentType);

            throw ExceptionUtilities.Unreachable();
        }

        /// <summary>
        /// async version of <see cref="CloneTextBuffer(Document, SourceText)"/>
        /// </summary>
        public async Task<ITextBuffer> CloneTextBufferAsync(CancellationToken cancellationToken)
        {
            var sourceText = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
            return CloneTextBuffer(document, sourceText);
        }
    }
}
