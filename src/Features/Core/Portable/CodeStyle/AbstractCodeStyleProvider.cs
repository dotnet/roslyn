// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeStyle
{
    // This file contains the "protected" surface area of the AbstractCodeStyleProvider.
    // It specifically is all the extensibility surface that a subclass needs to fill in
    // in order to properly expose a code style analyzer/fixer/refactoring.

    /// <summary>
    /// This is the core class a code-style feature needs to derive from.  All logic related to the
    /// feature will then be contained in this class.  This class will take care of many bit of
    /// common logic that all code style providers would have to care about and can thus do that
    /// logic in a consistent fashion without all providers having to do the same.  For example,
    /// this class will check the current value of the code style option. If it is 'refactoring
    /// only', it will not bother running any of the DiagnosticAnalyzer codepaths, and will only run
    /// the CodeRefactoringProvider codepaths.
    /// </summary>
    internal abstract partial class AbstractCodeStyleProvider<
        TOptionKind, TCodeStyleProvider>
        where TCodeStyleProvider : AbstractCodeStyleProvider<TOptionKind, TCodeStyleProvider>, new()
    {
        private readonly Option<CodeStyleOption<TOptionKind>> _option;
        private readonly string _language;
        private readonly string _descriptorId;
        private readonly LocalizableString _title;
        private readonly LocalizableString _message;

        protected AbstractCodeStyleProvider(
            Option<CodeStyleOption<TOptionKind>> option,
            string language,
            string descriptorId,
            LocalizableString title,
            LocalizableString message)
        {
            _option = option;
            _language = language;
            _descriptorId = descriptorId;
            _title = title;
            _message = message;
        }

        /// <summary>
        /// Helper to get the true ReportDiagnostic severity for a given option.  Importantly, this
        /// handle ReportDiagnostic.Default and will map that back to the appropriate value in that
        /// case.
        /// </summary>
        protected static ReportDiagnostic GetOptionSeverity(CodeStyleOption<TOptionKind> optionValue)
        {
            var severity = optionValue.Notification.Severity;
            return severity == ReportDiagnostic.Default
                ? severity.WithDefaultSeverity(DiagnosticSeverity.Hidden)
                : severity;
        }

        #region analysis

        protected abstract void DiagnosticAnalyzerInitialize(AnalysisContext context);
        protected abstract DiagnosticAnalyzerCategory GetAnalyzerCategory();

        protected DiagnosticDescriptor CreateDescriptorWithId(
            LocalizableString title, LocalizableString message)
        {
            return new DiagnosticDescriptor(
                this._descriptorId, title, message,
                DiagnosticCategory.Style,
                DiagnosticSeverity.Hidden,
                isEnabledByDefault: true);
        }

        #endregion

        #region fixing

        /// <summary>
        /// Subclasses must implement this method to provide fixes for any diagnostics that this
        /// type has registered.  If this subclass wants the same code to run for this single
        /// diagnostic as well as for when running fix-all, then it should call 
        /// <see cref="FixWithSyntaxEditorAsync"/> from its code action.  This will end up calling
        /// <see cref="FixAllAsync"/>, with that single <paramref name="diagnostic"/> in the 
        /// <see cref="ImmutableArray{T}"/> passed to that method.
        /// </summary>
        protected abstract Task<ImmutableArray<CodeAction>> ComputeCodeActionsAsync(
            Document document, Diagnostic diagnostic, CancellationToken cancellationToken);

        /// <summary>
        /// Subclasses should implement this to support fixing all given diagnostics efficiently.
        /// </summary>
        protected abstract Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics, SyntaxEditor editor, CancellationToken cancellationToken);

        protected Task<Document> FixWithSyntaxEditorAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
            => SyntaxEditorBasedCodeFixProvider.FixAllWithEditorAsync(
                document, editor => FixAllAsync(document, ImmutableArray.Create(diagnostic), editor, cancellationToken), cancellationToken);

        #endregion

        #region refactoring

        /// <summary>
        /// Subclasses should implement this to provide their feature as a refactoring.  This will
        /// be called when the user has the code style set to 'refactoring only' (or if the
        /// diagnostic is suppressed).
        ///
        /// The implementation of this should offer all refactorings it can that are relevant at the
        /// provided <paramref name="span"/>.  Specifically, because these are just refactorings,
        /// they should be offered when they would make the code match the desired user preference,
        /// or even for allowing the user to quickly switch their code to *not* follow their desired
        /// preference.
        /// </summary>
        protected abstract Task<ImmutableArray<CodeAction>> ComputeAllRefactoringsWhenAnalyzerInactiveAsync(
            Document document, TextSpan span, CancellationToken cancellationToken);

        /// <summary>
        /// Subclasses should implement this to provide the refactoring that works in the opposing
        /// direction of what the option preference is.  This is only called if the user has the
        /// code style enabled, and has it set to 'info/warning/error'.  In this case it is the
        /// *analyzer* responsible for making code compliant with the option.
        ///
        /// The refactoring then exists to allow the user to update their code to go against that
        /// option on an individual case by case basis.
        ///
        /// For example, if the user had set that they want expression-bodies for methods (at
        /// warning level), then this would offer 'use block body' on a method that had an
        /// expression body already.
        /// </summary>
        protected abstract Task<ImmutableArray<CodeAction>> ComputeOpposingRefactoringsWhenAnalyzerActiveAsync(
            Document document, TextSpan span, TOptionKind option, CancellationToken cancellationToken);

        #endregion
    }
}
