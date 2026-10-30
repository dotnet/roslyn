// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.LanguageServices.ExternalAccess.Unified.ProjectSystem.Api;

internal interface IWorkspaceProjectContextFactory
{
    Task<IWorkspaceProjectContext> CreateProjectContextAsync(
        Guid id,
        string uniqueName,
        string languageName,
        EvaluationData data,
        object? hostObject,
        CancellationToken cancellationToken);
}

internal abstract class EvaluationData
{
    public abstract string GetPropertyValue(string name);

    public virtual System.Collections.Immutable.ImmutableArray<string> GetItemValues(string name)
        => [];
}
