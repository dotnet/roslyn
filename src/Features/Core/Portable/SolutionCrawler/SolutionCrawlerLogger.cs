// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Internal.Log;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SolutionCrawler
{
    internal static class SolutionCrawlerLogger
    {
        private const string Id = nameof(Id);
        private const string Kind = nameof(Kind);
        private const string Analyzer = nameof(Analyzer);
        private const string DocumentCount = nameof(DocumentCount);
        private const string Languages = nameof(Languages);
        private const string HighPriority = nameof(HighPriority);
        private const string Enabled = nameof(Enabled);
        private const string AnalyzerCount = nameof(AnalyzerCount);
        private const string PersistentStorage = nameof(PersistentStorage);
        private const string GlobalOperation = nameof(GlobalOperation);
        private const string HigherPriority = nameof(HigherPriority);
        private const string LowerPriority = nameof(LowerPriority);
        private const string TopLevel = nameof(TopLevel);
        private const string MemberLevel = nameof(MemberLevel);
        private const string NewWorkItem = nameof(NewWorkItem);
        private const string UpdateWorkItem = nameof(UpdateWorkItem);
        private const string ProjectEnqueue = nameof(ProjectEnqueue);
        private const string ResetStates = nameof(ResetStates);
        private const string ProjectNotExist = nameof(ProjectNotExist);
        private const string DocumentNotExist = nameof(DocumentNotExist);
        private const string ProcessProject = nameof(ProcessProject);
        private const string OpenDocument = nameof(OpenDocument);
        private const string CloseDocument = nameof(CloseDocument);
        private const string SolutionHash = nameof(SolutionHash);
        private const string ProcessDocument = nameof(ProcessDocument);
        private const string ProcessDocumentCancellation = nameof(ProcessDocumentCancellation);
        private const string ProcessProjectCancellation = nameof(ProcessProjectCancellation);
        private const string ActiveFileEnqueue = nameof(ActiveFileEnqueue);
        private const string ActiveFileProcessDocument = nameof(ActiveFileProcessDocument);
        private const string ActiveFileProcessDocumentCancellation = nameof(ActiveFileProcessDocumentCancellation);

        private const string Max = "Maximum";
        private const string Min = "Minimum";
        private const string Median = nameof(Median);
        private const string Mean = nameof(Mean);
        private const string Mode = nameof(Mode);
        private const string Range = nameof(Range);
        private const string Count = nameof(Count);

        public static void LogRegistration(int correlationId, Workspace workspace)
        {
            Logger.Log(FunctionId.WorkCoordinatorRegistrationService_Register, KeyValueLogMessage.Create(m =>
            {
                m[Id] = correlationId;
                m[Kind] = workspace.Kind;
            }));
        }

        public static void LogUnregistration(int correlationId)
        {
            Logger.Log(FunctionId.WorkCoordinatorRegistrationService_Unregister, KeyValueLogMessage.Create(m =>
            {
                m[Id] = correlationId;
            }));
        }

        public static void LogReanalyze(
            int correlationId,
            IIncrementalAnalyzer analyzer,
            int documentCount,
            string languages,
            bool highPriority)
        {
            Logger.Log(FunctionId.WorkCoordinatorRegistrationService_Reanalyze, KeyValueLogMessage.Create(m =>
            {
                m[Id] = correlationId;
                m[Analyzer] = analyzer.ToString();
                m[DocumentCount] = documentCount;
                m[HighPriority] = highPriority;
                m[Languages] = languages;
            }));
        }

        public static void LogAnalyzers(int correlationId, Workspace workspace, ImmutableArray<IIncrementalAnalyzer> reordered, bool onlyHighPriorityAnalyzer)
        {
            if (onlyHighPriorityAnalyzer)
            {
                LogAnalyzersWorker(
                    FunctionId.IncrementalAnalyzerProcessor_ActiveFileAnalyzers, FunctionId.IncrementalAnalyzerProcessor_ActiveFileAnalyzer,
                    correlationId, workspace, reordered);
            }
            else
            {
                LogAnalyzersWorker(
                    FunctionId.IncrementalAnalyzerProcessor_Analyzers, FunctionId.IncrementalAnalyzerProcessor_Analyzer,
                    correlationId, workspace, reordered);
            }
        }

        private static void LogAnalyzersWorker(
            FunctionId analyzersId, FunctionId analyzerId, int correlationId, Workspace workspace, ImmutableArray<IIncrementalAnalyzer> reordered)
        {
            if (workspace.Kind == WorkspaceKind.Preview)
            {
                return;
            }

            // log registered analyzers.
            Logger.Log(analyzersId, KeyValueLogMessage.Create(m =>
            {
                m[Id] = correlationId;
                m[AnalyzerCount] = reordered.Length;
            }));

            foreach (var analyzer in reordered)
            {
                Logger.Log(analyzerId, KeyValueLogMessage.Create(m =>
                {
                    m[Id] = correlationId;
                    m[Analyzer] = analyzer.ToString();
                }));
            }
        }

        public static void LogWorkCoordinatorShutdownTimeout(int correlationId)
        {
            Logger.Log(FunctionId.WorkCoordinator_ShutdownTimeout, KeyValueLogMessage.Create(m =>
            {
                m[Id] = correlationId;
            }));
        }

        public static void LogWorkspaceEvent(LogAggregator logAggregator, int kind)
            => logAggregator.IncreaseCount(kind);

        public static void LogWorkCoordinatorShutdown(int correlationId, LogAggregator logAggregator)
        {
            Logger.Log(FunctionId.WorkCoordinator_Shutdown, KeyValueLogMessage.Create(m =>
            {
                m[Id] = correlationId;

                foreach (var kv in logAggregator)
                {
                    var change = ((WorkspaceChangeKind)kv.Key).ToString();
                    m[change] = kv.Value.GetCount();
                }
            }));
        }

        public static void LogGlobalOperation(LogAggregator logAggregator)
            => logAggregator.IncreaseCount(GlobalOperation);

        public static void LogActiveFileEnqueue(LogAggregator logAggregator)
            => logAggregator.IncreaseCount(ActiveFileEnqueue);

        public static void LogWorkItemEnqueue(LogAggregator logAggregator, ProjectId _)
            => logAggregator.IncreaseCount(ProjectEnqueue);

        public static void LogWorkItemEnqueue(
            LogAggregator logAggregator, string language, DocumentId? documentId, InvocationReasons reasons, bool lowPriority, SyntaxPath? activeMember, bool added)
        {
            logAggregator.IncreaseCount(language);
            logAggregator.IncreaseCount(added ? NewWorkItem : UpdateWorkItem);

            if (documentId != null)
            {
                logAggregator.IncreaseCount(activeMember == null ? TopLevel : MemberLevel);

                if (lowPriority)
                {
                    logAggregator.IncreaseCount(LowerPriority);
                    logAggregator.IncreaseCount(ValueTuple.Create(LowerPriority, documentId.Id));
                }
            }

            foreach (var reason in reasons)
            {
                logAggregator.IncreaseCount(reason);
            }
        }

        public static void LogHigherPriority(LogAggregator logAggregator, Guid documentId)
        {
            logAggregator.IncreaseCount(HigherPriority);
            logAggregator.IncreaseCount(ValueTuple.Create(HigherPriority, documentId));
        }

        public static void LogResetStates(LogAggregator logAggregator)
            => logAggregator.IncreaseCount(ResetStates);

        public static void LogIncrementalAnalyzerProcessorStatistics(int correlationId, Solution solution, LogAggregator logAggregator, ImmutableArray<IIncrementalAnalyzer> analyzers)
        {
            Logger.Log(FunctionId.IncrementalAnalyzerProcessor_Shutdown, KeyValueLogMessage.Create(m =>
            {
                var solutionHash = GetSolutionHash(solution);

                m[Id] = correlationId;
                m[SolutionHash] = solutionHash.ToString();

                var statMap = new Dictionary<string, List<int>>();
                foreach (var (key, counter) in logAggregator)
                {
                    if (key is string stringKey)
                    {
                        m[stringKey] = counter.GetCount();
                        continue;
                    }

                    if (key is ValueTuple<string, Guid> propertyNameAndId)
                    {
                        var list = statMap.GetOrAdd(propertyNameAndId.Item1, _ => new List<int>());
                        list.Add(counter.GetCount());
                        continue;
                    }

                    throw ExceptionUtilities.Unreachable;
                }

                foreach (var (propertyName, propertyValues) in statMap)
                {
                    var result = LogAggregator.GetStatistics(propertyValues);

                    m[CreateProperty(propertyName, Max)] = result.Maximum;
                    m[CreateProperty(propertyName, Min)] = result.Minimum;
                    m[CreateProperty(propertyName, Median)] = result.Median!.Value;
                    m[CreateProperty(propertyName, Mean)] = result.Mean;
                    m[CreateProperty(propertyName, Mode)] = result.Mode!.Value;
                    m[CreateProperty(propertyName, Range)] = result.Range;
                    m[CreateProperty(propertyName, Count)] = result.Count;
                }
            }));

            foreach (var analyzer in analyzers)
            {
                analyzer.LogAnalyzerCountSummary();
            }
        }

        private static int GetSolutionHash(Solution solution)
        {
            if (solution != null && solution.FilePath != null)
            {
                return solution.FilePath.ToLowerInvariant().GetHashCode();
            }

            return 0;
        }

        private static string CreateProperty(string parent, string child)
            => parent + "." + child;

        public static void LogProcessCloseDocument(LogAggregator logAggregator, Guid documentId)
        {
            logAggregator.IncreaseCount(CloseDocument);
            logAggregator.IncreaseCount(ValueTuple.Create(CloseDocument, documentId));
        }

        public static void LogProcessOpenDocument(LogAggregator logAggregator, Guid documentId)
        {
            logAggregator.IncreaseCount(OpenDocument);
            logAggregator.IncreaseCount(ValueTuple.Create(OpenDocument, documentId));
        }

        public static void LogProcessActiveFileDocument(LogAggregator logAggregator, Guid _, bool processed)
        {
            if (processed)
            {
                logAggregator.IncreaseCount(ActiveFileProcessDocument);
            }
            else
            {
                logAggregator.IncreaseCount(ActiveFileProcessDocumentCancellation);
            }
        }

        public static void LogProcessDocument(LogAggregator logAggregator, Guid documentId, bool processed)
        {
            if (processed)
            {
                logAggregator.IncreaseCount(ProcessDocument);
            }
            else
            {
                logAggregator.IncreaseCount(ProcessDocumentCancellation);
            }

            logAggregator.IncreaseCount(ValueTuple.Create(ProcessDocument, documentId));
        }

        public static void LogProcessDocumentNotExist(LogAggregator logAggregator)
            => logAggregator.IncreaseCount(DocumentNotExist);

        public static void LogProcessProject(LogAggregator logAggregator, Guid projectId, bool processed)
        {
            if (processed)
            {
                logAggregator.IncreaseCount(ProcessProject);
            }
            else
            {
                logAggregator.IncreaseCount(ProcessProjectCancellation);
            }

            logAggregator.IncreaseCount(ValueTuple.Create(ProcessProject, projectId));
        }

        public static void LogProcessProjectNotExist(LogAggregator logAggregator)
            => logAggregator.IncreaseCount(ProjectNotExist);
    }
}
