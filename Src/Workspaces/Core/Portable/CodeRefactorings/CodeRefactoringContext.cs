// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System.Threading;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeRefactorings
{
    /// <summary>
    /// Context for code refactorings provided by an <see cref="CodeRefactoringProvider"/>.
    /// </summary>
    public struct CodeRefactoringContext
    {
        private readonly Document document;
        private readonly TextSpan span;
        private readonly CancellationToken cancellationToken;

        /// <summary>
        /// Document corresponding to the <see cref="CodeRefactoringContext.Span"/> to refactor.
        /// </summary>
        public Document Document { get { return this.document; } }

        /// <summary>
        /// Text span within the <see cref="CodeRefactoringContext.Document"/> to refactor.
        /// </summary>
        public TextSpan Span { get { return this.span; } }

        /// <summary>
        /// CancellationToken.
        /// </summary>
        public CancellationToken CancellationToken { get { return this.cancellationToken; } }

        /// <summary>
        /// Creates a code refactoring context to be passed into <see cref="CodeRefactoringProvider.GetRefactoringsAsync(CodeRefactoringContext)"/> method.
        /// </summary>
        public CodeRefactoringContext(
            Document document,
            TextSpan span, 
            CancellationToken cancellationToken)
        {
            this.document = document;
            this.span = span;
            this.cancellationToken = cancellationToken;
        }
    }
}
