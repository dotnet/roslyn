// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.CodeAnalysis.Logging
{
    internal static class SolutionLogger
    {
        private static readonly CountLogAggregator<string> s_logAggregator = new();

        public static void UseExistingPartialProjectState()
            => s_logAggregator.IncreaseCount(nameof(UseExistingPartialProjectState));

        public static void UseExistingFullProjectState()
            => s_logAggregator.IncreaseCount(nameof(UseExistingFullProjectState));

        public static void CreatePartialProjectState()
            => s_logAggregator.IncreaseCount(nameof(CreatePartialProjectState));

        public static void UseExistingPartialSolution()
            => s_logAggregator.IncreaseCount(nameof(UseExistingPartialSolution));

        public static void CreatePartialSolution()
            => s_logAggregator.IncreaseCount(nameof(CreatePartialSolution));

        public static void ReportTelemetry()
        {
            Logger.Log(FunctionId.Workspace_Solution_Info, KeyValueLogMessage.Create(m =>
            {
                m[nameof(UseExistingPartialProjectState)] = s_logAggregator.GetCount(nameof(UseExistingPartialProjectState));
                m[nameof(UseExistingFullProjectState)] = s_logAggregator.GetCount(nameof(UseExistingFullProjectState));
                m[nameof(CreatePartialProjectState)] = s_logAggregator.GetCount(nameof(CreatePartialProjectState));
                m[nameof(UseExistingPartialSolution)] = s_logAggregator.GetCount(nameof(UseExistingPartialSolution));
                m[nameof(CreatePartialSolution)] = s_logAggregator.GetCount(nameof(CreatePartialSolution));
            }));
        }
    }
}
