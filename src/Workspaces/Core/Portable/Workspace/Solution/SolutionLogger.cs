// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.CodeAnalysis.Logging
{
    internal static class SolutionLogger
    {
        private static readonly LogAggregator s_logAggregator = new LogAggregator();

        public static void UseExistingPartialProjectState()
        {
            s_logAggregator.IncreaseCount(nameof(UseExistingPartialProjectState));
        }

        public static void UseExistingFullProjectState()
        {
            s_logAggregator.IncreaseCount(nameof(UseExistingFullProjectState));
        }

        public static void CreatePartialProjectState()
        {
            s_logAggregator.IncreaseCount(nameof(CreatePartialProjectState));
        }

        public static void UseExistingPartialSolution()
        {
            s_logAggregator.IncreaseCount(nameof(UseExistingPartialSolution));
        }

        public static void CreatePartialSolution()
        {
            s_logAggregator.IncreaseCount(nameof(CreatePartialSolution));
        }

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
