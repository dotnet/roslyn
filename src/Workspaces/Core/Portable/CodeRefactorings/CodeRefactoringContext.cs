// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using System.Threading;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeRefactorings;

/// <summary>
/// Context for code refactorings provided by a <see cref="CodeRefactoringProvider"/>.
/// </summary>
public readonly struct CodeRefactoringContext
{
    /// <summary>
    /// Document corresponding to the <see cref="CodeRefactoringContext.Span"/> to refactor.
    /// For code refactorings that support non-source documents by providing a non-default value for
    /// <see cref="ExportCodeRefactoringProviderAttribute.DocumentKinds"/>, this property will
    /// throw an <see cref="InvalidOperationException"/>. Such refactorings should use the
    /// <see cref="CodeRefactoringContext.TextDocument"/> property instead.
    /// </summary>
    public Document Document
    {
        get
        {
            if (TextDocument is not Document document)
            {
                throw new InvalidOperationException(WorkspacesResources.Use_TextDocument_property_instead_of_Document_property_as_the_provider_supports_non_source_text_documents);
            }

            return document;
        }
    }

    /// <summary>
    /// TextDocument corresponding to the <see cref="CodeRefactoringContext.Span"/> to refactor.
    /// This property should be used instead of <see cref="CodeRefactoringContext.Document"/> property by
    /// code refactorings that support non-source documents by providing a non-default value for
    /// <see cref="ExportCodeRefactoringProviderAttribute.DocumentKinds"/>
    /// </summary>
    public TextDocument TextDocument { get; }

    /// <summary>
    /// Text span within the <see cref="CodeRefactoringContext.Document"/> or <see cref="CodeRefactoringContext.TextDocument"/> to refactor.
    /// </summary>
    public TextSpan Span { get; }

    /// <summary>
    /// CancellationToken.
    /// </summary>
    public CancellationToken CancellationToken { get; }

    private readonly Action<CodeAction, TextSpan?> _registerRefactoring;

    /// <summary>
    /// Creates a code refactoring context to be passed into <see cref="CodeRefactoringProvider.ComputeRefactoringsAsync(CodeRefactoringContext)"/> method.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public CodeRefactoringContext(
        Document document,
        TextSpan span,
        Action<CodeAction> registerRefactoring,
        CancellationToken cancellationToken)
        : this(document, span, (action, textSpan) => registerRefactoring(action), cancellationToken)
    { }

    /// <summary>
    /// Creates a code refactoring context to be passed into <see cref="CodeRefactoringProvider.ComputeRefactoringsAsync(CodeRefactoringContext)"/> method.
    /// </summary>
    public CodeRefactoringContext(
        TextDocument document,
        TextSpan span,
        Action<CodeAction> registerRefactoring,
        CancellationToken cancellationToken)
        : this(document, span, (action, textSpan) => registerRefactoring(action), cancellationToken)
    { }

    /// <summary>
    /// Creates a code refactoring context to be passed into <see cref="CodeRefactoringProvider.ComputeRefactoringsAsync(CodeRefactoringContext)"/> method.
    /// </summary>
    internal CodeRefactoringContext(
        TextDocument document,
        TextSpan span,
        Action<CodeAction, TextSpan?> registerRefactoring,
        CancellationToken cancellationToken)
    {
        // NOTE/TODO: Don't make this overload public & obsolete the `Action<CodeAction> registerRefactoring`
        // overload to stop leaking the Lambda implementation detail.
        TextDocument = document ?? throw new ArgumentNullException(nameof(document));
        Span = span;
        _registerRefactoring = registerRefactoring ?? throw new ArgumentNullException(nameof(registerRefactoring));
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
