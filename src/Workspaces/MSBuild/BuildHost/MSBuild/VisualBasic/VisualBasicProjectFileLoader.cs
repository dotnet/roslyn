// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MSB = Microsoft.Build;

namespace Microsoft.CodeAnalysis.MSBuild;

internal sealed partial class VisualBasicProjectFileLoader : ProjectFileLoader
{
    public static readonly VisualBasicProjectFileLoader Instance = new();

    internal VisualBasicProjectFileLoader()
    {
    }

    public override string Language => LanguageNames.VisualBasic;

    protected override ProjectFile CreateProjectFile(MSB.Evaluation.Project? project, ProjectBuildManager buildManager, DiagnosticLog log)
        => new VisualBasicProjectFile(this, project, buildManager, log);
}
