// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using MSB = Microsoft.Build;

namespace Microsoft.CodeAnalysis.MSBuild;

/// <summary>
/// Reads langauge-specific project command line arguments from an MSBuild project instance.
/// </summary>
internal abstract class ProjectCommandLineProvider
{
    public abstract string Language { get; }
    public abstract IEnumerable<MSB.Framework.ITaskItem> GetCompilerCommandLineArgs(MSB.Execution.ProjectInstance executedProject);
    public abstract ImmutableArray<string> ReadCommandLineArgs(MSB.Execution.ProjectInstance project);

    public static ProjectCommandLineProvider? TryCreate(string languageName, ImmutableArray<string> knownCommandLineParserLanguages)
    {
        if (!knownCommandLineParserLanguages.Contains(languageName))
        {
            return null;
        }

        return languageName switch
        {
            LanguageNames.CSharp => CSharpProjectCommandLineProvider.Instance,
            LanguageNames.VisualBasic => VisualBasicProjectCommandLineProvider.Instance,
            _ => null,
        };
    }
}
