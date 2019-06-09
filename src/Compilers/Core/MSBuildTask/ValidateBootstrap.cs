// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.Build.Utilities;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Reflection;
using System;
using System.Threading;

namespace Microsoft.CodeAnalysis.BuildTasks
{
#if DEBUG || BOOTSTRAP
    /// <summary>
    /// This task exists to help us validate our bootstrap building phase is executing correctly.  The bootstrap
    /// phase of CI is the best way to validate the integration of our components is functioning correctly. Items
    /// which are difficult to validate in a unit test scenario.
    /// </summary>
    public sealed partial class ValidateBootstrap : Task
    {
        private static readonly ConcurrentDictionary<AssemblyName, byte> s_failedLoadSet = new ConcurrentDictionary<AssemblyName, byte>();
        private static int s_failedServerConnectionCount = 0;

        private string _tasksAssemblyFullPath;

        public string TasksAssemblyFullPath
        {
            get { return _tasksAssemblyFullPath; }
            set { _tasksAssemblyFullPath = NormalizePath(Path.GetFullPath(value)); }
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

            var allGood = true;
            var fullPath = typeof(ValidateBootstrap).Assembly.Location;
            if (!StringComparer.OrdinalIgnoreCase.Equals(TasksAssemblyFullPath, fullPath))
            {
                Log.LogError($"Bootstrap assembly {Path.GetFileName(fullPath)} incorrectly loaded from {fullPath} instead of {TasksAssemblyFullPath}");
                allGood = false;
            }

            var failedLoads = s_failedLoadSet.Keys.ToList();
            if (failedLoads.Count > 0)
            {
                foreach (var name in failedLoads.OrderBy(x => x.Name))
                {
                    Log.LogError($"Assembly resolution failed for {name}");
                    allGood = false;
                }
            }

            // The number chosen is arbitrary here.  The goal of this check is to catch cases where a coding error has 
            // broken our ability to use the compiler server in the bootstrap phase.
            //
            // It's possible on completely correct code for the server connection to fail.  There could be simply 
            // named pipe errors, CPU load causing timeouts, etc ...  Hence flagging a single failure would produce
            // a lot of false positives.  The current value was chosen as a reasonable number for warranting an 
            // investigation.
            if (s_failedServerConnectionCount > 20)
            {
                Log.LogError($"Too many compiler server connection failures detected: {s_failedServerConnectionCount}");
                allGood = false;
            }

            return allGood;
        }

        private static string NormalizePath(string path)
        {
            if (string.IsNullOrEmpty(path))
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

        private string GetDirectory(Assembly assembly) => Path.GetDirectoryName(Utilities.TryGetAssemblyPath(assembly));

        internal static void AddFailedLoad(AssemblyName name)
        {
            switch (name.Name)
            {
                case "System":
                case "System.Core":
                case "Microsoft.Build.Tasks.CodeAnalysis.resources":
                    // These are failures are expected by design.
                    break;
                default:
                    s_failedLoadSet.TryAdd(name, 0);
                    break;
            }
        }

        internal static void AddFailedServerConnection()
        {
            Interlocked.Increment(ref s_failedServerConnectionCount);
        }
    }
#endif

    internal static class ValidateBootstrapUtil
    {
        internal static void AddFailedLoad(AssemblyName name)
        {
#if DEBUG || BOOTSTRAP
            ValidateBootstrap.AddFailedLoad(name);
#endif
        }

        internal static void AddFailedServerConnection()
        {
#if DEBUG || BOOTSTRAP
            ValidateBootstrap.AddFailedServerConnection();
#endif
        }
    }
}
