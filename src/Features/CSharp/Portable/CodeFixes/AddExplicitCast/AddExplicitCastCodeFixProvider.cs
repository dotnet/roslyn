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
using Microsoft.CodeAnalysis.ConvertTupleToStruct;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

#nullable enable
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
            get => ImmutableArray.Create(CS0266, CS1503);
        }

        internal sealed override CodeFixCategory CodeFixCategory => CodeFixCategory.CodeStyle; // ?

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var document = context.Document;
            var cancellationToken = context.CancellationToken;
            var diagnostic = context.Diagnostics.First();

            var root = await document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            if (root != null && TryGetTargetNode(root, diagnostic.Location.SourceSpan) is ExpressionSyntax targetNode)
            {
                var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                if (semanticModel != null)
                {
                    var exactSolution = GetTypeInfo(semanticModel, root, targetNode, cancellationToken, out var nodeType, out var conversionType, out var potentialConvTypes);
                    if (exactSolution)
                    {
                        context.RegisterCodeFix(new MyCodeAction(
                        CSharpFeaturesResources.Add_explicit_cast,
                        c => FixAsync(context.Document, context.Diagnostics.First(), c)),
                        context.Diagnostics);
                    }
                    else if (potentialConvTypes.Length > 1)
                    {
                        var actions = new ArrayBuilder<CodeAction>();
                        foreach (var convType in potentialConvTypes)
                        {
                            actions.Add(new MyCodeAction(string.Format(CSharpFeaturesResources.Convert_type_to_0, convType.Name),
                                c => FixIt(context.Document, root, targetNode, convType, c)));
                        }

                        context.RegisterCodeFix(new CodeAction.CodeActionWithNestedActions(
                        CSharpFeaturesResources.Add_explicit_cast,
                        actions.ToImmutableArray(), false),
                        context.Diagnostics);
                    }
                }
            }
        }

        private async Task<Document> FixIt(Document document, SyntaxNode currentRoot, ExpressionSyntax targetNode, ITypeSymbol conversionType, CancellationToken cancellationToken)
        {
            var castExpression = targetNode.Cast(conversionType);
            var newRoot = currentRoot.ReplaceNode(targetNode, castExpression.WithAdditionalAnnotations(Simplifier.Annotation));
            return document.WithSyntaxRoot(newRoot);
        }

        // Output the current type info of the target node and the conversion type that the target node is going to be cast by
        // Implicit downcast can appear on Variable Declaration, Return Statement, and Function Invocation, for example:
        // Base b; Derived d = [||]b;       
        // object b is the current node with type *Base*, and the conversion type which object b is going to be cast by is *Derived*
        private bool GetTypeInfo(SemanticModel semanticModel, SyntaxNode root, SyntaxNode? targetNode, CancellationToken cancellationToken,
            out ITypeSymbol? nodeType, out ITypeSymbol? conversionType, out ImmutableArray<ITypeSymbol> potentialConvTypes)
        {
            nodeType = null;
            conversionType = null;
            potentialConvTypes = ImmutableArray<ITypeSymbol>.Empty;
            if (targetNode == null)
            {
                return false;
            }

            var nodeInfo = semanticModel.GetTypeInfo(targetNode, cancellationToken);
            nodeType = nodeInfo.Type;
            conversionType = nodeInfo.ConvertedType;

            var textSpan = targetNode.GetLocation().SourceSpan;
            if (TryGetNode(root, textSpan, SyntaxKind.Argument, targetNode, out var argumentNode) && argumentNode is ArgumentSyntax argument &&
                argument.Parent is ArgumentListSyntax argumentList && argumentList.Parent is SyntaxNode invocationNode)
            {
                // Implicit downcast appears on the arguments of function invocation, get all candidate functions and extract potential conversion types 
                conversionType = null;
                var symbolInfo = semanticModel.GetSymbolInfo(invocationNode, cancellationToken);
                var candidateSymbols = symbolInfo.CandidateSymbols;

                var potentialConversionTypes = new List<ITypeSymbol> { };
                foreach (var candidcateSymbol in candidateSymbols)
                {
                    var methodSymbol = candidcateSymbol as IMethodSymbol;
                    if (methodSymbol == null)
                    {
                        continue;
                    }

                    var parameterList = methodSymbol.Parameters;
                    if (parameterList.Length != argumentList.Arguments.Count)
                    {
                        continue;
                    }

                    // Test if all parameters are convertible, otherwise it is not the perfect match function. For example:
                    // class Base { }
                    // class Derived1 : Base { }
                    // class Derived2 : Base { }
                    // void DoSomething(int i, Derived1 d) { }
                    // void DoSomething(string s, Derived2 d) { }
                    // 
                    // Base b;
                    // DoSomething(1, [||]b);
                    //
                    // *void DoSomething(string s, Derived2 d) { }* is not the perfect match candidate function for
                    // *DoSomething(1, [||]b)* because int and string are not ancestor-descendant relationship. Thus,
                    // Derived2 is not a potential conversion type
                    var allValid = true;
                    for (var i = 0; i < parameterList.Length; i++)
                    {
                        var argType = semanticModel.GetTypeInfo(argumentList.Arguments[i].Expression, cancellationToken);
                        if (argType.Type == null || !semanticModel.Compilation.ClassifyCommonConversion(argType.Type, parameterList[i].Type).Exists)
                        {
                            allValid = false;
                            break;
                        }
                    }

                    if (allValid)
                    {
                        var targetArgumentIndex = argumentList.Arguments.IndexOf(argument);
                        var correspondingParameter = parameterList[targetArgumentIndex];
                        potentialConversionTypes.Add(correspondingParameter.Type);
                    }
                }
                potentialConvTypes = potentialConversionTypes.ToImmutableArray();
                // If there is no exact solution, then don't provide suggestions
                if (potentialConversionTypes.Count != 1)
                {
                    return false;
                }

                conversionType = potentialConversionTypes[0];
            }

            if (nodeType == null || conversionType == null)
            {
                return false;
            }

            var commonConversion = semanticModel.Compilation.ClassifyCommonConversion(nodeType, conversionType);
            if (targetNode.IsKind(SyntaxKind.ObjectCreationExpression) && !commonConversion.IsUserDefined)
            {
                conversionType = null;
                return false;
            }
            return commonConversion.Exists;
        }


        protected SyntaxNode? TryGetTargetNode(SyntaxNode root, TextSpan span)
        {
            var ancestors = root.FindToken(span.Start).GetAncestors<SyntaxNode>();

            var node = ancestors.FirstOrDefault(n => n.Span.Contains(span) && n != root);
            return node;
        }

        protected bool TryGetNode(SyntaxNode root, TextSpan span, SyntaxKind kind, SyntaxNode target, out SyntaxNode? node)
        {
            var ancestors = root.FindToken(span.Start).GetAncestors<SyntaxNode>();

            node = ancestors.FirstOrDefault(n => n.Span.Contains(span) && n != root && n != target && n.IsKind(kind));
            return node != null;
        }

        protected override async Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (root == null)
            {
                return;
            }
            var targetNodes = diagnostics.SelectAsArray(
                d => TryGetTargetNode(root, d.Location.SourceSpan) is ExpressionSyntax node ? node : null);

            await editor.ApplyExpressionLevelSemanticEditsAsync(
                document, targetNodes,
                (semanticModel, targetNode) => true,
                (semanticModel, currentRoot, targetNode) =>
                {
                    if (GetTypeInfo(semanticModel, currentRoot, targetNode, cancellationToken, out var nodeType, out var conversionType, out var potentialConvTypes) &&
                    nodeType != null && conversionType != null && !nodeType.Equals(conversionType) &&
                        targetNode is ExpressionSyntax expression)
                    {
                        var castExpression = expression.Cast(conversionType);

                        // TODO: castExpression.WithAdditionalAnnotations(Simplifier.Annotation) - the simplifier doesn't simplify the 
                        return currentRoot.ReplaceNode(expression, castExpression.WithAdditionalAnnotations(Simplifier.Annotation));
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
