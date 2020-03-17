// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes.AddExplicitCast
{
    internal abstract class AbstractAddExplicitCastCodeFixProvider<
        TExpressionSyntax,
        TArgumentListSyntax,
        TArgumentSyntax> : SyntaxEditorBasedCodeFixProvider
        where TExpressionSyntax : SyntaxNode
        where TArgumentListSyntax : SyntaxNode
        where TArgumentSyntax : SyntaxNode
    {
        /// <summary>
        /// Give a set of least specific types with a limit, and the part exceeding the limit doesn't show any code fix, but logs telemetry 
        /// </summary>
        private const int MaximumConversionOptions = 3;

        internal sealed override CodeFixCategory CodeFixCategory => CodeFixCategory.Compile;

        // First title is for single option, second title is for multiple options
        protected abstract string GetDescription(CodeFixContext context, SemanticModel semanticModel, ITypeSymbol? conversionType = null);
        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var document = context.Document;
            var cancellationToken = context.CancellationToken;
            var diagnostic = context.Diagnostics.First();

            var root = await document.GetRequiredSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var targetNode = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true)
                .GetAncestorsOrThis<TExpressionSyntax>().FirstOrDefault();
            if (targetNode == null)
                return;

            var hasSolution = TryGetTargetTypeInfo(
                semanticModel, root, diagnostic.Id, targetNode, cancellationToken,
                out var nodeType, out var potentialConversionTypes);
            if (!hasSolution)
            {
                return;
            }

            if (potentialConversionTypes.Length == 1)
            {
                context.RegisterCodeFix(new MyCodeAction(
                    GetDescription(context, semanticModel),
                    c => FixAsync(context.Document, context.Diagnostics.First(), c)),
                    context.Diagnostics);
            }
            else
            {
                var actions = ArrayBuilder<CodeAction>.GetInstance();

                // MaximumConversionOptions: we show at most [MaximumConversionOptions] options for this code fixer
                for (var i = 0; i < Math.Min(MaximumConversionOptions, potentialConversionTypes.Length); i++)
                {
                    var conversionType = potentialConversionTypes[i];
                    actions.Add(new MyCodeAction(
                        GetDescription(context, semanticModel, conversionType: conversionType),
                        _ => ApplySingleConversionToDocumentAsync(document, ApplyFix(root, targetNode, conversionType))));
                }

                if (potentialConversionTypes.Length > MaximumConversionOptions)
                {
                    // If the number of potential conversion types is larger than options we could show, report telemetry
                    Logger.Log(FunctionId.CodeFixes_AddExplicitCast,
                        KeyValueLogMessage.Create(m =>
                        {
                            m["NumberOfCandidates"] = potentialConversionTypes.Length;
                        }));
                }

                context.RegisterCodeFix(new CodeAction.CodeActionWithNestedActions(
                    GetDescription(context, semanticModel),
                    actions.ToImmutableAndFree(), isInlinable: false),
                    context.Diagnostics);
            }
        }

        protected abstract SyntaxNode ApplyFix(SyntaxNode currentRoot, TExpressionSyntax targetNode, ITypeSymbol conversionType);

        private static Task<Document> ApplySingleConversionToDocumentAsync(Document document, SyntaxNode currentRoot)
            => Task.FromResult(document.WithSyntaxRoot(currentRoot));

        /// <summary>
        /// Output the current type information of the target node and the conversion type(s) that the target node is going to be cast by.
        /// Implicit downcast can appear on Variable Declaration, Return Statement, and Function Invocation
        /// <para/>
        /// For example:
        /// Base b; Derived d = [||]b;       
        /// "b" is the current node with type "Base", and the potential conversion types list which "b" can be cast by is {Derived}
        /// </summary>
        /// <param name="diagnosticId"> The ID of the diagnostic.</param>
        /// <param name="targetNode"> The node to be cast.</param>
        /// <param name="targetNodeType"> Output the type of "targetNode".</param>
        /// <param name="potentialConversionTypes"> Output the potential conversions types that "targetNode" can be cast to</param>
        /// <returns>
        /// True, if the target node has at least one potential conversion type, and they are assigned to "potentialConversionTypes"
        /// False, if the target node has no conversion type.
        /// </returns>
        protected abstract bool TryGetTargetTypeInfo(
            SemanticModel semanticModel, SyntaxNode root, string diagnosticId, TExpressionSyntax targetNode,
            CancellationToken cancellationToken, [NotNullWhen(true)] out ITypeSymbol? targetNodeType,
            out ImmutableArray<ITypeSymbol> potentialConversionTypes);



        protected abstract bool IsObjectCreationExpression(TExpressionSyntax targetNode);

        protected ImmutableArray<ITypeSymbol> FilterValidPotentialConversionTypes(
            SemanticModel semanticModel, TExpressionSyntax targetNode, ITypeSymbol targetNodeType,
            ArrayBuilder<ITypeSymbol> mutablePotentialConversionTypes)
        {
            using var _ = ArrayBuilder<ITypeSymbol>.GetInstance(out var validPotentialConversionTypes);
            foreach (var targetNodeConversionType in mutablePotentialConversionTypes)
            {
                var commonConversion = semanticModel.Compilation.ClassifyCommonConversion(
                    targetNodeType, targetNodeConversionType);

                // For cases like object creation expression. for example:
                // Derived d = [||]new Base();
                // It is always invalid except the target node has explicit conversion operator or is numeric.
                if (IsObjectCreationExpression(targetNode)
                    && !commonConversion.IsUserDefined)
                {
                    continue;
                }

                if (commonConversion.Exists)
                {
                    validPotentialConversionTypes.Add(targetNodeConversionType);
                }
            }
            return validPotentialConversionTypes.Distinct().ToImmutableArray();
        }

        /// <summary>
        /// Test if all argument types can be converted to corresponding parameter types.
        /// </summary>
        /// For example:
        /// class Base { }
        /// class Derived1 : Base { }
        /// class Derived2 : Base { }
        /// class Derived3 : Base { }
        /// void DoSomething(int i, Derived1 d) { }
        /// void DoSomething(string s, Derived2 d) { }
        /// void DoSomething(int i, Derived3 d) { }
        /// 
        /// Base b;
        /// DoSomething(1, [||]b);
        ///
        /// *void DoSomething(string s, Derived2 d) { }* is not the perfect match candidate function for
        /// *DoSomething(1, [||]b)* because int and string are not ancestor-descendant relationship. Thus,
        /// Derived2 is not a potential conversion type.
        /// 
        /// <param name="argumentList"> The argument list of invocation expression</param>
        /// <param name="parameters"> The parameters of function</param>
        /// <param name="targetArgument"> The target argument that contains target node</param>
        /// <param name="targetArgumentConversionType"> Output the corresponding parameter type of
        /// the target arugment if function returns true</param>
        /// <returns>
        /// True, if arguments and parameters match perfectly. <paramref name="targetArgumentConversionType"/> Output the corresponding parameter type
        /// False, otherwise.
        /// </returns>
        protected abstract bool CanArgumentTypesBeConvertedToParameterTypes(
            SemanticModel semanticModel, SyntaxNode root, TArgumentListSyntax argumentList,
            ImmutableArray<IParameterSymbol> parameters, TArgumentSyntax targetArgument,
            CancellationToken cancellationToken, [NotNullWhen(true)] out ITypeSymbol? targetArgumentConversionType);

        protected ImmutableArray<ITypeSymbol> GetPotentialConversionTypes(
                SemanticModel semanticModel, SyntaxNode root, ITypeSymbol targetNodeType, TArgumentSyntax targetArgument,
                TArgumentListSyntax argumentList, SyntaxNode invocationNode, CancellationToken cancellationToken)
        {
            // Implicit downcast appears on the argument of invocation node,
            // get all candidate functions and extract potential conversion types 
            var symbolInfo = semanticModel.GetSymbolInfo(invocationNode, cancellationToken);
            var candidateSymbols = symbolInfo.CandidateSymbols;

            using var _ = ArrayBuilder<ITypeSymbol>.GetInstance(out var mutablePotentialConversionTypes);
            foreach (var candidateSymbol in candidateSymbols.OfType<IMethodSymbol>())
            {
                if (CanArgumentTypesBeConvertedToParameterTypes(
                        semanticModel, root, argumentList, candidateSymbol.Parameters, targetArgument,
                        cancellationToken, out var targetArgumentConversionType))
                {
                    mutablePotentialConversionTypes.Add(targetArgumentConversionType);
                }
            }

            // Sort the potential conversion types by inheritance distance, so that
            // operations are in order and user can choose least specific types(more accurate)
            mutablePotentialConversionTypes.Sort(new InheritanceDistanceComparer(semanticModel, targetNodeType));

            return mutablePotentialConversionTypes.ToImmutable();
        }

        protected bool FindCorrespondingParameterByName(
            string argumentName, ImmutableArray<IParameterSymbol> parameters, ref int parameterIndex)
        {
            for (var j = 0; j < parameters.Length; j++)
            {
                if (argumentName.Equals(parameters[j].Name))
                {
                    parameterIndex = j;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Check whether the invocation expression with new arguments is applicatble.
        /// </summary>
        /// <param name="oldArgumentList" >old argumentList node</param>
        /// <param name="newArguments"> new arguments that are cast by corresponding parameter types</param>
        /// <param name="targetNode"> The node needs to be cast.</param>
        /// <returns>
        /// Return true if the invocation expression with new arguments is applicatble.
        /// Otherwise, return false
        /// </returns>
        protected abstract bool IsInvocationExpressionWithNewArgumentsApplicable(
            SemanticModel semanticModel, SyntaxNode root, TArgumentListSyntax oldArgumentList,
            List<TArgumentSyntax> newArguments, SyntaxNode targetNode);

        protected override async Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var targetNodes = diagnostics.SelectAsArray(
                d => root.FindNode(d.Location.SourceSpan, getInnermostNodeForTie: true)
                         .GetAncestorsOrThis<TExpressionSyntax>().FirstOrDefault());

            await editor.ApplyExpressionLevelSemanticEditsAsync(
                document, targetNodes,
                (semanticModel, targetNode) => true,
                (semanticModel, currentRoot, targetNode) =>
                {
                    // All diagnostics have the same error code
                    if (TryGetTargetTypeInfo(semanticModel, currentRoot, diagnostics[0].Id, targetNode,
                        cancellationToken, out var nodeType, out var potentialConversionTypes)
                        && potentialConversionTypes.Length == 1)
                    {
                        return ApplyFix(currentRoot, targetNode, potentialConversionTypes[0]);
                    }

                    return currentRoot;
                },
                cancellationToken).ConfigureAwait(false);
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(title, createChangedDocument, equivalenceKey: title)
            {
            }
        }
    }
}
