// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Reflection;

namespace Microsoft.CodeAnalysis
{
    public class AnalyzerAssemblyLoadEventArgs : EventArgs
    {
        private readonly string _path;
        private readonly Assembly _loadedAssembly;

        public AnalyzerAssemblyLoadEventArgs(string path, Assembly loadedAssembly)
        {
            _path = path;
            _loadedAssembly = loadedAssembly;
        }

        public string Path
        {
            get { return _path; }
        }

        public Assembly LoadedAssembly
        {
            get { return _loadedAssembly; }
        }
    }
}
