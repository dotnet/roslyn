// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeRefactorings
{
    internal abstract partial class SyntaxEditorBasedCodeRefactoringProvider : CodeRefactoringProvider
    {
        private readonly bool _supportsFixAll;
        private readonly bool _supportsFixAllForSelection;
        private readonly bool _supportsFixAllForContainingMember;
        private readonly bool _supportsFixAllForContainingType;

        protected SyntaxEditorBasedCodeRefactoringProvider(
            bool supportsFixAll = true,
            bool supportsFixAllForSelection = true,
            bool supportsFixAllForContainingMember = true,
            bool supportsFixAllForContainingType = true)
        {
            _supportsFixAll = supportsFixAll;
            _supportsFixAllForSelection = supportsFixAllForSelection;
            _supportsFixAllForContainingMember = supportsFixAllForContainingMember;
            _supportsFixAllForContainingType = supportsFixAllForContainingType;
        }


        public sealed override FixAllProvider? GetFixAllProvider()
        {
            if (!_supportsFixAll)
                return null;

            return FixAllProvider.Create(
                async fixAllContext =>
                {
                    return await this.FixAllAsync(fixAllContext.Document, fixAllContext.FixAllSpan, fixAllContext.CodeAction, fixAllContext.CancellationToken).ConfigureAwait(false);
                },
                _supportsFixAllForSelection,
                _supportsFixAllForContainingMember,
                _supportsFixAllForContainingType);
        }

        protected Task<Document> FixAsync(
            Document document, TextSpan fixAllSpan, CancellationToken cancellationToken)
        {
            return FixAllWithEditorAsync(document,
                editor => FixAllAsync(document, fixAllSpan, originalCodeAction: null, editor, cancellationToken),
                cancellationToken);
        }

        protected Task<Document> FixAllAsync(
            Document document, TextSpan? fixAllSpan, CodeAction originalCodeAction, CancellationToken cancellationToken)
        {
            return FixAllWithEditorAsync(document,
                editor => FixAllAsync(document, fixAllSpan ?? editor.OriginalRoot.FullSpan, originalCodeAction, editor, cancellationToken),
                cancellationToken);
        }

        internal static async Task<Document> FixAllWithEditorAsync(
            Document document,
            Func<SyntaxEditor, Task> editAsync,
            CancellationToken cancellationToken)
        {
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var editor = new SyntaxEditor(root, document.Project.Solution.Workspace.Services);

            await editAsync(editor).ConfigureAwait(false);

            var newRoot = editor.GetChangedRoot();
            return document.WithSyntaxRoot(newRoot);
        }

        protected abstract Task FixAllAsync(
            Document document, TextSpan fixAllSpan, CodeAction? originalCodeAction, SyntaxEditor editor, CancellationToken cancellationToken);
    }
}
