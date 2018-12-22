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
        protected static (ISymbol, int parameterIndex) GuessCurrentSymbolAndParameter(
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
        /// If an argument name does not correspond to a parameter name, this method is inacceptable. We'll filter it out.
        /// </summary>
        protected static bool IsInacceptable(SeparatedSyntaxList<ArgumentSyntax> arguments, IMethodSymbol method)
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
            SemanticModel semanticModel, ISemanticFactsService semanticFactsService, out int parameterIndex)
        {
            parameterIndex = -1;
            var argumentIndexToSave = GetArgumentIndex(arguments, position);
            var parameters = method.Parameters;
            var parameterCount = parameters.Length;
            var seenParameters = BitVector.Create(parameterCount);
            var currentParameterIndex = 0;
            var seenOutOfPositionArgument = false;
            var inParams = false;
            for (var argumentIndex = 0; argumentIndex < arguments.Count; argumentIndex++)
            {
                if (argumentIndex >= parameterCount && !inParams)
                {
                    return false;
                }

                var argument = arguments[argumentIndex];
                IParameterSymbol parameter;
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

                    currentParameterIndex = namedParameterIndex;
                    seenParameters[namedParameterIndex] = true;
                    parameter = parameters[namedParameterIndex];
                    SaveCurrentParameterIndexIfNeeded(namedParameterIndex, argumentIndex, ref parameterIndex);
                    SetSeenParamsIfNeeded(parameter);

                    if (!seenOutOfPositionArgument)
                    {
                        IncrementParameterIndexIfNeeded();
                    }
                }
                else if (IsEmptyArgument(argument.Expression))
                {
                    if (!seenOutOfPositionArgument)
                    {
                        // We count the empty argument as a used position
                        SetSeenParamsIfNeeded(parameters[currentParameterIndex]);
                        IncrementParameterIndexIfNeeded();
                    }
                    else
                    {
                        currentParameterIndex = -1;
                    }

                    continue;
                }
                else if (seenOutOfPositionArgument)
                {
                    return false;
                }
                else
                {
                    seenParameters[currentParameterIndex] = true;
                    parameter = parameters[currentParameterIndex];
                    SaveCurrentParameterIndexIfNeeded(currentParameterIndex, argumentIndex, ref parameterIndex);
                    SetSeenParamsIfNeeded(parameter);
                    IncrementParameterIndexIfNeeded();
                }

                if (!IsAcceptableArgument(argument, parameter))
                {
                    currentParameterIndex = -1;
                    return false;
                }
            }

            if (seenOutOfPositionArgument && parameterIndex < 0)
            {
                // Find the first unspecified parameter
                for (int i = 0; i < parameterCount; i++)
                {
                    if (!seenParameters[i])
                    {
                        parameterIndex = i;
                        break;
                    }
                }
            }

            Debug.Assert(parameterIndex < parameterCount);
            return true;

            void SetSeenParamsIfNeeded(IParameterSymbol parameter)
            {
                if (parameter.IsParams)
                {
                    inParams = true;
                }
            }

            void SaveCurrentParameterIndexIfNeeded(int value, int argumentIndex, ref int output)
            {
                // if the position is on the current argument, we'll save the parameter index
                if (argumentIndex == argumentIndexToSave)
                {
                    output = value;
                }
            }

            void IncrementParameterIndexIfNeeded()
            {
                if (!inParams && arguments.SeparatorCount > currentParameterIndex)
                {
                    // Increment for `1, $$` but not for `1$$`, and not when we reached a params parameter
                    currentParameterIndex++;
                }
            }

            bool IsEmptyArgument(ExpressionSyntax expression)
            {
                return expression.IsKind(SyntaxKind.IdentifierName) &&
                    ((IdentifierNameSyntax)expression).Identifier.ValueText.Length == 0;
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
