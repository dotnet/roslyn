// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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

// TODO: This class is now an empty container just to hold onto the nested type. Renaming that is an invasive change that will be it's own commit.
internal static class VisualStudioMetadataReference
{
    /// <summary>
    /// Represents a metadata reference corresponding to a specific version of a file.
    /// If a file changes in future this reference will still refer to the original version.
    /// </summary>
    /// <remarks>
    /// The compiler observes the metadata content a reference refers to by calling <see cref="PortableExecutableReference.GetMetadataImpl()"/>
    /// and the observed metadata is memoized by the compilation. However we drop compilations to decrease memory consumption. 
    /// When the compilation is recreated for a solution the compiler asks for metadata again and we need to provide the original content,
    /// not read the file again. Therefore we need to save the timestamp on the <see cref="Snapshot"/>.
    /// 
    /// When the VS observes a change in a metadata reference file the project version is advanced and a new instance of 
    /// <see cref="Snapshot"/> is created for the corresponding reference.
    /// </remarks>
    [DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
    internal sealed class Snapshot : PortableExecutableReference, ISupportTemporaryStorage
    {
        private readonly VisualStudioMetadataReferenceManager _provider;
        private readonly Lazy<DateTime> _timestamp;
        private Exception _error;
        private readonly FileChangeTracker _fileChangeTrackerOpt;

        internal Snapshot(VisualStudioMetadataReferenceManager provider, MetadataReferenceProperties properties, string fullPath, FileChangeTracker fileChangeTrackerOpt)
            : base(properties, fullPath)
        {
            Debug.Assert(Properties.Kind == MetadataImageKind.Assembly);
            _provider = provider;
            _fileChangeTrackerOpt = fileChangeTrackerOpt;

            _timestamp = new Lazy<DateTime>(() =>
            {
                try
                {
                    _fileChangeTrackerOpt?.EnsureSubscription();

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

        protected override Metadata GetMetadataImpl()
        {
            // Fetch the timestamp first, so as to populate _error if needed
            var timestamp = _timestamp.Value;

            if (_error != null)
            {
                throw _error;
            }

            try
            {
                return _provider.GetMetadata(this.FilePath, timestamp);
            }
            catch (Exception e) when (SaveMetadataReadingException(e))
            {
                throw ExceptionUtilities.Unreachable();
            }
        }

        private bool SaveMetadataReadingException(Exception e)
        {
            // Save metadata reading failure so that future compilations created 
            // with this reference snapshot fail consistently in the same way.
            if (e is IOException or BadImageFormatException)
            {
                _error = e;
            }

            return false;
        }

        protected override DocumentationProvider CreateDocumentationProvider()
            => new VisualStudioDocumentationProvider(this.FilePath, _provider.XmlMemberIndexService);

        protected override PortableExecutableReference WithPropertiesImpl(MetadataReferenceProperties properties)
            => new Snapshot(_provider, properties, this.FilePath, _fileChangeTrackerOpt);

        private string GetDebuggerDisplay()
            => "Metadata File: " + FilePath;

        public IReadOnlyList<ITemporaryStreamStorageInternal> GetStorages()
            => _provider.GetStorages(this.FilePath, _timestamp.Value);
    }
}
