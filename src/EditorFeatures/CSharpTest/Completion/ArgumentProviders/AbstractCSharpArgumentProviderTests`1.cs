// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Test.Utilities.Completion;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.Completion.ArgumentProviders
{
    public abstract class AbstractCSharpArgumentProviderTests<TWorkspaceFixture>
        : AbstractArgumentProviderTests<TWorkspaceFixture>
        where TWorkspaceFixture : TestWorkspaceFixture, new()
    {
        protected override IParameterSymbol GetParameterSymbolInfo(SemanticModel semanticModel, SyntaxNode root, int position, CancellationToken cancellationToken)
        {
            var token = root.FindToken(position);
            var argumentList = token.GetRequiredParent().GetAncestorsOrThis<BaseArgumentListSyntax>().First();
            var symbols = semanticModel.GetSymbolInfo(argumentList.GetRequiredParent(), cancellationToken).GetAllSymbols();

            // if more than one symbol is found, filter to only include symbols with a matching number of arguments
            if (symbols.Length > 1)
            {
                symbols = symbols.WhereAsArray(
                    symbol =>
                    {
                        var parameters = symbol.GetParameters();
                        if (argumentList.Arguments.Count < GetMinimumArgumentCount(parameters))
                            return false;

                        if (argumentList.Arguments.Count > GetMaximumArgumentCount(parameters))
                            return false;

                        return true;
                    });
            }

            var symbol = symbols.Single();
            var parameters = symbol.GetParameters();

            Contract.ThrowIfTrue(argumentList.Arguments.Any(argument => argument.NameColon is not null), "Named arguments are not currently supported by this test.");
            Contract.ThrowIfTrue(parameters.Any(parameter => parameter.IsParams), "'params' parameters are not currently supported by this test.");

            var index = argumentList.Arguments.Any()
                ? argumentList.Arguments.IndexOf(argumentList.Arguments.Single(argument => argument.FullSpan.Start <= position && argument.FullSpan.End >= position))
                : 0;

            return parameters[index];
        }

        private static int GetMinimumArgumentCount(ImmutableArray<IParameterSymbol> parameters)
        {
            return parameters.Count(parameter => !parameter.IsOptional && !parameter.IsParams);
        }

        private static int GetMaximumArgumentCount(ImmutableArray<IParameterSymbol> parameters)
        {
            if (parameters.Any(parameter => parameter.IsParams))
                return int.MaxValue;

            return parameters.Length;
        }
    }
}
