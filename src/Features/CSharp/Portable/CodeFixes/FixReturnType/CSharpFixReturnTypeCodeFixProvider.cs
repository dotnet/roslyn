// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
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
                return;

            if (IsVoid(analyzedTypes.declarationToFix) && IsVoid(analyzedTypes.fixedDeclaration))
            {
                // Don't offer a code fix if the return type is void and return is followed by a void expression.
                // See https://github.com/dotnet/roslyn/issues/47089
                return;
            }

            context.RegisterCodeFix(
               new MyCodeAction(c => FixAsync(document, diagnostics.First(), c)),
               diagnostics);

            return;

            static bool IsVoid(TypeSyntax typeSyntax)
                => typeSyntax is PredefinedTypeSyntax { Keyword.RawKind: (int)SyntaxKind.VoidKeyword };
        }

        private static async Task<(TypeSyntax declarationToFix, TypeSyntax fixedDeclaration)> TryGetOldAndNewReturnTypeAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics, CancellationToken cancellationToken)
        {
            Debug.Assert(diagnostics.Length == 1);
            var location = diagnostics[0].Location;
            var node = location.FindNode(getInnermostNodeForTie: true, cancellationToken);
            var returnedValue = node is ReturnStatementSyntax returnStatement ? returnStatement.Expression : node;
            if (returnedValue == null)
                return default;

            var (declarationTypeToFix, useTask) = TryGetDeclarationTypeToFix(node);
            if (declarationTypeToFix == null)
                return default;

            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var returnedType = semanticModel.GetTypeInfo(returnedValue, cancellationToken).Type;
            returnedType ??= semanticModel.Compilation.ObjectType;

            TypeSyntax fixedDeclaration;
            if (useTask)
            {
                var taskOfTType = semanticModel.Compilation.TaskOfTType();
                if (taskOfTType is null)
                    return default;

                fixedDeclaration = taskOfTType.Construct(returnedType).GenerateTypeSyntax(allowVar: false);
            }
            else
            {
                fixedDeclaration = returnedType.GenerateTypeSyntax(allowVar: false);
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
            return node.GetAncestors().Select(TryGetReturnTypeToFix).FirstOrDefault(p => p.type != null);

            static (TypeSyntax type, bool useTask) TryGetReturnTypeToFix(SyntaxNode containingMember)
            {
                return containingMember switch
                {
                    // void M() { return 1; }
                    // async Task M() { return 1; }
                    MethodDeclarationSyntax method => (method.ReturnType, method.Modifiers.Any(SyntaxKind.AsyncKeyword)),
                    // void local() { return 1; }
                    // async Task local() { return 1; }
                    LocalFunctionStatementSyntax localFunction => (localFunction.ReturnType, localFunction.Modifiers.Any(SyntaxKind.AsyncKeyword)),
                    _ => default,
                };
            }
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(CSharpFeaturesResources.Fix_return_type, createChangedDocument, nameof(CSharpFeaturesResources.Fix_return_type))
            {
            }
        }
    }
}
