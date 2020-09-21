// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Reflection;

#if DEBUG || BOOTSTRAP
using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.Build.Utilities;
using Roslyn.Utilities;
#endif

namespace Microsoft.CodeAnalysis.BuildTasks
{
    using static Microsoft.CodeAnalysis.CommandLine.BuildResponse;

#if DEBUG || BOOTSTRAP
    /// <summary>
    /// This task exists to help us validate our bootstrap building phase is executing correctly.  The bootstrap
    /// phase of CI is the best way to validate the integration of our components is functioning correctly. Items
    /// which are difficult to validate in a unit test scenario.
    /// </summary>
    public sealed partial class ValidateBootstrap : Task
    {
        private static readonly ConcurrentDictionary<AssemblyName, byte> s_failedLoadSet = new ConcurrentDictionary<AssemblyName, byte>();
        private static readonly ConcurrentQueue<(ResponseType ResponseType, string? OutputAssembly)> s_failedQueue = new ConcurrentQueue<(ResponseType ResponseType, string? OutputAssembly)>();

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

            // This represents the maximum number of failed build attempts on the server before we will declare
            // that the overall build itself failed. 
            //
            // The goal is to keep this at zero. The errors here are a mix of repository construction errors (having
            // incompatible NuGet analyzers) and product errors (having flaky behavior in the server). Any time this
            // number goes above zero it means we are dropping connections during developer inner loop builds and 
            // hence measurably slowing down our productivity.
            //
            // When we find issues in the server or our infra we can temporarily raise this number while it is
            // being worked out but should file a bug to track getting this to zero.
            const int maxRejectCount = 0;
            var rejectCount = 0;
            foreach (var tuple in s_failedQueue.ToList())
            {
                switch (tuple.ResponseType)
                {
                    case ResponseType.AnalyzerInconsistency:
                        Log.LogError($"Analyzer inconsistency building {tuple.OutputAssembly}");
                        allGood = false;
                        break;
                    case ResponseType.MismatchedVersion:
                    case ResponseType.IncorrectHash:
                        Log.LogError($"Critical error {tuple.ResponseType} building {tuple.OutputAssembly}");
                        allGood = false;
                        break;
                    case ResponseType.Rejected:
                        rejectCount++;
                        if (rejectCount > maxRejectCount)
                        {
                            Log.LogError($"Too many compiler server connection failures detected");
                            allGood = false;
                        }
                        break;
                    case ResponseType.Completed:
                    case ResponseType.Shutdown:
                        // Expected messages
                        break;
                    default:
                        Log.LogError($"Unexpected response type {tuple.ResponseType}");
                        allGood = false;
                        break;
                }
            }

            return allGood;
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

        private string? GetDirectory(Assembly assembly) => Path.GetDirectoryName(Utilities.TryGetAssemblyPath(assembly));

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

        internal static void AddFailedServerConnection(ResponseType type, string? outputAssembly)
        {
            s_failedQueue.Enqueue((type, outputAssembly));
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

        internal static void AddFailedServerConnection(ResponseType type, string? outputAssembly)
        {
#if DEBUG || BOOTSTRAP
            ValidateBootstrap.AddFailedServerConnection(type, outputAssembly);
#endif
        }
    }
}
