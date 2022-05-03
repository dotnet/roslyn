// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.TypeStyle
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UseExplicitType), Shared]
    internal class UseExplicitTypeCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public UseExplicitTypeCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(IDEDiagnosticIds.UseExplicitTypeDiagnosticId);

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            RegisterCodeFix(context, CSharpAnalyzersResources.Use_explicit_type_instead_of_var, nameof(CSharpAnalyzersResources.Use_explicit_type_instead_of_var));
            return Task.CompletedTask;
        }

        protected override async Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken)
        {
            var root = editor.OriginalRoot;

            foreach (var diagnostic in diagnostics)
            {
                var node = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
                await HandleDeclarationAsync(document, editor, node, cancellationToken).ConfigureAwait(false);
            }
        }

        internal static async Task HandleDeclarationAsync(
            Document document, SyntaxEditor editor,
            SyntaxNode node, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var declarationContext = node.Parent;

            TypeSyntax typeSyntax = null;
            ParenthesizedVariableDesignationSyntax parensDesignation = null;
            if (declarationContext is RefTypeSyntax refType)
            {
                declarationContext = declarationContext.Parent;
            }

            if (declarationContext is VariableDeclarationSyntax varDecl)
            {
                typeSyntax = varDecl.Type;
            }
            else if (declarationContext is ForEachStatementSyntax forEach)
            {
                typeSyntax = forEach.Type;
            }
            else if (declarationContext is DeclarationExpressionSyntax declarationExpression)
            {
                typeSyntax = declarationExpression.Type;
                if (declarationExpression.Designation.IsKind(SyntaxKind.ParenthesizedVariableDesignation, out ParenthesizedVariableDesignationSyntax variableDesignation))
                {
                    parensDesignation = variableDesignation;
                }
            }
            else
            {
                throw ExceptionUtilities.UnexpectedValue(declarationContext.Kind());
            }

            if (parensDesignation is null)
            {
                typeSyntax = typeSyntax.StripRefIfNeeded();

                // We're going to be passed through the simplifier.  Tell it to not just convert this back to var (as
                // that would defeat the purpose of this refactoring entirely).
                var newTypeSyntax =
                    semanticModel.GetTypeInfo(typeSyntax, cancellationToken).ConvertedType
                                 .GenerateTypeSyntax(allowVar: false)
                                 .WithTriviaFrom(typeSyntax);

                Debug.Assert(!newTypeSyntax.ContainsDiagnostics, "Explicit type replacement likely introduced an error in code");

                editor.ReplaceNode(typeSyntax, newTypeSyntax);
            }
            else
            {
                var tupleTypeSymbol = semanticModel.GetTypeInfo(typeSyntax.Parent, cancellationToken).ConvertedType;

                var leadingTrivia = node.GetLeadingTrivia()
                    .Concat(parensDesignation.GetAllPrecedingTriviaToPreviousToken().Where(t => !t.IsWhitespace()).Select(t => t.WithoutAnnotations(SyntaxAnnotation.ElasticAnnotation)));

                var tupleDeclaration = GenerateTupleDeclaration(tupleTypeSymbol, parensDesignation).WithLeadingTrivia(leadingTrivia);

                editor.ReplaceNode(declarationContext, tupleDeclaration);
            }
        }

        private static ExpressionSyntax GenerateTupleDeclaration(ITypeSymbol typeSymbol, ParenthesizedVariableDesignationSyntax parensDesignation)
        {
            Debug.Assert(typeSymbol.IsTupleType);
            var elements = ((INamedTypeSymbol)typeSymbol).TupleElements;
            Debug.Assert(elements.Length == parensDesignation.Variables.Count);

            using var builderDisposer = ArrayBuilder<SyntaxNode>.GetInstance(elements.Length, out var builder);
            for (var i = 0; i < elements.Length; i++)
            {
                var designation = parensDesignation.Variables[i];
                var type = elements[i].Type;
                ExpressionSyntax newDeclaration;
                switch (designation.Kind())
                {
                    case SyntaxKind.SingleVariableDesignation:
                    case SyntaxKind.DiscardDesignation:
                        var typeName = type.GenerateTypeSyntax(allowVar: false);
                        newDeclaration = SyntaxFactory.DeclarationExpression(typeName, designation);
                        break;
                    case SyntaxKind.ParenthesizedVariableDesignation:
                        newDeclaration = GenerateTupleDeclaration(type, (ParenthesizedVariableDesignationSyntax)designation);
                        break;
                    default:
                        throw ExceptionUtilities.UnexpectedValue(designation.Kind());
                }

                newDeclaration = newDeclaration
                    .WithLeadingTrivia(designation.GetAllPrecedingTriviaToPreviousToken())
                    .WithTrailingTrivia(designation.GetTrailingTrivia());

                builder.Add(SyntaxFactory.Argument(newDeclaration));
            }

            var separatorBuilder = ArrayBuilder<SyntaxToken>.GetInstance(builder.Count - 1, SyntaxFactory.Token(leading: default, SyntaxKind.CommaToken, trailing: default));

            return SyntaxFactory.TupleExpression(
                SyntaxFactory.Token(SyntaxKind.OpenParenToken).WithTrailingTrivia(),
                SyntaxFactory.SeparatedList(builder.ToImmutable(), separatorBuilder.ToImmutableAndFree()),
                SyntaxFactory.Token(SyntaxKind.CloseParenToken))
                .WithTrailingTrivia(parensDesignation.GetTrailingTrivia());
        }
    }
}
