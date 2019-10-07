// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeFixes.Iterator;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.Iterator
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.ChangeReturnType), Shared]
    internal class CSharpChangeToIEnumerableCodeFixProvider : AbstractIteratorCodeFixProvider
    {
        /// <summary>
        /// CS1624: The body of 'x' cannot be an iterator block because 'y' is not an iterator interface type
        /// </summary>
        private const string CS1624 = nameof(CS1624);

        [ImportingConstructor]
        public CSharpChangeToIEnumerableCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(CS1624); }
        }

        protected override async Task<CodeAction> GetCodeFixAsync(SyntaxNode root, SyntaxNode node, Document document, Diagnostic diagnostics, CancellationToken cancellationToken)
        {
            var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var methodSymbol = model.GetDeclaredSymbol(node, cancellationToken) as IMethodSymbol;
            // IMethod symbol can either be a regular method or an accessor
            if (methodSymbol?.ReturnType == null || methodSymbol.ReturnsVoid)
            {
                return null;
            }

            var type = methodSymbol.ReturnType;
            if (!TryGetIEnumerableSymbols(model, out var ienumerableSymbol, out var ienumerableGenericSymbol))
            {
                return null;
            }

            if (type.InheritsFromOrEquals(ienumerableSymbol, includeInterfaces: true))
            {
                var arity = type.GetArity();
                if (arity == 1)
                {
                    var typeArg = type.GetTypeArguments().First();
                    ienumerableGenericSymbol = ienumerableGenericSymbol.ConstructWithNullability(typeArg);
                }
                else if (arity == 0 && type is IArrayTypeSymbol)
                {
                    ienumerableGenericSymbol = ienumerableGenericSymbol.ConstructWithNullability((type as IArrayTypeSymbol).ElementType);
                }
                else
                {
                    return null;
                }
            }
            else
            {
                ienumerableGenericSymbol = ienumerableGenericSymbol.Construct(type);
            }

            var newReturnType = ienumerableGenericSymbol.GenerateTypeSyntax();
            Document newDocument = null;
            var newMethodDeclarationSyntax = (node as MethodDeclarationSyntax)?.WithReturnType(newReturnType);
            if (newMethodDeclarationSyntax != null)
            {
                newDocument = document.WithSyntaxRoot(root.ReplaceNode(node, newMethodDeclarationSyntax));
            }

            var newOperator = (node as OperatorDeclarationSyntax)?.WithReturnType(newReturnType);
            if (newOperator != null)
            {
                newDocument = document.WithSyntaxRoot(root.ReplaceNode(node, newOperator));
            }

            var oldAccessor = (node?.Parent?.Parent as PropertyDeclarationSyntax);
            if (oldAccessor != null)
            {
                newDocument = document.WithSyntaxRoot(root.ReplaceNode(oldAccessor, oldAccessor.WithType(newReturnType)));
            }

            var oldIndexer = (node?.Parent?.Parent as IndexerDeclarationSyntax);
            if (oldIndexer != null)
            {
                newDocument = document.WithSyntaxRoot(root.ReplaceNode(oldIndexer, oldIndexer.WithType(newReturnType)));
            }

            if (newDocument == null)
            {
                return null;
            }

            return new MyCodeAction(
                string.Format(CSharpFeaturesResources.Change_return_type_from_0_to_1,
                    type.ToMinimalDisplayString(model, node.SpanStart),
                    ienumerableGenericSymbol.ToMinimalDisplayString(model, node.SpanStart)), newDocument);
        }

        private static bool TryGetIEnumerableSymbols(SemanticModel model, out INamedTypeSymbol ienumerableSymbol, out INamedTypeSymbol ienumerableGenericSymbol)
        {
            ienumerableSymbol = model.Compilation.GetTypeByMetadataName(typeof(IEnumerable).FullName);
            ienumerableGenericSymbol = model.Compilation.GetTypeByMetadataName(typeof(IEnumerable<>).FullName);

            if (ienumerableGenericSymbol == null ||
                ienumerableSymbol == null)
            {
                return false;
            }

            return true;
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(string title, Document newDocument)
                : base(title, c => Task.FromResult(newDocument))
            {
            }
        }
    }
}
