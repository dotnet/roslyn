// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Reflection;
using System.Threading;

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
    internal class DesktopAnalyzerAssemblyLoader : AnalyzerAssemblyLoader
    {
        private int _hookedAssemblyResolve;

        protected override Assembly LoadFromPathImpl(string fullPath)
        {
            if (Interlocked.CompareExchange(ref _hookedAssemblyResolve, 0, 1) == 0)
            {
                AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            }

            return LoadImpl(fullPath);
        }

        protected virtual Assembly LoadImpl(string fullPath) => Assembly.LoadFrom(fullPath);

        private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            try
            {
                return Load(AppDomain.CurrentDomain.ApplyPolicy(args.Name));
            }
            catch
            {
                return null;
            }
        }
    }
}
