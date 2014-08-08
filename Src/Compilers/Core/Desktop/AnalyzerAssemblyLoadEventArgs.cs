// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Reflection;

namespace Microsoft.CodeAnalysis
{
    public class AnalyzerAssemblyLoadEventArgs : EventArgs
    {
        private readonly string path;
        private readonly Assembly loadedAssembly;

        public AnalyzerAssemblyLoadEventArgs(string path, Assembly loadedAssembly)
        {
            this.path = path;
            this.loadedAssembly = loadedAssembly;
        }

        public string Path
        {
            get { return this.path; }
        }

        public Assembly LoadedAssembly
        {
            get { return this.loadedAssembly; }
        }
    }
}