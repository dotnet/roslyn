// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using MSB = Microsoft.Build;

namespace Microsoft.CodeAnalysis.MSBuild;

internal sealed class VisualBasicProjectCommandLineProvider : ProjectCommandLineProvider
{
    public static readonly VisualBasicProjectCommandLineProvider Instance = new();

    public override string Language
        => LanguageNames.VisualBasic;

    public override IEnumerable<MSB.Framework.ITaskItem> GetCompilerCommandLineArgs(MSB.Execution.ProjectInstance executedProject)
        => executedProject.GetItems(ItemNames.VbcCommandLineArgs);

    public override ImmutableArray<string> ReadCommandLineArgs(MSB.Execution.ProjectInstance project)
        => VisualBasicCommandLineArgumentReader.Read(project);
}
