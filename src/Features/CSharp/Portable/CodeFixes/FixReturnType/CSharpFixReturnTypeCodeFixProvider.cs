// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.FixReturnType
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.FixReturnType), Shared]
    internal class CSharpFixReturnTypeCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        // error CS0127: Since 'M()' returns void, a return keyword must not be followed by an object expression
        private const string CS0127 = nameof(CS0127);

        // error CS1997: Since 'M()' is an async method that returns 'Task', a return keyword must not be followed by an object expression. Did you intend to return 'Task<T>'?
        private const string CS1997 = nameof(CS1997);

        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(CS0127, CS1997);

        public CSharpFixReturnTypeCodeFixProvider()
            : base(supportsFixAll: false)
        {
        }

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            if (await TryGetOldAndNewReturnTypeAsync(context.Document, context.Diagnostics, context.CancellationToken).ConfigureAwait(false) == default)
            {
                return;
            }

            context.RegisterCodeFix(
               new MyCodeAction(c => FixAsync(context.Document, context.Diagnostics.First(), c)),
               context.Diagnostics);
        }

        private async Task<(TypeSyntax declarationToFix, TypeSyntax fixedDeclaration)> TryGetOldAndNewReturnTypeAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics, CancellationToken cancellationToken)
        {
            Debug.Assert(diagnostics.Length == 1);
            var location = diagnostics[0].Location;
            var returnStatement = (ReturnStatementSyntax)location.FindNode(getInnermostNodeForTie: true, cancellationToken: cancellationToken);

            var returnedValue = returnStatement.Expression;
            if (returnedValue == null)
            {
                return default;
            }

            var (declarationTypeToFix, useTask) = TryGetDeclarationTypeToFix(returnStatement);
            if (declarationTypeToFix == null)
            {
                return default;
            }

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var returnedType = semanticModel.GetTypeInfo(returnedValue, cancellationToken).Type;
            if (returnedType == null)
            {
                return default;
            }

            var fixedType = useTask
                ? semanticModel.Compilation.TaskOfTType().Construct(returnedType)
                : returnedType;
            var fixedDeclaration = fixedType.GenerateTypeSyntax().WithTriviaFrom(declarationTypeToFix);

            return (declarationTypeToFix, fixedDeclaration);
        }

        protected override async Task FixAllAsync(Document document, ImmutableArray<Diagnostic> diagnostics, SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var (declarationTypeToFix, fixedDeclaration) =
                await TryGetOldAndNewReturnTypeAsync(document, diagnostics, cancellationToken).ConfigureAwait(false);

            editor.ReplaceNode(declarationTypeToFix, fixedDeclaration);
            return;
        }

        private static (TypeSyntax type, bool useTask) TryGetDeclarationTypeToFix(SyntaxNode node)
        {
            if (!node.IsKind(SyntaxKind.ReturnStatement))
            {
                return default;
            }

            var containingMember = node.GetAncestors().FirstOrDefault(a => a.IsKind(
                SyntaxKind.MethodDeclaration, SyntaxKind.PropertyDeclaration, SyntaxKind.ParenthesizedLambdaExpression, SyntaxKind.SimpleLambdaExpression,
                SyntaxKind.LocalFunctionStatement, SyntaxKind.AnonymousMethodExpression, SyntaxKind.ConstructorDeclaration, SyntaxKind.DestructorDeclaration,
                SyntaxKind.OperatorDeclaration, SyntaxKind.IndexerDeclaration, SyntaxKind.EventDeclaration));

            if (containingMember == null)
            {
                return default;
            }

            switch (containingMember)
            {
                case MethodDeclarationSyntax method:
                    // void M() { return 1; }
                    // async Task M() { return 1; }
                    return (method.ReturnType, IsAsync(method.Modifiers));

                case LocalFunctionStatementSyntax localFunction:
                    // void local() { return 1; }
                    // async Task local() { return 1; }
                    return (localFunction.ReturnType, IsAsync(localFunction.Modifiers));

                default:
                    return default;
            }

            // Local functions
            bool IsAsync(SyntaxTokenList modifiers)
            {
                return modifiers.Any(SyntaxKind.AsyncKeyword);
            }
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument) :
                base(CSharpFeaturesResources.Fix_return_type,
                     createChangedDocument,
                     CSharpFeaturesResources.Fix_return_type)
            {
            }
        }
    }
}
