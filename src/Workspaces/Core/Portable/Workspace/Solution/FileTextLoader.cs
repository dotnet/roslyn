// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

[DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
public class FileTextLoader : TextLoader
{
    private static readonly ConditionalWeakTable<Type, StrongBox<bool>> s_isObsoleteCreateTextOverriden = new();

    /// <summary>
    /// Absolute path of the file.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Specifies an encoding to be used if the actual encoding of the file 
    /// can't be determined from the stream content (the stream doesn't start with Byte Order Mark).
    /// If <c>null</c> auto-detect heuristics are used to determine the encoding. 
    /// Note that if the stream starts with Byte Order Mark the value of <see cref="DefaultEncoding"/> is ignored.
    /// </summary>
    public Encoding? DefaultEncoding { get; }

    /// <summary>
    /// Creates a content loader for specified file.
    /// </summary>
    /// <param name="path">An absolute file path.</param>
    /// <param name="defaultEncoding">
    /// Specifies an encoding to be used if the actual encoding can't be determined from the stream content (the stream doesn't start with Byte Order Mark).
    /// If not specified auto-detect heuristics are used to determine the encoding.
    /// Note that if the stream starts with Byte Order Mark the value of <paramref name="defaultEncoding"/> is ignored.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is not an absolute path.</exception>
    public FileTextLoader(string path, Encoding? defaultEncoding)
    {
        CompilerPathUtilities.RequireAbsolutePath(path, "path");

        Path = path;
        DefaultEncoding = defaultEncoding;
    }

    /// <summary>
    /// We have this limit on file size to reduce a chance of OOM when user adds massive files to the solution (often by accident).
    /// The threshold is 100MB which came from some internal data on big files and some discussion.
    /// </summary>
    internal virtual int MaxFileLength => 100 * 1024 * 1024;

    internal sealed override string FilePath => Path;

    internal override bool CanReloadText => !IsObsoleteCreateTextOverridden;

#pragma warning disable CS0618 // Type or member is obsolete
    private bool IsObsoleteCreateTextOverridden
        => s_isObsoleteCreateTextOverriden.GetValue(
            GetType(), _ => new StrongBox<bool>(new Func<Stream, Workspace, SourceText>(CreateText).Method.DeclaringType != typeof(FileTextLoader))).Value;
#pragma warning restore

    /// <summary>
    /// Creates <see cref="SourceText"/> from <see cref="Stream"/>.
    /// </summary>
    /// <param name="stream">Stream.</param>
    /// <param name="workspace">Obsolete. Null.</param>
    protected virtual SourceText CreateText(Stream stream, Workspace? workspace)
        => EncodedStringText.Create(stream, DefaultEncoding, checksumAlgorithm: SourceHashAlgorithms.Default);

    /// <summary>
    /// Creates <see cref="SourceText"/> from <see cref="Stream"/>.
    /// </summary>
#pragma warning disable CS0618 // Type or member is obsolete
    private protected virtual SourceText CreateText(Stream stream, LoadTextOptions options, CancellationToken cancellationToken)
        => IsObsoleteCreateTextOverridden
            ? CreateText(stream, workspace: null)
            : EncodedStringText.Create(stream, DefaultEncoding, checksumAlgorithm: options.ChecksumAlgorithm);
#pragma warning restore

    [Obsolete("Use/override LoadTextAndVersionAsync(LoadTextOptions, CancellationToken)", false)]
    public override Task<TextAndVersion> LoadTextAndVersionAsync(Workspace? workspace, DocumentId? documentId, CancellationToken cancellationToken)
        => base.LoadTextAndVersionAsync(workspace, documentId, cancellationToken);

    private FileStream OpenFile(int bufferSize, bool useAsync)
        => new(this.FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, bufferSize, useAsync);

    /// <summary>
    /// Load a text and a version of the document in the workspace.
    /// </summary>
    /// <exception cref="IOException"></exception>
    /// <exception cref="InvalidDataException"></exception>
    public override async Task<TextAndVersion> LoadTextAndVersionAsync(LoadTextOptions options, CancellationToken cancellationToken)
    {
        FileUtilities.GetFileLengthAndTimeStamp(Path, out var fileLength, out var prevLastWriteTime);

        ValidateFileLength(Path, fileLength);

        // In many .NET Framework versions (specifically the 4.5.* series, but probably much earlier
        // and also later) there is this particularly interesting bit in FileStream.BeginReadAsync:
        //
        //     // [ed: full comment clipped for brevity]
        //     //
        //     // If we did a sync read to fill the buffer, we could avoid the
        //     // problem, and any async read less than 64K gets turned into a
        //     // synchronous read by NT anyways...
        //     if (numBytes < _bufferSize)
        //     {
        //         if (_buffer == null) _buffer = new byte[_bufferSize];
        //         IAsyncResult bufferRead = BeginReadCore(_buffer, 0, _bufferSize, null, null, 0);
        //         _readLen = EndRead(bufferRead);
        //
        // In English, this means that if you do a asynchronous read for smaller than _bufferSize,
        // this is implemented by the framework by starting an asynchronous read, and then
        // blocking your thread until that read is completed. The comment implies this is "fine"
        // because the asynchronous read will actually be synchronous and thus EndRead won't do
        // any blocking -- it'll be an effective no-op. In theory, everything is fine here.
        //
        // In reality, this can end very poorly. That read in fact can be asynchronous, which means the
        // EndRead will enter a wait and block the thread. If we are running that call to ReadAsync on a
        // thread pool thread that completed a previous piece of IO, it means there has to be another
        // thread available to service the completion of that request in order for our thread to make
        // progress. Why is this worse than the claim about the operating system turning an
        // asynchronous read into a synchronous one? If the underlying native ReadFile completes
        // synchronously, that would mean just our thread is being blocked, and will be unblocked once
        // the kernel gets done with our work. In this case, if the OS does do the read asynchronously
        // we are now dependent on another thread being available to unblock us.
        //
        // So how does ths manifest itself? We have seen dumps from customers reporting hangs where
        // we have over a hundred thread pool threads all blocked on EndRead() calls as we read this stream.
        // In these cases, the user had just completed a build that had a bunch of XAML files, and
        // this resulted in many .g.i.cs files being written and updated. As a result, Roslyn is trying to
        // re-read them to provide a new compilation to the XAML language service that is asking for it.
        // Inspecting these dumps and sampling some of the threads made some notable discoveries:
        //
        // 1. When there was a read blocked, it was the _last_ chunk that we were reading in the file in
        //    the file that we were reading. This leads me to believe that it isn't simply very slow IO
        //    (like a network drive), because in that case I'd expect to see some threads in different
        //    places than others.
        // 2. Some stacks were starting by the continuation of a ReadAsync, and some were the first read
        //    of a file from the background parser. In the first case, all of those threads were if the
        //    files were over 4K in size. The ones with the BackgroundParser still on the stack were files
        //    less than 4K in size.
        // 3. The "time unresponsive" in seconds correlated with roughly the number of threads we had
        //    blocked, which makes me think we were impacted by the once-per-second hill climbing algorithm
        //    used by the thread pool.
        //
        // So what's my analysis? When the XAML language service updated all the files, we kicked off
        // background parses for all of them. If the file was over 4K the asynchronous read actually did
        // happen (see point #2), but we'd eventually block the thread pool reading the last chunk.
        // Point #1 confirms that it was always the last chunk. And in small file cases, we'd block on
        // the first chunk. But in either case, we'd be blocking off a thread pool thread until another
        // thread pool thread was available. Since we had enough requests going (over a hundred),
        // sometimes the user got unlucky and all the threads got blocked. At this point, the CLR
        // started slowly kicking off more threads, but each time it'd start a new thread rather than
        // starting work that would be needed to unblock a thread, it just handled an IO that resulted
        // in another file read hitting the end of the file and another thread would get blocked. The
        // CLR then must kick off another thread, rinse, repeat. Eventually it'll make progress once
        // there's no more pending IO requests, everything will complete, and life then continues.
        //
        // To work around this issue, we set bufferSize to 1, which means that all reads should bypass
        // this logic. This is tracked by https://github.com/dotnet/corefx/issues/6007, at least in
        // corefx. We also open the file for reading with FileShare mode read/write/delete so that
        // we do not lock this file.

        var textAndVersion = await FileUtilities.RethrowExceptionsAsIOExceptionAsync(
            async static t =>
            {
                using var stream = t.self.OpenFile(bufferSize: 1, useAsync: true);
                var version = VersionStamp.Create(t.prevLastWriteTime);

                // we do this so that we asynchronously read from file. and this should allocate less for IDE case. 
                // but probably not for command line case where it doesn't use more sophisticated services.
                using var readStream = await SerializableBytes.CreateReadableStreamAsync(stream, cancellationToken: t.cancellationToken).ConfigureAwait(false);
                var text = t.self.CreateText(readStream, t.options, t.cancellationToken);
                return TextAndVersion.Create(text, version, t.self.Path);
            }, (self: this, prevLastWriteTime, options, cancellationToken)).ConfigureAwait(false);

        return CheckForConcurrentFileWrites(prevLastWriteTime, textAndVersion);
    }

    /// <summary>
    /// Load a text and a version of the document.
    /// </summary>
    /// <exception cref="IOException"></exception>
    /// <exception cref="InvalidDataException"></exception>
    internal override TextAndVersion LoadTextAndVersionSynchronously(LoadTextOptions options, CancellationToken cancellationToken)
    {
        FileUtilities.GetFileLengthAndTimeStamp(Path, out var fileLength, out var prevLastWriteTime);

        ValidateFileLength(Path, fileLength);

        var textAndVersion = FileUtilities.RethrowExceptionsAsIOException(
            static t =>
            {
                // Open file for reading with FileShare mode read/write/delete so that we do not lock this file.
                using var stream = t.self.OpenFile(bufferSize: 4096, useAsync: false);
                var version = VersionStamp.Create(t.prevLastWriteTime);
                var text = t.self.CreateText(stream, t.options, t.cancellationToken);
                return TextAndVersion.Create(text, version, t.self.Path);
            }, (self: this, prevLastWriteTime, options, cancellationToken));

        return CheckForConcurrentFileWrites(prevLastWriteTime, textAndVersion);
    }

    private TextAndVersion CheckForConcurrentFileWrites(DateTime prevLastWriteTime, TextAndVersion textAndVersion)
    {
        // Check if the file was definitely modified and closed while we were reading. In this case, we know the
        // read we got was probably invalid, so throw an IOException which indicates to our caller that we should
        // automatically attempt a re-read. If the file hasn't been closed yet and there's another writer, we will
        // rely on file change notifications to notify us and reload the file.
        var newLastWriteTime = FileUtilities.GetFileTimeStamp(Path);
        if (!newLastWriteTime.Equals(prevLastWriteTime))
        {
            var message = string.Format(WorkspacesResources.File_was_externally_modified_colon_0, Path);
            throw new IOException(message);
        }

        return textAndVersion;
    }

    private string GetDebuggerDisplay()
        => nameof(Path) + " = " + Path;

    private void ValidateFileLength(string path, long fileLength)
    {
        // Validate file length is under our threshold. 
        // Otherwise, rather than reading the content into the memory, we will throw
        // InvalidDataException to caller of FileTextLoader.LoadText to deal with 
        // the situation.
        // 
        // check this (http://source.roslyn.io/#Microsoft.CodeAnalysis.Workspaces/Workspace/Solution/TextDocumentState.cs,132)
        // to see how workspace deal with exception from FileTextLoader. other consumer can handle the exception differently
        if (fileLength > MaxFileLength)
        {
            // log max file length which will log to VS telemetry in VS host
            Logger.Log(FunctionId.FileTextLoader_FileLengthThresholdExceeded, KeyValueLogMessage.Create(m =>
            {
                m["FileLength"] = fileLength;
                m["Ext"] = PathUtilities.GetExtension(path);
            }));

            var message = string.Format(WorkspacesResources.File_0_size_of_1_exceeds_maximum_allowed_size_of_2, path, fileLength, MaxFileLength);
            throw new InvalidDataException(message);
        }
    }
}
