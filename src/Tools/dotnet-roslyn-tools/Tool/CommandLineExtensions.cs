// Licensed to the.NET Foundation under one or more agreements.
// The.NET Foundation licenses this file to you under the MIT license.
// See the License.txt file in the project root for more information.

using System.CommandLine;
using System.CommandLine.Parsing;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.RoslynTools;

internal static class CommandLineExtensions
{
    internal static OptionResult? GetOptionResult(this ParseResult result, string alias)
    {
        return GetOptionResult(result.CommandResult, alias);
    }

    internal static ArgumentResult? GetArgumentResult(this ParseResult result, string alias)
    {
        return GetArgumentResult(result.CommandResult, alias);
    }

    internal static OptionResult? GetOptionResult(this CommandResult result, string alias)
    {
        return result.Children.GetByAlias(alias) as OptionResult;
    }

    internal static ArgumentResult? GetArgumentResult(this CommandResult result, string alias)
    {
        return result.Children.GetByAlias(alias) as ArgumentResult;
    }

    [return: MaybeNull]
    internal static T GetValueForArgument<T>(this ParseResult result, string alias)
    {
        return GetValueForArgument<T>(result.CommandResult, alias);
    }

    [return: MaybeNull]
    internal static T GetValueForArgument<T>(this ParseResult result, Argument<T> argument)
    {
        return GetValueForArgument<T>(result.CommandResult, argument);
    }

    [return: MaybeNull]
    internal static T GetValueForOption<T>(this ParseResult result, string alias)
    {
        return GetValueForOption<T>(result.CommandResult, alias);
    }

    [return: MaybeNull]
    internal static T GetValueForArgument<T>(this CommandResult result, Argument<T> argumentDefinition)
    {
        var arguments = result.Children.Where(x => x.Symbol.Name == argumentDefinition.Name).ToArray();
        if (arguments.Length == 1 &&
            arguments.SingleOrDefault() is ArgumentResult argument &&
            argument.GetValueOrDefault<T>() is T t)
        {
            return t;
        }

        return default;
    }

    [return: MaybeNull]
    internal static T GetValueForArgument<T>(this CommandResult result, string alias)
    {
        if (result.GetArgumentResult(alias) is ArgumentResult argument &&
            argument.GetValueOrDefault<T>() is { } t)
        {
            return t;
        }

        return default;
    }

    [return: MaybeNull]
    internal static T GetValueForOption<T>(this CommandResult result, string alias)
    {
        if (result.GetOptionResult(alias) is OptionResult option &&
            option.GetValueOrDefault<T>() is { } t)
        {
            return t;
        }

        return default;
    }

    internal static bool WasOptionUsed(this ParseResult result, params string[] aliases)
    {
        return result.Tokens
            .Where(token => token.Type == TokenType.Option)
            .Any(token => aliases.Contains(token.Value));
    }
}
