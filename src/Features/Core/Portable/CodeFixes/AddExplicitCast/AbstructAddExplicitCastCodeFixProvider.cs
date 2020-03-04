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
    internal abstract class AbstractAddExplicitCastCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        /// <summary>
        /// Give a set of least specific types with a limit, and the part exceeding the limit doesn't show any code fix, but logs telemetry 
        /// </summary>
        private const int MaximumConversionOptions = 3;

        internal sealed override CodeFixCategory CodeFixCategory => CodeFixCategory.Compile;

        // First title is for single option, second title is for multiple options
        protected abstract Task<string> GetDescriptionAsync(CodeFixContext context, SemanticModel semanticModel, ITypeSymbol? conversionType = null);

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var document = context.Document;
            var cancellationToken = context.CancellationToken;
            var diagnostic = context.Diagnostics.First();

            var root = await document.GetRequiredSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var targetNode = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true)
                .GetAncestorsOrThis<SyntaxNode>().FirstOrDefault();
            if (targetNode != null)
            {
                var hasSolution = TryGetTargetTypeInfo(semanticModel, targetNode, cancellationToken, out var nodeType, out var potentialConversionTypes);
                if (!hasSolution)
                {
                    return;
                }

                if (potentialConversionTypes.Length == 1)
                {
                    context.RegisterCodeFix(new MyCodeAction(
                        await GetDescriptionAsync(context, semanticModel).ConfigureAwait(false),
                        c => FixAsync(context.Document, context.Diagnostics.First(), c)),
                        context.Diagnostics);
                }
                else
                {
                    var actions = ArrayBuilder<CodeAction>.GetInstance();

                    // MaximumConversionOptions: we show at most [MaximumConversionOptions] options for this code fixer
                    for (var i = 0; i < Math.Min(MaximumConversionOptions, potentialConversionTypes.Length); i++)
                    {
                        var convType = potentialConversionTypes[i];
                        actions.Add(new MyCodeAction(await GetDescriptionAsync(context, semanticModel, conversionType: potentialConversionTypes[i]).ConfigureAwait(false),
                            c => Task.FromResult(context.Document.WithSyntaxRoot(ApplyFixOnRoot(root, targetNode, convType)))));
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
                        await GetDescriptionAsync(context, semanticModel).ConfigureAwait(false),
                        actions.ToImmutableAndFree(), isInlinable: false),
                        context.Diagnostics);
                }
            }
        }

        protected abstract SyntaxNode ApplyFixOnRoot(SyntaxNode currentRoot, SyntaxNode targetNode, ITypeSymbol conversionType);

        protected abstract bool TryGetTargetTypeInfo(SemanticModel semanticModel, SyntaxNode targetNode, CancellationToken cancellationToken,
            [NotNullWhen(true)] out ITypeSymbol? targetNodeType, out ImmutableArray<ITypeSymbol> potentialConversionTypes);

        /// <summary>
        /// Test if all argument types can convert to corresponding parameter types, otherwise they are not the perfect matched.
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
        /// arguments: The arguments of invocation expression
        /// parameters: The parameters of function
        /// targetArgument: The target argument that contains target node
        /// targetParamIndex: Output the corresponding parameter index of the target arugment if function returns true
        /// <returns>
        /// True, if arguments and parameters match perfectly.
        /// False, otherwise.
        /// </returns>
        // TODO: May need an API to replace this function,
        // link: https://github.com/dotnet/roslyn/issues/42149
        //private static bool IsArgumentListAndParameterListPerfectMatch(SemanticModel semanticModel, SeparatedSyntaxList<ArgumentSyntax> arguments,
        //    ImmutableArray<IParameterSymbol> parameters, ArgumentSyntax targetArgument, CancellationToken cancellationToken, out int targetParamIndex)
        //{
        //    targetParamIndex = -1; // return invalid index if it is not a perfect match

        //    var matchedTypes = new bool[parameters.Length]; // default value is false
        //    var paramsMatchedByArray = false; // the parameter with keyword params can be either matched by an array type or a variable number of arguments
        //    var inOrder = true; // assume the arguments are in order in default

        //    for (var i = 0; i < arguments.Count; i++)
        //    {
        //        // Parameter index cannot out of its range, #arguments is larger than #parameter only if the last parameter with keyword params
        //        var parameterIndex = Math.Min(i, parameters.Length - 1);

        //        // If the argument has a name, get the corresponding parameter index
        //        var nameSyntax = arguments[i].NameColon?.Name;
        //        if (nameSyntax != null)
        //        {
        //            var name = nameSyntax.ToString();
        //            var found = false;
        //            for (var j = 0; j < parameters.Length; j++)
        //            {
        //                if (name.Equals(parameters[j].Name))
        //                {
        //                    // Check if the argument is in order with parameters.
        //                    // If the argument breaks the order, the rest arguments of matched functions must have names
        //                    if (i != j)
        //                        inOrder = false;
        //                    parameterIndex = j;
        //                    found = true;
        //                    break;
        //                }
        //            }
        //            if (!found) return false;
        //        }

        //        // The argument is either in order with parameters, or have a matched name with parameters
        //        var argType = semanticModel.GetTypeInfo(arguments[i].Expression, cancellationToken);
        //        if (argType.Type != null && (inOrder || nameSyntax is object))
        //        {
        //            // The type of argument must be convertible to the type of parameter
        //            if (!parameters[parameterIndex].IsParams
        //                && semanticModel.Compilation.ClassifyCommonConversion(argType.Type, parameters[parameterIndex].Type).Exists)
        //            {
        //                if (matchedTypes[parameterIndex]) return false;
        //                matchedTypes[parameterIndex] = true;
        //            }
        //            else if (parameters[parameterIndex].IsParams
        //                && semanticModel.Compilation.ClassifyCommonConversion(argType.Type, parameters[parameterIndex].Type).Exists)
        //            {
        //                // The parameter with keyword params takes an array type, then it cannot be matched more than once
        //                if (matchedTypes[parameterIndex]) return false;
        //                matchedTypes[parameterIndex] = true;
        //                paramsMatchedByArray = true;
        //            }
        //            else if (parameters[parameterIndex].IsParams
        //                     && parameters.Last().Type is IArrayTypeSymbol paramsType
        //                     && semanticModel.Compilation.ClassifyCommonConversion(argType.Type, paramsType.ElementType).Exists)
        //            {
        //                // The parameter with keyword params takes a variable number of arguments, compare its element type with the argument's type.
        //                if (matchedTypes[parameterIndex] && paramsMatchedByArray) return false;
        //                matchedTypes[parameterIndex] = true;
        //                paramsMatchedByArray = false;
        //            }
        //            else return false;

        //            if (targetArgument.Equals(arguments[i])) targetParamIndex = parameterIndex;
        //        }
        //        else return false;
        //    }

        //    // mark all optional parameters as matched
        //    for (var i = 0; i < parameters.Length; i++)
        //    {
        //        if (parameters[i].IsOptional || parameters[i].IsParams)
        //        {
        //            matchedTypes[i] = true;
        //        }
        //    }

        //    return Array.TrueForAll(matchedTypes, (item => item));
        //}
        protected override async Task FixAllAsync(Document document,
                                                  ImmutableArray<Diagnostic> diagnostics,
                                                  SyntaxEditor editor,
                                                  CancellationToken cancellationToken)
        {
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var targetNodes = diagnostics.SelectAsArray(
                d => root.FindNode(d.Location.SourceSpan, getInnermostNodeForTie: true)
                         .GetAncestorsOrThis<SyntaxNode>().FirstOrDefault());

            await editor.ApplyExpressionLevelSemanticEditsAsync(
                document, targetNodes,
                (semanticModel, targetNode) => true,
                (semanticModel, currentRoot, targetNode) =>
                {
                    if (TryGetTargetTypeInfo(semanticModel, targetNode, cancellationToken, out var nodeType, out var potentialConversionTypes)
                        && potentialConversionTypes.Length == 1)
                    {
                        return ApplyFixOnRoot(currentRoot, targetNode, potentialConversionTypes[0]);
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

        protected sealed class InheritanceDistanceComparer : IComparer<ITypeSymbol>
        {
            private readonly ITypeSymbol _baseType;
            private readonly SemanticModel _semanticModel;

            private int GetInheritanceDistance(ITypeSymbol baseType, ITypeSymbol? derivedType)
            {
                if (derivedType == null) return int.MaxValue;
                if (derivedType.Equals(baseType)) return 0;

                var distance = GetInheritanceDistance(baseType, derivedType.BaseType);

                if (derivedType.Interfaces.Length != 0)
                {
                    foreach (var interfaceType in derivedType.Interfaces)
                    {
                        distance = Math.Min(GetInheritanceDistance(baseType, interfaceType), distance);
                    }
                }

                return distance == int.MaxValue ? distance : distance + 1;
            }
            public int Compare(ITypeSymbol x, ITypeSymbol y)
            {
                // if the node has the explicit conversion operator, then it has the shortest distance
                var xComversion = _semanticModel.Compilation.ClassifyCommonConversion(_baseType, x);
                var xDist = xComversion.IsUserDefined || xComversion.IsNumeric ?
                    0 : GetInheritanceDistance(_baseType, x);

                var yComversion = _semanticModel.Compilation.ClassifyCommonConversion(_baseType, y);
                var yDist = yComversion.IsUserDefined || yComversion.IsNumeric ?
                    0 : GetInheritanceDistance(_baseType, y);
                return xDist.CompareTo(yDist);
            }

            public InheritanceDistanceComparer(SemanticModel semanticModel, ITypeSymbol baseType)
            {
                _semanticModel = semanticModel;
                _baseType = baseType;
            }
        }
    }
}
