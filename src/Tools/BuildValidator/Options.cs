// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;

namespace BuildValidator
{
    internal record Options(
        string[] AssembliesPaths,
        string[] ReferencesPaths,
        string SourcePath,
        bool Verbose,
        bool Quiet,
        bool Debug,
        string DebugPath);
}
