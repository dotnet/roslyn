// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.WorkspaceServices;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host
{
    internal class BackgroundParser
    {
        private readonly Workspace workspace;
        private readonly IWorkspaceTaskScheduler taskScheduler;

        private readonly ReaderWriterLockSlim stateLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

        private readonly object parseGate = new object();
        private ImmutableDictionary<DocumentId, CancellationTokenSource> workMap = ImmutableDictionary.Create<DocumentId, CancellationTokenSource>();

        public bool IsStarted { get; private set; }

        public BackgroundParser(Workspace workspace)
        {
            this.workspace = workspace;

            var taskSchedulerFactory = WorkspaceService.GetService<IWorkspaceTaskSchedulerFactory>(workspace);
            this.taskScheduler = taskSchedulerFactory.CreateTaskScheduler(TaskScheduler.Default);
            this.workspace.WorkspaceChanged += this.OnWorkspaceChanged;

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
            using (this.stateLock.DisposableRead())
            {
                if (!this.IsStarted)
                {
                    this.IsStarted = true;
                }
            }
        }

        public void Stop()
        {
            using (this.stateLock.DisposableWrite())
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
            using (this.stateLock.DisposableWrite())
            {
                this.CancelAllParses_NoLock();
            }
        }

        private void CancelAllParses_NoLock()
        {
            this.stateLock.AssertCanWrite();

            foreach (var tuple in this.workMap)
            {
                tuple.Value.Cancel();
            }

            this.workMap = ImmutableDictionary.Create<DocumentId, CancellationTokenSource>();
        }

        public void CancelParse(DocumentId documentId)
        {
            if (documentId != null)
            {
                using (this.stateLock.DisposableWrite())
                {
                    CancellationTokenSource cancellationTokenSource;
                    if (this.workMap.TryGetValue(documentId, out cancellationTokenSource))
                    {
                        cancellationTokenSource.Cancel();
                        this.workMap = this.workMap.Remove(documentId);
                    }
                }
            }
        }

        public void Parse(Document document)
        {
            if (document != null)
            {
                lock (this.parseGate)
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

            using (this.stateLock.DisposableWrite())
            {
                this.workMap = this.workMap.Add(document.Id, cancellationTokenSource);
            }

            var cancellationToken = cancellationTokenSource.Token;

            var task = this.taskScheduler.ScheduleTask(
                () => document.GetSyntaxTreeAsync(cancellationToken),
                "BackgroundParser.ParseDocumentAsync",
                cancellationToken);

            // Always ensure that we mark this work as done from the workmap.
            task.SafeContinueWith(
                _ =>
                {
                    using (this.stateLock.DisposableWrite())
                    {
                        this.workMap = this.workMap.Remove(document.Id);
                    }
                }, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);
        }
    }
}
