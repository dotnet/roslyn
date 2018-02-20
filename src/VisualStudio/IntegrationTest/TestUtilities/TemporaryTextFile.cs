// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities
{
    /// <summary>
    /// Provides methods for supporting temporary files within Roslyn tests.
    /// </summary>
    public class TemporaryTextFile : IDisposable
    {
        private readonly string _fileName;
        private readonly string _content;
        private readonly string _path;

        public TemporaryTextFile(string fileName, string content)
        {
            _fileName = fileName;
            _content = content;
            _path = IntegrationHelper.CreateTemporaryPath();
            FullName = Path.Combine(_path, _fileName);
        }

        public void Create()
        {
            IntegrationHelper.CreateDirectory(_path, deleteExisting: true);

            using (FileStream stream = File.Create(FullName))
            {
            }

            File.WriteAllText(FullName, _content);
        }

        public string FullName { get; }

        public void Dispose()
        {
            IntegrationHelper.DeleteDirectoryRecursively(_path);
        }
    }
}
