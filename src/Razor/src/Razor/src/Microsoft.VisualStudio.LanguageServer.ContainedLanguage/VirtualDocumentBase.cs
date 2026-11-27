// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServer.ContainedLanguage.Extensions;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServer.ContainedLanguage;

public abstract class VirtualDocumentBase<T> : VirtualDocument where T : VirtualDocumentSnapshot
{
    private T _currentSnapshot;
    private int _hostDocumentSyncVersion;

    protected VirtualDocumentBase(Uri uri, ITextBuffer textBuffer)
    {
        if (uri is null)
        {
            throw new ArgumentNullException(nameof(uri));
        }

        if (textBuffer is null)
        {
            throw new ArgumentNullException(nameof(textBuffer));
        }

        Uri = uri;
        TextBuffer = textBuffer;
        _currentSnapshot = GetUpdatedSnapshot(state: null);
    }

    public override Uri Uri { get; }

    public override ITextBuffer TextBuffer { get; }

    public override int HostDocumentVersion => _hostDocumentSyncVersion;

    public override VirtualDocumentSnapshot CurrentSnapshot => _currentSnapshot;

    public override VirtualDocumentSnapshot Update(IReadOnlyList<ITextChange> changes, int hostDocumentVersion, object? state)
    {
        if (changes is null)
        {
            throw new ArgumentNullException(nameof(changes));
        }

        _hostDocumentSyncVersion = hostDocumentVersion;
        TextBuffer.SetHostDocumentSyncVersion(_hostDocumentSyncVersion);

        using var edit = TextBuffer.CreateEdit(EditOptions.None, reiteratedVersionNumber: null, InviolableEditTag.Instance);
        for (var i = 0; i < changes.Count; i++)
        {
            var change = changes[i];
            edit.Replace(change.OldSpan.Start, change.OldSpan.Length, change.NewText);
        }

        edit.Apply();
        _currentSnapshot = GetUpdatedSnapshot(state);

        return _currentSnapshot;
    }

    protected abstract T GetUpdatedSnapshot(object? state);

    [SuppressMessage("Usage", "CA1816:Dispose methods should call SuppressFinalize", Justification = "https://github.com/dotnet/roslyn-analyzers/issues/4801")]
    public override void Dispose()
    {
        TextBuffer.ChangeContentType(InertContentType.Instance, null);

        if (TextBuffer.Properties != null && TextBuffer.Properties.TryGetProperty(typeof(ITextDocument), out ITextDocument textDocument))
        {
            TextBuffer.Properties.RemoveProperty(typeof(ITextDocument));

            try
            {
                textDocument.Dispose();
            }
            catch
            {
                // Eat the exception for now while we are investigating an issue.
                // There is System.OperationCanceledException: 'Project unload has already occurred or begun.'
                // that gets thrown if Razor file is open when you are shutting down VS at
                // Microsoft.VisualStudio.ProjectSystem.ProjectAsynchronousTasksServiceBase.RegisterAsyncTask(Microsoft.VisualStudio.Threading.JoinableTask, Microsoft.VisualStudio.ProjectSystem.ProjectCriticalOperation, bool)
                // Microsoft.VisualStudio.ProjectSystem.VS.Implementation.CodeGenerators.GeneratorScheduler.ScheduleFileGeneration(Microsoft.VisualStudio.ProjectSystem.VS.Implementation.CodeGenerators.IGeneratorSchedulerRequest)
                // Microsoft.VisualStudio.ProjectSystem.VS.Implementation.CodeGenerators.SingleFileGeneratorsService.ScheduleRefreshGeneratedFile(string)
                // Microsoft.VisualStudio.ProjectSystem.VS.Implementation.CodeGenerators.SingleFileGeneratorsService.TextDocumentFactoryService_TextDocumentDisposed(object, Microsoft.VisualStudio.Text.TextDocumentEventArgs)
                // Microsoft.VisualStudio.Text.Implementation.TextDocumentFactoryService.RaiseTextDocumentDisposed(Microsoft.VisualStudio.Text.ITextDocument)
                // Microsoft.VisualStudio.Text.Implementation.TextDocument.Dispose()
                // Microsoft.VisualStudio.LanguageServer.ContainedLanguage.VirtualDocumentBase<T>.Dispose() in VirtualDocumentBase
            }
        }
    }
}
