// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Options;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host
{
    /// <summary>
    /// when users type, we chain all those changes as incremental parsing requests 
    /// but doesn't actually realize those changes. it is saved as a pending request. 
    /// so if nobody asks for final parse tree, those chain can keep grow. 
    /// we do this since Roslyn is lazy at the core (don't do work if nobody asks for it)
    /// 
    /// but certain host such as VS, we have this (BackgroundParser) which preemptively 
    /// trying to realize such trees for open/active files expecting users will use them soonish.
    /// </summary>
    internal class BackgroundParser
    {
        private readonly Workspace _workspace;
        private readonly IWorkspaceTaskScheduler _taskScheduler;

        private readonly ReaderWriterLockSlim _stateLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

        private readonly object _parseGate = new object();
        private ImmutableDictionary<DocumentId, CancellationTokenSource> _workMap = ImmutableDictionary.Create<DocumentId, CancellationTokenSource>();

        public bool IsStarted { get; private set; }

        public BackgroundParser(Workspace workspace)
        {
            _workspace = workspace;

            var taskSchedulerFactory = workspace.Services.GetService<IWorkspaceTaskSchedulerFactory>();
            _taskScheduler = taskSchedulerFactory.CreateBackgroundTaskScheduler();
            _workspace.WorkspaceChanged += OnWorkspaceChanged;

            workspace.DocumentOpened += OnDocumentOpened;
            workspace.DocumentClosed += OnDocumentClosed;
        }

        private void OnDocumentOpened(object sender, DocumentEventArgs args)
        {
            Parse(args.Document);
        }

        private void OnDocumentClosed(object sender, DocumentEventArgs args)
        {
            CancelParse(args.Document.Id);
        }

        private void OnWorkspaceChanged(object sender, WorkspaceChangeEventArgs args)
        {
            switch (args.Kind)
            {
                case WorkspaceChangeKind.SolutionCleared:
                case WorkspaceChangeKind.SolutionRemoved:
                case WorkspaceChangeKind.SolutionAdded:
                    CancelAllParses();
                    break;

                case WorkspaceChangeKind.DocumentRemoved:
                    CancelParse(args.DocumentId);
                    break;

                case WorkspaceChangeKind.DocumentChanged:
                    ParseIfOpen(args.NewSolution.GetDocument(args.DocumentId));
                    break;

                case WorkspaceChangeKind.ProjectChanged:

                    var oldProject = args.OldSolution.GetProject(args.ProjectId);
                    var newProject = args.NewSolution.GetProject(args.ProjectId);

                    // Perf optimization: don't rescan the new project if parse options didn't change. When looking
                    // at the perf of changing configurations that resulted in many reference additions/removals,
                    // this consumed around 2%-3% of the trace after some other optimizations I did. Most of that
                    // was actually walking the documents list since this was causing all the Documents to be realized.
                    // Since this is on the UI thread, it's best just to not do the work if we don't need it.
                    if (!ServiceFeatureOnOffOptions.IsPowerSaveModeEnabled(newProject) &&
                        oldProject.SupportsCompilation &&
                        !object.Equals(oldProject.ParseOptions, newProject.ParseOptions))
                    {
                        foreach (var doc in newProject.Documents)
                        {
                            ParseIfOpen(doc);
                        }
                    }

                    break;
            }
        }

        public void Start()
        {
            using (_stateLock.DisposableRead())
            {
                if (!IsStarted)
                {
                    IsStarted = true;
                }
            }
        }

        public void Stop()
        {
            using (_stateLock.DisposableWrite())
            {
                if (IsStarted)
                {
                    CancelAllParses_NoLock();
                    IsStarted = false;
                }
            }
        }

        public void CancelAllParses()
        {
            using (_stateLock.DisposableWrite())
            {
                CancelAllParses_NoLock();
            }
        }

        private void CancelAllParses_NoLock()
        {
            _stateLock.AssertCanWrite();

            foreach (var tuple in _workMap)
            {
                tuple.Value.Cancel();
            }

            _workMap = ImmutableDictionary.Create<DocumentId, CancellationTokenSource>();
        }

        public void CancelParse(DocumentId documentId)
        {
            if (documentId != null)
            {
                using (_stateLock.DisposableWrite())
                {
                    if (_workMap.TryGetValue(documentId, out var cancellationTokenSource))
                    {
                        cancellationTokenSource.Cancel();
                        _workMap = _workMap.Remove(documentId);
                    }
                }
            }
        }

        public void Parse(Document document)
        {
            if (document != null && !ServiceFeatureOnOffOptions.IsPowerSaveModeEnabled(document.Project))
            {
                lock (_parseGate)
                {
                    CancelParse(document.Id);

                    if (IsStarted)
                    {
                        ParseDocumentAsync(document);
                    }
                }
            }
        }

        private void ParseIfOpen(Document document)
        {
            if (document != null && document.IsOpen())
            {
                Parse(document);
            }
        }

        private void ParseDocumentAsync(Document document)
        {
            var cancellationTokenSource = new CancellationTokenSource();

            using (_stateLock.DisposableWrite())
            {
                _workMap = _workMap.Add(document.Id, cancellationTokenSource);
            }

            var cancellationToken = cancellationTokenSource.Token;

            // We end up creating a chain of parsing tasks that each attempt to produce 
            // the appropriate syntax tree for any given document. Once we start work to create 
            // the syntax tree for a given document, we don't want to stop. 
            // Otherwise we can end up in the unfortunate scenario where we keep cancelling work, 
            // and then having the next task re-do the work we were just in the middle of. 
            // By not cancelling, we can reuse the useful results of previous tasks when performing later steps in the chain.
            //
            // we still cancel whole task if the task didn't start yet. we just don't cancel if task is started but not finished yet.
            var task = _taskScheduler.ScheduleTask(
                () => document.GetSyntaxTreeAsync(CancellationToken.None),
                "BackgroundParser.ParseDocumentAsync",
                cancellationToken);

            // Always ensure that we mark this work as done from the workmap.
            task.SafeContinueWith(
                _ =>
                {
                    using (_stateLock.DisposableWrite())
                    {
                        // Check that we are still the active parse in the workmap before we remove it.
                        // Concievably if this continuation got delayed and another parse was put in, we might
                        // end up removing the tracking for another in-flight task.
                        if (_workMap.TryGetValue(document.Id, out var sourceInMap) && sourceInMap == cancellationTokenSource)
                        {
                            _workMap = _workMap.Remove(document.Id);
                        }
                    }
                }, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);
        }
    }
}
