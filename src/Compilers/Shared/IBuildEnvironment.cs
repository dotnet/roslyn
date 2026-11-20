
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Roslyn.Utilities;
namespace Microsoft.CodeAnalysis;

internal interface IBuildEnvironment
{
    public string CurrentDirectory { get; }
    public string? GetTempPath();
    public string GetFullyQualifiedPath(string path);
    public string? GetEnvironmentVariable(string name);
    public IReadOnlyDictionary<string, string> GetEnvironmentVariables();
}

internal static class IBuildEnvironmentExtensions
{
    extension(IBuildEnvironment buildEnvironment)
    {
        internal string GetFullyQualifiedPathNoThrow(string path)
        {
            try
            {
                path = buildEnvironment.GetFullyQualifiedPath(path);
            }
            catch (Exception)
            {

            }
            return path;
        }
    }
}