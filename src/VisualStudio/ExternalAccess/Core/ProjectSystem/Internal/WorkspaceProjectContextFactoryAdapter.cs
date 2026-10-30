// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Api = Microsoft.VisualStudio.LanguageServices.ExternalAccess.Unified.ProjectSystem.Api;
using Legacy = Microsoft.VisualStudio.LanguageServices.ProjectSystem;

namespace Microsoft.VisualStudio.LanguageServices.ExternalAccess.Unified.ProjectSystem.Internal;

[Export(typeof(Api.IWorkspaceProjectContextFactory)), Shared]
internal sealed class WorkspaceProjectContextFactoryAdapter : Api.IWorkspaceProjectContextFactory
{
    private readonly Legacy.IWorkspaceProjectContextFactory _factory;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public WorkspaceProjectContextFactoryAdapter(Legacy.IWorkspaceProjectContextFactory factory)
        => _factory = factory;

    public async Task<Api.IWorkspaceProjectContext> CreateProjectContextAsync(
        Guid id,
        string uniqueName,
        string languageName,
        Api.EvaluationData data,
        object? hostObject,
        CancellationToken cancellationToken)
    {
        var context = await _factory.CreateProjectContextAsync(
            id,
            uniqueName,
            languageName,
            new EvaluationDataAdapter(data),
            hostObject,
            cancellationToken).ConfigureAwait(false);

        return new WorkspaceProjectContextAdapter(context);
    }

    private sealed class EvaluationDataAdapter(Api.EvaluationData data) : Legacy.EvaluationData
    {
        public override string GetPropertyValue(string name)
            => data.GetPropertyValue(name);

        public override ImmutableArray<string> GetItemValues(string name)
            => data.GetItemValues(name);
    }
}
