﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.SimplifyTypeNames;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Diagnostics.SimplifyTypeNames
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class CSharpSimplifyTypeNamesDiagnosticAnalyzer
        : SimplifyTypeNamesDiagnosticAnalyzerBase<SyntaxKind>
    {
        private static readonly ImmutableArray<SyntaxKind> s_kindsOfInterest =
            ImmutableArray.Create(
                SyntaxKind.QualifiedName,
                SyntaxKind.AliasQualifiedName,
                SyntaxKind.GenericName,
                SyntaxKind.IdentifierName,
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxKind.QualifiedCref);

        protected override void AnalyzeSemanticModel(SemanticModelAnalysisContext context)
        {
            var semanticModel = context.SemanticModel;
            var cancellationToken = context.CancellationToken;

            var syntaxTree = semanticModel.SyntaxTree;
            var options = context.Options;
            var optionSet = options.GetDocumentOptionSetAsync(syntaxTree, cancellationToken).GetAwaiter().GetResult()!;
            var root = syntaxTree.GetRoot(cancellationToken);

            var simplifier = new TypeSyntaxSimplifierWalker(this, semanticModel, optionSet, cancellationToken);
            simplifier.Visit(root);

            foreach (var diagnostic in simplifier.Diagnostics)
            {
                context.ReportDiagnostic(diagnostic);
            }
        }

        internal override bool IsCandidate(SyntaxNode node)
            => node != null && s_kindsOfInterest.Contains(node.Kind());

        internal override bool CanSimplifyTypeNameExpression(
            SemanticModel model, SyntaxNode node, OptionSet optionSet,
            out TextSpan issueSpan, out string diagnosticId, out bool inDeclaration,
            CancellationToken cancellationToken)
        {
            inDeclaration = false;
            issueSpan = default;
            diagnosticId = IDEDiagnosticIds.SimplifyNamesDiagnosticId;

            if (node is MemberAccessExpressionSyntax memberAccess && memberAccess.Expression.IsKind(SyntaxKind.ThisExpression))
            {
                // don't bother analyzing "this.Goo" expressions.  They will be analyzed by
                // the CSharpSimplifyThisOrMeDiagnosticAnalyzer.
                return false;
            }

            if (node.ContainsDiagnostics)
            {
                return false;
            }

            SyntaxNode replacementSyntax;
            if (node.IsKind(SyntaxKind.QualifiedCref, out QualifiedCrefSyntax crefSyntax))
            {
                if (!crefSyntax.TryReduceOrSimplifyExplicitName(model, out var replacement, out issueSpan, optionSet, cancellationToken))
                    return false;

                replacementSyntax = replacement;
            }
            else
            {
                var expression = (ExpressionSyntax)node;

                // in case of an As or Is expression we need to handle the binary expression, because it might be 
                // required to add parenthesis around the expression. Adding the parenthesis is done in the CSharpNameSimplifier.Rewriter
                var expressionToCheck = expression.Kind() == SyntaxKind.AsExpression || expression.Kind() == SyntaxKind.IsExpression
                    ? ((BinaryExpressionSyntax)expression).Right
                    : expression;
                if (!expressionToCheck.TryReduceOrSimplifyExplicitName(model, out var replacement, out issueSpan, optionSet, cancellationToken))
                    return false;

                replacementSyntax = replacement;
            }

            // set proper diagnostic ids.
            if (replacementSyntax.HasAnnotations(nameof(CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInDeclaration)))
            {
                inDeclaration = true;
                diagnosticId = IDEDiagnosticIds.PreferBuiltInOrFrameworkTypeDiagnosticId;
            }
            else if (replacementSyntax.HasAnnotations(nameof(CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInMemberAccess)))
            {
                inDeclaration = false;
                diagnosticId = IDEDiagnosticIds.PreferBuiltInOrFrameworkTypeDiagnosticId;
            }
            else if (node.Kind() == SyntaxKind.SimpleMemberAccessExpression)
            {
                diagnosticId = IDEDiagnosticIds.SimplifyMemberAccessDiagnosticId;
            }

            return true;
        }

        protected override string GetLanguageName()
        {
            return LanguageNames.CSharp;
        }
    }
}
