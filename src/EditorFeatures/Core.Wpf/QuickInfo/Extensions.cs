// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

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
            var cloneServices = workspace.Services.SolutionServices.ExportProvider.GetExports<ITextBufferCloneService>();
            foreach (var cloneService in cloneServices)
                return cloneService.Value.CloneWithRoslynContentType(sourceText);

            throw ExceptionUtilities.Unreachable();
        }

        /// <summary>
        /// clone content of <paramref name="sourceText"/> to new <see cref="ITextBuffer"/>
        /// with content type of the document
        /// </summary>
        public static ITextBuffer CloneTextBuffer(this Document document, SourceText sourceText)
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
        public static async Task<ITextBuffer> CloneTextBufferAsync(this Document document, CancellationToken cancellationToken)
        {
            var sourceText = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
            return CloneTextBuffer(document, sourceText);
        }
    }
}
