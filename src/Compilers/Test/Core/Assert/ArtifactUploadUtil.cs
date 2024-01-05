// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Roslyn.Test.Utilities
{
    /// <summary>
    /// There are test failures that are virtually impossible to debug without artifacts generated during
    /// the test execution. This utility is helpful at getting those artifacts attached to AzDO / Helix 
    /// when executed in our CI process. 
    /// 
    /// The utilility works by collecting a set of file paths to artifacts. If the test succeeds it should 
    /// call the <see cref="SetSucceeded"/> method. Otherwise if the test fails an exception is generated,
    /// the call is skipped and in <see cref="Dispose"/> we prepare the artifacts for upload.
    /// </summary>
    public sealed class ArtifactUploadUtil : IDisposable
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly string _baseDirectoryName;
        private readonly List<string> _filePaths = new List<string>();
        private readonly List<string> _directoryPaths = new List<string>();
        private bool _success = false;

        public ArtifactUploadUtil(ITestOutputHelper testOutputHelper, string? baseDirectoryName = null)
        {
            _testOutputHelper = testOutputHelper;
            _baseDirectoryName = baseDirectoryName ?? Guid.NewGuid().ToString();
        }

        public void AddFile(string filePath)
        {
            _filePaths.Add(filePath);
        }

        public void AddDirectory(string directory)
        {
            _directoryPaths.Add(directory);
        }

        public void SetSucceeded()
        {
            _success = true;
        }

        public void Dispose()
        {
            if (_success)
            {
                return;
            }

            var uploadDir = Environment.GetEnvironmentVariable("HELIX_WORKITEM_UPLOAD_ROOT");
            if (string.IsNullOrEmpty(uploadDir))
            {
                _testOutputHelper.WriteLine("Skipping artifact upload as not running in helix");
                _testOutputHelper.WriteLine("Files:");
                foreach (var filePath in _filePaths)
                {
                    _testOutputHelper.WriteLine(filePath);
                }

                _testOutputHelper.WriteLine("Directories:");
                foreach (var directory in _directoryPaths)
                {
                    _testOutputHelper.WriteLine(directory);
                }
            }
            else
            {
                uploadDir = Path.Combine(uploadDir, _baseDirectoryName);
                Directory.CreateDirectory(uploadDir);
                _testOutputHelper.WriteLine($"Uploading artifacts by copying to {uploadDir}");
                foreach (var filePath in _filePaths)
                {
                    _testOutputHelper.WriteLine($"Copying file {filePath}");
                    var fileName = Path.GetFileName(filePath);
                    File.Copy(filePath, Path.Combine(uploadDir, fileName));
                }

                foreach (var directory in _directoryPaths)
                {
                    _testOutputHelper.WriteLine($"Copying directory {directory}");
                    var destDirectory = Path.Combine(uploadDir, Path.GetFileName(directory));
                    Directory.CreateDirectory(destDirectory);
                    foreach (var filePath in Directory.EnumerateFiles(directory, searchPattern: "*.*", SearchOption.AllDirectories))
                    {
                        _testOutputHelper.WriteLine($"\tCopying file {filePath}");

                        var destFilePath = filePath.Substring(directory.Length);
                        if (destFilePath.Length > 0 && destFilePath[0] == Path.DirectorySeparatorChar)
                        {
                            destFilePath = destFilePath.Substring(1);
                        }

                        destFilePath = Path.Combine(destDirectory, destFilePath);
                        Directory.CreateDirectory(Path.GetDirectoryName(destFilePath)!);
                        File.Copy(filePath, destFilePath);
                    }
                }
            }
        }
    }
}
