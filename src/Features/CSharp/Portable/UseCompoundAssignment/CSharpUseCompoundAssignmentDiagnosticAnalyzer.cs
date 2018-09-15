// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseCompoundAssignment
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class CSharpUseCompoundAssignmentDiagnosticAnalyzer : AbstractCodeStyleDiagnosticAnalyzer
    {
        public static readonly ImmutableDictionary<SyntaxKind, SyntaxKind> BinaryToAssignmentMap =
            new Dictionary<SyntaxKind, SyntaxKind>
            {
                { SyntaxKind.AddExpression, SyntaxKind.AddAssignmentExpression },
                { SyntaxKind.SubtractExpression, SyntaxKind.SubtractAssignmentExpression },
                { SyntaxKind.MultiplyExpression, SyntaxKind.MultiplyAssignmentExpression },
                { SyntaxKind.DivideExpression, SyntaxKind.DivideAssignmentExpression },
                { SyntaxKind.ModuloExpression, SyntaxKind.ModuloAssignmentExpression },
                { SyntaxKind.BitwiseAndExpression, SyntaxKind.AndAssignmentExpression },
                { SyntaxKind.ExclusiveOrExpression, SyntaxKind.ExclusiveOrAssignmentExpression },
                { SyntaxKind.BitwiseOrExpression, SyntaxKind.OrAssignmentExpression },
                { SyntaxKind.LeftShiftExpression, SyntaxKind.LeftShiftAssignmentExpression },
                { SyntaxKind.RightShiftExpression, SyntaxKind.RightShiftAssignmentExpression },
            }.ToImmutableDictionary();

        public static readonly ImmutableDictionary<SyntaxKind, SyntaxKind> AssignmentToTokenMap =
            BinaryToAssignmentMap.Values.ToImmutableDictionary(v => v, FindOperatorToken);

        private static SyntaxKind FindOperatorToken(SyntaxKind assignmentExpressionKind)
        {
            for (var current = SyntaxKind.None; current <= SyntaxKind.ThrowExpression; current++)
            {
                if (SyntaxFacts.GetAssignmentExpression(current) == assignmentExpressionKind)
                {
                    return current;
                }
            }

            throw ExceptionUtilities.UnexpectedValue(assignmentExpressionKind);
        }

        public CSharpUseCompoundAssignmentDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.UseCompoundAssignmentDiagnosticId,
                   new LocalizableResourceString(
                       nameof(FeaturesResources.Use_compound_assignment), FeaturesResources.ResourceManager, typeof(FeaturesResources)))
        {
        }

        public override bool OpenFileOnly(Workspace workspace)
            => false;

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterSyntaxNodeAction(AnalyzerAssignment, SyntaxKind.SimpleAssignmentExpression);

        private void AnalyzerAssignment(SyntaxNodeAnalysisContext context)
        {
            var cancellationToken = context.CancellationToken;
            var assignment = (AssignmentExpressionSyntax)context.Node;

            var options = context.Options;
            var syntaxTree = assignment.SyntaxTree;
            var optionSet = options.GetDocumentOptionSetAsync(syntaxTree, cancellationToken).GetAwaiter().GetResult();
            if (optionSet == null)
            {
                return;
            }

            var option = optionSet.GetOption(CodeStyleOptions.PreferCompoundAssignment, assignment.Language);
            if (!option.Value)
            {
                // Bail immediately if the user has disabled this feature.
                return;
            }

            // has to be of the form:  a = b op c
            // op has to be a form we could convert into op=
            if (!(assignment.Right is BinaryExpressionSyntax rightBinary) ||
                !IsConvertibleBinaryKind(rightBinary.Kind()))
            {
                return;
            }
            
            // has to be of the form:   expr = expr op ...
            if (!SyntaxFactory.AreEquivalent(assignment.Left, rightBinary.Left))
            {
                return;
            }

            // Syntactically looks promising.  But we can only safely do this if 'expr'
            // is side-effect-free since we will be changing the number of times it is
            // execute from twice to once.
            var semanticModel = context.SemanticModel;
            if (!IsSideEffectFree(assignment.Left, semanticModel, cancellationToken))
            {
                return;
            }

            context.ReportDiagnostic(DiagnosticHelper.Create(
                Descriptor,
                assignment.OperatorToken.GetLocation(),
                option.Notification.Severity,
                additionalLocations: ImmutableArray.Create(assignment.GetLocation()),
                properties: null));
        }

        private bool IsSideEffectFree(
            ExpressionSyntax expr, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            // it basically has to be of the form "a.b.c", where all components are locals,
            // parameters or fields.  Basically, nothing that can cause arbitrary user code
            // execution when being evaluated by the compiler.
            switch (expr.Kind())
            {
                default:
                    // Something we don't explicitly handle.  Assume this may have side effects.
                    return false;

                case SyntaxKind.ThisExpression:
                case SyntaxKind.BaseExpression:
                    // Referencing this/base like  this.a.b.c causes no side effects itself.
                    return true;

                case SyntaxKind.IdentifierName:
                    return IsSideEffectFreeSymbol(expr, semanticModel, cancellationToken);

                case SyntaxKind.ParenthesizedExpression:
                    var parenthesized = (ParenthesizedExpressionSyntax)expr;
                    return IsSideEffectFree(parenthesized.Expression, semanticModel, cancellationToken);

                case SyntaxKind.SimpleMemberAccessExpression:
                    var memberAccess = (MemberAccessExpressionSyntax)expr;
                    return IsSideEffectFree(memberAccess.Expression, semanticModel, cancellationToken) &&
                           IsSideEffectFreeSymbol(memberAccess, semanticModel, cancellationToken);

                case SyntaxKind.ConditionalAccessExpression:
                    var conditionalAccess = (ConditionalAccessExpressionSyntax)expr;
                    return IsSideEffectFree(conditionalAccess.Expression, semanticModel, cancellationToken) &&
                           IsSideEffectFree(conditionalAccess.WhenNotNull, semanticModel, cancellationToken);

                case SyntaxKind.MemberBindingExpression:
                    var memberBinding = (MemberBindingExpressionSyntax)expr;
                    return IsSideEffectFree(memberBinding.Name, semanticModel, cancellationToken);
            }
        }

        private bool IsSideEffectFreeSymbol(
            ExpressionSyntax expr, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var symbolInfo = semanticModel.GetSymbolInfo(expr, cancellationToken);
            if (symbolInfo.CandidateSymbols.Length > 0 ||
                symbolInfo.Symbol == null)
            {
                // couldn't bind successfully, assume that this might have side-effects.
                return false;
            }

            var symbol = symbolInfo.Symbol;
            switch (symbol.Kind)
            {
                case SymbolKind.Namespace:
                case SymbolKind.NamedType:
                case SymbolKind.Field:
                case SymbolKind.Parameter:
                case SymbolKind.Local:
                    return true;
            }

            return false;
        }

        private static bool IsConvertibleBinaryKind(SyntaxKind kind)
            => BinaryToAssignmentMap.ContainsKey(kind);
    }
}
