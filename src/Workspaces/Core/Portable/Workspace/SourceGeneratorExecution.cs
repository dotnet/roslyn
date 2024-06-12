// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host;

internal enum SourceGeneratorExecutionPreference
{
    /// <summary>
    /// Source generators should re-run after any change to a project.
    /// </summary>
    Automatic,

    /// <summary>
    /// Source generators should re-run only when certain changes happen.  The set of things is host dependent, but
    /// generally should be things like "builds" or "file saves".  Larger events (not just text changes) which indicate
    /// that it's a more reasonable time to run generators.
    /// </summary>
    Manual,
}

internal static class SourceGeneratorExecutionPreferenceUtilities
{
    private const string automatic = "automatic";
    private const string manual = "manual";

    // Default to beginning_of_line if we don't know the value.
    public static string GetEditorConfigString(SourceGeneratorExecutionPreference? value)
    {
        return value switch
        {
            SourceGeneratorExecutionPreference.Automatic => automatic,
            SourceGeneratorExecutionPreference.Manual => manual,
            null => "",
            _ => throw ExceptionUtilities.UnexpectedValue(value),
        };
    }

    public static SourceGeneratorExecutionPreference? Parse(
        string optionString)
    {
        return optionString switch
        {
            automatic => SourceGeneratorExecutionPreference.Automatic,
            manual => SourceGeneratorExecutionPreference.Manual,
            _ => null,
        };
    }
}
