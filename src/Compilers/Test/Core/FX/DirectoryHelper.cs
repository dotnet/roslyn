// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    public class DirectoryHelper
    {
        public event Action<string> FileFound;

        private readonly string _rootPath;
        public DirectoryHelper(string path)
        {
            if (!Directory.Exists(path))
            {
                throw new ArgumentException("Directory '" + path + "' does not exist.", nameof(path));
            }

            _rootPath = path;
        }

        public void IterateFiles(string[] searchPatterns)
        {
            IterateFiles(searchPatterns, _rootPath);
        }

        private void IterateFiles(string[] searchPatterns, string directoryPath)
        {
            var files = new List<string>();
            foreach (var pattern in searchPatterns)
            {
                files.AddRange(Directory.GetFiles(directoryPath, pattern, SearchOption.TopDirectoryOnly));
            }

            foreach (var f in files)
            {
                FileFound(f);
            }

            foreach (var d in Directory.GetDirectories(directoryPath))
            {
                IterateFiles(searchPatterns, d);
            }
        }
    }
}
