// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable
#pragma warning disable RS0041 // uses oblivious reference types

using System;
using System.Reflection;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Handles loading analyzer assemblies and their dependencies.
    /// 
    /// Before an analyzer assembly is loaded with <see cref="LoadFromPath(string)"/>,
    /// its location and the location of all of its dependencies must first be specified
    /// by calls to <see cref="AddDependencyLocation(string)"/>.
    /// </summary>
    /// <remarks>
    /// To the extent possible, implementations should remain consistent in the face
    /// of exceptions and allow the caller to handle them. This allows the caller to
    /// decide how to surface issues to the user and whether or not they are fatal. For
    /// example, if asked to load an a non-existent or inaccessible file a command line
    /// tool may wish to exit immediately, while an IDE may wish to keep going and give
    /// the user a chance to correct the issue.
    /// </remarks>
    public interface IAnalyzerAssemblyLoader
    {
        /// <summary>
        /// Given the full path to an assembly on disk, loads and returns the
        /// corresponding <see cref="Assembly"/> object.
        /// </summary>
        /// <remarks>
        /// Multiple calls with the same path should return the same 
        /// <see cref="Assembly"/> instance.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="fullPath" /> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="fullPath" /> is not a full path.</exception>
        Assembly LoadFromPath(string fullPath);

        /// <summary>
        /// Adds a file to consider when loading an analyzer or its dependencies.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="fullPath" /> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="fullPath" /> is not a full path.</exception>
        void AddDependencyLocation(string fullPath);
    }
}
