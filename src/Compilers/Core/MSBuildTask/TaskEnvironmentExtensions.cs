// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Build.Framework;
using Microsoft.CodeAnalysis.BuildTasks;
using Roslyn.Utilities;
namespace Microsoft.CodeAnalysis.CommandLine;

internal static class TaskEnvironmentExtensions
{
    extension(TaskEnvironment taskEnvironment)
    {
        public TaskBuildEnvironment BuildEnvironment => new TaskBuildEnvironment(taskEnvironment);

        /// <summary>
        /// Get the temporary path to use for this task environment.
        /// </summary>
        /// <remarks>
        /// Delete when the MSBuild API is available https://github.com/dotnet/msbuild/issues/14583
        /// </remarks>
#pragma warning disable RS0030 // Do not use banned APIs
        public string? GetTempPath() => Path.GetTempPath();
#pragma warning restore RS0030 // Do not use banned APIs

        public string GetFullPathNoThrow(string path)
        {
            try
            {
                var absolutePath = taskEnvironment.GetAbsolutePath(path);
                return absolutePath.Value;
            }
            catch (Exception)
            {
                return path;
            }
        }

        public void DeleteNoThrow(FileInfo fileInfo) => taskEnvironment.DeleteNoThrow(taskEnvironment.GetAbsolutePath(fileInfo.FullName));

        public void DeleteNoThrow(AbsolutePath path)
        {
#pragma warning disable RS0030 // Do not used banned APIs
            try
            {
                File.Delete(path.Value);
            }
            catch (Exception)
            {

            }
#pragma warning restore RS0030 // Do not used banned APIs
        }

        public bool FileExists(string path) => taskEnvironment.FileExists(taskEnvironment.GetAbsolutePath(path));

        public bool FileExists(AbsolutePath path)
        {
#pragma warning disable RS0030 // Do not used banned APIs
            return File.Exists(path.Value);
#pragma warning restore RS0030 // Do not used banned APIs
        }

        public void FileMove(string sourceFileName, string destFileName) =>
            taskEnvironment.FileMove(
                taskEnvironment.GetAbsolutePath(sourceFileName),
                taskEnvironment.GetAbsolutePath(destFileName));

        public void FileMove(AbsolutePath sourceFilePath, AbsolutePath destFilePath)
        {
#pragma warning disable RS0030 // Do not used banned APIs
            File.Move(sourceFilePath.Value, destFilePath.Value);
#pragma warning restore RS0030 // Do not used banned APIs
        }

        public FileInfo CreateFileInfo(string path) => taskEnvironment.CreateFileInfo(taskEnvironment.GetAbsolutePath(path));

        public FileInfo CreateFileInfo(AbsolutePath path)
        {
#pragma warning disable RS0030 // Do not used banned APIs
            return new FileInfo(path.Value);
#pragma warning restore RS0030 // Do not used banned APIs
        }

    }
}