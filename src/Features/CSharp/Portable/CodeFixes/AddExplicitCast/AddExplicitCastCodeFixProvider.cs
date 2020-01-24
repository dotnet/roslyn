// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.AddExplicitCast
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.AddExplicitCast), Shared]
    [ExtensionOrder(After = PredefinedCodeFixProviderNames.ChangeToYield, Before = PredefinedCodeFixProviderNames.GenerateConstructor)]
    internal partial class AddExplicitCastCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        /// <summary>
        /// CS0266: Cannot implicitly convert from type 'x' to 'y'. An explicit conversion exists (are you missing a cast?)
        /// </summary>
        private const string CS0266 = nameof(CS0266);

        /// <summary>
        /// CS1503: Argument 1: cannot convert from 'double' to 'int'
        /// </summary>
        private const string CS1503 = nameof(CS1503);

        [ImportingConstructor]
        public AddExplicitCastCodeFixProvider()
        {
        }

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(CS0266, CS1503); }
        }

        internal sealed override CodeFixCategory CodeFixCategory => CodeFixCategory.CodeStyle; // ?

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context) // ?
        {
            context.RegisterCodeFix(new MyCodeAction(
                CSharpFeaturesResources.Add_Explicit_Cast,
                c => FixAsync(context.Document, context.Diagnostics.First(), c)),
                context.Diagnostics);
            return Task.CompletedTask;

            //var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            //var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);

            //if (!TryGetTargetNode(root, context.Span, out var target))
            //{
            //    return;
            //}

            //var nodeInfo = semanticModel.GetTypeInfo(target);
            //var nodeType = nodeInfo.Type;
            //var conversionType = nodeInfo.ConvertedType;

            //TryGetNode(root, context.Span, SyntaxKind.Argument, target, out var argument);
            //if (argument != null)
            //{
            //    // todo: constructor as well?
            //    TryGetNode(root, context.Span, SyntaxKind.InvocationExpression, target, out var invocationNode);
            //    TryGetNode(root, context.Span, SyntaxKind.ArgumentList, target, out var argumentListNode);
            //    var symbolInfo = semanticModel.GetSymbolInfo(invocationNode);
            //    var candidcateSymbols = symbolInfo.CandidateSymbols;

            //    if (candidcateSymbols.Length != 1)
            //    {
            //        // either no candidates or multiple candidates
            //        return;
            //    }

            //    var methodSymbol = (IMethodSymbol)candidcateSymbols[0];
            //    var parameterList = methodSymbol.Parameters;

            //    var argumentList = (ArgumentListSyntax)argumentListNode;
            //    var argIndex = argumentList.Arguments.IndexOf((ArgumentSyntax)argument);

            //    var parameter = parameterList[argIndex];
            //    conversionType = parameter.Type;
            //}

            //// check if target node type and target converted type have a base-derived relationship
            //if (IsValid(nodeType, conversionType) && target is ExpressionSyntax expression)
            //{
            //    var castExpression = expression.Cast(conversionType);
            //    root = root.ReplaceNode(expression, castExpression);
            //    context.RegisterCodeFix(new MyCodeAction(
            //        CSharpFeaturesResources.Add_Explicit_Cast,
            //        context.Document.WithSyntaxRoot(root)),
            //        context.Diagnostics);
            //}
        }

        private void GetTypeInfo(SemanticModel semanticModel, SyntaxNode root, SyntaxNode targetNode, CancellationToken cancellationToken, out ITypeSymbol nodeType, out ITypeSymbol conversionType) // add CancellationToken cancellationToken ?
        {
            var nodeInfo = semanticModel.GetTypeInfo(targetNode, cancellationToken);
            nodeType = nodeInfo.Type;
            conversionType = nodeInfo.ConvertedType;

            // CS1503 
            var textSpan = targetNode.GetLocation().SourceSpan;
            TryGetNode(root, textSpan, SyntaxKind.Argument, targetNode, out var argument);
            if (argument != null)
            {
                conversionType = null;
                // todo: constructor as well?
                TryGetNode(root, textSpan, SyntaxKind.InvocationExpression, targetNode, out var invocationNode);
                var symbolInfo = semanticModel.GetSymbolInfo(invocationNode, cancellationToken);
                var candidcateSymbols = symbolInfo.CandidateSymbols;

                TryGetNode(root, textSpan, SyntaxKind.ArgumentList, targetNode, out var argumentListNode);
                var argumentList = (ArgumentListSyntax)argumentListNode;

                var potentialConversionTypes = new List<ITypeSymbol> {};
                foreach (var candidcateSymbol in candidcateSymbols)
                {
                    var methodSymbol = (IMethodSymbol)candidcateSymbol;
                    var parameterList = methodSymbol.Parameters;
                    if (parameterList.Length != argumentList.Arguments.Count) 
                    {
                        continue;
                    }

                    var argIndex = argumentList.Arguments.IndexOf((ArgumentSyntax)argument);

                    var parameter = parameterList[argIndex];
                    if (IsTypeValid(nodeType, parameter.Type))
                    {
                        potentialConversionTypes.Add(parameter.Type);
                    }
                }

                if (potentialConversionTypes.Count != 1)
                {
                    return;
                }

                conversionType = potentialConversionTypes[0];
            }
        }

        private bool IsTypeValid(ITypeSymbol nodeType, ITypeSymbol conversionType)
        {
            var iterator = conversionType;
            while (iterator != null)
            {
                if (nodeType == iterator.BaseType) return true;
                iterator = iterator.BaseType;
            }
            return false;
        }

        private bool CanReplace(SemanticModel semanticModel, SyntaxNode root, SyntaxNode targetNode, CancellationToken cancellationToken)
        {
            GetTypeInfo(semanticModel, root, targetNode, cancellationToken, out var nodeType, out var conversionType);
            return IsTypeValid(nodeType, conversionType);
        }

        protected SyntaxNode TryGetTargetNode(SyntaxNode root, TextSpan span)
        {
            var ancestors = root.FindToken(span.Start).GetAncestors<SyntaxNode>();
            if (!ancestors.Any())
            {
                return null;
            }

            var node = ancestors.FirstOrDefault(n => n.Span.Contains(span) && n != root);
            return node;
        }

        protected bool TryGetNode(SyntaxNode root, TextSpan span, SyntaxKind kind, SyntaxNode target, out SyntaxNode node)
        {
            node = null;
            var ancestors = root.FindToken(span.Start).GetAncestors<SyntaxNode>();
            if (!ancestors.Any())
            {
                return false;
            }

            node = ancestors.FirstOrDefault(n => n.Span.Contains(span) && n != root && n != target && n.IsKind(kind));
            return node != null;
        }

        protected override async Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var targetNodes = diagnostics.SelectAsArray(
                d => (ExpressionSyntax)TryGetTargetNode(root, d.Location.SourceSpan));

            await editor.ApplyExpressionLevelSemanticEditsAsync(
                document, targetNodes,
                (semanticModel, targetNode) => CanReplace(semanticModel, root, targetNode, cancellationToken),
                (semanticModel, currentRoot, targetNode) =>
                {
                    GetTypeInfo(semanticModel, currentRoot, targetNode, cancellationToken, out var nodeType, out var conversionType);
                    if (targetNode is ExpressionSyntax expression)
                    {
                        var castExpression = expression.Cast(conversionType);
                        return currentRoot.ReplaceNode(expression, castExpression);
                    }

                    return currentRoot;
                },
                cancellationToken).ConfigureAwait(false);
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(title, createChangedDocument)
            {
            }
        }
    }
}
