// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Emit;

namespace Microsoft.CodeAnalysis.EditAndContinue;

internal sealed class ProjectBaseline
{
    public ProjectId ProjectId { get; }
    public EmitBaseline EmitBaseline { get; }
    public int Generation { get; }

    public ProjectBaseline(ProjectId projectId, EmitBaseline emitBaseline, int generation)
    {
        ProjectId = projectId;
        EmitBaseline = emitBaseline;
        Generation = generation;
    }
}
