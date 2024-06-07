// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Extensions;

internal static class ArgumentSyntaxExtensions
{
    public static SyntaxTokenList GenerateParameterModifiers(this ArgumentSyntax argument)
    {
        if (argument.RefKindKeyword != default)
        {
            return SyntaxTokenList.Create(SyntaxFactory.Token(argument.RefKindKeyword.Kind()));
        }

        return default;
    }

    public static RefKind GetRefKind(this ArgumentSyntax? argument)
        => argument?.RefKindKeyword.Kind() switch
        {
            SyntaxKind.RefKeyword => RefKind.Ref,
            SyntaxKind.OutKeyword => RefKind.Out,
            SyntaxKind.InKeyword => RefKind.In,
            _ => RefKind.None,
        };

    /// <summary>
    /// Returns the parameter to which this argument is passed. If <paramref name="allowParams"/>
    /// is true, the last parameter will be returned if it is params parameter and the index of
    /// the specified argument is greater than the number of parameters.
    /// </summary>
    public static IParameterSymbol? DetermineParameter(
        this ArgumentSyntax argument,
        SemanticModel semanticModel,
        bool allowUncertainCandidates = false,
        bool allowParams = false,
        CancellationToken cancellationToken = default)
    {
        if (argument.Parent is not BaseArgumentListSyntax argumentList ||
            argumentList.Parent is null)
        {
            return null;
        }

        // Get the symbol as long if it's not null or if there is only one candidate symbol
        var symbolInfo = semanticModel.GetSymbolInfo(argumentList.Parent, cancellationToken);
        var symbols = symbolInfo.GetBestOrAllSymbols();

        if (symbols.Length >= 2 && !allowUncertainCandidates)
            return null;

        foreach (var symbol in symbols)
        {
            var parameters = symbol.GetParameters();

            // Handle named argument
            if (argument.NameColon != null && !argument.NameColon.IsMissing)
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
            {
                var parameter = parameters[index];
                if (argument.RefOrOutKeyword.Kind() == SyntaxKind.OutKeyword &&
                    parameter.RefKind != RefKind.Out)
                {
                    continue;
                }

                if (argument.RefOrOutKeyword.Kind() == SyntaxKind.RefKeyword &&
                    parameter.RefKind != RefKind.Ref)
                {
                    continue;
                }

                return parameter;
            }

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
