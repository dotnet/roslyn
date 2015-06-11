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
            using (var stream = FileUtilities.OpenAsyncRead(_path))
            {
                var version = VersionStamp.Create(prevLastWriteTime);

                Contract.Requires(stream.Position == 0);

                // we do this so that we asynchronously read from file. and this should allocate less for IDE case. 
                // but probably not for command line case where it doesn't use more sophisticated services.
                using (var readStream = await SerializableBytes.CreateReadableStreamAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false))
                {
                    var text = CreateText(readStream, workspace);
                    textAndVersion = TextAndVersion.Create(text, version, _path);
                }
            }

            // this has a potential to return corrupted state text if someone changed text in the middle of us reading it.
            // previously, we attempted to detect such case and return empty string with workspace failed event. 
            // but that is nothing better or even worse than returning what we have read so far.
            //
            // I am letting it to return what we have read so far. and hopefully, file change event let us re-read this file.
            // (* but again, there is still a chance where file change event happens even before writing has finished which ends up
            //    let us stay in corrupted state)
            DateTime newLastWriteTime = FileUtilities.GetFileTimeStamp(_path);
            if (!newLastWriteTime.Equals(prevLastWriteTime))
            {
                // TODO: remove this once we know how often this can happen.
                //       I am leaving this here for now for diagnostic purpose.
                var message = string.Format(WorkspacesResources.FileWasExternallyModified, _path);
                workspace.OnWorkspaceFailed(new DocumentDiagnostic(WorkspaceDiagnosticKind.Failure, message, documentId));
            }

            return textAndVersion;
        }
    }
}
