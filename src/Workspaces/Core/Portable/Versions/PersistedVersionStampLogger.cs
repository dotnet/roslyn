// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        private const string Semantic = nameof(Semantic);
        private const string DependentSemantic = nameof(DependentSemantic);

        private const string ProjectCount = nameof(ProjectCount);
        private const string InitialSemanticVersionCount = nameof(InitialSemanticVersionCount);
        private const string InitialDependentSemanticVersionCount = nameof(InitialDependentSemanticVersionCount);

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

        public static void LogPersistedSemanticVersionUsage(bool succeeded)
        {
            if (!succeeded)
            {
                return;
            }

            s_logAggregator.IncreaseCount(Semantic);
        }

        public static void LogPersistedDependentSemanticVersionUsage(bool succeeded)
        {
            if (!succeeded)
            {
                return;
            }

            s_logAggregator.IncreaseCount(DependentSemantic);
        }

        public static void LogProject()
        {
            s_logAggregator.IncreaseCount(ProjectCount);
        }

        public static void LogInitialSemanticVersion()
        {
            s_logAggregator.IncreaseCount(InitialSemanticVersionCount);
        }

        public static void LogInitialDependentSemanticVersion()
        {
            s_logAggregator.IncreaseCount(InitialDependentSemanticVersionCount);
        }

        public static void LogSummary()
        {
            Logger.Log(FunctionId.PersistedSemanticVersion_Info, KeyValueLogMessage.Create(m =>
            {
                m[ProjectCount] = s_logAggregator.GetCount(ProjectCount);
                m[InitialSemanticVersionCount] = s_logAggregator.GetCount(InitialSemanticVersionCount);
                m[InitialDependentSemanticVersionCount] = s_logAggregator.GetCount(InitialDependentSemanticVersionCount);

                m[Text] = s_logAggregator.GetCount(Text);
                m[SyntaxTree] = s_logAggregator.GetCount(SyntaxTree);
                m[Project] = s_logAggregator.GetCount(Project);
                m[DependentProject] = s_logAggregator.GetCount(DependentProject);
                m[Semantic] = s_logAggregator.GetCount(Semantic);
                m[DependentSemantic] = s_logAggregator.GetCount(DependentSemantic);
            }));
        }
    }
}
