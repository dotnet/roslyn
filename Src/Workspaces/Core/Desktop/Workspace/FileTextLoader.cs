// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    public class FileTextLoader : TextLoader
    {
        private readonly string path;
        private readonly Encoding defaultEncoding;

        /// <summary>
        /// Creates a content loader for specified file.
        /// </summary>
        /// <param name="path">An absolute file path.</param>
        /// <param name="defaultEncoding">
        /// Specifies an encoding to be used if the actual encoding can't be determined from the stream content (the stream doesn't start with Byte Order Mark).
        /// If not specified auto-detect heuristics are used to determine the encoding. If these heuristics fail the decoding is assumed to be <see cref="Encoding.Default"/>.
        /// Note that if the stream starts with Byte Order Mark the value of <paramref name="defaultEncoding"/> is ignored.
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> is not an absolute path.</exception>
        public FileTextLoader(string path, Encoding defaultEncoding)
        {
            FilePathUtilities.RequireAbsolutePath(path, "path");

            this.path = path;
            this.defaultEncoding = defaultEncoding;
        }

        /// <summary>
        /// Absolute path of the file.
        /// </summary>
        public string Path
        {
            get { return path; }
        }

        /// <summary>
        /// Specifies an encoding to be used if the actual encoding of the file 
        /// can't be determined from the stream content (the stream doesn't start with Byte Order Mark).
        /// If <c>null</c> auto-detect heristics are used to determine the encoding. 
        /// If these heuristics fail the decoding is assumed to be <see cref="Encoding.Default"/>.
        /// Note that if the stream starts with Byte Order Mark the value of <see cref="DefaultEncoding"/> is ignored.
        /// </summary>
        public Encoding DefaultEncoding
        {
            get { return defaultEncoding; }
        }

        protected virtual SourceText CreateText(Stream stream, Workspace workspace)
        {
            var factory = workspace.Services.GetService<ITextFactoryService>();
            return factory.CreateText(stream, defaultEncoding);
        }

        /// <exception cref="IOException"></exception>
        public override async Task<TextAndVersion> LoadTextAndVersionAsync(Workspace workspace, DocumentId documentId, CancellationToken cancellationToken)
        {
            DateTime prevLastWriteTime = FileUtilities.GetFileTimeStamp(this.path);

            // Open file for reading with FileShare mode read/write/delete so that we do not lock this file.
            // Allowing other theads/processes to write or delete the file is essential for scenarios such as
            // Rename refactoring where File.Replace API is invoked for updating the modified file. 
            TextAndVersion textAndVersion;
            using (var stream = FileUtilities.OpenAsyncRead(this.path))
            {
                System.Diagnostics.Debug.Assert(stream.IsAsync);
                var version = VersionStamp.Create(prevLastWriteTime);
                var memoryStream = await this.ReadStreamAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);

                var text = CreateText(memoryStream, workspace);
                textAndVersion = TextAndVersion.Create(text, version, path);
            }

            // this has a potential to return corrupted state text if someone changed text in the middle of us reading it.
            // previously, we attempted to detect such case and return empty string with workspace failed event. 
            // but that is nothing better or even worse than returning what we have read so far.
            //
            // I am letting it to return what we have read so far. and hopefully, file change event let us re-read this file.
            // (* but again, there is still a chance where file change event happens even before writing has finished which ends up
            //    let us stay in corrupted state)
            DateTime newLastWriteTime = FileUtilities.GetFileTimeStamp(this.path);
            if (!newLastWriteTime.Equals(prevLastWriteTime))
            {
                // TODO: remove this once we know how often this can happen.
                //       I am leaving this here for now for diagnostic purpose.
                var message = string.Format(WorkspacesResources.FileWasExternallyModified, this.path);
                workspace.OnWorkspaceFailed(new DocumentDiagnostic(WorkspaceDiagnosticKind.Failure, message, documentId));
            }

            return textAndVersion;
        }

        private async Task<MemoryStream> ReadStreamAsync(FileStream stream, CancellationToken cancellationToken)
        {
            Contract.ThrowIfFalse(stream.Position == 0);

            byte[] buffer = new byte[(int)stream.Length];

            await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);

            // publiclyVisible must be true to enable optimizations in Roslyn.Compilers.TextUtilities.DetectEncodingAndDecode
            return new MemoryStream(buffer, index: 0, count: buffer.Length, writable: false, publiclyVisible: true);
        }
    }
}