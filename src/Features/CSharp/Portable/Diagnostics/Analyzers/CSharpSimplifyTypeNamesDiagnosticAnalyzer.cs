﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
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

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterCompilationStartAction(AnalyzeCompilation);
        }

        private void AnalyzeCompilation(CompilationStartAnalysisContext context)
        {
            var compilationTypeNames = GetCompilationTypeNames(context.Compilation);

            context.RegisterSemanticModelAction(c => AnalyzeSemanticModel(c, compilationTypeNames));
        }

        private static ICollection<string> GetCompilationTypeNames(Compilation compilation)
        {
            var builder = ArrayBuilder<ICollection<string>>.GetInstance();
            builder.Add(compilation.Assembly.TypeNames);
            foreach (var reference in compilation.References)
            {
                if (compilation.GetAssemblyOrModuleSymbol(reference) is IAssemblySymbol assembly)
                    builder.Add(assembly.TypeNames);
            }

            return UnionCollection<string>.Create(builder.ToImmutableAndFree(), _ => _);
        }

        private void AnalyzeSemanticModel(
            SemanticModelAnalysisContext context, ICollection<string> compilationTypeNames)
        {
            var semanticModel = context.SemanticModel;
            var cancellationToken = context.CancellationToken;

            var syntaxTree = semanticModel.SyntaxTree;
            var options = context.Options;
            var optionSet = options.GetDocumentOptionSetAsync(syntaxTree, cancellationToken).GetAwaiter().GetResult()!;
            var root = syntaxTree.GetRoot(cancellationToken);

            var simplifier = new TypeSyntaxSimplifierWalker(this, semanticModel, optionSet, compilationTypeNames, cancellationToken);
            simplifier.Visit(root);

            foreach (var diagnostic in simplifier.Diagnostics)
            {
                context.ReportDiagnostic(diagnostic);
            }
        }

        internal override bool IsCandidate(SyntaxNode node)
            => node != null && s_kindsOfInterest.Contains(node.Kind());

        protected sealed override bool CanSimplifyTypeNameExpressionCore(
            SemanticModel model, SyntaxNode node, OptionSet optionSet,
            out TextSpan issueSpan, out string diagnosticId, out bool inDeclaration,
            CancellationToken cancellationToken)
        {
            return CanSimplifyTypeNameExpression(
                model, node, optionSet,
                out issueSpan, out diagnosticId, out inDeclaration,
                cancellationToken);
        }

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
            if (node.IsKind(SyntaxKind.QualifiedCref, out QualifiedCrefSyntax? crefSyntax))
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
