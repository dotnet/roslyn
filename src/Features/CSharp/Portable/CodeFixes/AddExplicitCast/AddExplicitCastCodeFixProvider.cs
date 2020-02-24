// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageServices;
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
        private const int MaximumConversionOptions = 3;

        [ImportingConstructor]
        public AddExplicitCastCodeFixProvider()
        {
        }

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get => ImmutableArray.Create(CS0266, CS1503);
        }

        internal sealed override CodeFixCategory CodeFixCategory => CodeFixCategory.Compile;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var document = context.Document;
            var cancellationToken = context.CancellationToken;
            var diagnostic = context.Diagnostics.First();

            var root = await document.GetRequiredSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            if (root != null && syntaxFacts != null && semanticModel != null &&
                TryGetTargetNode(root, diagnostic.Location.SourceSpan) is ExpressionSyntax targetNode)
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
                    var actions = ArrayBuilder<CodeAction>.GetInstance();
                    for (var i = 0; i < Math.Min(MaximumConversionOptions, potentialConvTypes.Length); i++)
                    {
                        var convType = potentialConvTypes[i];
                        actions.Add(new MyCodeAction(string.Format(CSharpFeaturesResources.Convert_type_to_0, convType.ToDisplayString()),
                            c => FixIt(context.Document, root, targetNode, convType, c)));
                    }

                    if (potentialConvTypes.Length > 3)
                    {
                        Logger.Log(FunctionId.CodeFixes_AddExplicitCast,
                            KeyValueLogMessage.Create(m =>
                            {
                                m["NumberOfCandidates"] = potentialConvTypes.Length;
                            }));
                    }

                    context.RegisterCodeFix(new CodeAction.CodeActionWithNestedActions(
                        CSharpFeaturesResources.Add_explicit_cast,
                        actions.ToImmutableArray(), false),
                        context.Diagnostics);
                }
            }
        }

        private async Task<Document> FixIt(Document document, SyntaxNode currentRoot, ExpressionSyntax targetNode, ITypeSymbol conversionType, CancellationToken cancellationToken)
        {
            var castExpression = targetNode.Cast(conversionType);
            var newRoot = currentRoot.ReplaceNode(targetNode, castExpression.WithAdditionalAnnotations(Simplifier.Annotation));
            return document.WithSyntaxRoot(newRoot);
        }

        /// <summary>
        /// Output the current type info of the target node and the conversion type(s) that the target node is going to be cast by.
        /// Implicit downcast can appear on Variable Declaration, Return Statement, and Function Invocation, for example:
        /// Base b; Derived d = [||]b;       
        /// object b is the current node with type *Base*, and the conversion type which object b is going to be cast by is *Derived*
        /// </summary>
        /// <param name="semanticModel"></param>
        /// <param name="root">The root of the tree of nodes.</param>
        /// <param name="targetNode">The node to be cast.</param>
        /// <param name="cancellationToken"></param>
        /// <param name="targetNodeType">Output the type of <paramref name="targetNode"/>.</param>
        /// <param name="targetNodeConversionType">Output the exact conversion type that <paramref name="targetNode"/> is going to be cast.</param>
        /// <param name="potentialConvTypes">>Output the potential conversions types that <paramref name="targetNode"/> can be cast</param>
        /// <returns>
        /// True, if the target node has exactly one conversion type, and it is assigned to <paramref name="targetNodeConversionType"/>
        /// False, if the target node has no conversion type or multiple conversion types. Multiple conversion types are assigned to <paramref name="potentialConvTypes"/>
        /// </returns>
        private bool GetTypeInfo(SemanticModel semanticModel, SyntaxNode root, SyntaxNode? targetNode, CancellationToken cancellationToken,
            out ITypeSymbol? targetNodeType, out ITypeSymbol? targetNodeConversionType, out ImmutableArray<ITypeSymbol> potentialConvTypes)
        {
            targetNodeType = null;
            targetNodeConversionType = null;
            potentialConvTypes = ImmutableArray<ITypeSymbol>.Empty;
            if (targetNode == null)
            {
                return false;
            }

            var targetNodeInfo = semanticModel.GetTypeInfo(targetNode, cancellationToken);
            targetNodeType = targetNodeInfo.Type;
            targetNodeConversionType = targetNodeInfo.ConvertedType;
            if (targetNodeType == null)
            {
                return false;
            }

            var textSpan = targetNode.GetLocation().SourceSpan;
            if (TryGetNode(root, textSpan, SyntaxKind.Argument, targetNode, out var argumentNode) && argumentNode is ArgumentSyntax targetArgument &&
                targetArgument.Parent is ArgumentListSyntax argumentList && argumentList.Parent is SyntaxNode invocationNode) // invocation node could be Invocation Expression, Object Creation, Base Constructor...
            {
                // Implicit downcast appears on the argument of invocation node, get all candidate functions and extract potential conversion types 
                targetNodeConversionType = null;
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

                    // Test if all arguments can match all parameters, otherwise it is not the perfect match function. For example:
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
                    if (IsArgumentListAndParameterListPerfactMatch(semanticModel, argumentList.Arguments, methodSymbol.Parameters, targetArgument, cancellationToken, out var paramIndex))
                    {
                        var correspondingParameter = methodSymbol.Parameters[paramIndex];
                        var argumentConversionType = correspondingParameter.Type;

                        if (correspondingParameter.IsParams && correspondingParameter.Type is IArrayTypeSymbol arrayType && !(targetNodeType is IArrayTypeSymbol))
                        {
                            // target argument is matched to the parameter with keyword params
                            argumentConversionType = arrayType.ElementType;
                        }

                        potentialConversionTypes.Add(argumentConversionType);
                    }
                }

                // Sort the potential conversion types by inheritance distance
                var comparer = new InheritanceDistanceComparer(semanticModel, targetNodeType);
                potentialConversionTypes.Sort(comparer);
                potentialConvTypes = potentialConversionTypes.Distinct().ToImmutableArray(); // clear up duplicate types
                if (potentialConvTypes.Length != 1)
                {
                    return false;
                }

                targetNodeConversionType = potentialConvTypes[0];
            }

            if (targetNodeConversionType == null)
            {
                return false;
            }

            var commonConversion = semanticModel.Compilation.ClassifyCommonConversion(targetNodeType, targetNodeConversionType);
            // example: Derived d = [||]new Base();
            // It is invalid except the target node has explicit conversion operator 
            if (targetNode.IsKind(SyntaxKind.ObjectCreationExpression) && !commonConversion.IsUserDefined)
            {
                targetNodeConversionType = null;
                return false;
            }
            return commonConversion.Exists;
        }

        /// <summary>
        /// Try to test if the invocation node is available to invoke the method.
        /// </summary>
        /// <param name="semanticModel"></param>
        /// <param name="arguments">The arguments of invocation</param>
        /// <param name="parameters">The parameters of </param>
        /// <param name="targetArgument">The target argument that contains target node</param>
        /// <param name="cancellationToken"></param>
        /// <param name="targetParamIndex">Output the corresponding parameter index of the target arugment</param>
        /// <returns>
        /// True, if arguments and parameters match perfectly.
        /// False, otherwise.
        /// </returns>
        private bool IsArgumentListAndParameterListPerfactMatch(SemanticModel semanticModel, SeparatedSyntaxList<ArgumentSyntax> arguments,
            ImmutableArray<IParameterSymbol> parameters, ArgumentSyntax targetArgument, CancellationToken cancellationToken, out int targetParamIndex)
        {
            targetParamIndex = -1; // return invalid index if it is not a perfact match

            var matchedTypes = new bool[parameters.Length]; // default value is false
            var inOrder = true; // assume the arguments are in order

            for (var i = 0; i < arguments.Count; i++)
            {
                // Parameter index cannot out of its range, #arguments is larger than #parameter only if the last parameter with keyword params
                var parameterIndex = Math.Min(i, parameters.Length - 1);

                // If the argument has a name, get the corresponding parameter index
                var nameSyntax = arguments[i].NameColon?.Name;
                if (nameSyntax != null)
                {
                    var name = nameSyntax.ToString();
                    var found = false;
                    for (var j = 0; j < parameters.Length; j++)
                    {
                        if (name.Equals(parameters[j].Name))
                        {
                            // Check if the argument is in order with parameters.
                            // If the argument breaks the order, the rest arguments of matched functions must have names
                            if (i != j) inOrder = false;
                            parameterIndex = j;
                            found = true;
                            break;
                        }
                    }
                    if (!found) return false;
                }

                // The argument is either in order with parameters, or have a matched name with parameters
                var argType = semanticModel.GetTypeInfo(arguments[i].Expression, cancellationToken);
                if (argType.Type != null && (inOrder || !(nameSyntax is null)))
                {
                    // The type of argument must be convertible to the type of parameter
                    if (semanticModel.Compilation.ClassifyCommonConversion(argType.Type, parameters[parameterIndex].Type).Exists)
                    {
                        if (matchedTypes[parameterIndex]) return false;
                        matchedTypes[parameterIndex] = true;
                    }
                    else if (parameters[parameterIndex].IsParams && parameters.Last().Type is IArrayTypeSymbol paramsType &&
                      semanticModel.Compilation.ClassifyCommonConversion(argType.Type, paramsType.ElementType).Exists)
                    {
                        // For the parameter with keyword params, compare its element type.
                        matchedTypes[parameterIndex] = true;
                    }
                    else return false;

                    if (targetArgument.Equals(arguments[i])) targetParamIndex = parameterIndex;
                }
                else return false;
            }

            // mark all optional parameters as matched
            for (var i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].IsOptional || parameters[i].IsParams)
                {
                    matchedTypes[i] = true;
                }
            }

            return Array.TrueForAll(matchedTypes, (item => item));
        }

        class InheritanceDistanceComparer : IComparer<ITypeSymbol>
        {
            private ITypeSymbol baseType;
            private SemanticModel semanticModel;

            private int GetInheritanceDistance(ITypeSymbol baseType, ITypeSymbol? derivedType)
            {
                if (derivedType == null) return int.MaxValue;
                if (derivedType.Equals(baseType)) return 0;

                var distance = int.MaxValue;
                distance = Math.Min(GetInheritanceDistance(baseType, derivedType.BaseType), distance);

                if (derivedType.Interfaces.Length != 0)
                {
                    foreach (var interface_type in derivedType.Interfaces)
                    {
                        distance = Math.Min(GetInheritanceDistance(baseType, interface_type), distance);
                    }
                }

                return distance == int.MaxValue ? distance : distance + 1;
            }
            public int Compare(ITypeSymbol x, ITypeSymbol y)
            {
                // if the node has the explicit conversion operator, then it has the shortest distance
                var x_dist = semanticModel.Compilation.ClassifyCommonConversion(baseType, x).IsUserDefined ?
                    0 : GetInheritanceDistance(baseType, x);
                var y_dist = semanticModel.Compilation.ClassifyCommonConversion(baseType, y).IsUserDefined ?
                    0 : GetInheritanceDistance(baseType, y);
                return x_dist.CompareTo(y_dist);
            }

            public InheritanceDistanceComparer(SemanticModel semanticModel, ITypeSymbol baseType)
            {
                this.semanticModel = semanticModel;
                this.baseType = baseType;
            }
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

                        // TODO: castExpression.WithAdditionalAnnotations(Simplifier.Annotation) 
                        // - the Simplifier doesn't remove the redundant cast from the expression
                        // Issue link: https://github.com/dotnet/roslyn/issues/41500
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
