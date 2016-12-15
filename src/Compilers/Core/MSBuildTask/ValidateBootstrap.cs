// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.Build.Utilities;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Reflection;
using System;

namespace Microsoft.CodeAnalysis.BuildTasks
{
#if DEBUG || BOOTSTRAP
    /// <summary>
    /// This task exists to help us validate our bootstrap building phase is executing correctly.  It 
    /// is very easy for us, or MSBuild, to accidentally load DLLs from locations that we are 
    /// not expecting.  This task takes steps to validate the bootstrap phase is executing as expected.
    /// </summary>
    public sealed class ValidateBootstrap : Task
    {
        private static readonly ConcurrentDictionary<AssemblyName, byte> s_failedLoadSet = new ConcurrentDictionary<AssemblyName, byte>();

        private string _bootstrapPath;

        public string BootstrapPath
        {
            get { return _bootstrapPath; }
            set { _bootstrapPath = NormalizePath(value); }
        }

        public ValidateBootstrap()
        {


        }

        public override bool Execute()
        {
            if (_bootstrapPath == null)
            {
                Log.LogError($"{nameof(ValidateBootstrap)} task must have a {nameof(BootstrapPath)} parameter.");
                return false;
            }

            var dependencies = new[]
            {
                typeof(ValidateBootstrap).GetTypeInfo().Assembly,
            };

            var allGood = true;
            var comparer = StringComparer.OrdinalIgnoreCase;
            foreach (var dependency in dependencies)
            {
                var path = GetDirectory(dependency);
                path = NormalizePath(path);
                if (!comparer.Equals(path, _bootstrapPath))
                {
                    Log.LogError($"Bootstrap assembly {dependency.GetName().Name} incorrectly loaded from {path} instead of {_bootstrapPath}");
                    allGood = false;
                }
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

        private string GetDirectory(Assembly assembly) => Path.GetDirectoryName(Utilities.GetLocation(assembly));

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
    }
#endif
}
