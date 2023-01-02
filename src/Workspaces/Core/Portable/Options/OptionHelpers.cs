// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeStyle;

namespace Microsoft.CodeAnalysis.Options;

internal static class OptionHelpers
{
    /// <summary>
    /// Returns true if the type of <paramref name="value"/> represents the value of the option internally,
    /// false if the value must be translated to public representation when returned from public API.
    /// Returns true for values whose internal and public representations are the same.
    /// </summary>
    public static bool IsInternalOptionValue(object? value)
        => value is not ICodeStyleOption codeStyle || ReferenceEquals(codeStyle, codeStyle.AsInternalCodeStyleOption());

    public static object? ToPublicOptionValue(object? value)
        => value is ICodeStyleOption codeStyleOption ? codeStyleOption.AsPublicCodeStyleOption() : value;

    public static object? ToInternalOptionValue(object? value)
        => value is ICodeStyleOption codeStyleOption ? codeStyleOption.AsInternalCodeStyleOption() : value;
}
