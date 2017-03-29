// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.CSharp.UseExpressionBody
{
    internal abstract class AbstractUseExpressionBodyDiagnosticAnalyzer<TDeclaration> :
        AbstractCodeStyleDiagnosticAnalyzer
        where TDeclaration : SyntaxNode
    {
        private readonly ImmutableArray<SyntaxKind> _syntaxKinds;
        private readonly Option<CodeStyleOption<ExpressionBodyPreference>> _option;
        private readonly LocalizableString _expressionBodyTitle;
        private readonly LocalizableString _blockBodyTitle;

        public override bool OpenFileOnly(Workspace workspace) => false;

        protected AbstractUseExpressionBodyDiagnosticAnalyzer(
            string diagnosticId,
            LocalizableString expressionBodyTitle,
            LocalizableString blockBodyTitle,
            ImmutableArray<SyntaxKind> syntaxKinds,
            Option<CodeStyleOption<ExpressionBodyPreference>> option)
            : base(diagnosticId, expressionBodyTitle)
        {
            _syntaxKinds = syntaxKinds;
            _option = option;
            _expressionBodyTitle = expressionBodyTitle;
            _blockBodyTitle = blockBodyTitle;
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory() => DiagnosticAnalyzerCategory.SemanticDocumentAnalysis;

        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterSyntaxNodeAction(AnalyzeSyntax, _syntaxKinds);

        private void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
        {
            var options = context.Options;
            var syntaxTree = context.Node.SyntaxTree;
            var cancellationToken = context.CancellationToken;
            var optionSet = options.GetDocumentOptionSetAsync(syntaxTree, cancellationToken).GetAwaiter().GetResult();
            if (optionSet == null)
            {
                return;
            }

            var diagnostic = AnalyzeSyntax(optionSet, (TDeclaration)context.Node);
            if (diagnostic != null)
            {
                context.ReportDiagnostic(diagnostic);
            }
        }

        internal virtual Diagnostic AnalyzeSyntax(OptionSet optionSet, TDeclaration declaration)
        {
            // Note: we will always offer to convert a block to an expression-body (and vice versa)
            // if possible.  All the user preference does is determine if we show them anything in
            // the UI (i.e. suggestion dots, or a squiggle) to let them know that they can make the
            // change.  
            //
            // This way, users can turn off the option so they don't get notified, but they can still
            // make the transformation on a case by case basis.
            //
            // Note: if we decide to hide any adornments, then we also broaden the location where the
            // fix is available.  That way the user can go to any place in the member and choose to
            // convert it.  Otherwise, they'd have no idea what the 'right' location was to invoke
            // the conversion.
            //
            // Also, if the diagnostic is hidden, we'll lower the priority of the code action.  We
            // always want it to be available.  But we don't want it to override issues that are
            // actually being reported in the UI.

            var preferExpressionBodiedOption = optionSet.GetOption(_option);

            var expressionBody = GetExpressionBody(declaration);

            if (expressionBody == null)
            {
                // They don't have an expression body.  See if we can convert into one.
                // If so, offer the conversion (with the proper severity depending on their options).
                var options = declaration.SyntaxTree.Options;
                var body = GetBody(declaration);
                if (body.TryConvertToExpressionBody(options, ExpressionBodyPreference.WhenOnSingleLine, out var expressionWhenOnSingleLine, out var semicolonWhenOnSingleLine))
                {
                    // See if it can be converted to an expression and is on a single line.  If so,
                    // we'll show the diagnostic if either 'use expression body' preference is set.
                    var severity =
                        preferExpressionBodiedOption.Value == ExpressionBodyPreference.WhenOnSingleLine || preferExpressionBodiedOption.Value == ExpressionBodyPreference.WhenPossible
                            ? preferExpressionBodiedOption.Notification.Value
                            : DiagnosticSeverity.Hidden;

                    return GetDiagnostic(declaration, severity);
                }

                if (body.TryConvertToExpressionBody(options, ExpressionBodyPreference.WhenPossible, out var expressionWhenPossible, out var semicolonWhenPossible))
                {
                    // It wasn't an expression that was on a single line.  But it was something we
                    // could convert to an expression body.  We'll show the diagnostic only if 
                    // the option to report when possible is set.
                    var severity =
                        preferExpressionBodiedOption.Value == ExpressionBodyPreference.WhenPossible
                            ? preferExpressionBodiedOption.Notification.Value
                            : DiagnosticSeverity.Hidden;

                    return GetDiagnostic(declaration, severity);
                }

                // Can't be converted.
                return null;
            }
            else
            {
                // They have an expression body.  These can always be converted into blocks.
                // Offer to convert this to a block, with the appropriate severity based
                // on their options.
                var severity = preferExpressionBodiedOption.Value != ExpressionBodyPreference.Never
                    ? DiagnosticSeverity.Hidden
                    : preferExpressionBodiedOption.Notification.Value;

                var location = severity == DiagnosticSeverity.Hidden
                    ? declaration.GetLocation()
                    : expressionBody.GetLocation();

                var additionalLocations = ImmutableArray.Create(declaration.GetLocation());
                return Diagnostic.Create(
                    CreateDescriptorWithTitle(_blockBodyTitle, severity, GetCustomTags(severity)),
                    location, additionalLocations: additionalLocations);
            }
        }

        private Diagnostic GetDiagnostic(TDeclaration declaration, DiagnosticSeverity severity)
        {
            var location = severity == DiagnosticSeverity.Hidden
                ? declaration.GetLocation()
                : GetBody(declaration).Statements[0].GetLocation();

            var additionalLocations = ImmutableArray.Create(declaration.GetLocation());
            return Diagnostic.Create(
                CreateDescriptorWithTitle(_expressionBodyTitle, severity, GetCustomTags(severity)),
                location, additionalLocations: additionalLocations);
        }

        private static string[] GetCustomTags(DiagnosticSeverity severity)
            => severity == DiagnosticSeverity.Hidden
                ? new[] { WellKnownDiagnosticTags.NotConfigurable }
                : Array.Empty<string>();

        protected static BlockSyntax GetBodyFromSingleGetAccessor(AccessorListSyntax accessorList)
        {
            if (accessorList != null &&
                accessorList.Accessors.Count == 1 &&
                accessorList.Accessors[0].AttributeLists.Count == 0 && 
                accessorList.Accessors[0].IsKind(SyntaxKind.GetAccessorDeclaration))
            {
                return accessorList.Accessors[0].Body;
            }

            return null;
        }

        protected abstract BlockSyntax GetBody(TDeclaration declaration);
        protected abstract ArrowExpressionClauseSyntax GetExpressionBody(TDeclaration declaration);
    }
}