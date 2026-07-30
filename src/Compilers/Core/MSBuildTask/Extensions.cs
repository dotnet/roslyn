// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Build.Framework;
using Roslyn.Utilities;
namespace Microsoft.CodeAnalysis.CommandLine;

internal static class Extensions
{
    extension(TaskEnvironment taskEnvironment)
    {
        internal string GetFullPath(string path)
        {
            var fullPath = taskEnvironment.GetAbsolutePath(path).Value;
            return Path.GetFullPath(fullPath);
        }

        /// <summary>
        /// Gets a dictionary containing the environment variables for the current task environment. This 
        /// dictionary has the same semantics as <cref see="System.Environment.GetEnvironmentVariables()"/> where
        /// missing is represented as null. 
        /// </summary>
        internal IReadOnlyDictionary<string, string?> GetEnvironmentVariablesMap()
        {
#pragma warning disable RS0030 // Do not use banned APIs
            var environmentVariables = taskEnvironment.GetEnvironmentVariables();
#pragma warning restore RS0030 // Do not use banned APIs

            var map = new Dictionary<string, string?>(capacity: environmentVariables.Count, Environment.EnvironmentVariableComparer);
            foreach (var tuple in environmentVariables)
            {
                var value = tuple.Value == "" && taskEnvironment.GetEnvironmentVariable(tuple.Key) == null ? null : tuple.Value;
                map[tuple.Key] = value;
            }

            return map;
        }
    }
}