﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeFixes.Async;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.Async
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.ConvertToAsync), Shared]
    internal class CSharpConvertToAsyncMethodCodeFixProvider : AbstractChangeToAsyncCodeFixProvider
    {
        /// <summary>
        /// Cannot await void.
        /// </summary>
        private const string CS4008 = nameof(CS4008);

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpConvertToAsyncMethodCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(CS4008); }
        }

        protected override async Task<string> GetDescriptionAsync(
            Diagnostic diagnostic,
            SyntaxNode node,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            var methodNode = await GetMethodDeclarationAsync(node, semanticModel, cancellationToken).ConfigureAwait(false);

            // We only call GetDescription when we already know that we succeeded (so it's safe to
            // assume we have a methodNode here).
            return string.Format(CSharpFeaturesResources.Make_0_return_Task_instead_of_void, methodNode!.WithBody(null));
        }

        protected override async Task<Tuple<SyntaxTree, SyntaxNode>?> GetRootInOtherSyntaxTreeAsync(
            SyntaxNode node,
            SemanticModel semanticModel,
            Diagnostic diagnostic,
            CancellationToken cancellationToken)
        {
            var methodDeclaration = await GetMethodDeclarationAsync(node, semanticModel, cancellationToken).ConfigureAwait(false);
            if (methodDeclaration == null)
            {
                return null;
            }

            var oldRoot = await methodDeclaration.SyntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);
            var newRoot = oldRoot.ReplaceNode(methodDeclaration, ConvertToAsyncFunction(methodDeclaration));
            return Tuple.Create(oldRoot.SyntaxTree, newRoot);
        }

        private static async Task<MethodDeclarationSyntax?> GetMethodDeclarationAsync(
            SyntaxNode node,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            var invocationExpression = node.ChildNodes().FirstOrDefault(n => n.IsKind(SyntaxKind.InvocationExpression));
            if (invocationExpression == null)
            {
                return null;
            }

            if (!(semanticModel.GetSymbolInfo(invocationExpression, cancellationToken).Symbol is IMethodSymbol methodSymbol))
            {
                return null;
            }

            var methodReference = methodSymbol.DeclaringSyntaxReferences.FirstOrDefault();
            if (methodReference == null)
            {
                return null;
            }

            if (!((await methodReference.GetSyntaxAsync(cancellationToken).ConfigureAwait(false)) is MethodDeclarationSyntax methodDeclaration))
            {
                return null;
            }

            if (!methodDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.AsyncKeyword)))
            {
                return null;
            }

            return methodDeclaration;
        }

        private static MethodDeclarationSyntax ConvertToAsyncFunction(MethodDeclarationSyntax methodDeclaration)
        {
            return methodDeclaration.WithReturnType(
                SyntaxFactory.ParseTypeName("Task")
                    .WithTriviaFrom(methodDeclaration));
        }
    }
}
