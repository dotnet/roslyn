// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Completion
{
    /// <summary>
    /// The ICustomCommitCompletionProvider in the EditorFeatures layer marks completion providers
    /// that do fancy things with the caret. In the move to Editor Completion, we use this
    /// interface to mark <see cref="CompletionProvider" />s that need to do more than insert the
    /// InsertText.
    /// </summary>
    interface IFeaturesCustomCommitCompletionProvider
    {
        Task<CompletionChange> GetChangeAsync(Document document, CompletionItem item, char? commitKey = default, CancellationToken cancellationToken = default);
    }
}
