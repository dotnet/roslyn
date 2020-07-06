﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.LanguageServices;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.DisambiguateSameVariable
{
    using static SyntaxFactory;

    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    internal class CSharpDisambiguateSameVariableCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        private const string CS1717 = nameof(CS1717); // Assignment made to same variable; did you mean to assign something else?
        private const string CS1718 = nameof(CS1718); // Comparison made to same variable; did you mean to compare something else?

        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public CSharpDisambiguateSameVariableCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds { get; }
            = ImmutableArray.Create(CS1717, CS1718);

        internal override CodeFixCategory CodeFixCategory
            => CodeFixCategory.Compile;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var document = context.Document;
            var diagnostic = context.Diagnostics.First();
            var cancellationToken = context.CancellationToken;

            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (CanFix(
                    semanticModel, diagnostic, cancellationToken,
                    out _, out _, out var title))
            {
                context.RegisterCodeFix(new MyCodeAction(
                    title,
                    c => FixAsync(document, diagnostic, c)),
                    context.Diagnostics);
            }
        }

        private static bool CanFix(
            SemanticModel semanticModel, Diagnostic diagnostic, CancellationToken cancellationToken,
            [NotNullWhen(true)] out SimpleNameSyntax? leftName,
            [NotNullWhen(true)] out ISymbol? matchingMember,
            [NotNullWhen(true)] out string? title)
        {
            leftName = null;
            matchingMember = null;
            title = null;

            var span = diagnostic.Location.SourceSpan;
            var node = diagnostic.Location.FindNode(getInnermostNodeForTie: true, cancellationToken);
            var (left, right, titleFormat) = node switch
            {
                BinaryExpressionSyntax binary => (binary.Left, binary.Right, CSharpFeaturesResources.Compare_to_0),
                AssignmentExpressionSyntax assignment => (assignment.Left, assignment.Right, CSharpFeaturesResources.Assign_to_0),
                _ => default,
            };

            if (left == null || right == null)
                return false;

            if (!(left is IdentifierNameSyntax) && !(left is MemberAccessExpressionSyntax))
                return false;

            var leftSymbol = semanticModel.GetSymbolInfo(left, cancellationToken).GetAnySymbol();
            var rightSymbol = semanticModel.GetSymbolInfo(right, cancellationToken).GetAnySymbol();

            if (leftSymbol == null || rightSymbol == null)
                return false;

            // Since this is a self assignment/compare, these symbols should be the same.
            Debug.Assert(leftSymbol.Equals(rightSymbol));

            if (leftSymbol.Kind != SymbolKind.Local &&
                leftSymbol.Kind != SymbolKind.Parameter &&
                leftSymbol.Kind != SymbolKind.Field &&
                leftSymbol.Kind != SymbolKind.Property)
            {
                return false;
            }

            var localOrParamName = leftSymbol.Name;
            if (string.IsNullOrWhiteSpace(localOrParamName))
                return false;

            var enclosingType = semanticModel.GetEnclosingNamedType(span.Start, cancellationToken);
            if (enclosingType == null)
                return false;

            // Given a local/param called 'x' See if we have an instance field or prop in the containing type
            // called 'x' or 'X' or '_x'.

            var pascalName = localOrParamName.ToPascalCase();
            var camelName = localOrParamName.ToCamelCase();
            var underscoreName = "_" + localOrParamName;
            var members = from t in enclosingType.GetBaseTypesAndThis()
                          from m in t.GetMembers()
                          where !m.IsStatic
                          where m is IFieldSymbol || m is IPropertySymbol
                          where !m.Equals(leftSymbol)
                          where m.Name == localOrParamName ||
                                m.Name == pascalName ||
                                m.Name == camelName ||
                                m.Name == underscoreName
                          where m.IsAccessibleWithin(enclosingType)
                          select m;

            matchingMember = members.FirstOrDefault();
            if (matchingMember == null)
                return false;

            var memberContainer = matchingMember.ContainingType.ToMinimalDisplayString(semanticModel, span.Start);
            title = string.Format(titleFormat, $"{memberContainer}.{matchingMember.Name}");

            leftName = left is MemberAccessExpressionSyntax memberAccess
                ? memberAccess.Name
                : (IdentifierNameSyntax)left;

            return true;
        }

        protected override async Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var syntaxFacts = CSharpSyntaxFacts.Instance;
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            foreach (var diagnostic in diagnostics)
            {
                if (!CanFix(semanticModel, diagnostic, cancellationToken,
                        out var nameNode, out var matchingMember, out _))
                {
                    continue;
                }

                var newNameNode = matchingMember.Name.ToIdentifierName();
                var newExpr = (ExpressionSyntax)newNameNode;
                if (!syntaxFacts.IsNameOfMemberAccessExpression(nameNode))
                {
                    newExpr = MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression, ThisExpression(), newNameNode).WithAdditionalAnnotations(Simplifier.Annotation);
                }

                newExpr = newExpr.WithTriviaFrom(nameNode);
                editor.ReplaceNode(nameNode, newExpr);
            }
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(title, createChangedDocument, nameof(CSharpDisambiguateSameVariableCodeFixProvider))
            {
            }
        }
    }
}
