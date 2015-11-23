// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SolutionCrawler
{
    internal class SolutionCrawlerLogger
    {
        private const string Id = "Id";
        private const string Kind = "Kind";
        private const string Analyzer = "Analyzer";
        private const string DocumentCount = "DocumentCount";
        private const string HighPriority = "HighPriority";
        private const string Enabled = "Enabled";
        private const string AnalyzerCount = "AnalyzerCount";
        private const string PersistentStorage = "PersistentStorage";
        private const string GlobalOperation = "GlobalOperation";
        private const string HigherPriority = "HigherPriority";
        private const string LowerPriority = "LowerPriority";
        private const string TopLevel = "TopLevel";
        private const string MemberLevel = "MemberLevel";
        private const string NewWorkItem = "NewWorkItem";
        private const string UpdateWorkItem = "UpdateWorkItem";
        private const string ProjectEnqueue = "ProjectEnqueue";
        private const string ResetStates = "ResetStates";
        private const string ProjectNotExist = "ProjectNotExist";
        private const string DocumentNotExist = "DocumentNotExist";
        private const string ProcessProject = "ProcessProject";
        private const string OpenDocument = "OpenDocument";
        private const string CloseDocument = "CloseDocument";
        private const string SolutionHash = "SolutionHash";
        private const string ProcessDocument = "ProcessDocument";
        private const string ProcessDocumentCancellation = "ProcessDocumentCancellation";
        private const string ProcessProjectCancellation = "ProcessProjectCancellation";
        private const string ActiveFileEnqueue = "ActiveFileEnqueue";
        private const string ActiveFileProcessDocument = "ActiveFileProcessDocument";
        private const string ActiveFileProcessDocumentCancellation = "ActiveFileProcessDocumentCancellation";

        private const string Max = "Maximum";
        private const string Min = "Minimum";
        private const string Median = "Median";
        private const string Mean = "Mean";
        private const string Mode = "Mode";
        private const string Range = "Range";
        private const string Count = "Count";

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

        public static void LogReanalyze(int correlationId, IIncrementalAnalyzer analyzer, IEnumerable<DocumentId> documentIds, bool highPriority)
        {
            Logger.Log(FunctionId.WorkCoordinatorRegistrationService_Reanalyze, KeyValueLogMessage.Create(m =>
            {
                m[Id] = correlationId;
                m[Analyzer] = analyzer.ToString();
                m[DocumentCount] = documentIds == null ? 0 : documentIds.Count();
                m[HighPriority] = highPriority;
            }));
        }

        public static void LogOptionChanged(int correlationId, bool value)
        {
            Logger.Log(FunctionId.WorkCoordinator_SolutionCrawlerOption, KeyValueLogMessage.Create(m =>
            {
                m[Id] = correlationId;
                m[Enabled] = value;
            }));
        }

        public static void LogActiveFileAnalyzers(int correlationId, Workspace workspace, ImmutableArray<IIncrementalAnalyzer> reordered)
        {
            LogAnalyzersWorker(
                FunctionId.IncrementalAnalyzerProcessor_ActiveFileAnalyzers, FunctionId.IncrementalAnalyzerProcessor_ActiveFileAnalyzer,
                correlationId, workspace, reordered);
        }

        public static void LogAnalyzers(int correlationId, Workspace workspace, ImmutableArray<IIncrementalAnalyzer> reordered)
        {
            LogAnalyzersWorker(
                FunctionId.IncrementalAnalyzerProcessor_Analyzers, FunctionId.IncrementalAnalyzerProcessor_Analyzer,
                correlationId, workspace, reordered);
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
        {
            logAggregator.IncreaseCount(kind);
        }

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
        {
            logAggregator.IncreaseCount(GlobalOperation);
        }

        public static void LogActiveFileEnqueue(LogAggregator logAggregator)
        {
            logAggregator.IncreaseCount(ActiveFileEnqueue);
        }

        public static void LogWorkItemEnqueue(LogAggregator logAggregator, ProjectId projectId)
        {
            logAggregator.IncreaseCount(ProjectEnqueue);
        }

        public static void LogWorkItemEnqueue(
            LogAggregator logAggregator, string language, DocumentId documentId, InvocationReasons reasons, bool lowPriority, SyntaxPath activeMember, bool added)
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
        {
            logAggregator.IncreaseCount(ResetStates);
        }

        public static void LogIncrementalAnalyzerProcessorStatistics(int correlationId, Solution solution, LogAggregator logAggregator, ImmutableArray<IIncrementalAnalyzer> analyzers)
        {
            Logger.Log(FunctionId.IncrementalAnalyzerProcessor_Shutdown, KeyValueLogMessage.Create(m =>
            {
                var solutionHash = GetSolutionHash(solution);

                m[Id] = correlationId;
                m[SolutionHash] = solutionHash.ToString();

                var statMap = new Dictionary<string, List<int>>();
                foreach (var kv in logAggregator)
                {
                    if (kv.Key is string)
                    {
                        m[kv.Key.ToString()] = kv.Value.GetCount();
                        continue;
                    }

                    if (kv.Key is ValueTuple<string, Guid>)
                    {
                        var tuple = (ValueTuple<string, Guid>)kv.Key;
                        var list = statMap.GetOrAdd(tuple.Item1, _ => new List<int>());
                        list.Add(kv.Value.GetCount());
                        continue;
                    }

                    throw ExceptionUtilities.Unreachable;
                }

                foreach (var kv in statMap)
                {
                    var key = kv.Key.ToString();
                    var result = LogAggregator.GetStatistics(kv.Value);

                    m[CreateProperty(key, Max)] = result.Maximum;
                    m[CreateProperty(key, Min)] = result.Minimum;
                    m[CreateProperty(key, Median)] = result.Median;
                    m[CreateProperty(key, Mean)] = result.Mean;
                    m[CreateProperty(key, Mode)] = result.Mode;
                    m[CreateProperty(key, Range)] = result.Range;
                    m[CreateProperty(key, Count)] = result.Count;
                }
            }));

            foreach (var analyzer in analyzers)
            {
                var diagIncrementalAnalyzer = analyzer as BaseDiagnosticIncrementalAnalyzer;
                if (diagIncrementalAnalyzer != null)
                {
                    diagIncrementalAnalyzer.LogAnalyzerCountSummary();
                    break;
                }
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
        {
            return parent + "." + child;
        }

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

        public static void LogProcessActiveFileDocument(LogAggregator logAggregator, Guid documentId, bool processed)
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
        {
            logAggregator.IncreaseCount(DocumentNotExist);
        }

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
        {
            logAggregator.IncreaseCount(ProjectNotExist);
        }
    }
}
