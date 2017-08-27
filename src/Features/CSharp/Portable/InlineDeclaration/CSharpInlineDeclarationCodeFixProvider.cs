﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.CodeStyle.TypeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.InlineDeclaration
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    internal partial class CSharpInlineDeclarationCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(IDEDiagnosticIds.InlineDeclarationDiagnosticId);

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(new MyCodeAction(
                c => FixAsync(context.Document, context.Diagnostics.First(), c)),
                context.Diagnostics);
            return SpecializedTasks.EmptyTask;
        }

        protected override async Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics, 
            SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var options = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);

            // Attempt to use an out-var declaration if that's the style the user prefers.
            // Note: if using 'var' would cause a problem, we will use the actual type
            // of the local.  This is necessary in some cases (for example, when the
            // type of the out-var-decl affects overload resolution or generic instantiation).

            foreach (var diagnostic in diagnostics)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await AddEditsAsync(
                    document, editor, diagnostic, 
                    options, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task AddEditsAsync(
            Document document, SyntaxEditor editor, Diagnostic diagnostic, 
            OptionSet options, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);

            // Recover the nodes we care about.
            var declaratorLocation = diagnostic.AdditionalLocations[0];
            var identifierLocation = diagnostic.AdditionalLocations[1];
            var invocationOrCreationLocation = diagnostic.AdditionalLocations[2];
            var outArgumentContainingStatementLocation = diagnostic.AdditionalLocations[3];

            var root = declaratorLocation.SourceTree.GetRoot(cancellationToken);

            var declarator = (VariableDeclaratorSyntax)declaratorLocation.FindNode(cancellationToken);
            var identifier = (IdentifierNameSyntax)identifierLocation.FindNode(cancellationToken);
            var invocationOrCreation = (ExpressionSyntax)invocationOrCreationLocation.FindNode(
                getInnermostNodeForTie: true, cancellationToken: cancellationToken);
            var outArgumentContainingStatement = (StatementSyntax)outArgumentContainingStatementLocation.FindNode(cancellationToken);

            var declaration = (VariableDeclarationSyntax)declarator.Parent;
            var singleDeclarator = declaration.Variables.Count == 1;

            if (singleDeclarator)
            {
                // This was a local statement with a single variable in it.  Just Remove 
                // the entire local declaration statement.  Note that comments belonging to
                // this local statement will be moved to be above the statement containing
                // the out-var. 
                var localDeclarationStatement = (LocalDeclarationStatementSyntax)declaration.Parent;
                var block = (BlockSyntax)localDeclarationStatement.Parent;
                var declarationIndex = block.Statements.IndexOf(localDeclarationStatement);

                if (declarationIndex > 0 &&
                    sourceText.AreOnSameLine(block.Statements[declarationIndex - 1].GetLastToken(), localDeclarationStatement.GetFirstToken()))
                {
                    // There's another statement on the same line as this declaration statement.
                    // i.e.   int a; int b;
                    //
                    // Just move all trivia from our statement to be trailing trivia of the previous
                    // statement
                    editor.ReplaceNode(
                        block.Statements[declarationIndex - 1],
                        (s, g) => s.WithAppendedTrailingTrivia(localDeclarationStatement.GetTrailingTrivia()));
                }
                else
                {
                    // Trivia on the local declaration will move to the next statement.
                    // use the callback form as the next statement may be the place where we're 
                    // inlining the declaration, and thus need to see the effects of that change.
                    editor.ReplaceNode(
                        block.Statements[declarationIndex + 1],
                        (s, g) => s.WithPrependedNonIndentationTriviaFrom(localDeclarationStatement));
                }

                editor.RemoveNode(localDeclarationStatement, SyntaxRemoveOptions.KeepUnbalancedDirectives);
            }
            else
            {
                // Otherwise, just remove the single declarator. Note: we'll move the comments
                // 'on' the declarator to the out-var location.  This is a little bit trickier
                // than normal due to how our comment-association rules work.  i.e. if you have:
                //
                //      var /*c1*/ i /*c2*/, /*c3*/ j /*c4*/;
                //
                // In this case 'c1' is owned by the 'var' token, not 'i', and 'c3' is owned by 
                // the comment token not 'j'.  

                editor.RemoveNode(declarator);
                if (declarator == declaration.Variables[0])
                {
                    // If we're removing the first declarator, and it's on the same line
                    // as the previous token, then we want to remove all the trivia belonging
                    // to the previous token.  We're going to move it along with this declarator.
                    // If we don't, then the comment will stay with the previous token.
                    //
                    // Note that the moving of the comment happens later on when we make the
                    // declaration expression.
                    if (sourceText.AreOnSameLine(declarator.GetFirstToken(), declarator.GetFirstToken().GetPreviousToken(includeSkipped: true)))
                    {
                        editor.ReplaceNode(
                            declaration.Type, 
                            (t, g) => t.WithTrailingTrivia(SyntaxFactory.ElasticSpace).WithoutAnnotations(Formatter.Annotation));
                    }
                }
            }

            // get the type that we want to put in the out-var-decl based on the user's options.
            // i.e. prefer 'out var' if that is what the user wants.  Note: if we have:
            //
            //      Method(out var x)
            //
            // Then the type is not-apparent, and we should not use var if the user only wants
            // it for apparent types

            var local = (ILocalSymbol)semanticModel.GetDeclaredSymbol(declarator);
            var newType = local.Type.GenerateTypeSyntaxOrVar(options, typeIsApparent: false);

            var declarationExpression = GetDeclarationExpression(
                sourceText, identifier, newType, singleDeclarator ? null : declarator);

            // Check if using out-var changed problem semantics.
            var semanticsChanged = await SemanticsChangedAsync(
                document, declaration, invocationOrCreation, newType,
                identifier, declarationExpression, cancellationToken).ConfigureAwait(false);
            if (semanticsChanged && newType.IsVar)
            {
                // Switching to 'var' changed semantics.  Just use the original type of the local.

                // If the user originally wrote it something other than 'var', then use what they
                // wrote.  Otherwise, synthesize the actual type of the local.
                var explicitType = declaration.Type.IsVar ? local.Type?.GenerateTypeSyntax() : declaration.Type;
                declarationExpression = GetDeclarationExpression(
                    sourceText, identifier, explicitType, singleDeclarator ? null : declarator);
            }

            editor.ReplaceNode(identifier, declarationExpression);
        }

        private static DeclarationExpressionSyntax GetDeclarationExpression(
            SourceText sourceText, IdentifierNameSyntax identifier,
            TypeSyntax newType, VariableDeclaratorSyntax declaratorOpt)
        {
            newType = newType.WithoutTrivia().WithAdditionalAnnotations(Formatter.Annotation);
            var designation = SyntaxFactory.SingleVariableDesignation(identifier.Identifier);

            if (declaratorOpt != null)
            {
                // We're removing a single declarator.  Copy any comments it has to the out-var.
                //
                // Note: this is tricky due to comment ownership.  We want the comments that logically
                // belong to the declarator, even if our syntax model attaches them to other tokens.
                var precedingTrivia = declaratorOpt.GetAllPrecedingTriviaToPreviousToken(
                    sourceText, includePreviousTokenTrailingTriviaOnlyIfOnSameLine: true);
                if (precedingTrivia.Any(t => t.IsSingleOrMultiLineComment()))
                {
                    designation = designation.WithPrependedLeadingTrivia(MassageTrivia(precedingTrivia));
                }

                if (declaratorOpt.GetTrailingTrivia().Any(t => t.IsSingleOrMultiLineComment()))
                {
                    designation = designation.WithAppendedTrailingTrivia(MassageTrivia(declaratorOpt.GetTrailingTrivia()));
                }
            }

            return SyntaxFactory.DeclarationExpression(newType, designation);
        }

        private static IEnumerable<SyntaxTrivia> MassageTrivia(IEnumerable<SyntaxTrivia> triviaList)
        {
            foreach (var trivia in triviaList)
            {
                if (trivia.IsSingleOrMultiLineComment())
                {
                    yield return trivia;
                }
                else if (trivia.IsWhitespace())
                {
                    // Condense whitespace down to single spaces. We don't want things like
                    // indentation spaces to be inserted in the out-var location.  It is appropriate
                    // though to have single spaces to help separate out things like comments and
                    // tokens though.
                    yield return SyntaxFactory.Space;
                }
            }
        }

        private async Task<bool> SemanticsChangedAsync(
            Document document,
            VariableDeclarationSyntax declaration,
            ExpressionSyntax invocationOrCreation,
            TypeSyntax newType,
            IdentifierNameSyntax identifier,
            DeclarationExpressionSyntax declarationExpression,
            CancellationToken cancellationToken)
        {
            if (newType.IsVar)
            {
                // Options want us to use 'var' if we can.  Make sure we didn't change
                // the semantics of the call by doing this.

                // Find the symbol that the existing invocation points to.
                var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                var previousSymbol = semanticModel.GetSymbolInfo(invocationOrCreation).Symbol;

                // Now, create a speculative model in which we make the change.  Make sure
                // we still point to the same symbol afterwards.

                var topmostContainer = GetTopmostContainer(invocationOrCreation);
                if (topmostContainer == null)
                {
                    // Couldn't figure out what we were contained in.  Have to assume that semantics
                    // Are changing.
                    return true;
                }

                var annotation = new SyntaxAnnotation();
                var updatedTopmostContainer = topmostContainer.ReplaceNode(
                    invocationOrCreation, invocationOrCreation.ReplaceNode(identifier, declarationExpression)
                                                              .WithAdditionalAnnotations(annotation));

                if (!TryGetSpeculativeSemanticModel(semanticModel,
                        topmostContainer.SpanStart, updatedTopmostContainer, out var speculativeModel))
                {
                    // Couldn't figure out the new semantics.  Assume semantics changed.
                    return true;
                }

                var updatedInvocationOrCreation = updatedTopmostContainer.GetAnnotatedNodes(annotation).Single();
                var updatedSymbolInfo = speculativeModel.GetSymbolInfo(updatedInvocationOrCreation);

                if (!SymbolEquivalenceComparer.Instance.Equals(previousSymbol, updatedSymbolInfo.Symbol))
                {
                    // We're pointing at a new symbol now.  Semantic have changed.
                    return true;
                }
            }

            return false;
        }

        private SyntaxNode GetTopmostContainer(ExpressionSyntax expression)
        {
            return expression.GetAncestorsOrThis(
                a => a is StatementSyntax ||
                     a is EqualsValueClauseSyntax ||
                     a is ArrowExpressionClauseSyntax ||
                     a is ConstructorInitializerSyntax).LastOrDefault();
        }

        private bool TryGetSpeculativeSemanticModel(
            SemanticModel semanticModel, 
            int position, SyntaxNode topmostContainer,
            out SemanticModel speculativeModel)
        {
            switch (topmostContainer)
            {
                case StatementSyntax statement:
                    return semanticModel.TryGetSpeculativeSemanticModel(position, statement, out speculativeModel);
                case EqualsValueClauseSyntax equalsValue:
                    return semanticModel.TryGetSpeculativeSemanticModel(position, equalsValue, out speculativeModel);
                case ArrowExpressionClauseSyntax arrowExpression:
                    return semanticModel.TryGetSpeculativeSemanticModel(position, arrowExpression, out speculativeModel);
                case ConstructorInitializerSyntax constructorInitializer:
                    return semanticModel.TryGetSpeculativeSemanticModel(position, constructorInitializer, out speculativeModel);
            }

            speculativeModel = null;
            return false;
        }

        private TypeSyntax GetDeclarationType(
            TypeSyntax type, bool useVarWhenDeclaringLocals, bool useImplicitTypeForIntrinsicTypes)
        {
            if (useVarWhenDeclaringLocals)
            {
                if (useImplicitTypeForIntrinsicTypes ||
                    !TypeStyleHelper.IsPredefinedType(type))
                {
                    return SyntaxFactory.IdentifierName("var");
                }
            }

            return type;
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument) 
                : base(FeaturesResources.Inline_variable_declaration,
                       createChangedDocument,
                       FeaturesResources.Inline_variable_declaration)
            {
            }
        }
    }
}
