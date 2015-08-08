// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Resolves metadata references specified in source code (#r directives).
    /// </summary>
    internal abstract class MetadataFileReferenceResolver
    {
        internal static readonly RelativePathReferenceResolver Default = new RelativePathReferenceResolver(ImmutableArray<string>.Empty, baseDirectory: null);

        internal static void ValidateSearchPaths(ImmutableArray<string> paths, string argName)
        {
            if (paths.IsDefault)
            {
                throw ExceptionUtilities.Unreachable;
                ////throw new ArgumentNullException(argName);
            }

            if (!paths.All(PathUtilities.IsAbsolute))
            {
                throw ExceptionUtilities.Unreachable;
                ////throw new ArgumentException(CodeAnalysisResources.AbsolutePathExpected, argName);
            }
        }

        /// <summary>
        /// Search paths used when resolving metadata references.
        /// </summary>
        /// <remarks>
        /// All search paths are absolute.
        /// </remarks>
        public abstract ImmutableArray<string> SearchPaths
        {
            get;
        }

        /// <summary>
        /// Directory used for resolution of relative paths.
        /// A full directory path or null if not available.
        /// </summary>
        /// <remarks>
        /// This directory is only used if the base directory isn't implied by the context within which the path is being resolved.
        /// 
        /// It is used, for example, when resolving a strong name key file specified in <see cref="System.Reflection.AssemblyKeyFileAttribute"/>,
        /// or a metadata file path specified in <see cref="PortableExecutableReference.FilePath"/>.
        /// 
        /// Resolution of a relative path that needs the base directory fails if the base directory is null.
        /// </remarks>
        public abstract string BaseDirectory
        {
            get;
        }

        internal abstract MetadataFileReferenceResolver WithSearchPaths(ImmutableArray<string> searchPaths);

        internal abstract MetadataFileReferenceResolver WithBaseDirectory(string baseDirectory);

        /// <summary>
        /// Resolves a metadata reference that is a path or an assembly name.
        /// </summary>
        /// <param name="reference">Reference path.</param>
        /// <param name="baseFilePath">
        /// The base file path to use to resolve relative paths against.
        /// Null to use the <see cref="BaseDirectory"/> as a base for relative paths.
        /// </param>
        /// <returns>
        /// Normalized absolute path to the referenced file or null if it can't be resolved.
        /// </returns>
        public abstract string ResolveReference(string reference, string baseFilePath);
    }
}
