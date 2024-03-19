// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CodeFixes;

/// <summary>
/// Represents a single fix. This is essentially a tuple
/// that holds on to a <see cref="CodeAction"/> and the set of
/// <see cref="Diagnostic"/>s that this <see cref="CodeAction"/> will fix.
/// </summary>
internal sealed class CodeFix
{
    internal readonly Project Project;
    internal readonly CodeAction Action;
    internal readonly ImmutableArray<Diagnostic> Diagnostics;

    /// <summary>
    /// This is the diagnostic that will show up in the preview pane header when a particular fix
    /// is selected in the light bulb menu. We also group all fixes with the same <see cref="PrimaryDiagnostic"/>
    /// together (into a single SuggestedActionSet) in the light bulb menu.
    /// </summary>
    /// <remarks>
    /// A given fix can fix one or more diagnostics. However, our light bulb UI (preview pane, grouping
    /// of fixes in the light bulb menu etc.) currently keeps things simple and pretends that
    /// each fix fixes a single <see cref="PrimaryDiagnostic"/>.
    /// 
    /// Implementation-wise the <see cref="PrimaryDiagnostic"/> is always the first diagnostic that
    /// the <see cref="CodeFixProvider"/> supplied when registering the fix (<see 
    /// cref="CodeFixContext.RegisterCodeFix(CodeAction, IEnumerable{Diagnostic})"/>). This could change
    /// in the future, if we decide to change the UI to depict the true mapping between fixes and diagnostics
    /// or if we decide to use some other heuristic to determine the <see cref="PrimaryDiagnostic"/>.
    /// </remarks>
    internal Diagnostic PrimaryDiagnostic => Diagnostics[0];

    internal CodeFix(Project project, CodeAction action, Diagnostic diagnostic)
    {
        Project = project;
        Action = action;
        Diagnostics = [diagnostic];
    }

    internal CodeFix(Project project, CodeAction action, ImmutableArray<Diagnostic> diagnostics)
    {
        Debug.Assert(!diagnostics.IsDefaultOrEmpty);
        Project = project;
        Action = action;
        Diagnostics = diagnostics;
    }

    internal DiagnosticData GetPrimaryDiagnosticData()
    {
        var diagnostic = PrimaryDiagnostic;

        if (diagnostic.Location.IsInSource)
        {
            var document = Project.GetDocument(diagnostic.Location.SourceTree);
            if (document != null)
            {
                return DiagnosticData.Create(diagnostic, document);
            }
        }
        else if (diagnostic.Location.Kind == LocationKind.ExternalFile)
        {
            var document = Project.Documents.FirstOrDefault(d => d.FilePath == diagnostic.Location.GetLineSpan().Path);
            if (document != null)
            {
                return DiagnosticData.Create(diagnostic, document);
            }
        }

        return DiagnosticData.Create(Project.Solution, diagnostic, Project);
    }
}
