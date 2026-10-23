// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Api = Microsoft.VisualStudio.LanguageServices.ExternalAccess.Unified.ProjectSystem.Api;
using Legacy = Microsoft.VisualStudio.LanguageServices.ProjectSystem;

namespace Microsoft.VisualStudio.LanguageServices.ExternalAccess.Unified.ProjectSystem.Internal;

internal sealed class WorkspaceProjectContextAdapter(Legacy.IWorkspaceProjectContext context) : Api.IWorkspaceProjectContext
{
    private Legacy.IWorkspaceProjectContext? _context = context;

    private Legacy.IWorkspaceProjectContext UnderlyingContext
        => Volatile.Read(ref _context) ?? throw new ObjectDisposedException(nameof(WorkspaceProjectContextAdapter));

    public string? ProjectFilePath
    {
        get => UnderlyingContext.ProjectFilePath;
        set => UnderlyingContext.ProjectFilePath = value;
    }

    public bool LastDesignTimeBuildSucceeded
    {
        get => UnderlyingContext.LastDesignTimeBuildSucceeded;
        set => UnderlyingContext.LastDesignTimeBuildSucceeded = value;
    }

    public string? BinOutputPath
    {
        get => UnderlyingContext.BinOutputPath;
        set => UnderlyingContext.BinOutputPath = value;
    }

    public string DisplayName
    {
        set => UnderlyingContext.DisplayName = value;
    }

    public bool IsPrimary
    {
        set => UnderlyingContext.IsPrimary = value;
    }

    public ProjectId Id
        => UnderlyingContext.Id;

    public void SetOptions(ImmutableArray<string> arguments)
        => UnderlyingContext.SetOptions(arguments);

    public void SetProperty(string name, string value)
        => UnderlyingContext.SetProperty(name, value);

    public void AddMetadataReference(string referencePath, MetadataReferenceProperties properties)
        => UnderlyingContext.AddMetadataReference(referencePath, properties);

    public void RemoveMetadataReference(string referencePath)
        => UnderlyingContext.RemoveMetadataReference(referencePath);

    public void AddAnalyzerReference(string referencePath)
        => UnderlyingContext.AddAnalyzerReference(referencePath);

    public void RemoveAnalyzerReference(string referencePath)
        => UnderlyingContext.RemoveAnalyzerReference(referencePath);

    public void AddSourceFile(string filePath, bool isInCurrentContext = true, IEnumerable<string>? folderNames = null, SourceCodeKind sourceCodeKind = SourceCodeKind.Regular)
        => UnderlyingContext.AddSourceFile(filePath, isInCurrentContext, folderNames, sourceCodeKind);

    public void RemoveSourceFile(string filePath)
        => UnderlyingContext.RemoveSourceFile(filePath);

    public void AddAdditionalFile(string filePath, bool isInCurrentContext = true)
    {
#pragma warning disable CS0618 // Call the overload available to the project-system contract.
        UnderlyingContext.AddAdditionalFile(filePath, isInCurrentContext);
#pragma warning restore CS0618
    }

    public void RemoveAdditionalFile(string filePath)
        => UnderlyingContext.RemoveAdditionalFile(filePath);

    public void AddAnalyzerConfigFile(string filePath)
        => UnderlyingContext.AddAnalyzerConfigFile(filePath);

    public void RemoveAnalyzerConfigFile(string filePath)
        => UnderlyingContext.RemoveAnalyzerConfigFile(filePath);

    public ValueTask<IAsyncDisposable> CreateBatchScopeAsync(CancellationToken cancellationToken)
        => UnderlyingContext.CreateBatchScopeAsync(cancellationToken);

    public void Dispose()
    {
        var context = Interlocked.Exchange(ref _context, null);
        if (context is null)
            return;

#pragma warning disable CS0618 // Use the legacy synchronous disposal path only when the caller requests it.
        context.Dispose();
#pragma warning restore CS0618
    }

    public async ValueTask DisposeAsync()
    {
        var context = Interlocked.Exchange(ref _context, null);
        if (context is not null)
            await context.DisposeAsync().ConfigureAwait(false);
    }

    internal static Legacy.IWorkspaceProjectContext GetUnderlying(Api.IWorkspaceProjectContext context)
    {
        if (context is WorkspaceProjectContextAdapter adapter)
            return adapter.UnderlyingContext;

        throw new ArgumentException("The project context was not created by this adapter.", nameof(context));
    }
}
