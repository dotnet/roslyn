// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        private readonly string _path;
        private readonly Encoding _defaultEncoding;

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

            _path = path;
            _defaultEncoding = defaultEncoding;
        }

        /// <summary>
        /// Absolute path of the file.
        /// </summary>
        public string Path
        {
            get { return _path; }
        }

        /// <summary>
        /// Specifies an encoding to be used if the actual encoding of the file 
        /// can't be determined from the stream content (the stream doesn't start with Byte Order Mark).
        /// If <c>null</c> auto-detect heuristics are used to determine the encoding. 
        /// If these heuristics fail the decoding is assumed to be <see cref="Encoding.Default"/>.
        /// Note that if the stream starts with Byte Order Mark the value of <see cref="DefaultEncoding"/> is ignored.
        /// </summary>
        public Encoding DefaultEncoding
        {
            get { return _defaultEncoding; }
        }

        protected virtual SourceText CreateText(Stream stream, Workspace workspace)
        {
            var factory = workspace.Services.GetService<ITextFactoryService>();
            return factory.CreateText(stream, _defaultEncoding);
        }

        /// <summary>
        /// Load a text and a version of the document in the workspace.
        /// </summary>
        /// <exception cref="IOException"></exception>
        public override async Task<TextAndVersion> LoadTextAndVersionAsync(Workspace workspace, DocumentId documentId, CancellationToken cancellationToken)
        {
            DateTime prevLastWriteTime = FileUtilities.GetFileTimeStamp(_path);

            TextAndVersion textAndVersion;

            // Open file for reading with FileShare mode read/write/delete so that we do not lock this file.
            using (var stream = FileUtilities.RethrowExceptionsAsIOException(() => new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, bufferSize: 4096, useAsync: true)))
            {
                var version = VersionStamp.Create(prevLastWriteTime);

                // we do this so that we asynchronously read from file. and this should allocate less for IDE case. 
                // but probably not for command line case where it doesn't use more sophisticated services.
                using (var readStream = await SerializableBytes.CreateReadableStreamAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false))
                {
                    var text = CreateText(readStream, workspace);
                    textAndVersion = TextAndVersion.Create(text, version, _path);
                }
            }

            // Check if the file was definitely modified and closed while we were reading. In this case, we know the read we got was
            // probably invalid, so throw an IOException which indicates to our caller that we should automatically attempt a re-read.
            // If the file hasn't been closed yet and there's another writer, we will rely on file change notifications to notify us
            // and reload the file.
            DateTime newLastWriteTime = FileUtilities.GetFileTimeStamp(_path);
            if (!newLastWriteTime.Equals(prevLastWriteTime))
            {
                var message = string.Format(WorkspacesResources.FileWasExternallyModified, _path);
                throw new IOException(message);
            }

            return textAndVersion;
        }
    }
}
