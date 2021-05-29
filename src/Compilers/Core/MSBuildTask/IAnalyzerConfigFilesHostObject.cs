// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using Microsoft.Build.Framework;

namespace Microsoft.CodeAnalysis.BuildTasks
{
    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("31891ED8-BEB5-43BF-A90D-9E7E1CE9BA84")]
    public interface IAnalyzerConfigFilesHostObject
    {
        bool SetAnalyzerConfigFiles(ITaskItem[]? analyzerConfigFiles);
        bool SetPotentialAnalyzerConfigFiles(ITaskItem[]? potentialAnalyzerConfigfiles);
    }
}
