// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Build.Framework;
using Microsoft.CodeAnalysis.BuildTasks;
using Roslyn.Utilities;
namespace Microsoft.CodeAnalysis.CommandLine;

internal static class TaskEnvironmentExtensions
{
    extension(TaskEnvironment taskEnvironment)
    {
        public TaskBuildEnvironment BuildEnvironment => new TaskBuildEnvironment(taskEnvironment);

    }
}
