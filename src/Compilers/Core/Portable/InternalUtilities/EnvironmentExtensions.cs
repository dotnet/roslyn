// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Roslyn.Utilities;

internal static class EnvironmentExtensions
{
    extension(Environment)
    {
        internal static StringComparer EnvironmentVariableComparer => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
                StringComparer.OrdinalIgnoreCase :
                StringComparer.Ordinal;

        internal static StringComparison EnvironmentVariableComparison => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
                StringComparison.OrdinalIgnoreCase :
                StringComparison.Ordinal;

#if !MICROSOFT_CODEANALYSIS_MSBUILD_TASK
        /// <summary>
        /// Gets the environment variables as a strongly-typed dictionary.
        /// </summary>
        internal static Dictionary<string, string> GetEnvironmentVariablesTyped()
        {
            var environmentVariables = Environment.GetEnvironmentVariables();
            var map = new Dictionary<string, string>(capacity: environmentVariables.Count, Environment.EnvironmentVariableComparer);
            foreach (System.Collections.DictionaryEntry entry in environmentVariables)
            {
                map[(string)entry.Key] = (string?)entry.Value ?? "";
            }

            return map;
        }
#endif
    }
}
