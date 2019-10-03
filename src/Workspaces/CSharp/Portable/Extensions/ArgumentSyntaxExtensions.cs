// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal static class ArgumentSyntaxExtensions
    {
        public static SyntaxTokenList GenerateParameterModifiers(this ArgumentSyntax argument)
        {
            if (argument.RefKindKeyword != default)
            {
                return SyntaxFactory.TokenList(SyntaxFactory.Token(argument.RefKindKeyword.Kind()));
            }

            return default;
        }

        public static RefKind GetRefKind(this ArgumentSyntax argument)
        {
            switch (argument?.RefKindKeyword.Kind())
            {
                case SyntaxKind.RefKeyword:
                    return RefKind.Ref;
                case SyntaxKind.OutKeyword:
                    return RefKind.Out;
                case SyntaxKind.InKeyword:
                    return RefKind.In;
                default:
                    return RefKind.None;
            }
        }

        /// <summary>
        /// Returns the parameter to which this argument is passed. If <paramref name="allowParams"/>
        /// is true, the last parameter will be returned if it is params parameter and the index of
        /// the specified argument is greater than the number of parameters.
        /// </summary>
        public static IParameterSymbol DetermineParameter(
            this ArgumentSyntax argument,
            SemanticModel semanticModel,
            bool allowParams = false,
            CancellationToken cancellationToken = default)
        {
            if (!(argument.Parent is BaseArgumentListSyntax argumentList))
            {
                return null;
            }

            if (!(argumentList.Parent is ExpressionSyntax invocableExpression))
            {
                return null;
            }

            var symbol = semanticModel.GetSymbolInfo(invocableExpression, cancellationToken).Symbol;
            if (symbol == null)
            {
                return null;
            }

            var parameters = symbol.GetParameters();

            // Handle named argument
            if (argument.NameColon != null && !argument.NameColon.IsMissing)
            {
                var name = argument.NameColon.Name.Identifier.ValueText;
                return parameters.FirstOrDefault(p => p.Name == name);
            }

            // Handle positional argument
            var index = argumentList.Arguments.IndexOf(argument);
            if (index < 0)
            {
                return null;
            }

            if (index < parameters.Length)
            {
                return parameters[index];
            }

            if (allowParams)
            {
                var lastParameter = parameters.LastOrDefault();
                if (lastParameter == null)
                {
                    return null;
                }

                if (lastParameter.IsParams)
                {
                    return lastParameter;
                }
            }

            return null;
        }
    }
}
