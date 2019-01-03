// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SignatureHelp;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.SignatureHelp
{
    internal abstract class AbstractCSharpSignatureHelpProvider : AbstractSignatureHelpProvider
    {
        protected AbstractCSharpSignatureHelpProvider()
        {
        }

        protected static SymbolDisplayPart Keyword(SyntaxKind kind)
        {
            return new SymbolDisplayPart(SymbolDisplayPartKind.Keyword, null, SyntaxFacts.GetText(kind));
        }

        protected static SymbolDisplayPart Punctuation(SyntaxKind kind)
        {
            return new SymbolDisplayPart(SymbolDisplayPartKind.Punctuation, null, SyntaxFacts.GetText(kind));
        }

        protected static SymbolDisplayPart Text(string text)
        {
            return new SymbolDisplayPart(SymbolDisplayPartKind.Text, null, text);
        }

        protected static SymbolDisplayPart Space()
        {
            return new SymbolDisplayPart(SymbolDisplayPartKind.Space, null, " ");
        }

        protected static SymbolDisplayPart NewLine()
        {
            return new SymbolDisplayPart(SymbolDisplayPartKind.LineBreak, null, "\r\n");
        }

        private static readonly IList<SymbolDisplayPart> _separatorParts = new List<SymbolDisplayPart>
            {
                Punctuation(SyntaxKind.CommaToken),
                Space()
            };

        protected static IList<SymbolDisplayPart> GetSeparatorParts() => _separatorParts;

        protected static SignatureHelpSymbolParameter Convert(
            IParameterSymbol parameter,
            SemanticModel semanticModel,
            int position,
            IDocumentationCommentFormattingService formatter,
            CancellationToken cancellationToken)
        {
            return new SignatureHelpSymbolParameter(
                parameter.Name,
                parameter.IsOptional,
                parameter.GetDocumentationPartsFactory(semanticModel, position, formatter),
                parameter.ToMinimalDisplayParts(semanticModel, position));
        }

        protected IList<TaggedText> GetAwaitableUsage(IMethodSymbol method, SemanticModel semanticModel, int position)
        {
            if (method.IsAwaitableNonDynamic(semanticModel, position))
            {
                return method.ToAwaitableParts(SyntaxFacts.GetText(SyntaxKind.AwaitKeyword), "x", semanticModel, position)
                             .ToTaggedText();
            }

            return SpecializedCollections.EmptyList<TaggedText>();
        }

        /// <summary>
        /// If the symbol could not be bound, we could be dealing with a partial invocation, we'll try to find a possible overload.
        /// If it was bound, we'll find which parameter to highlight.
        /// Either way, we'll eliminate unsuitable candidates (based on argument names used in the invocation).
        /// </summary>
        protected static (ISymbol symbol, int parameterIndex) GuessCurrentSymbolAndParameter(
            SeparatedSyntaxList<ArgumentSyntax> arguments, ImmutableArray<IMethodSymbol> methodGroup, int position,
            SemanticModel semanticModel, ISemanticFactsService semanticFactsService, CancellationToken cancellationToken)
        {
            if (arguments.Count != 0)
            {
                foreach (var method in methodGroup)
                {
                    if (IsAcceptable(arguments, method, position, semanticModel, semanticFactsService, out var parameterIndex))
                    {
                        return (method, parameterIndex);
                    }
                }
            }

            // Note: Providing no recommendation if no arguments allows the model to keep the last implicit choice
            return (null, -1);
        }

        /// <summary>
        /// If an argument name does not correspond to a parameter name, this method is unacceptable. We'll filter it out.
        /// </summary>
        protected static bool IsUnacceptable(SeparatedSyntaxList<ArgumentSyntax> arguments, IMethodSymbol method)
        {
            if (arguments.Count == 0)
            {
                return false;
            }

            var parameterNames = method.Parameters.SelectAsArray(p => p.Name);
            foreach (var argument in arguments)
            {
                if (HasName(argument, out var name))
                {
                    if (!parameterNames.Contains(name))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        protected static bool IsAcceptable(SeparatedSyntaxList<ArgumentSyntax> arguments, IMethodSymbol method, int position,
            SemanticModel semanticModel, ISemanticFactsService semanticFactsService, out int foundParameterIndex)
        {
            int argumentCount = arguments.Count;
            var parameters = method.Parameters;
            var parameterCount = parameters.Length;

            // map the arguments to their corresponding parameters
            var map = ArrayBuilder<int>.GetInstance(argumentCount, -1);
            if (!PrepareMap())
            {
                foundParameterIndex = -1;
                return false;
            }

            // verify that the arguments are compatible with their corresponding parameters
            for (var argumentIndex = 0; argumentIndex < argumentCount; argumentIndex++)
            {
                var parameterIndex = map[argumentIndex];
                if (parameterIndex < 0)
                {
                    continue;
                }

                var parameter = parameters[parameterIndex];
                var argument = arguments[argumentIndex];

                if (!IsAcceptableArgument(argument, parameter))
                {
                    foundParameterIndex = -1;
                    return false;
                }
            }

            // find the parameter at the cursor position
            var argumentIndexToSave = GetArgumentIndex(arguments, position);
            if (argumentIndexToSave >= 0)
            {
                var found = map[argumentIndexToSave];
                if (found >= 0)
                {
                    foundParameterIndex = found;
                }
                else
                {
                    var firstUnspecified = FirstUnspecifiedParameter();
                    foundParameterIndex = firstUnspecified < 0 ? 0 : firstUnspecified;
                }
            }
            else
            {
                foundParameterIndex = argumentIndexToSave >= 0 ? map[argumentIndexToSave] : -1;
            }

            Debug.Assert(foundParameterIndex < parameterCount);
            map.Free();

            return true;

            bool PrepareMap()
            {
                var currentParameterIndex = 0;
                var seenOutOfPositionArgument = false;
                var inParams = false;

                for (var argumentIndex = 0; argumentIndex < argumentCount; argumentIndex++)
                {
                    if (argumentIndex >= parameterCount && !inParams)
                    {
                        return false;
                    }

                    var argument = arguments[argumentIndex];
                    if (HasName(argument, out var name))
                    {
                        var namedParameterIndex = parameters.IndexOf(p => p.Name == name);
                        if (namedParameterIndex < 0)
                        {
                            return false;
                        }

                        if (namedParameterIndex != currentParameterIndex)
                        {
                            seenOutOfPositionArgument = true;
                        }

                        AddMapping(argumentIndex, namedParameterIndex);
                        IncrementParameterIndexIfNeeded();
                    }
                    else if (IsEmptyArgument(argument.Expression))
                    {
                        if (!seenOutOfPositionArgument)
                        {
                            // We count the empty argument as a used position
                            AddMapping(argumentIndex, currentParameterIndex);
                            IncrementParameterIndexIfNeeded();
                        }
                    }
                    else if (seenOutOfPositionArgument)
                    {
                        // Unnamed arguments are not allowed after an out-of-position argument
                        return false;
                    }
                    else
                    {
                        AddMapping(argumentIndex, currentParameterIndex);
                        IncrementParameterIndexIfNeeded();
                    }
                }

                return true;

                void IncrementParameterIndexIfNeeded()
                {
                    if (!seenOutOfPositionArgument && !inParams)
                    {
                        currentParameterIndex++;
                    }
                }

                void AddMapping(int argumentIndex, int parameterIndex)
                {
                    Debug.Assert(parameterIndex >= 0);
                    Debug.Assert(parameterIndex < parameterCount);

                    if (parameters[parameterIndex].IsParams)
                    {
                        inParams = true;
                    }

                    map[argumentIndex] = parameterIndex;
                }
            }

            bool IsEmptyArgument(ExpressionSyntax expression)
            {
                return expression.IsKind(SyntaxKind.IdentifierName) &&
                    ((IdentifierNameSyntax)expression).Identifier.ValueText.Length == 0;
            }

            int FirstUnspecifiedParameter()
            {
                var specified = ArrayBuilder<bool>.GetInstance(argumentCount, false);
                for (var i = 0; i < argumentCount; i++)
                {
                    var parameterIndex = map[i];
                    if (parameterIndex >= 0)
                    {
                        specified[parameterIndex] = true;
                    }
                }

                var first = specified.FindIndex(s => !s);
                specified.Free();
                return first;
            }

            bool IsAcceptableArgument(ArgumentSyntax argument, IParameterSymbol parameter)
            {
                var parameterRefKind = parameter.RefKind;
                if (parameterRefKind == RefKind.None)
                {
                    if (IsEmptyArgument(argument.Expression))
                    {
                        return true;
                    }

                    var type = parameter.Type;
                    if (parameter.IsParams && semanticFactsService.CanConvert(semanticModel, argument.Expression, ((IArrayTypeSymbol)type).ElementType))
                    {
                        return true;
                    }

                    return semanticFactsService.CanConvert(semanticModel, argument.Expression, type);
                }

                // We don't have an API to check conversion between type symbols, so we just check ref kind
                var argumentRefKind = argument.GetRefKind();
                if (parameterRefKind == RefKind.In && argumentRefKind == RefKind.None)
                {
                    return true;
                }

                if (parameterRefKind == argumentRefKind)
                {
                    return true;
                }

                return true;
            }
        }

        protected static int GetArgumentIndex(SeparatedSyntaxList<ArgumentSyntax> arguments, int position)
        {
            if (arguments.Count == 0)
            {
                return -1;
            }

            if (position < arguments.Span.Start)
            {
                return -1;
            }

            if (position > arguments.Span.End)
            {
                return -1;
            }

            for (int i = 0; i < arguments.Count - 1; i++)
            {
                // `$$,` points to the argument before the separator
                // but `,$$` points to the argument following the separator
                if (position <= arguments.GetSeparator(i).Span.Start)
                {
                    return i;
                }
            }

            return arguments.Count - 1;
        }

        private static bool HasName(ArgumentSyntax argument, out string name)
        {
            var nameColon = argument.NameColon;
            if (nameColon is null)
            {
                name = null;
                return false;
            }
            name = nameColon.Name.Identifier.ValueText;
            return true;
        }
    }
}
