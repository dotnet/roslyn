// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Serialization;
using Microsoft.VisualStudio.LanguageServices.Implementation.DocumentationComments;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;

internal partial class VisualStudioMetadataReferenceManager
{
    /// <summary>
    /// Represents a metadata reference corresponding to a specific version of a file. If a file changes in future this
    /// reference will still refer to the original version.
    /// </summary>
    /// <remarks>
    /// The compiler observes the metadata content a reference refers to by calling <see
    /// cref="PortableExecutableReference.GetMetadataImpl()"/> and the observed metadata is memoized by the compilation.
    /// <para/> When the VS observes a change in a metadata reference file the project version is advanced and a new
    /// instance of <see cref="VisualStudioPortableExecutableReference"/> is created for the corresponding reference.
    /// </remarks>
    [DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
    private sealed class VisualStudioPortableExecutableReference : PortableExecutableReference, ISupportTemporaryStorage
    {
        private readonly VisualStudioMetadataReferenceManager _provider;
        private readonly Lazy<DateTime> _timestamp;
        private readonly FileChangeTracker? _fileChangeTracker;

        private Exception? _error;

        internal VisualStudioPortableExecutableReference(
            VisualStudioMetadataReferenceManager provider,
            MetadataReferenceProperties properties,
            string fullPath,
            FileChangeTracker? fileChangeTracker)
            : base(properties, fullPath)
        {
            Debug.Assert(Properties.Kind == MetadataImageKind.Assembly);
            _provider = provider;
            _fileChangeTracker = fileChangeTracker;

            _timestamp = new Lazy<DateTime>(() =>
            {
                try
                {
                    _fileChangeTracker?.EnsureSubscription();

                    return FileUtilities.GetFileTimeStamp(this.FilePath);
                }
                catch (IOException e)
                {
                    // Reading timestamp of a file might fail. 
                    // Let's remember the failure and report it to the compiler when it asks for metadata.
                    // We could let the Lazy hold onto this (since it knows how to rethrow exceptions), but
                    // our support of GetStorages needs to gracefully handle the case where we have no timestamp.
                    // If Lazy had a "IsValueFaulted" we could be cleaner here.
                    _error = e;
                    return DateTime.MinValue;
                }
            }, LazyThreadSafetyMode.PublicationOnly);
        }

        private new string FilePath => base.FilePath!;

        protected override Metadata GetMetadataImpl()
        {
            // Fetch the timestamp first, so as to populate _error if needed
            var timestamp = _timestamp.Value;

            if (_error != null)
                throw _error;

            try
            {
                return _provider.GetMetadata(this.FilePath, timestamp);
            }
            catch (Exception e) when (SaveMetadataReadingException(e))
            {
                throw ExceptionUtilities.Unreachable();
            }

            bool SaveMetadataReadingException(Exception e)
            {
                // Save metadata reading failure so that future compilations created 
                // with this reference snapshot fail consistently in the same way.
                if (e is IOException or BadImageFormatException)
                    _error = e;

                return false;
            }
        }

        protected override DocumentationProvider CreateDocumentationProvider()
            => new VisualStudioDocumentationProvider(this.FilePath, _provider._xmlMemberIndexService);

        protected override PortableExecutableReference WithPropertiesImpl(MetadataReferenceProperties properties)
            => new VisualStudioPortableExecutableReference(_provider, properties, this.FilePath, _fileChangeTracker);

        private string GetDebuggerDisplay()
            => "Metadata File: " + FilePath;

        public IReadOnlyList<ITemporaryStorageStreamHandle>? StorageHandles
            => _provider.GetStorageHandles(this.FilePath, _timestamp.Value);
    }
}
