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
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.FixReturnType
{
    /// <summary>
    /// Helps fix void-returning methods or local functions to return a correct type.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.FixReturnType), Shared]
    internal class CSharpFixReturnTypeCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        // error CS0127: Since 'M()' returns void, a return keyword must not be followed by an object expression
        // error CS1997: Since 'M()' is an async method that returns 'Task', a return keyword must not be followed by an object expression. Did you intend to return 'Task<T>'?
        // error CS0201: Only assignment, call, increment, decrement, await, and new object expressions can be used as a statement
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create("CS0127", "CS1997", "CS0201");

        [ImportingConstructor]
        public CSharpFixReturnTypeCodeFixProvider()
            : base(supportsFixAll: false)
        {
        }

        internal override CodeFixCategory CodeFixCategory => CodeFixCategory.Compile;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var document = context.Document;
            var diagnostics = context.Diagnostics;
            var cancellationToken = context.CancellationToken;

            var analyzedTypes = await TryGetOldAndNewReturnTypeAsync(document, diagnostics, cancellationToken).ConfigureAwait(false);
            if (analyzedTypes == default)
            {
                return;
            }

            context.RegisterCodeFix(
               new MyCodeAction(c => FixAsync(document, diagnostics.First(), c)),
               diagnostics);
        }

        private async Task<(TypeSyntax declarationToFix, TypeSyntax fixedDeclaration)> TryGetOldAndNewReturnTypeAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics, CancellationToken cancellationToken)
        {
            Debug.Assert(diagnostics.Length == 1);
            var location = diagnostics[0].Location;
            var node = location.FindNode(getInnermostNodeForTie: true, cancellationToken);
            var returnedValue = node is ReturnStatementSyntax returnStatement ? returnStatement.Expression : node;
            if (returnedValue == null)
            {
                return default;
            }

            var (declarationTypeToFix, useTask) = TryGetDeclarationTypeToFix(node);
            if (declarationTypeToFix == null)
            {
                return default;
            }

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var returnedType = semanticModel.GetTypeInfo(returnedValue, cancellationToken).Type;
            if (returnedType == null)
            {
                returnedType = semanticModel.Compilation.ObjectType;
            }

            TypeSyntax fixedDeclaration;
            if (useTask)
            {
                var taskOfTType = semanticModel.Compilation.TaskOfTType();
                if (taskOfTType is null)
                {
                    return default;
                }

                fixedDeclaration = taskOfTType.Construct(returnedType).GenerateTypeSyntax();
            }
            else
            {
                fixedDeclaration = returnedType.GenerateTypeSyntax();
            }

            fixedDeclaration = fixedDeclaration.WithAdditionalAnnotations(Simplifier.Annotation).WithTriviaFrom(declarationTypeToFix);

            return (declarationTypeToFix, fixedDeclaration);
        }

        protected override async Task FixAllAsync(Document document, ImmutableArray<Diagnostic> diagnostics, SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var (declarationTypeToFix, fixedDeclaration) =
                await TryGetOldAndNewReturnTypeAsync(document, diagnostics, cancellationToken).ConfigureAwait(false);

            editor.ReplaceNode(declarationTypeToFix, fixedDeclaration);
        }

        private static (TypeSyntax type, bool useTask) TryGetDeclarationTypeToFix(SyntaxNode node)
        {
            return node.GetAncestors().Select(a => TryGetReturnTypeToFix(a)).FirstOrDefault(p => p != default);

            // Local functions
            static (TypeSyntax type, bool useTask) TryGetReturnTypeToFix(SyntaxNode containingMember)
            {
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
            }

            static bool IsAsync(SyntaxTokenList modifiers)
            {
                return modifiers.Any(SyntaxKind.AsyncKeyword);
            }
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(CSharpFeaturesResources.Fix_return_type,
                     createChangedDocument,
                     CSharpFeaturesResources.Fix_return_type)
            {
            }
        }
    }
}
