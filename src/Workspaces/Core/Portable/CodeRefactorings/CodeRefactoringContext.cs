// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeRefactorings
{
    /// <summary>
    /// Context for code refactorings provided by a <see cref="CodeRefactoringProvider"/>.
    /// </summary>
    public struct CodeRefactoringContext : ITypeScriptCodeRefactoringContext
    {
        /// <summary>
        /// Document corresponding to the <see cref="CodeRefactoringContext.Span"/> to refactor.
        /// </summary>
        public Document Document { get; }

        /// <summary>
        /// Text span within the <see cref="CodeRefactoringContext.Document"/> to refactor.
        /// </summary>
        public TextSpan Span { get; }

        /// <summary>
        /// CancellationToken.
        /// </summary>
        public CancellationToken CancellationToken { get; }

        private readonly bool _isBlocking;
        bool ITypeScriptCodeRefactoringContext.IsBlocking => _isBlocking;

        private readonly Action<CodeAction, TextSpan?> _registerRefactoring;

        /// <summary>
        /// Creates a code refactoring context to be passed into <see cref="CodeRefactoringProvider.ComputeRefactoringsAsync(CodeRefactoringContext)"/> method.
        /// </summary>
        public CodeRefactoringContext(
            Document document,
            TextSpan span,
            Action<CodeAction> registerRefactoring,
            CancellationToken cancellationToken)
            : this(document, span, (action, textSpan) => registerRefactoring(action), isBlocking: false, cancellationToken)
        { }

        internal CodeRefactoringContext(
            Document document,
            TextSpan span,
            Action<CodeAction, TextSpan?> registerRefactoring,
            bool isBlocking,
            CancellationToken cancellationToken)
        {
            // NOTE/TODO: Don't make this overload public & obsolete the `Action<CodeAction> registerRefactoring`
            // overload to stop leaking the Lambda implementation detail.
            Document = document ?? throw new ArgumentNullException(nameof(document));
            Span = span;
            _registerRefactoring = registerRefactoring ?? throw new ArgumentNullException(nameof(registerRefactoring));
            _isBlocking = isBlocking;
            CancellationToken = cancellationToken;
        }

        /// <summary>
        /// Add supplied <paramref name="action"/> to the list of refactorings that will be offered to the user.
        /// </summary>
        /// <param name="action">The <see cref="CodeAction"/> that will be invoked to apply the refactoring.</param>
        public void RegisterRefactoring(CodeAction action) => RegisterRefactoring(action, applicableToSpan: null); // We could pass this.Span as applicableToSpan instead but that would cause these refactorings to always be closest to current selection

        /// <summary>
        /// Add supplied <paramref name="action"/> applicable to <paramref name="applicableToSpan"/> to the list of refactorings that will be offered to the user.
        /// </summary>
        /// <param name="action">The <see cref="CodeAction"/> that will be invoked to apply the refactoring.</param>
        /// <param name="applicableToSpan">The <see cref="TextSpan"/> within original document the <paramref name="action"/> is applicable to.</param>
        /// <remarks>
        /// <paramref name="applicableToSpan"/> should represent a logical section within the original document that the <paramref name="action"/> is 
        /// applicable to. It doesn't have to precisely represent the exact <see cref="TextSpan"/> that will get changed.
        /// </remarks>
        internal void RegisterRefactoring(CodeAction action, TextSpan applicableToSpan) => RegisterRefactoring(action, new Nullable<TextSpan>(applicableToSpan));

        private void RegisterRefactoring(CodeAction action, TextSpan? applicableToSpan)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            _registerRefactoring(action, applicableToSpan);
        }

        internal void Deconstruct(out Document document, out TextSpan span, out CancellationToken cancellationToken)
        {
            document = Document;
            span = Span;
            cancellationToken = CancellationToken;
        }
    }

    internal interface ITypeScriptCodeRefactoringContext
    {
        bool IsBlocking { get; }
    }
}
