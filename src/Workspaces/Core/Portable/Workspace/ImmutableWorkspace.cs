// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

/// <summary>
/// An instance of <see cref="Workspace"/> that does not allow any changes to be made (open documents or update the current solution) and is thus immutable.
/// The purpose of this API is to pass a <see cref="Workspace"/> instance to deprecated APIs that require it but the new code path does not have it.
/// </summary>
internal sealed class ImmutableEmptyWorkspace : Workspace
{
    public ImmutableEmptyWorkspace(HostServices host, string? workspaceKind)
        : base(host, workspaceKind)
    {
    }

    internal override bool CanAddProjectReference(ProjectId referencingProject, ProjectId referencedProject) => false;
    public override bool CanApplyChange(ApplyChangesKind feature) => false;
    protected override bool CanApplyCompilationOptionChange(CompilationOptions oldOptions, CompilationOptions newOptions, Project project) => false;
    public override bool CanApplyParseOptionChange(ParseOptions oldOptions, ParseOptions newOptions, Project project) => false;
    internal override bool CanChangeActiveContextDocument => false;
    public override bool CanOpenDocuments => false;
    internal override bool CanUpdateOptions => false;

    protected override void ClearSolutionData()
    {
        // nop - overridden to prevent base.Dispose from updating the current solution
    }

    public override bool TryApplyChanges(Solution newSolution)
        => throw ExceptionUtilities.Unreachable;

    internal override bool TryApplyChanges(Solution newSolution, IProgressTracker progressTracker)
        => throw ExceptionUtilities.Unreachable;
}
