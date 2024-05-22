
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Storage;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host;

internal abstract class AbstractPersistentStorage : IChecksummedPersistentStorage
{
    public string WorkingFolderPath { get; }
    public string SolutionFilePath { get; }

    public string DatabaseFile { get; }
    public string DatabaseDirectory => Path.GetDirectoryName(DatabaseFile) ?? throw ExceptionUtilities.UnexpectedValue(DatabaseFile);

    private bool _isDisabled;

    protected AbstractPersistentStorage(
        string workingFolderPath,
        string solutionFilePath,
        string databaseFile)
    {
        this.WorkingFolderPath = workingFolderPath;
        this.SolutionFilePath = solutionFilePath;
        this.DatabaseFile = databaseFile;

        if (!Directory.Exists(this.DatabaseDirectory))
        {
            Directory.CreateDirectory(this.DatabaseDirectory);
        }
    }

    private bool IsDisabled
        => Volatile.Read(ref _isDisabled);

    protected void DisableStorage()
        => Volatile.Write(ref _isDisabled, true);

    public abstract void Dispose();
    public abstract ValueTask DisposeAsync();

    public abstract Task<bool> ChecksumMatchesAsync(string name, Checksum checksum, CancellationToken cancellationToken);
    public abstract Task<Stream?> ReadStreamAsync(string name, Checksum? checksum, CancellationToken cancellationToken);
    public abstract Task<bool> WriteStreamAsync(string name, Stream stream, Checksum? checksum, CancellationToken cancellationToken);

    protected abstract Task<bool> ChecksumMatchesAsync(ProjectKey projectKey, Project? project, string name, Checksum checksum, CancellationToken cancellationToken);
    protected abstract Task<bool> ChecksumMatchesAsync(DocumentKey documentKey, Document? document, string name, Checksum checksum, CancellationToken cancellationToken);
    protected abstract Task<Stream?> ReadStreamAsync(ProjectKey projectKey, Project? project, string name, Checksum? checksum, CancellationToken cancellationToken);
    protected abstract Task<Stream?> ReadStreamAsync(DocumentKey documentKey, Document? document, string name, Checksum? checksum, CancellationToken cancellationToken);
    protected abstract Task<bool> WriteStreamAsync(ProjectKey projectKey, Project? project, string name, Stream stream, Checksum? checksum, CancellationToken cancellationToken);
    protected abstract Task<bool> WriteStreamAsync(DocumentKey documentKey, Document? document, string name, Stream stream, Checksum? checksum, CancellationToken cancellationToken);

    public Task<bool> ChecksumMatchesAsync(ProjectKey projectKey, string name, Checksum checksum, CancellationToken cancellationToken)
        => IsDisabled ? SpecializedTasks.False : ChecksumMatchesAsync(projectKey, project: null, name, checksum, cancellationToken);

    public Task<bool> ChecksumMatchesAsync(DocumentKey documentKey, string name, Checksum checksum, CancellationToken cancellationToken)
        => IsDisabled ? SpecializedTasks.False : ChecksumMatchesAsync(documentKey, document: null, name, checksum, cancellationToken);

    public Task<Stream?> ReadStreamAsync(ProjectKey projectKey, string name, Checksum? checksum, CancellationToken cancellationToken)
        => IsDisabled ? SpecializedTasks.Null<Stream>() : ReadStreamAsync(projectKey, project: null, name, checksum, cancellationToken);

    public Task<Stream?> ReadStreamAsync(DocumentKey documentKey, string name, Checksum? checksum, CancellationToken cancellationToken)
        => IsDisabled ? SpecializedTasks.Null<Stream>() : ReadStreamAsync(documentKey, document: null, name, checksum, cancellationToken);

    public Task<bool> WriteStreamAsync(ProjectKey projectKey, string name, Stream stream, Checksum? checksum, CancellationToken cancellationToken)
        => IsDisabled ? SpecializedTasks.False : WriteStreamAsync(projectKey, project: null, name, stream, checksum, cancellationToken);

    public Task<bool> WriteStreamAsync(DocumentKey documentKey, string name, Stream stream, Checksum? checksum, CancellationToken cancellationToken)
        => IsDisabled ? SpecializedTasks.False : WriteStreamAsync(documentKey, document: null, name, stream, checksum, cancellationToken);

    public Task<bool> ChecksumMatchesAsync(Project project, string name, Checksum checksum, CancellationToken cancellationToken)
        => IsDisabled ? SpecializedTasks.False : ChecksumMatchesAsync(ProjectKey.ToProjectKey(project), project, name, checksum, cancellationToken);

    public Task<bool> ChecksumMatchesAsync(Document document, string name, Checksum checksum, CancellationToken cancellationToken)
        => IsDisabled ? SpecializedTasks.False : ChecksumMatchesAsync(DocumentKey.ToDocumentKey(document), document, name, checksum, cancellationToken);

    public Task<Stream?> ReadStreamAsync(Project project, string name, Checksum? checksum, CancellationToken cancellationToken)
        => IsDisabled ? SpecializedTasks.Null<Stream>() : ReadStreamAsync(ProjectKey.ToProjectKey(project), project, name, checksum, cancellationToken);

    public Task<Stream?> ReadStreamAsync(Document document, string name, Checksum? checksum, CancellationToken cancellationToken)
        => IsDisabled ? SpecializedTasks.Null<Stream>() : ReadStreamAsync(DocumentKey.ToDocumentKey(document), document, name, checksum, cancellationToken);

    public Task<Stream?> ReadStreamAsync(string name, CancellationToken cancellationToken)
        => IsDisabled ? SpecializedTasks.Null<Stream>() : ReadStreamAsync(name, checksum: null, cancellationToken);

    public Task<Stream?> ReadStreamAsync(Project project, string name, CancellationToken cancellationToken)
        => IsDisabled ? SpecializedTasks.Null<Stream>() : ReadStreamAsync(project, name, checksum: null, cancellationToken);

    public Task<Stream?> ReadStreamAsync(Document document, string name, CancellationToken cancellationToken)
        => IsDisabled ? SpecializedTasks.Null<Stream>() : ReadStreamAsync(document, name, checksum: null, cancellationToken);

    public Task<bool> WriteStreamAsync(Project project, string name, Stream stream, Checksum? checksum, CancellationToken cancellationToken)
        => IsDisabled ? SpecializedTasks.False : WriteStreamAsync(ProjectKey.ToProjectKey(project), project, name, stream, checksum, cancellationToken);

    public Task<bool> WriteStreamAsync(Document document, string name, Stream stream, Checksum? checksum, CancellationToken cancellationToken)
        => IsDisabled ? SpecializedTasks.False : WriteStreamAsync(DocumentKey.ToDocumentKey(document), document, name, stream, checksum, cancellationToken);

    public Task<bool> WriteStreamAsync(string name, Stream stream, CancellationToken cancellationToken)
        => IsDisabled ? SpecializedTasks.False : WriteStreamAsync(name, stream, checksum: null, cancellationToken);

    public Task<bool> WriteStreamAsync(Project project, string name, Stream stream, CancellationToken cancellationToken)
        => IsDisabled ? SpecializedTasks.False : WriteStreamAsync(project, name, stream, checksum: null, cancellationToken);

    public Task<bool> WriteStreamAsync(Document document, string name, Stream stream, CancellationToken cancellationToken)
        => IsDisabled ? SpecializedTasks.False : WriteStreamAsync(document, name, stream, checksum: null, cancellationToken);
}
