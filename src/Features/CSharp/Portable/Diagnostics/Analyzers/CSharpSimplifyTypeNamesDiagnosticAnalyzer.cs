// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Simplification;
using Microsoft.CodeAnalysis.CSharp.Simplification.Simplifiers;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.SimplifyTypeNames;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Diagnostics.SimplifyTypeNames
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class CSharpSimplifyTypeNamesDiagnosticAnalyzer
        : SimplifyTypeNamesDiagnosticAnalyzerBase<SyntaxKind, CSharpSimplifierOptions>
    {
        private static readonly ImmutableArray<SyntaxKind> s_kindsOfInterest =
            ImmutableArray.Create(
                SyntaxKind.QualifiedName,
                SyntaxKind.AliasQualifiedName,
                SyntaxKind.GenericName,
                SyntaxKind.IdentifierName,
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxKind.QualifiedCref);

        protected override bool IsIgnoredCodeBlock(SyntaxNode codeBlock)
        {
            // Avoid analysis of compilation units and types in AnalyzeCodeBlock. These nodes appear in code block
            // callbacks when they include attributes, but analysis of the node at this level would block more efficient
            // analysis of descendant members.
            return codeBlock.IsKind(
                SyntaxKind.CompilationUnit,
                SyntaxKind.ClassDeclaration,
                SyntaxKind.RecordDeclaration,
                SyntaxKind.StructDeclaration,
                SyntaxKind.RecordStructDeclaration,
                SyntaxKind.InterfaceDeclaration,
                SyntaxKind.DelegateDeclaration,
                SyntaxKind.EnumDeclaration);
        }

        protected override ImmutableArray<Diagnostic> AnalyzeCodeBlock(CodeBlockAnalysisContext context)
        {
            var semanticModel = context.SemanticModel;
            var cancellationToken = context.CancellationToken;

            var syntaxTree = semanticModel.SyntaxTree;
            var options = context.Options.GetCSharpSimplifierOptions(syntaxTree);
            using var simplifier = new TypeSyntaxSimplifierWalker(this, semanticModel, options, ignoredSpans: null, cancellationToken);
            simplifier.Visit(context.CodeBlock);
            return simplifier.Diagnostics;
        }

        protected override ImmutableArray<Diagnostic> AnalyzeSemanticModel(SemanticModelAnalysisContext context, SimpleIntervalTree<TextSpan, TextSpanIntervalIntrospector>? codeBlockIntervalTree)
        {
            var semanticModel = context.SemanticModel;
            var cancellationToken = context.CancellationToken;

            var syntaxTree = semanticModel.SyntaxTree;
            var simplifierOptions = context.Options.GetCSharpSimplifierOptions(syntaxTree);
            var root = syntaxTree.GetRoot(cancellationToken);

            var simplifier = new TypeSyntaxSimplifierWalker(this, semanticModel, simplifierOptions, ignoredSpans: codeBlockIntervalTree, cancellationToken);
            simplifier.Visit(root);
            return simplifier.Diagnostics;
        }

        internal override bool IsCandidate(SyntaxNode node)
            => node != null && s_kindsOfInterest.Contains(node.Kind());

        internal override bool CanSimplifyTypeNameExpression(
            SemanticModel model, SyntaxNode node, CSharpSimplifierOptions options,
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
                if (!QualifiedCrefSimplifier.Instance.TrySimplify(crefSyntax, model, options, out var replacement, out issueSpan, cancellationToken))
                    return false;

                replacementSyntax = replacement;
            }
            else
            {
                if (!ExpressionSimplifier.Instance.TrySimplify((ExpressionSyntax)node, model, options, out var replacement, out issueSpan, cancellationToken))
                    return false;

                replacementSyntax = replacement;
            }

            // set proper diagnostic ids.
            if (replacementSyntax.HasAnnotations(nameof(CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInDeclaration)))
            {
                inDeclaration = true;
                diagnosticId = IDEDiagnosticIds.PreferBuiltInOrFrameworkTypeDiagnosticId;
            }
            else if (replacementSyntax.HasAnnotations(nameof(CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInMemberAccess)))
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
            => LanguageNames.CSharp;
    }
}
