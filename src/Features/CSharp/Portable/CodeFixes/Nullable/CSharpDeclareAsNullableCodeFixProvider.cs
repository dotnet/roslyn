// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.DeclareAsNullable
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.DeclareAsNullable), Shared]
    internal class CSharpDeclareAsNullableCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        private const string IsConditionalOperatorEquivalenceKey = nameof(IsConditionalOperatorEquivalenceKey);
        private const string IsOtherEquivalenceKey = nameof(IsOtherEquivalenceKey);

        [ImportingConstructor]
        public CSharpDeclareAsNullableCodeFixProvider()
        {
        }

        // warning CS8603: Possible null reference return.
        // warning CS8600: Converting null literal or possible null value to non-nullable type.
        // warning CS8625: Cannot convert null literal to non-nullable reference type.
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create("CS8603", "CS8600", "CS8625");

        internal sealed override CodeFixCategory CodeFixCategory => CodeFixCategory.Compile;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var model = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
            var node = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);

            var declarationTypeToFix = TryGetDeclarationTypeToFix(node, model);
            if (declarationTypeToFix == null)
            {
                return;
            }

            context.RegisterCodeFix(new MyCodeAction(
                c => FixAsync(context.Document, diagnostic, c),
                GetEquivalenceKey(node)),
                context.Diagnostics);
        }

        private string GetEquivalenceKey(SyntaxNode node)
        {
            return node.IsKind(SyntaxKind.ConditionalAccessExpression) ? IsConditionalOperatorEquivalenceKey : IsOtherEquivalenceKey;
        }

        protected override async Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CancellationToken cancellationToken)
        {
            // a method can have multiple `return null;` statements, but we should only fix its return type once
            using var _ = PooledHashSet<TypeSyntax>.GetInstance(out var alreadyHandled);

            var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            foreach (var diagnostic in diagnostics)
            {
                var node = diagnostic.Location.FindNode(getInnermostNodeForTie: true, cancellationToken);
                MakeDeclarationNullable(editor, node, alreadyHandled, model);
            }

            alreadyHandled.Free();
        }

        protected override bool IncludeDiagnosticDuringFixAll(Diagnostic diagnostic, Document document, string equivalenceKey, CancellationToken cancellationToken)
        {
            var node = diagnostic.Location.FindNode(getInnermostNodeForTie: true, cancellationToken);
            return equivalenceKey == GetEquivalenceKey(node);
        }

        private static void MakeDeclarationNullable(SyntaxEditor editor, SyntaxNode node, HashSet<TypeSyntax> alreadyHandled, SemanticModel model)
        {
            var declarationTypeToFix = TryGetDeclarationTypeToFix(node, model);
            if (declarationTypeToFix != null && alreadyHandled.Add(declarationTypeToFix))
            {
                var fixedDeclaration = SyntaxFactory.NullableType(declarationTypeToFix.WithoutTrivia()).WithTriviaFrom(declarationTypeToFix);
                editor.ReplaceNode(declarationTypeToFix, fixedDeclaration);
            }
        }

        private static TypeSyntax TryGetDeclarationTypeToFix(SyntaxNode node, SemanticModel model)
        {
            if (!IsExpressionSupported(node))
            {
                return null;
            }

            if (node.IsParentKind(SyntaxKind.ReturnStatement, SyntaxKind.YieldReturnStatement))
            {
                var containingMember = node.GetAncestors().FirstOrDefault(a => a.IsKind(
                    SyntaxKind.MethodDeclaration, SyntaxKind.PropertyDeclaration, SyntaxKind.ParenthesizedLambdaExpression, SyntaxKind.SimpleLambdaExpression,
                    SyntaxKind.LocalFunctionStatement, SyntaxKind.AnonymousMethodExpression, SyntaxKind.ConstructorDeclaration, SyntaxKind.DestructorDeclaration,
                    SyntaxKind.OperatorDeclaration, SyntaxKind.IndexerDeclaration, SyntaxKind.EventDeclaration));

                if (containingMember == null)
                {
                    return null;
                }

                var onYield = node.IsParentKind(SyntaxKind.YieldReturnStatement);

                return containingMember switch
                {
                    MethodDeclarationSyntax method =>
                        // string M() { return null; }
                        // async Task<string> M() { return null; }
                        // IEnumerable<string> M() { yield return null; }
                        TryGetReturnType(method.ReturnType, method.Modifiers, onYield),

                    LocalFunctionStatementSyntax localFunction =>
                        // string local() { return null; }
                        // async Task<string> local() { return null; }
                        // IEnumerable<string> local() { yield return null; }
                        TryGetReturnType(localFunction.ReturnType, localFunction.Modifiers, onYield),

                    PropertyDeclarationSyntax property =>
                        // string x { get { return null; } }
                        // IEnumerable<string> Property { get { yield return null; } }
                        TryGetReturnType(property.Type, modifiers: default, onYield),

                    _ => null,
                };
            }

            // string x = null;
            if (node.Parent?.Parent?.IsParentKind(SyntaxKind.VariableDeclaration) == true)
            {
                var variableDeclaration = (VariableDeclarationSyntax)node.Parent.Parent.Parent;
                if (variableDeclaration.Variables.Count != 1)
                {
                    // string x = null, y = null;
                    return null;
                }

                return variableDeclaration.Type;
            }

            // Method(null)
            if (node.Parent is ArgumentSyntax argument && argument.Parent.Parent is InvocationExpressionSyntax invocation)
            {
                var symbol = model.GetSymbolInfo(invocation.Expression).Symbol;
                if (!(symbol is IMethodSymbol method) || method.PartialImplementationPart is object)
                {
                    // We don't handle partial methods yet
                    return null;
                }

                if (argument?.NameColon?.Name is IdentifierNameSyntax { Identifier: var identifier })
                {
                    var parameter = method.Parameters.Where(p => p.Name == identifier.Text).FirstOrDefault();
                    if (parameter is object)
                    {
                        return parameter.DeclaringSyntaxReferences.Select(r => ((ParameterSyntax)r.GetSyntax()).Type).FirstOrDefault();
                    }
                    return null;
                }

                var index = invocation.ArgumentList.Arguments.IndexOf(argument);
                if (index >= 0 && index < method.Parameters.Length)
                {
                    var parameter = method.Parameters[index];
                    return parameter.DeclaringSyntaxReferences.Select(r => ((ParameterSyntax)r.GetSyntax()).Type).FirstOrDefault();
                }

                return null;
            }

            // string x { get; set; } = null;
            if (node.Parent.IsParentKind(SyntaxKind.PropertyDeclaration, out PropertyDeclarationSyntax propertyDeclaration))
            {
                return propertyDeclaration.Type;
            }

            // void M(string x = null) { }
            if (node.Parent.IsParentKind(SyntaxKind.Parameter, out ParameterSyntax parameter))
            {
                return parameter.Type;
            }

            // static string M() => null;
            if (node.IsParentKind(SyntaxKind.ArrowExpressionClause) &&
                node.Parent.IsParentKind(SyntaxKind.MethodDeclaration, out MethodDeclarationSyntax arrowMethod))
            {
                return arrowMethod.ReturnType;
            }

            return null;

            // local functions
            static TypeSyntax TryGetReturnType(TypeSyntax returnType, SyntaxTokenList modifiers, bool onYield)
            {
                if (modifiers.Any(SyntaxKind.AsyncKeyword) || onYield)
                {
                    // async Task<string> M() { return null; }
                    // async IAsyncEnumerable<string> M() { yield return null; }
                    // IEnumerable<string> M() { yield return null; }
                    return TryGetSingleTypeArgument(returnType);
                }

                // string M() { return null; }
                return returnType;
            }

            static TypeSyntax TryGetSingleTypeArgument(TypeSyntax type)
            {
                switch (type)
                {
                    case QualifiedNameSyntax qualified:
                        return TryGetSingleTypeArgument(qualified.Right);

                    case GenericNameSyntax generic:
                        var typeArguments = generic.TypeArgumentList.Arguments;
                        if (typeArguments.Count == 1)
                        {
                            return typeArguments[0];
                        }
                        break;
                }
                return null;
            }
        }

        private static bool IsExpressionSupported(SyntaxNode node)
        {
            return node.IsKind(
                SyntaxKind.NullLiteralExpression,
                SyntaxKind.AsExpression,
                SyntaxKind.DefaultExpression,
                SyntaxKind.DefaultLiteralExpression,
                SyntaxKind.ConditionalExpression,
                SyntaxKind.ConditionalAccessExpression);
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument, string equivalenceKey)
                : base(CSharpFeaturesResources.Declare_as_nullable,
                       createChangedDocument,
                       equivalenceKey)
            {
            }
        }
    }
}
