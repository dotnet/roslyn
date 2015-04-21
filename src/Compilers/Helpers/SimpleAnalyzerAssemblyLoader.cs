// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Reflection;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Loads analyzer assemblies from their original locations in the file system.
    /// Assemblies will only be loaded from the locations specified when the loader
    /// is instantiated.
    /// </summary>
    /// <remarks>
    /// This type is meant to be used in scenarios where it is OK for the analyzer
    /// assemblies to be locked on disk for the lifetime of the host; for example,
    /// csc.exe and vbc.exe. In scenarios where support for updating or deleting
    /// the analyzer on disk is required a different loader should be used.
    /// </remarks>
    internal sealed class SimpleAnalyzerAssemblyLoader : AbstractAnalyzerAssemblyLoader
    {
        protected override Assembly LoadCore(string fullPath)
        {
            return Assembly.LoadFrom(fullPath);
        }
    }
}
