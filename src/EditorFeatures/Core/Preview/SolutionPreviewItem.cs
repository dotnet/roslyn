// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor
{
    /// <summary>
    /// Construct an instance of <see cref="SolutionPreviewItem"/>
    /// </summary>
    /// <param name="projectId"><see cref="ProjectId"/> for the <see cref="Project"/> that contains the content being visualized in the supplied <paramref name="lazyPreviewAsync"/></param>
    /// <param name="documentId"><see cref="DocumentId"/> for the <see cref="Document"/> being visualized in the supplied <paramref name="lazyPreviewAsync"/></param>
    /// <param name="lazyPreviewAsync">Lazily instantiated preview content.</param>
    /// <remarks>Use lazy instantiation to ensure that any <see cref="ITextView"/> that may be present inside a given preview are only instantiated at the point
    /// when the VS lightbulb requests that preview. Otherwise, we could end up instantiating a bunch of <see cref="ITextView"/>s most of which will never get
    /// passed to the VS lightbulb. Such zombie <see cref="ITextView"/>s will never get closed and we will end up leaking memory.</remarks>
    internal class SolutionPreviewItem(ProjectId? projectId, DocumentId? documentId, Func<CancellationToken, Task<object?>> lazyPreviewAsync)
    {
        public readonly ProjectId? ProjectId = projectId;
        public readonly DocumentId? DocumentId = documentId;
        public readonly string? Text;
        private readonly Func<CancellationToken, Task<object?>> _lazyPreviewAsync = lazyPreviewAsync;

        public SolutionPreviewItem(ProjectId? projectId, DocumentId? documentId, string text)
            : this(projectId, documentId, c => Task.FromResult<object?>(text))
        {
            Text = text;
        }

        public async Task<PreviewWrapper?> TryGetPreviewAsync(CancellationToken cancellationToken)
        {
            var preview = await _lazyPreviewAsync(cancellationToken).ConfigureAwaitRunInline();
            if (preview is IReferenceCountedDisposable<IDisposable> referenceCounted)
            {
                // PreviewWrapper will obtain its own shared reference to the reference counted preview. Make sure to
                // dispose the one owned separately here.
                using var _ = referenceCounted;
                return PreviewWrapper.FromReferenceCounted(referenceCounted);
            }
            else if (preview is not null)
            {
                return PreviewWrapper.FromNonReferenceCounted(preview);
            }
            else
            {
                return null;
            }
        }
    }
}
