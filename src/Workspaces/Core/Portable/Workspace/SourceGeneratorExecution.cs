// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host;

internal enum SourceGeneratorExecution
{
    Automatic,
    Balanced,
}

internal static class SourceGeneratorExecutionUtilities
{
    private const string automatic = "automatic";
    private const string balanced = "balanced";

    // Default to beginning_of_line if we don't know the value.
    public static string GetEditorConfigString(SourceGeneratorExecution? value)
    {
        return value switch
        {
            SourceGeneratorExecution.Automatic => automatic,
            SourceGeneratorExecution.Balanced => balanced,
            null => "",
            _ => throw ExceptionUtilities.UnexpectedValue(value),
        };
    }

    public static SourceGeneratorExecution? Parse(
        string optionString)
    {
        return optionString switch
        {
            automatic => SourceGeneratorExecution.Automatic,
            balanced => SourceGeneratorExecution.Balanced,
            _ => null,
        };
    }
}
