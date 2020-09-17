// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.CodeAnalysis.Versions
{
    internal static class PersistedVersionStampLogger
    {
        // we have 6 different versions to track various changes
        private const string Text = nameof(Text);
        private const string SyntaxTree = nameof(SyntaxTree);
        private const string Project = nameof(Project);
        private const string DependentProject = nameof(DependentProject);

        private static readonly LogAggregator s_logAggregator = new LogAggregator();

        public static void LogPersistedTextVersionUsage(bool succeeded)
        {
            if (!succeeded)
            {
                return;
            }

            s_logAggregator.IncreaseCount(Text);
        }

        public static void LogPersistedSyntaxTreeVersionUsage(bool succeeded)
        {
            if (!succeeded)
            {
                return;
            }

            s_logAggregator.IncreaseCount(SyntaxTree);
        }

        public static void LogPersistedProjectVersionUsage(bool succeeded)
        {
            if (!succeeded)
            {
                return;
            }

            s_logAggregator.IncreaseCount(Project);
        }

        public static void LogPersistedDependentProjectVersionUsage(bool succeeded)
        {
            if (!succeeded)
            {
                return;
            }

            s_logAggregator.IncreaseCount(DependentProject);
        }

        public static void ReportTelemetry()
        {
            Logger.Log(FunctionId.PersistedSemanticVersion_Info, KeyValueLogMessage.Create(m =>
            {
                m[Text] = s_logAggregator.GetCount(Text);
                m[SyntaxTree] = s_logAggregator.GetCount(SyntaxTree);
                m[Project] = s_logAggregator.GetCount(Project);
                m[DependentProject] = s_logAggregator.GetCount(DependentProject);
            }));
        }
    }
}
