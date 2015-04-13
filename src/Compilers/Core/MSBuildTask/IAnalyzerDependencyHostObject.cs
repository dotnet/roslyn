// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using Microsoft.Build.Framework;

namespace Microsoft.CodeAnalysis.BuildTasks
{
    [ComVisible(true)]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("13926637-09A6-438A-9418-DE6B9D2BEC6B")]
    public interface IAnalyzerDependencyHostObject
    {
        bool SetAnalyzerDependencies(ITaskItem[] analyzerDependencies);
    }
}
