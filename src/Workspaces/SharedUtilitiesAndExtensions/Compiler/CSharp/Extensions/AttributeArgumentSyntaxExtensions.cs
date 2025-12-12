// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Extensions;

internal static class AttributeArgumentSyntaxExtensions
{
    /// <summary>
    /// Returns the parameter to which this argument is passed. If <paramref name="allowParams"/>
    /// is true, the last parameter will be returned if it is params parameter and the index of
    /// the specified argument is greater than the number of parameters.
    /// </summary>
    /// <remarks>
    /// Returns null if the <paramref name="argument"/> is a named argument.
    /// </remarks>
    public static IParameterSymbol? DetermineParameter(
        this AttributeArgumentSyntax argument,
        SemanticModel semanticModel,
        bool allowUncertainCandidates = false,
        bool allowParams = false,
        CancellationToken cancellationToken = default)
    {
        // if argument is a named argument it can't map to a parameter.
        if (argument.NameEquals != null)
            return null;

        if (argument.Parent is not AttributeArgumentListSyntax argumentList)
            return null;

        if (argumentList.Parent is not AttributeSyntax invocableExpression)
            return null;

        var symbols = semanticModel.GetSymbolInfo(invocableExpression, cancellationToken).GetBestOrAllSymbols();
        if (symbols.Length >= 2 && !allowUncertainCandidates)
            return null;

        foreach (var symbol in symbols)
        {
            var parameters = symbol.GetParameters();

            // Handle named argument
            if (argument.NameColon is { IsMissing: false })
            {
                var name = argument.NameColon.Name.Identifier.ValueText;
                var parameter = parameters.FirstOrDefault(p => p.Name == name);
                if (parameter != null)
                    return parameter;

                continue;
            }

            // Handle positional argument
            var index = argumentList.Arguments.IndexOf(argument);
            if (index < 0)
                continue;

            if (index < parameters.Length)
                return parameters[index];

            if (allowParams)
            {
                var lastParameter = parameters.LastOrDefault();
                if (lastParameter == null)
                    continue;

                if (lastParameter.IsParams)
                    return lastParameter;
            }
        }

        return null;
    }
}
