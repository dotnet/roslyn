// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        private const string CS1624 = "CS1624";

        public override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(CS1624); }
        }

        protected override async Task<CodeAction> GetCodeFixAsync(SyntaxNode root, SyntaxNode node, Document document, Diagnostic diagnostics, CancellationToken cancellationToken)
        {
            var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var methodSymbol = model.GetDeclaredSymbol(node, cancellationToken) as IMethodSymbol;
            if (methodSymbol.ReturnsVoid)
            {
                return null;
            }

            var ienumerableSymbol = model.Compilation.GetTypeByMetadataName("System.Collections.IEnumerable");
            var ienumeratorSymbol = model.Compilation.GetTypeByMetadataName("System.Collections.IEnumerator");
            var ienumerableGenericSymbol = model.Compilation.GetTypeByMetadataName("System.Collections.Generic.IEnumerable`1");
            var ienumeratorGenericSymbol = model.Compilation.GetTypeByMetadataName("System.Collections.Generic.IEnumerator`1");

            if (ienumerableGenericSymbol == null ||
                ienumerableSymbol == null ||
                ienumeratorGenericSymbol == null ||
                ienumeratorSymbol == null)
            {
                return null;
            }

            var returnType = methodSymbol.ReturnType;

            if (returnType.InheritsFromOrEquals(ienumerableSymbol))
            {
                if (returnType.GetArity() != 1)
                {
                    return null;
                }

                var typeArg = returnType.GetTypeArguments().First();
                ienumerableGenericSymbol = ienumerableGenericSymbol.Construct(typeArg);
            }
            else
            {
                ienumerableGenericSymbol = ienumerableGenericSymbol.Construct(returnType);
            }

            TypeSyntax oldReturnType;
            var newReturnType = ienumerableGenericSymbol.GenerateTypeSyntax();
            var newMethodDeclarationNode = WithReturnType(node, newReturnType, out oldReturnType);
            if (newMethodDeclarationNode == null)
            {
                // the language updated and added a new kind of MethodDeclarationSyntax without updating this file
                return null;
            }

            root = root.ReplaceNode(node, newMethodDeclarationNode);
            var newDocument = document.WithSyntaxRoot(root);
            return new MyCodeAction(
                string.Format(CSharpFeaturesResources.ChangeReturnType,
                    oldReturnType.ToString(),
                    ienumerableGenericSymbol.ToMinimalDisplayString(model, node.SpanStart)), newDocument);
        }

        private SyntaxNode WithReturnType(SyntaxNode methodOrFunction, TypeSyntax newReturnType, out TypeSyntax oldReturnType)
        {
            var method = methodOrFunction as MethodDeclarationSyntax;
            if (method != null)
            {
                oldReturnType = method.ReturnType;
                return method.WithReturnType(newReturnType);
            }

            var localFunction = methodOrFunction as LocalFunctionStatementSyntax;
            if (localFunction != null)
            {
                oldReturnType = localFunction.ReturnType;
                return localFunction.WithReturnType(newReturnType);
            }

            oldReturnType = null;
            return null;
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(string title, Document newDocument) :
                base(title, c => Task.FromResult(newDocument))
            {
            }
        }
    }
}
