// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        bool SetAnalyzerConfigFiles(ITaskItem[] analyzerConfigFiles);
        bool SetPotentialAnalyzerConfigFiles(ITaskItem[] potentialAnalyzerConfigfiles);
    }
}
