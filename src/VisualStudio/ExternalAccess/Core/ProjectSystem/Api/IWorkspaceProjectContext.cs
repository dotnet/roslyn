// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.LanguageServices.ExternalAccess.Unified.ProjectSystem.Api;

internal interface IWorkspaceProjectContext : IDisposable, IAsyncDisposable
{
    [Obsolete("Use DisposeAsync instead.")]
    new void Dispose();

    string? ProjectFilePath { get; set; }
    bool LastDesignTimeBuildSucceeded { get; set; }
    string? BinOutputPath { get; set; }
    string DisplayName { set; }
    bool IsPrimary { set; }
    ProjectId Id { get; }

    void SetOptions(ImmutableArray<string> arguments);
    void SetProperty(string name, string value);

    void AddMetadataReference(string referencePath, MetadataReferenceProperties properties);
    void RemoveMetadataReference(string referencePath);
    void AddAnalyzerReference(string referencePath);
    void RemoveAnalyzerReference(string referencePath);

    void AddSourceFile(string filePath, bool isInCurrentContext = true, IEnumerable<string>? folderNames = null, SourceCodeKind sourceCodeKind = SourceCodeKind.Regular);
    void RemoveSourceFile(string filePath);

    void AddAdditionalFile(string filePath, bool isInCurrentContext = true);
    void RemoveAdditionalFile(string filePath);

    void AddAnalyzerConfigFile(string filePath);
    void RemoveAnalyzerConfigFile(string filePath);

    ValueTask<IAsyncDisposable> CreateBatchScopeAsync(CancellationToken cancellationToken);
}
