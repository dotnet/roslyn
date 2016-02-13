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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods",
            MessageId = "System.Reflection.Assembly.LoadFrom",
            Justification = @"We need to call Assembly.LoadFrom in order to load analyzer assemblies. 
We can't use Assembly.Load(AssemblyName) because we need to be able to load assemblies outside of the csc/vbc/vbcscompiler/VS binding paths.
We can't use Assembly.Load(byte[]) because VS won't load resource assemblies for those due to an assembly binding optimization.
That leaves Assembly.LoadFrom(string) as the only option that works everywhere.")]
        protected override Assembly LoadCore(string fullPath)
        {
            return Assembly.LoadFrom(fullPath);
        }
    }
}
