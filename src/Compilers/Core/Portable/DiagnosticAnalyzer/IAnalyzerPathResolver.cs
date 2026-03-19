
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
    /// This interface gives the host the ability to control the actual path used to load an analyzer into the 
    /// compiler.
    ///
    /// Instances of these types are considered in the order they are added to the <see cref="AnalyzerAssemblyLoader"/>.
    /// The first instance to return true from <see cref="IsAnalyzerPathHandled(string)"/> will be considered to 
    /// be the owner of that path. From then on only that instance will be called for the other methods on this
    /// interface.
    /// 
    /// For example in a typical session: the <see cref="ProgramFilesAnalyzerPathResolver"/> will return true for 
    /// analyzer paths under C:\Program Files\dotnet. That means the <see cref="ShadowCopyAnalyzerPathResolver"/>,
    /// which appears last on Windows, will never see these paths and hence won't shadow copy them.
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
        string GetResolvedAnalyzerPath(string originalAnalyzerPath);

        /// <summary>
        /// This method is used to allow compiler hosts to intercept an analyzer satellite path and redirect it to a
        /// a different location. A null return here means there is no available satellite assembly for that 
        /// culture.
        /// </summary>
        /// <remarks>
        /// This will only be called for paths that return true from <see cref="IsAnalyzerPathHandled(string)"/>.
        /// </remarks>
        string? GetResolvedSatellitePath(string originalAnalyzerPath, CultureInfo cultureInfo);
    }
}
