// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

            using (var stream = File.Create(FullName))
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
