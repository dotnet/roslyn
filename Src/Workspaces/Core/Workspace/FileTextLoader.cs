// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
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

        public FileTextLoader(string path)
        {
            this.path = path;
        }

        protected virtual SourceText CreateText(Stream stream, Workspace workspace)
        {
            var factory = workspace.Services.GetService<ITextFactoryService>();
            return factory.CreateText(stream);
        }

        public override async Task<TextAndVersion> LoadTextAndVersionAsync(Workspace workspace, DocumentId documentId, CancellationToken cancellationToken)
        {
            DateTime prevLastWriteTime = File.GetLastWriteTimeUtc(this.path);

            // Open file for reading with FileShare mode read/write/delete so that we do not lock this file.
            // Allowing other theads/processes to write or delete the file is essential for scenarios such as
            // Rename refactoring where File.Replace API is invoked for updating the modified file. 
            TextAndVersion textAndVersion;
            using (var stream = File.Open(this.path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            {
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
            DateTime newLastWriteTime = File.GetLastWriteTimeUtc(this.path);
            if (!newLastWriteTime.Equals(prevLastWriteTime))
            {
                // TODO: remove this once we know how often this can happen.
                //       I am leaving this here for now for diagnostic purpose.
                var message = string.Format(WorkspacesResources.FileWasExternallyModified, this.path);
                workspace.OnWorkspaceFailed(new DocumentDiagnostic(WorkspaceDiagnosticKind.FileAccessFailure, message, documentId));
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