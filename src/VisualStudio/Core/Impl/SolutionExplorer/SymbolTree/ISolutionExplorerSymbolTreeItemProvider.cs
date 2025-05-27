// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer;

internal interface ISolutionExplorerSymbolTreeItemProvider : ILanguageService
{
    ImmutableArray<SymbolTreeItemData> GetItems(SyntaxNode declarationNode, CancellationToken cancellationToken);
}

internal abstract class AbstractSolutionExplorerSymbolTreeItemProvider : ISolutionExplorerSymbolTreeItemProvider
{
    public abstract ImmutableArray<SymbolTreeItemData> GetItems(SyntaxNode declarationNode, CancellationToken cancellationToken);

    protected static void AppendCommaSeparatedList<TArgumentList, TArgument>(
        StringBuilder builder,
        string openBrace,
        string closeBrace,
        TArgumentList? argumentList,
        Func<TArgumentList, IEnumerable<TArgument>> getArguments,
        Action<TArgument, StringBuilder> append,
        string separator = ", ")
        where TArgumentList : SyntaxNode
        where TArgument : SyntaxNode
    {
        if (argumentList is null)
            return;

        AppendCommaSeparatedList(builder, openBrace, closeBrace, getArguments(argumentList), append, separator);
    }

    protected static void AppendCommaSeparatedList<TArgument>(
        StringBuilder builder,
        string openBrace,
        string closeBrace,
        IEnumerable<TArgument> arguments,
        Action<TArgument, StringBuilder> append,
        string separator = ", ")
        where TArgument : SyntaxNode
    {
        builder.Append(openBrace);
        builder.AppendJoinedValues(separator, arguments, append);
        builder.Append(closeBrace);
    }
}
