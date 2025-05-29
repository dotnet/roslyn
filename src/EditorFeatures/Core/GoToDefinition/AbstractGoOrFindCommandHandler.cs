// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding;

namespace Microsoft.CodeAnalysis.GoToDefinition;

internal abstract class AbstractGoOrFindCommandHandler<TLanguageService, TCommandArgs>(
    AbstractGoOrFindNavigationService<TLanguageService> navigationService) : ICommandHandler<TCommandArgs>
    where TLanguageService : class, ILanguageService
    where TCommandArgs : EditorCommandArgs
{
    private readonly AbstractGoOrFindNavigationService<TLanguageService> _navigationService = navigationService;

    public string DisplayName => _navigationService.DisplayName;

    public CommandState GetCommandState(TCommandArgs args)
    {
        var document = args.SubjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
        return _navigationService.IsAvailable(document)
            ? CommandState.Available
            : CommandState.Unspecified;
    }

    public bool ExecuteCommand(TCommandArgs args, CommandExecutionContext context)
    {
        _navigationService.ThreadingContext.ThrowIfNotOnUIThread();

        var subjectBuffer = args.SubjectBuffer;
        var caret = args.TextView.GetCaretPoint(subjectBuffer);
        if (!caret.HasValue)
            return false;

        var document = args.SubjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
        if (!_navigationService.IsAvailable(document))
            return false;

        _navigationService.ExecuteCommand(document, caret.Value.Position);
        return true;
    }

    public TestAccessor GetTestAccessor()
        => new(this);

    public readonly struct TestAccessor(AbstractGoOrFindCommandHandler<TLanguageService, TCommandArgs> instance)
    {
        public void SetDelayHook(Func<CancellationToken, Task> hook)
            => instance._navigationService.GetTestAccessor().DelayHook = hook;
    }
}
