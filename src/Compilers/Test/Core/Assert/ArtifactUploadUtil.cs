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
        private readonly List<string> _artifactFilePath = new List<string>();
        private bool _success = false;

        public ArtifactUploadUtil(ITestOutputHelper testOutputHelper, string? baseDirectoryName = null)
        {
            _testOutputHelper = testOutputHelper;
            _baseDirectoryName = baseDirectoryName ?? Guid.NewGuid().ToString();
        }

        public void AddArtifact(string filePath)
        {
            _artifactFilePath.Add(filePath);
        }

        public void SetSucceeded()
        {
            _success = true;
        }

        public void Dispose()
        {
            if (!_success)
            {
                var uploadDir = Environment.GetEnvironmentVariable("HELIX_WORKITEM_UPLOAD_ROOT");
                if (string.IsNullOrEmpty(uploadDir))
                {
                    _testOutputHelper.WriteLine("Skipping artifact upload as not running in helix");
                    _testOutputHelper.WriteLine("Artifacts");
                    foreach (var filePath in _artifactFilePath)
                    {
                        _testOutputHelper.WriteLine(filePath);
                    }
                }
                else
                {
                    uploadDir = Path.Combine(uploadDir, _baseDirectoryName);
                    Directory.CreateDirectory(uploadDir);
                    _testOutputHelper.WriteLine($"Uploading artifacts by copying to {uploadDir}");
                    foreach (var filePath in _artifactFilePath)
                    {
                        _testOutputHelper.WriteLine($"Copying {filePath}");
                        var fileName = Path.GetFileName(filePath);
                        File.Copy(filePath, Path.Combine(uploadDir, fileName));
                    }
                }
            }
        }
    }
}
