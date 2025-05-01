// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Resources;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.BuildTasks
{
#if NETFRAMEWORK && DEBUG

    internal sealed class TaskTestHost : MarshalByRefObject
    {
        internal bool IsSdkFrameworkToCoreBridgeTask => ManagedToolTask.CalculateIsSdkFrameworkToCoreBridgeTask();
    }

#endif
}
