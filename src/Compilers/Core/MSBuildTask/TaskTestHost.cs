// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.BuildTasks
{
#if NETFRAMEWORK && DEBUG

    /// <summary>
    /// This class is used in <see cref="AppDomain"/> to allow us to test custom
    /// disk layouts and its behavior in the context of the managed tool task.
    /// </summary>
    internal sealed class TaskTestHost : MarshalByRefObject
    {
        internal bool IsSdkFrameworkToCoreBridgeTask => ManagedToolTask.CalculateIsSdkFrameworkToCoreBridgeTask();
    }

#endif
}
