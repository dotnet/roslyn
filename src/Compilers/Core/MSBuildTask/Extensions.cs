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
    }
}