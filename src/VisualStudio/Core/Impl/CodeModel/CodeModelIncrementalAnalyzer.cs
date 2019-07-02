// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel
{
    [ExportIncrementalAnalyzerProvider(nameof(CodeModelIncrementalAnalyzerProvider), new[] { WorkspaceKind.Host }), Shared]
    internal class CodeModelIncrementalAnalyzerProvider : IIncrementalAnalyzerProvider
    {
        private readonly IAsynchronousOperationListener _listener;
        private readonly IForegroundNotificationService _notificationService;
        private readonly ProjectCodeModelFactory _projectCodeModelFactory;

        [ImportingConstructor]
        public CodeModelIncrementalAnalyzerProvider(
            IForegroundNotificationService notificationService,
            IAsynchronousOperationListenerProvider listenerProvider,
            ProjectCodeModelFactory projectCodeModelFactory)
        {
            _listener = listenerProvider.GetListener(FeatureAttribute.CodeModel);
            _notificationService = notificationService;
            _projectCodeModelFactory = projectCodeModelFactory;
        }

        public IIncrementalAnalyzer CreateIncrementalAnalyzer(Workspace workspace)
        {
            if (!(workspace is VisualStudioWorkspace visualStudioWorkspace))
            {
                return null;
            }

            return new Analyzer(_notificationService, _listener, visualStudioWorkspace, _projectCodeModelFactory);
        }

        private class Analyzer : IIncrementalAnalyzer
        {
            private readonly IForegroundNotificationService _notificationService;
            private readonly IAsynchronousOperationListener _listener;
            private readonly VisualStudioWorkspace _workspace;
            private readonly ProjectCodeModelFactory _projectCodeModelFactory;

            public Analyzer(IForegroundNotificationService notificationService, IAsynchronousOperationListener listener, VisualStudioWorkspace workspace, ProjectCodeModelFactory projectCodeModelFactory)
            {
                _notificationService = notificationService;
                _listener = listener;
                _workspace = workspace;
                _projectCodeModelFactory = projectCodeModelFactory;
            }

            public Task AnalyzeSyntaxAsync(Document document, InvocationReasons reasons, CancellationToken cancellationToken)
            {
                FireEvents(document.Id, cancellationToken);

                return Task.CompletedTask;
            }

            public void RemoveDocument(DocumentId documentId)
            {
                // REVIEW: do we need to fire events when a document is removed from the solution?
                FireEvents(documentId, CancellationToken.None);
            }

            public void FireEvents(DocumentId documentId, CancellationToken cancellationToken)
            {
                _notificationService.RegisterNotification(() =>
                {
                    var projectCodeModel = _projectCodeModelFactory.TryGetProjectCodeModel(documentId.ProjectId);

                    if (projectCodeModel == null)
                    {
                        return false;
                    }

                    var filename = _workspace.GetFilePath(documentId);
                    if (filename == null)
                    {
                        return false;
                    }

                    if (!projectCodeModel.TryGetCachedFileCodeModel(filename, out var fileCodeModelHandle))
                    {
                        return false;
                    }

                    var codeModel = fileCodeModelHandle.Object;
                    return codeModel.FireEvents();
                },
                _listener.BeginAsyncOperation("CodeModelEvent"),
                cancellationToken);
            }

            #region unused
            public Task NewSolutionSnapshotAsync(Solution solution, CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }

            public Task DocumentOpenAsync(Document document, CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }

            public Task DocumentCloseAsync(Document document, CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }

            public Task DocumentResetAsync(Document document, CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }

            public bool NeedsReanalysisOnOptionChanged(object sender, OptionChangedEventArgs e)
            {
                return false;
            }

            public Task AnalyzeDocumentAsync(Document document, SyntaxNode bodyOpt, InvocationReasons reasons, CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }

            public Task AnalyzeProjectAsync(Project project, bool semanticsChanged, InvocationReasons reasons, CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }

            public void RemoveProject(ProjectId projectId)
            {
            }
            #endregion
        }
    }
}
