// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.InlineDeclaration
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    internal partial class CSharpInlineDeclarationCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        [ImportingConstructor]
        public CSharpInlineDeclarationCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(IDEDiagnosticIds.InlineDeclarationDiagnosticId);

        internal sealed override CodeFixCategory CodeFixCategory => CodeFixCategory.CodeStyle;

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(new MyCodeAction(
                c => FixAsync(context.Document, context.Diagnostics.First(), c)),
                context.Diagnostics);
            return Task.CompletedTask;
        }

        protected override async Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var options = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);

            // Gather all statements to be removed
            // We need this to find the statements we can safely attach trivia to
            var declarationsToRemove = new HashSet<StatementSyntax>();
            foreach (var diagnostic in diagnostics)
            {
                declarationsToRemove.Add((LocalDeclarationStatementSyntax)diagnostic.AdditionalLocations[0].FindNode(cancellationToken).Parent.Parent);
            }

            // Attempt to use an out-var declaration if that's the style the user prefers.
            // Note: if using 'var' would cause a problem, we will use the actual type
            // of the local.  This is necessary in some cases (for example, when the
            // type of the out-var-decl affects overload resolution or generic instantiation).
            var originalRoot = editor.OriginalRoot;

            var originalNodes = diagnostics.SelectAsArray(diagnostic => FindDiagnosticNodes(document, diagnostic, options, cancellationToken));

            await editor.ApplyExpressionLevelSemanticEditsAsync(
                document,
                originalNodes,
                t =>
                {
                    using var additionalNodesToTrackDisposer = ArrayBuilder<SyntaxNode>.GetInstance(capacity: 2, out var additionalNodesToTrack);
                    additionalNodesToTrack.Add(t.identifier);
                    additionalNodesToTrack.Add(t.declarator);

                    return (t.invocationOrCreation, additionalNodesToTrack.ToImmutable());
                },
                (_1, _2, _3) => true,
                (semanticModel, currentRoot, t, currentNode)
                    => ReplaceIdentifierWithInlineDeclaration(
                        options, semanticModel, currentRoot, t.declarator,
                        t.identifier, t.invocationOrCreation, currentNode, declarationsToRemove),
                cancellationToken).ConfigureAwait(false);
        }

        private (VariableDeclaratorSyntax declarator, IdentifierNameSyntax identifier, SyntaxNode invocationOrCreation) FindDiagnosticNodes(
                    Document document, Diagnostic diagnostic,
                    OptionSet options, CancellationToken cancellationToken)
        {
            // Recover the nodes we care about.
            var declaratorLocation = diagnostic.AdditionalLocations[0];
            var identifierLocation = diagnostic.AdditionalLocations[1];
            var invocationOrCreationLocation = diagnostic.AdditionalLocations[2];
            var outArgumentContainingStatementLocation = diagnostic.AdditionalLocations[3];

            var declarator = (VariableDeclaratorSyntax)declaratorLocation.FindNode(cancellationToken);
            var identifier = (IdentifierNameSyntax)identifierLocation.FindNode(cancellationToken);
            var invocationOrCreation = (ExpressionSyntax)invocationOrCreationLocation.FindNode(
                getInnermostNodeForTie: true, cancellationToken: cancellationToken);

            return (declarator, identifier, invocationOrCreation);
        }

        private SyntaxNode ReplaceIdentifierWithInlineDeclaration(
            OptionSet options, SemanticModel semanticModel,
            SyntaxNode currentRoot, VariableDeclaratorSyntax declarator,
            IdentifierNameSyntax identifier, SyntaxNode invocationOrCreation,
            SyntaxNode currentNode, HashSet<StatementSyntax> declarationsToRemove)
        {
            declarator = currentRoot.GetCurrentNode(declarator);
            identifier = currentRoot.GetCurrentNode(identifier);

            var editor = new SyntaxEditor(currentRoot, CSharpSyntaxGenerator.Instance);
            var sourceText = currentRoot.GetText();

            var declaration = (VariableDeclarationSyntax)declarator.Parent;
            var singleDeclarator = declaration.Variables.Count == 1;

            if (singleDeclarator)
            {
                // This was a local statement with a single variable in it.  Just Remove
                // the entire local declaration statement. Note that comments belonging to
                // this local statement will be moved to be above the statement containing
                // the out-var.
                var localDeclarationStatement = (LocalDeclarationStatementSyntax)declaration.Parent;
                var block = (BlockSyntax)localDeclarationStatement.Parent;
                var declarationIndex = block.Statements.IndexOf(localDeclarationStatement);

                // Try to find a predecessor Statement on the same line that isn't going to be removed
                StatementSyntax priorStatementSyntax = null;
                var localDeclarationToken = localDeclarationStatement.GetFirstToken();
                for (var i = declarationIndex - 1; i >= 0; i--)
                {
                    var statementSyntax = block.Statements[i];
                    if (declarationsToRemove.Contains(statementSyntax))
                    {
                        continue;
                    }

                    if (sourceText.AreOnSameLine(statementSyntax.GetLastToken(), localDeclarationToken))
                    {
                        priorStatementSyntax = statementSyntax;
                    }

                    break;
                }

                if (priorStatementSyntax != null)
                {
                    // There's another statement on the same line as this declaration statement.
                    // i.e.   int a; int b;
                    //
                    // Just move all trivia from our statement to be trailing trivia of the previous
                    // statement
                    editor.ReplaceNode(
                        priorStatementSyntax,
                        (s, g) => s.WithAppendedTrailingTrivia(localDeclarationStatement.GetTrailingTrivia()));
                }
                else
                {
                    // Trivia on the local declaration will move to the next statement.
                    // use the callback form as the next statement may be the place where we're
                    // inlining the declaration, and thus need to see the effects of that change.

                    // Find the next Statement that isn't going to be removed.
                    // We initialize this to null here but we must see at least the statement
                    // into which the declaration is going to be inlined so this will be not null
                    StatementSyntax nextStatementSyntax = null;
                    for (var i = declarationIndex + 1; i < block.Statements.Count; i++)
                    {
                        var statement = block.Statements[i];
                        if (!declarationsToRemove.Contains(statement))
                        {
                            nextStatementSyntax = statement;
                            break;
                        }
                    }

                    editor.ReplaceNode(
                        nextStatementSyntax,
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
            var newType = GenerateTypeSyntaxOrVar(local.Type, options);

            var declarationExpression = GetDeclarationExpression(
                sourceText, identifier, newType, singleDeclarator ? null : declarator);

            // Check if using out-var changed problem semantics.
            var semanticsChanged = SemanticsChanged(semanticModel, currentRoot, currentNode, identifier, declarationExpression);
            if (semanticsChanged)
            {
                // Switching to 'var' changed semantics.  Just use the original type of the local.

                // If the user originally wrote it something other than 'var', then use what they
                // wrote.  Otherwise, synthesize the actual type of the local.
                var explicitType = declaration.Type.IsVar ? local.Type?.GenerateTypeSyntax() : declaration.Type;
                declarationExpression = SyntaxFactory.DeclarationExpression(explicitType, declarationExpression.Designation);
            }

            editor.ReplaceNode(identifier, declarationExpression);

            return editor.GetChangedRoot();
        }

        public static TypeSyntax GenerateTypeSyntaxOrVar(
           ITypeSymbol symbol, OptionSet options)
        {
            var useVar = IsVarDesired(symbol, options);

            // Note: we cannot use ".GenerateTypeSyntax()" only here.  that's because we're
            // actually creating a DeclarationExpression and currently the Simplifier cannot
            // analyze those due to limitations between how it uses Speculative SemanticModels
            // and how those don't handle new declarations well.
            return useVar
                ? SyntaxFactory.IdentifierName("var")
                : symbol.GenerateTypeSyntax();
        }

        private static bool IsVarDesired(ITypeSymbol type, OptionSet options)
        {
            // If they want it for intrinsics, and this is an intrinsic, then use var.
            if (type.IsSpecialType() == true)
            {
                return options.GetOption(CSharpCodeStyleOptions.VarForBuiltInTypes).Value;
            }

            // If they want "var" whenever possible, then use "var".
            return options.GetOption(CSharpCodeStyleOptions.VarElsewhere).Value;
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

        private bool SemanticsChanged(
            SemanticModel semanticModel,
            SyntaxNode root,
            SyntaxNode nodeToReplace,
            IdentifierNameSyntax identifier,
            DeclarationExpressionSyntax declarationExpression)
        {
            if (declarationExpression.Type.IsVar)
            {
                // Options want us to use 'var' if we can.  Make sure we didn't change
                // the semantics of the call by doing this.

                // Find the symbol that the existing invocation points to.
                var previousSymbol = semanticModel.GetSymbolInfo(nodeToReplace).Symbol;

                // Now, create a speculative model in which we make the change.  Make sure
                // we still point to the same symbol afterwards.

                var topmostContainer = GetTopmostContainer(nodeToReplace);
                if (topmostContainer == null)
                {
                    // Couldn't figure out what we were contained in.  Have to assume that semantics
                    // Are changing.
                    return true;
                }

                var annotation = new SyntaxAnnotation();
                var updatedTopmostContainer = topmostContainer.ReplaceNode(
                    nodeToReplace, nodeToReplace.ReplaceNode(identifier, declarationExpression)
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

        private SyntaxNode GetTopmostContainer(SyntaxNode expression)
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
