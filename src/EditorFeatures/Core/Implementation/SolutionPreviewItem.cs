// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor
{
    internal class SolutionPreviewItem
    {
        public readonly ProjectId? ProjectId;
        public readonly DocumentId? DocumentId;
        public readonly Func<CancellationToken, Task<object?>> LazyPreview;
        public readonly string? Text;

        /// <summary>
        /// Construct an instance of <see cref="SolutionPreviewItem"/>
        /// </summary>
        /// <param name="projectId"><see cref="ProjectId"/> for the <see cref="Project"/> that contains the content being visualized in the supplied <paramref name="lazyPreview"/></param>
        /// <param name="documentId"><see cref="DocumentId"/> for the <see cref="Document"/> being visualized in the supplied <paramref name="lazyPreview"/></param>
        /// <param name="lazyPreview">Lazily instantiated preview content.</param>
        /// <remarks>Use lazy instantiation to ensure that any <see cref="ITextView"/> that may be present inside a given preview are only instantiated at the point
        /// when the VS lightbulb requests that preview. Otherwise, we could end up instantiating a bunch of <see cref="ITextView"/>s most of which will never get
        /// passed to the VS lightbulb. Such zombie <see cref="ITextView"/>s will never get closed and we will end up leaking memory.</remarks>
        public SolutionPreviewItem(ProjectId? projectId, DocumentId? documentId, Func<CancellationToken, Task<object?>> lazyPreview)
        {
            ProjectId = projectId;
            DocumentId = documentId;
            LazyPreview = lazyPreview;
        }

        public SolutionPreviewItem(ProjectId? projectId, DocumentId? documentId, string text)
            : this(projectId, documentId, c => Task.FromResult<object?>(text))
        {
            Text = text;
        }
    }
}
