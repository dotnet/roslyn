// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Emit;

namespace Microsoft.CodeAnalysis.EditAndContinue;

internal sealed class ProjectBaseline(Guid moduleId, ProjectId projectId, EmitBaseline emitBaseline, ImmutableDictionary<string, OneOrMany<AssemblyIdentity>> initiallyReferencedAssemblies, int generation)
{
    public Guid ModuleId { get; } = moduleId;
    public ProjectId ProjectId { get; } = projectId;
    public EmitBaseline EmitBaseline { get; } = emitBaseline;
    public ImmutableDictionary<string, OneOrMany<AssemblyIdentity>> InitiallyReferencedAssemblies { get; } = initiallyReferencedAssemblies;
    public int Generation { get; } = generation;
}
