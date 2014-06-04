// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Utilities;
using System;
using System.IO;
using System.Threading;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Reference to metadata stored in the standard ECMA-335 metadata format.
    /// </summary>
    public abstract class PortableExecutableReference : MetadataReference
    {
        private readonly string fullPath;

        private DocumentationProvider lazyDocumentation;

        protected PortableExecutableReference(
            MetadataReferenceProperties properties,
            string fullPath = null,
            DocumentationProvider initialDocumentation = null)
            : base(properties)
        {
            this.fullPath = fullPath;
            this.lazyDocumentation = initialDocumentation;
        }

        /// <summary>
        /// Path or name used in error messages to identity the reference.
        /// </summary>
        public override string Display
        {
            get { return FilePath; }
        }

        /// <summary>
        /// Full path describing the location of the metadata, or null if the metadata have no location.
        /// </summary>
        public string FilePath
        {
            get { return fullPath; }
        }

        /// <summary>
        /// Create documentation provider for the reference.
        /// </summary>
        /// <remarks>
        /// Called when the compiler needs to read the documentation for the reference. 
        /// This method is called at most once per metadata reference and its result is cached on the reference object.
        /// </remarks>
        protected abstract DocumentationProvider CreateDocumentationProvider();

        /// <summary>
        /// XML documentation comments provider for the reference.
        /// </summary>
        internal DocumentationProvider DocumentationProvider
        {
            get
            {
                if (lazyDocumentation == null)
                {
                    Interlocked.CompareExchange(ref lazyDocumentation, CreateDocumentationProvider(), null);
                }

                return lazyDocumentation;
            }
        }

        /// <summary>
        /// Get metadata representation for the PE file.
        /// </summary>
        /// <exception cref="BadImageFormatException">If the PE image format is invalid.</exception>
        /// <exception cref="IOException">The metadata image content can't be read.</exception>
        /// <exception cref="FileNotFoundException">The metadata image is stored in a file that can't be found.</exception>
        /// <remarks>
        /// Called when the <see cref="Compilation"/> needs to read the reference metadata.
        /// 
        /// The listed exceptions are caught and converted to compilation diagnostics.
        /// Any other exception is considered an unexpected error in the implementation and is not caught.
        ///
        /// <see cref="Metadata"/> objects may cache information decoded from the PE image.
        /// Reusing <see cref="Metadata"/> instances accross metadata references will result in better performance.
        /// 
        /// The calling <see cref="Compilation"/> doesn't take ownership of the <see cref="Metadata"/> objects returned by this method.
        /// The implementation needs to retrieve the object from a provider that manages their lifetime (such as metadata cache).
        /// The <see cref="Metadata"/> object is kept alive by the <see cref="Compilation"/> that called <see cref="GetMetadata"/>
        /// and by all compilations created from it via calls to With- factory methods on <see cref="Compilation"/>, 
        /// other than <see cref="M:Compilation.WithReferences"/> overloads. A compilation created using 
        /// <see cref="M:Compilation.WithReferences"/> will call to <see cref="GetMetadata"/> again.
        /// </remarks>
        protected abstract Metadata GetMetadataImpl();

        internal Metadata GetMetadata()
        {
            return GetMetadataImpl();
        }

        internal static Diagnostic ExceptionToDiagnostic(Exception e, CommonMessageProvider messageProvider, Location location, string display, MetadataImageKind kind)
        {
            if (e is BadImageFormatException)
            {
                int errorCode = (kind == MetadataImageKind.Assembly) ? messageProvider.ERR_InvalidAssemblyMetadata : messageProvider.ERR_InvalidModuleMetadata;
                return messageProvider.CreateDiagnostic(errorCode, location, display, e.Message);
            }

            var fileNotFound = e as FileNotFoundException;
            if (fileNotFound != null)
            {
                return messageProvider.CreateDiagnostic(messageProvider.ERR_MetadataFileNotFound, location, fileNotFound.FileName ?? string.Empty);
            }
            else
            {
                int errorCode = (kind == MetadataImageKind.Assembly) ? messageProvider.ERR_ErrorOpeningAssemblyFile : messageProvider.ERR_ErrorOpeningModuleFile;
                return messageProvider.CreateDiagnostic(errorCode, location, display, e.Message);
            }
        }
    }
}
