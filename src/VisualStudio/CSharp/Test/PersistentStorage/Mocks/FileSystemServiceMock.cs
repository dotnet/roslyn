// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Copy of https://devdiv.visualstudio.com/DevDiv/_git/VS.CloudCache?path=%2Ftest%2FMicrosoft.VisualStudio.Cache.Tests%2FMocks&_a=contents&version=GBmain
// Try to keep in sync and avoid unnecessary changes here.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceHub.Framework;
using Microsoft.VisualStudio.RpcContracts.FileSystem;

namespace Microsoft.CodeAnalysis.UnitTests.WorkspaceServices.Mocks
{
    internal class FileSystemServiceMock : IFileSystem
    {
        public event EventHandler<DirectoryEntryChangedEventArgs>? DirectoryEntryChanged;

        public event EventHandler<RootEntriesChangedEventArgs>? RootEntriesChanged;

        public Task<Uri> ConvertLocalFileNameToRemoteUriAsync(string fileName, CancellationToken cancellationToken) => throw new NotImplementedException();

        public Task<Uri> ConvertLocalFileNameToRemoteUriAsync(string fileName, string remoteScheme, CancellationToken cancellationToken) => throw new NotImplementedException();

        public Task<Uri> ConvertLocalUriToRemoteUriAsync(Uri localUri, CancellationToken cancellationToken) => throw new NotImplementedException();

        public Task<Uri> ConvertLocalUriToRemoteUriAsync(Uri localUri, string remoteScheme, CancellationToken cancellationToken) => throw new NotImplementedException();

        public Task<Uri> ConvertRemoteFileNameToRemoteUriAsync(string _1, CancellationToken _2) => throw new NotImplementedException();

        public Task<Uri> ConvertRemoteFileNameToRemoteUriAsync(string _1, string _2, CancellationToken _3) => throw new NotImplementedException();

        public Task<Uri> ConvertRemoteUriToLocalUriAsync(Uri remoteUri, CancellationToken cancellationToken) => Task.FromResult(remoteUri);

        public Task<string> ConvertRemoteUriToRemoteFileNameAsync(Uri _1, CancellationToken _2) => throw new NotImplementedException();

        public Task CopyAsync(Uri sourceUri, Uri destinationUri, bool overwrite, IProgress<OperationProgressData>? progress, CancellationToken cancellationToken) => throw new NotImplementedException();

        public Task CreateDirectoryAsync(Uri uri, CancellationToken cancellationToken) => throw new NotImplementedException();

        public Task DeleteAsync(Uri uri, bool recursive, IProgress<OperationProgressData>? progress, CancellationToken cancellationToken) => throw new NotImplementedException();

        public Task<Uri> DownloadFileAsync(Uri remoteUri, IProgress<OperationProgressData>? progress, CancellationToken cancellationToken) => throw new NotImplementedException();

        public IAsyncEnumerable<Microsoft.VisualStudio.RpcContracts.FileSystem.DirectoryInfo> EnumerateDirectoriesAsync(Uri uri, string searchPattern, SearchOption searchOption, CancellationToken cancellationToken) => throw new NotImplementedException();

        public IAsyncEnumerable<DirectoryEntryInfo> EnumerateDirectoryEntriesAsync(Uri uri, string searchPattern, SearchOption searchOption, CancellationToken cancellationToken) => throw new NotImplementedException();

        public IAsyncEnumerable<Microsoft.VisualStudio.RpcContracts.FileSystem.FileInfo> EnumerateFilesAsync(Uri uri, string searchPattern, SearchOption searchOption, CancellationToken cancellationToken) => throw new NotImplementedException();

        public Task<string> GetDefaultRemoteUriSchemeAsync(CancellationToken cancellationToken) => throw new NotImplementedException();

        public Task<UriDisplayInfo> GetDisplayInfoAsync(Uri uri, CancellationToken cancellationToken) => throw new NotImplementedException();

        public Task<UriDisplayInfo> GetDisplayInfoAsync(string fileName, CancellationToken cancellationToken) => throw new NotImplementedException();

        public Task<DirectoryEntryInfo?> GetInfoAsync(Uri uri, CancellationToken cancellationToken) => throw new NotImplementedException();

        public Task<ServiceMoniker> GetMonikerForFileSystemProviderAsync(string scheme, CancellationToken cancellationToken) => throw new NotImplementedException();

        public Task<ServiceMoniker> GetMonikerForRemoteFileSystemProviderAsync(string scheme, CancellationToken cancellationToken) => throw new NotImplementedException();

        public Task<IReadOnlyList<Uri>> GetRootEntriesAsync(string scheme, CancellationToken cancellationToken) => throw new NotImplementedException();

        public Task<IReadOnlyList<Uri>> GetRootEntriesAsync(CancellationToken cancellationToken) => throw new NotImplementedException();

        public Task<IReadOnlyList<string>> GetSupportedSchemesAsync(CancellationToken cancellationToken) => throw new NotImplementedException();

        public Task MoveAsync(Uri oldUri, Uri newUri, bool overwrite, IProgress<OperationProgressData>? progress, CancellationToken cancellationToken) => throw new NotImplementedException();

        public Task ReadFileAsync(Uri uri, PipeWriter writer, CancellationToken cancellationToken) => throw new NotImplementedException();

        public ValueTask UnwatchAsync(WatchResult watchResult, CancellationToken cancellationToken) => throw new NotImplementedException();

        public ValueTask<WatchResult> WatchDirectoryAsync(Uri uri, bool recursive, CancellationToken cancellationToken) => throw new NotImplementedException();

        public ValueTask<WatchResult> WatchFileAsync(Uri uri, CancellationToken cancellationToken) => throw new NotImplementedException();

        public Task WriteFileAsync(Uri uri, PipeReader reader, bool overwrite, CancellationToken cancellationToken) => throw new NotImplementedException();

        protected virtual void OnDirectoryEntryChanged(DirectoryEntryChangedEventArgs args) => this.DirectoryEntryChanged?.Invoke(this, args);

        protected virtual void OnRootEntriesChanged(RootEntriesChangedEventArgs args) => this.RootEntriesChanged?.Invoke(this, args);
    }
}
