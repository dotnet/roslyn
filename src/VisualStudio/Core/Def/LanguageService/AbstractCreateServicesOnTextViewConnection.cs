// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Snippets;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService;

/// <summary>
/// Creates services on the first connection of an applicable subject buffer to an IWpfTextView. 
/// This ensures the services are available by the time an open document or the interactive window needs them.
/// </summary>
internal abstract class AbstractCreateServicesOnTextViewConnection : IWpfTextViewConnectionListener
{
    private readonly string _languageName;
    private readonly AsyncBatchingWorkQueue<ProjectId?> _workQueue;
    private bool _initialized = false;

    protected VisualStudioWorkspace Workspace { get; }
    protected IGlobalOptionService GlobalOptions { get; }

    protected virtual Task InitializeServiceForProjectWithOpenedDocumentAsync(Project project)
        => Task.CompletedTask;

    public AbstractCreateServicesOnTextViewConnection(
        VisualStudioWorkspace workspace,
        IGlobalOptionService globalOptions,
        IAsynchronousOperationListenerProvider listenerProvider,
        IThreadingContext threadingContext,
        string languageName)
    {
        Workspace = workspace;
        GlobalOptions = globalOptions;
        _languageName = languageName;

        _workQueue = new AsyncBatchingWorkQueue<ProjectId?>(
                TimeSpan.FromSeconds(1),
                BatchProcessProjectsWithOpenedDocumentAsync,
                EqualityComparer<ProjectId?>.Default,
                listenerProvider.GetListener(FeatureAttribute.CompletionSet),
                threadingContext.DisposalToken);

        Workspace.DocumentOpened += QueueWorkOnDocumentOpened;
    }

    void IWpfTextViewConnectionListener.SubjectBuffersConnected(IWpfTextView textView, ConnectionReason reason, Collection<ITextBuffer> subjectBuffers)
    {
        if (!_initialized)
        {
            _initialized = true;
            // use `null` to trigger per VS session intialization task
            _workQueue.AddWork((ProjectId?)null);
        }
    }

    void IWpfTextViewConnectionListener.SubjectBuffersDisconnected(IWpfTextView textView, ConnectionReason reason, Collection<ITextBuffer> subjectBuffers)
    {
    }

    private async ValueTask BatchProcessProjectsWithOpenedDocumentAsync(ImmutableSegmentedList<ProjectId?> projectIds, CancellationToken cancellationToken)
    {
        foreach (var projectId in projectIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (projectId is null)
            {
                InitializePerVSSessionServices();
            }
            else if (Workspace.CurrentSolution.GetProject(projectId) is Project project)
            {
                // Preload project completion providers at document open also helps avoid redundant file reads
                // from a race caused by multiple features (codefix, refactoring, etc.) attempting to get extensions
                // from analyzer references at the same time when they are not cached.
                if (project.GetLanguageService<CompletionService>() is CompletionService completionService)
                    completionService.TriggerLoadProjectProviders(project, GlobalOptions.GetCompletionOptions(project.Language));

                await InitializeServiceForProjectWithOpenedDocumentAsync(project).ConfigureAwait(false);
            }
        }
    }

    private void QueueWorkOnDocumentOpened(object sender, DocumentEventArgs e)
    {
        if (e.Document.Project.Language == _languageName)
            _workQueue.AddWork(e.Document.Project.Id);
    }

    private void InitializePerVSSessionServices()
    {
        var languageServices = Workspace.Services.GetExtendedLanguageServices(_languageName);

        _ = languageServices.GetService<ISnippetInfoService>();

        // Preload completion providers on a background thread since assembly loads can be slow
        // https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1242321
        languageServices.GetService<CompletionService>()?.LoadImportedProviders();
    }
}
