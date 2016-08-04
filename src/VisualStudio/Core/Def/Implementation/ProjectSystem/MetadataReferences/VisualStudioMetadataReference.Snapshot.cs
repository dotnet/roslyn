// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.Implementation.DocumentationComments;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    internal partial class VisualStudioMetadataReference
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
        /// When the VS observes a change in a metadata reference file the <see cref="Project"/> version is advanced and a new instance of 
        /// <see cref="Snapshot"/> is created for the corresponding reference.
        /// </remarks>
        [DebuggerDisplay("{GetDebuggerDisplay(),nq}")]
        internal sealed class Snapshot : PortableExecutableReference
        {
            private readonly VisualStudioMetadataReferenceManager _provider;
            private readonly DateTime _timestamp;
            private Exception _error;

            internal Snapshot(VisualStudioMetadataReferenceManager provider, MetadataReferenceProperties properties, string fullPath)
                : base(properties, fullPath)
            {
                Contract.Requires(Properties.Kind == MetadataImageKind.Assembly);
                _provider = provider;

                try
                {
                    _timestamp = FileUtilities.GetFileTimeStamp(this.FilePath);
                }
                catch (IOException e)
                {
                    // Reading timestamp of a file might fail. 
                    // Let's remember the failure and report it to the compiler when it asks for metadata.
                    _error = e;
                }
            }

            protected override Metadata GetMetadataImpl()
            {
                if (_error != null)
                {
                    throw _error;
                }

                try
                {
                    return _provider.GetMetadata(this.FilePath, _timestamp);
                }
                catch (Exception e) when (SaveMetadataReadingException(e))
                {
                    throw ExceptionUtilities.Unreachable;
                }
            }

            private bool SaveMetadataReadingException(Exception e)
            {
                // Save metadata reading failure so that future compilations created 
                // with this reference snapshot fail consistently in the same way.
                if (e is IOException || e is BadImageFormatException)
                {
                    _error = e;
                }

                return false;
            }

            protected override DocumentationProvider CreateDocumentationProvider()
            {
                return new VisualStudioDocumentationProvider(this.FilePath, _provider.XmlMemberIndexService);
            }

            protected override PortableExecutableReference WithPropertiesImpl(MetadataReferenceProperties properties)
            {
                return new Snapshot(_provider, properties, this.FilePath);
            }

            private string GetDebuggerDisplay()
            {
                return "Metadata File: " + FilePath;
            }
        }
    }
}
