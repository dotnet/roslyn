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

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {

            var document = context.Document;
            var cancellationToken = context.CancellationToken;
            var diagnostic = context.Diagnostics.First();

            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var targetNode = (ExpressionSyntax)TryGetTargetNode(root, diagnostic.Location.SourceSpan);
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            if (CanReplace(semanticModel, root, targetNode, cancellationToken))
            {
                context.RegisterCodeFix(new MyCodeAction(
                    CSharpFeaturesResources.Add_Explicit_Cast,
                    c => FixAsync(context.Document, context.Diagnostics.First(), c)),
                    context.Diagnostics);
            }
        }

        private void GetTypeInfo(SemanticModel semanticModel, SyntaxNode root, SyntaxNode targetNode, CancellationToken cancellationToken, out ITypeSymbol nodeType, out ITypeSymbol conversionType)
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
                var argumentListNode = argument.Parent;
                var invocationNode = argumentListNode.Parent;
                var symbolInfo = semanticModel.GetSymbolInfo(invocationNode, cancellationToken);
                var candidcateSymbols = symbolInfo.CandidateSymbols;

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

                    var allValid = true;
                    for (var i = 0; i < parameterList.Length; i++)
                    {
                        var argType = semanticModel.GetTypeInfo(argumentList.Arguments[i].Expression, cancellationToken);
                        if (!IsTypeConvertible(argType.Type, parameterList[i].Type))
                        {
                            allValid = false;
                            break;
                        }
                    }

                    if (allValid)
                    {
                        var targetArgumentIndex = argumentList.Arguments.IndexOf((ArgumentSyntax)argument);
                        var correspondingParameter = parameterList[targetArgumentIndex];
                        potentialConversionTypes.Add(correspondingParameter.Type);
                    }
                }

                if (potentialConversionTypes.Count != 1)
                {
                    return;
                }

                conversionType = potentialConversionTypes[0];
            }
        }

        private bool IsTypeConvertible(ITypeSymbol nodeType, ITypeSymbol conversionType)
        {
            var iterator = conversionType;
            while (iterator != null)
            {
                if (nodeType == iterator) return true;
                iterator = iterator.BaseType;
            }
            return false;
        }

        private bool CanReplace(SemanticModel semanticModel, SyntaxNode root, SyntaxNode targetNode, CancellationToken cancellationToken)
        {
            GetTypeInfo(semanticModel, root, targetNode, cancellationToken, out var nodeType, out var conversionType);
            return nodeType != conversionType && IsTypeConvertible(nodeType, conversionType);
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
                (_, targetNode) => true,
                (semanticModel, currentRoot, targetNode) =>
                {
                    GetTypeInfo(semanticModel, currentRoot, targetNode, cancellationToken, out var nodeType, out var conversionType);
                    if (nodeType != conversionType && conversionType != null && IsTypeConvertible(nodeType, conversionType) && targetNode is ExpressionSyntax expression)
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
