// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.CodeAnalysis.Versions
{
    internal static class PersistedVersionStampLogger
    {
        // we have 6 different versions to track various changes
        private const string Text = "Text";
        private const string SyntaxTree = "SyntaxTree";
        private const string Project = "Project";
        private const string DependentProject = "DependentProject";
        private const string Semantic = "Semantic";
        private const string DependentSemantic = "DependentSemantic";

        private const string ProjectCount = "ProjectCount";
        private const string InitialSemanticVersionCount = "InitialSemanticVersionCount";
        private const string InitialDependentSemanticVersionCount = "InitialDependentSemanticVersionCount";

        private static readonly LogAggregator logAggregator = new LogAggregator();

        public static void LogPersistedTextVersionUsage(bool succeeded)
        {
            if (!succeeded)
            {
                return;
            }

            logAggregator.IncreaseCount(Text);
        }

        public static void LogPersistedSyntaxTreeVersionUsage(bool succeeded)
        {
            if (!succeeded)
            {
                return;
            }

            logAggregator.IncreaseCount(SyntaxTree);
        }

        public static void LogPersistedProjectVersionUsage(bool succeeded)
        {
            if (!succeeded)
            {
                return;
            }

            logAggregator.IncreaseCount(Project);
        }

        public static void LogPersistedDependentProjectVersionUsage(bool succeeded)
        {
            if (!succeeded)
            {
                return;
            }

            logAggregator.IncreaseCount(DependentProject);
        }

        public static void LogPersistedSemanticVersionUsage(bool succeeded)
        {
            if (!succeeded)
            {
                return;
            }

            logAggregator.IncreaseCount(Semantic);
        }

        public static void LogPersistedDependentSemanticVersionUsage(bool succeeded)
        {
            if (!succeeded)
            {
                return;
            }

            logAggregator.IncreaseCount(DependentSemantic);
        }

        public static void LogProject()
        {
            logAggregator.IncreaseCount(ProjectCount);
        }

        public static void LogInitialSemanticVersion()
        {
            logAggregator.IncreaseCount(InitialSemanticVersionCount);
        }

        public static void LogInitialDependentSemanticVersion()
        {
            logAggregator.IncreaseCount(InitialDependentSemanticVersionCount);
        }

        public static void LogSummary()
        {
            Logger.Log(FunctionId.PersistedSemanticVersion_Info, KeyValueLogMessage.Create(m =>
            {
                m[ProjectCount] = logAggregator.GetCount(ProjectCount).ToString();
                m[InitialSemanticVersionCount] = logAggregator.GetCount(InitialSemanticVersionCount).ToString();
                m[InitialDependentSemanticVersionCount] = logAggregator.GetCount(InitialDependentSemanticVersionCount).ToString();

                m[Text] = logAggregator.GetCount(Text).ToString();
                m[SyntaxTree] = logAggregator.GetCount(SyntaxTree).ToString();
                m[Project] = logAggregator.GetCount(Project).ToString();
                m[DependentProject] = logAggregator.GetCount(DependentProject).ToString();
                m[Semantic] = logAggregator.GetCount(Semantic).ToString();
                m[DependentSemantic] = logAggregator.GetCount(DependentSemantic).ToString();
            }));
        }
    }
}
