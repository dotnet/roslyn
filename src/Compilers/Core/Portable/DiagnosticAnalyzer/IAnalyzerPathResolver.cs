
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// This interface allows hosts to control where an analyzer is loaded from. It can redirect the path 
    /// originally passed to the compiler to a new path. Or it can take ownership of a path to prevent 
    /// other instances of <see cref="IAnalyzerPathResolver"/> from redirecting it.
    /// </summary>
    /// <remarks>
    /// Instances of this type will be accessed from multiple threads. All method implementations are expected 
    /// to be idempotent.
    /// </remarks>
    internal interface IAnalyzerPathResolver
    {
        /// <summary>
        /// Is this path handled by this instance?
        /// </summary>
        bool IsAnalyzerPathHandled(string analyzerPath);

        /// <summary>
        /// This method is used to allow compiler hosts to intercept an analyzer path and redirect it to a
        /// a different location.
        /// </summary>
        /// <remarks>
        /// This will only be called for paths that return true from <see cref="IsAnalyzerPathHandled(string)"/>.
        /// </remarks>
        string GetResolvedAnalyzerPath(string analyzerPath);

        /// <summary>
        /// This method is used to allow compiler hosts to intercept an analyzer satellite path and redirect it to a
        /// a different location.
        /// </summary>
        /// <remarks>
        /// This will only be called for paths that return true from <see cref="IsAnalyzerPathHandled(string)"/>.
        /// </remarks>
        string? GetResolvedSatellitePath(string analyzerPath, CultureInfo cultureInfo);
    }
}
