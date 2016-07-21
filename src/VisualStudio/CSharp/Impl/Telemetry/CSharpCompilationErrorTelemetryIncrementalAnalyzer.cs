// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.Internal.VisualStudio.Shell;
using Microsoft.VisualStudio.LanguageServices.Implementation.CompilationErrorTelemetry;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Telemetry
{
    [ExportPerLanguageIncrementalAnalyzerProvider(CompilationErrorTelemetryIncrementalAnalyzer.Name, LanguageNames.CSharp), Shared]
    internal class CSharpCompilationErrorTelemetryIncrementalAnalyzer : IPerLanguageIncrementalAnalyzerProvider
    {
        public IIncrementalAnalyzer CreatePerLanguageIncrementalAnalyzer(Workspace workspace, IIncrementalAnalyzerProvider provider)
        {
            return new Analyzer();
        }

        private class Analyzer : IIncrementalAnalyzer
        {
            private const string EventPrefix = "VS/Compilers/Compilation/";
            private const string PropertyPrefix = "VS.Compilers.Compilation.Error.";

            private const string TelemetryEventPath = EventPrefix + "Error";
            private const string TelemetryExceptionEventPath = EventPrefix + "TelemetryUnhandledException";
            private const string TelemetryErrorId = PropertyPrefix + "ErrorId";
            private const string TelemetryMethodName = PropertyPrefix + "MethodName";
            private const string TelemetryUnresolvedMemberName = PropertyPrefix + "UnresolvedMemberName";
            private const string TelemetryLeftExpressionDocId = PropertyPrefix + "LeftExpressionDocId";
            private const string TelemetryBaseTypes = PropertyPrefix + "LeftExpressionBaseTypeDocIds";
            private const string TelemetryGenericArguments = PropertyPrefix + "GenericArgumentDocIds";
            private const string TelemetryProjectGuid = PropertyPrefix + "ProjectGuid";
            private const string TelemetryMismatchedArgumentTypeDocIds = PropertyPrefix + "MismatchedArgumentDocIds";
            private const string UnspecifiedProjectGuid = "unspecified";

            private SolutionId _currentSolutionId;

            private readonly CompilationErrorDetailDiscoverer _errorDetailDiscoverer = new CompilationErrorDetailDiscoverer();
            private readonly ProjectGuidCache _projectGuidCache = new ProjectGuidCache();
            private readonly CompilationErrorDetailCache _errorDetailCache = new CompilationErrorDetailCache();

            public async Task AnalyzeDocumentAsync(Document document, SyntaxNode bodyOpt, InvocationReasons reasons, CancellationToken cancellationToken)
            {
                try
                {
                    // do nothing if this document is not currently open.
                    // to prevent perf from getting too poor for large projects, only examine open documents.
                    if (!document.IsOpen())
                    {
                        return;
                    }

                    // Only analyzing C# for now
                    if (document.Project.Language != LanguageNames.CSharp)
                    {
                        return;
                    }

                    List<CompilationErrorDetails> errorDetails = await _errorDetailDiscoverer.GetCompilationErrorDetails(document, bodyOpt, cancellationToken).ConfigureAwait(false);

                    var errorsToReport = _errorDetailCache.GetErrorsToReportAndRecordErrors(document.Id, errorDetails);
                    if (errorsToReport != null)
                    {
                        using (var hashProvider = new SHA256CryptoServiceProvider())
                        {
                            foreach (CompilationErrorDetails errorDetail in errorsToReport)
                            {
                                var telemetryEvent = TelemetryHelper.TelemetryService.CreateEvent(TelemetryEventPath);
                                telemetryEvent.SetStringProperty(TelemetryErrorId, errorDetail.ErrorId);

                                string projectGuid = _projectGuidCache.GetProjectGuidFromProjectPath(document.Project.FilePath);
                                telemetryEvent.SetStringProperty(TelemetryProjectGuid, string.IsNullOrEmpty(projectGuid) ? UnspecifiedProjectGuid : projectGuid.ToString());

                                if (!string.IsNullOrEmpty(errorDetail.UnresolvedMemberName))
                                {
                                    telemetryEvent.SetStringProperty(TelemetryUnresolvedMemberName, GetHashedString(errorDetail.UnresolvedMemberName, hashProvider));
                                }

                                if (!string.IsNullOrEmpty(errorDetail.LeftExpressionDocId))
                                {
                                    telemetryEvent.SetStringProperty(TelemetryLeftExpressionDocId, GetHashedString(errorDetail.LeftExpressionDocId, hashProvider));
                                }

                                if (!IsArrayNullOrEmpty(errorDetail.LeftExpressionBaseTypeDocIds))
                                {
                                    string telemetryBaseTypes = string.Join(";", errorDetail.LeftExpressionBaseTypeDocIds.Select(docId => GetHashedString(docId, hashProvider)));
                                    telemetryEvent.SetStringProperty(TelemetryBaseTypes, telemetryBaseTypes);
                                }

                                if (!IsArrayNullOrEmpty(errorDetail.GenericArguments))
                                {
                                    string telemetryGenericArguments = string.Join(";", errorDetail.GenericArguments.Select(docId => GetHashedString(docId, hashProvider)));
                                    telemetryEvent.SetStringProperty(TelemetryGenericArguments, telemetryGenericArguments);
                                }

                                if (!string.IsNullOrEmpty(errorDetail.MethodName))
                                {
                                    telemetryEvent.SetStringProperty(TelemetryMethodName, GetHashedString(errorDetail.MethodName, hashProvider));
                                }

                                if (!IsArrayNullOrEmpty(errorDetail.ArgumentTypes))
                                {
                                    string telemetryMisMatchedArgumentTypeDocIds = string.Join(";", errorDetail.ArgumentTypes.Select(docId => GetHashedString(docId, hashProvider)));
                                    telemetryEvent.SetStringProperty(TelemetryMismatchedArgumentTypeDocIds, telemetryMisMatchedArgumentTypeDocIds);
                                }

                                TelemetryHelper.DefaultTelemetrySession.PostEvent(telemetryEvent);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    // The telemetry service itself can throw.
                    // So, to be very careful, put this in a try/catch too.
                    try
                    {
                        var exceptionEvent = TelemetryHelper.TelemetryService.CreateEvent(TelemetryExceptionEventPath);
                        exceptionEvent.SetStringProperty("Type", e.GetTypeDisplayName());
                        exceptionEvent.SetStringProperty("Message", e.Message);
                        exceptionEvent.SetStringProperty("StackTrace", e.StackTrace);
                        TelemetryHelper.DefaultTelemetrySession.PostEvent(exceptionEvent);
                    }
                    catch
                    {
                    }
                }
            }

            public Task AnalyzeProjectAsync(Project project, bool semanticsChanged, InvocationReasons reasons, CancellationToken cancellationToken)
            {
                return SpecializedTasks.EmptyTask;
            }

            public Task AnalyzeSyntaxAsync(Document document, InvocationReasons reasons, CancellationToken cancellationToken)
            {
                return SpecializedTasks.EmptyTask;
            }

            public Task DocumentOpenAsync(Document document, CancellationToken cancellationToken)
            {
                return SpecializedTasks.EmptyTask;
            }

            public Task DocumentCloseAsync(Document document, CancellationToken cancellationToken)
            {
                return SpecializedTasks.EmptyTask;
            }

            public Task DocumentResetAsync(Document document, CancellationToken cancellationToken)
            {
                _errorDetailCache.ClearCacheForDocument(document.Id);
                return SpecializedTasks.EmptyTask;
            }

            public bool NeedsReanalysisOnOptionChanged(object sender, OptionChangedEventArgs e)
            {
                return false;
            }

            public Task NewSolutionSnapshotAsync(Solution solution, CancellationToken cancellationToken)
            {
                if (solution.Id != _currentSolutionId)
                {
                    _projectGuidCache.Clear();
                    _errorDetailCache.Clear();

                    _currentSolutionId = solution.Id;
                }

                return SpecializedTasks.EmptyTask;
            }

            public void RemoveDocument(DocumentId documentId)
            {
                _errorDetailCache.ClearCacheForDocument(documentId);
            }

            public void RemoveProject(ProjectId projectId)
            {
            }

            private static string GetHashedString(string unhashedString, SHA256CryptoServiceProvider hashProvider)
            {
                // The point of hashing here is to obscure customer data.  
                // So, if we get any empty values (noone knows why this happens for certain... has only been observed in dumps)
                // an empty string is fine.  It will appear as an extra semicolon in the MD value.
                if (string.IsNullOrEmpty(unhashedString))
                {
                    return string.Empty;
                }

                if (TelemetryHelper.DefaultTelemetrySession.CanCollectPrivateInformation())
                {
                    return unhashedString;
                }

                byte[] theHash = hashProvider.ComputeHash(Encoding.UTF8.GetBytes(unhashedString));
                StringBuilder sb = new StringBuilder(theHash.Length);
                for (int i = 0; i < theHash.Length; i++)
                {
                    sb.AppendFormat("{0:X}", theHash[i]);
                }

                return sb.ToString();
            }

            private static bool IsArrayNullOrEmpty(Array list)
            {
                return (list == null) || (list.Length == 0);
            }
        }

        private class CompilationErrorDetailCache
        {
            private readonly Dictionary<DocumentId, HashSet<CompilationErrorDetails>> _errorCache = new Dictionary<DocumentId, HashSet<CompilationErrorDetails>>();
            private readonly object _lockObject = new object();

            public IEnumerable<CompilationErrorDetails> GetErrorsToReportAndRecordErrors(DocumentId documentId, IEnumerable<CompilationErrorDetails> errors)
            {
                if (errors == null)
                {
                    return Enumerable.Empty<CompilationErrorDetails>();
                }

                lock (_lockObject)
                {
                    List<CompilationErrorDetails> ret = null;

                    HashSet<CompilationErrorDetails> cachedErrorsForDocument;
                    if (!_errorCache.TryGetValue(documentId, out cachedErrorsForDocument))
                    {
                        cachedErrorsForDocument = new HashSet<CompilationErrorDetails>();
                        _errorCache[documentId] = cachedErrorsForDocument;
                    }

                    foreach (var error in errors)
                    {
                        if (!cachedErrorsForDocument.Contains(error))
                        {
                            ret = ret ?? new List<CompilationErrorDetails>();
                            ret.Add(error);
                        }
                    }

                    // Only add errors to the cache after looping through the whole list.  This is so that if there are multiple instances of the same error
                    // (with all the same data that we gather) in a document, that it willget reported multiple 
                    if (ret != null)
                    {
                        foreach (var error in ret)
                        {
                            cachedErrorsForDocument.Add(error);
                        }
                    }

                    return ret;
                }
            }

            public void ClearCacheForDocument(DocumentId documentId)
            {
                lock (_lockObject)
                {
                    _errorCache.Remove(documentId);
                }
            }

            public void Clear()
            {
                lock (_lockObject)
                {
                    _errorCache.Clear();
                }
            }
        }

        private class ProjectGuidCache
        {
            private readonly Dictionary<string, string> _projectGuids = new Dictionary<string, string>();
            private readonly object _lockObject = new object();

            public void Clear()
            {
                lock (_lockObject)
                {
                    _projectGuids.Clear();
                }
            }

            public string GetProjectGuidFromProjectPath(string projectPath)
            {
                // misc project, web site and etc might not have project path.
                if (string.IsNullOrEmpty(projectPath))
                {
                    return null;
                }

                lock (_lockObject)
                {
                    string ret;
                    if (_projectGuids.TryGetValue(projectPath, out ret))
                    {
                        return ret;
                    }

                    try
                    {
                        XDocument project = XDocument.Load(projectPath);
                        ret = (from xElement in project.Descendants()
                               where xElement.Name.LocalName.Equals("ProjectGuid", StringComparison.InvariantCultureIgnoreCase) &&
                               xElement.Parent != null &&
                               xElement.Parent.Name.LocalName.Equals("PropertyGroup", StringComparison.InvariantCultureIgnoreCase)
                               select xElement.Value).FirstOrDefault();
                        if (ret != null)
                        {
                            _projectGuids[projectPath] = ret;
                            return ret;
                        }
                    }
                    catch
                    {
                    }

                    return null;
                }
            }
        }
    }
}
