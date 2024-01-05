// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if BOOTSTRAP

using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.Build.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.BuildTasks
{
    /// <summary>
    /// This task exists to help us validate our bootstrap build is loading the correct binary from disk. Ensuring
    /// it loads the bootstrap binaries and not the standard build binaries.
    /// </summary>
    public sealed partial class ValidateBootstrap : Task
    {
        private string? _tasksAssemblyFullPath;

        [DisallowNull]
        public string? TasksAssemblyFullPath
        {
            get { return _tasksAssemblyFullPath; }
            set { _tasksAssemblyFullPath = NormalizePath(Path.GetFullPath(value!)); }
        }

        public ValidateBootstrap()
        {

        }

        public override bool Execute()
        {
            if (TasksAssemblyFullPath is null)
            {
                Log.LogError($"{nameof(ValidateBootstrap)} task must have a {nameof(TasksAssemblyFullPath)} parameter.");
                return false;
            }

            var fullPath = typeof(ValidateBootstrap).Assembly.Location;
            if (!StringComparer.OrdinalIgnoreCase.Equals(TasksAssemblyFullPath, fullPath))
            {
                Log.LogError($"Bootstrap assembly {Path.GetFileName(fullPath)} incorrectly loaded from {fullPath} instead of {TasksAssemblyFullPath}");
                return false;
            }

            return true;
        }

        [return: NotNullIfNotNull("path")]
        private static string? NormalizePath(string? path)
        {
            if (RoslynString.IsNullOrEmpty(path))
            {
                return path;
            }

            var c = path[path.Length - 1];
            if (c == Path.DirectorySeparatorChar || c == Path.AltDirectorySeparatorChar)
            {
                path = path.Substring(0, path.Length - 1);
            }

            return path;
        }
    }
}
#endif
