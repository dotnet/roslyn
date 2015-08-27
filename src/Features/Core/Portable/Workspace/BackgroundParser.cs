// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host
{
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
            _taskScheduler = taskSchedulerFactory.CreateTaskScheduler(TaskScheduler.Default);
            _workspace.WorkspaceChanged += this.OnWorkspaceChanged;

            var editorWorkspace = workspace as Workspace;
            if (editorWorkspace != null)
            {
                editorWorkspace.DocumentOpened += this.OnDocumentOpened;
                editorWorkspace.DocumentClosed += this.OnDocumentClosed;
            }
        }

        private void OnDocumentOpened(object sender, DocumentEventArgs args)
        {
            this.Parse(args.Document);
        }

        private void OnDocumentClosed(object sender, DocumentEventArgs args)
        {
            this.CancelParse(args.Document.Id);
        }

        private void OnWorkspaceChanged(object sender, WorkspaceChangeEventArgs args)
        {
            switch (args.Kind)
            {
                case WorkspaceChangeKind.SolutionCleared:
                case WorkspaceChangeKind.SolutionRemoved:
                case WorkspaceChangeKind.SolutionAdded:
                    this.CancelAllParses();
                    break;

                case WorkspaceChangeKind.DocumentRemoved:
                    this.CancelParse(args.DocumentId);
                    break;

                case WorkspaceChangeKind.DocumentChanged:
                    this.ParseIfOpen(args.NewSolution.GetDocument(args.DocumentId));
                    break;

                case WorkspaceChangeKind.ProjectChanged:
                    foreach (var doc in args.NewSolution.GetProject(args.ProjectId).Documents)
                    {
                        this.ParseIfOpen(doc);
                    }

                    break;
            }
        }

        public void Start()
        {
            using (_stateLock.DisposableRead())
            {
                if (!this.IsStarted)
                {
                    this.IsStarted = true;
                }
            }
        }

        public void Stop()
        {
            using (_stateLock.DisposableWrite())
            {
                if (this.IsStarted)
                {
                    this.CancelAllParses_NoLock();
                    this.IsStarted = false;
                }
            }
        }

        public void CancelAllParses()
        {
            using (_stateLock.DisposableWrite())
            {
                this.CancelAllParses_NoLock();
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
                    CancellationTokenSource cancellationTokenSource;
                    if (_workMap.TryGetValue(documentId, out cancellationTokenSource))
                    {
                        cancellationTokenSource.Cancel();
                        _workMap = _workMap.Remove(documentId);
                    }
                }
            }
        }

        public void Parse(Document document)
        {
            if (document != null)
            {
                lock (_parseGate)
                {
                    this.CancelParse(document.Id);

                    if (this.IsStarted)
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
                this.Parse(document);
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

            var task = _taskScheduler.ScheduleTask(
                () => document.GetSyntaxTreeAsync(cancellationToken),
                "BackgroundParser.ParseDocumentAsync",
                cancellationToken);

            // Always ensure that we mark this work as done from the workmap.
            task.SafeContinueWith(
                _ =>
                {
                    using (_stateLock.DisposableWrite())
                    {
                        _workMap = _workMap.Remove(document.Id);
                    }
                }, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);
        }
    }
}
