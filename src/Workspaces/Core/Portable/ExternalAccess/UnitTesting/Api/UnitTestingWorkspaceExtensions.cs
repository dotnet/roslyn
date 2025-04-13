// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api;

internal static class UnitTestingWorkspaceExtensions
{
    public static IDisposable RegisterTextDocumentOpenedEventHandler(this Workspace workspace, Action<UnitTestingTextDocumentEventArgsWrapper> action)
        => new EventHandlerWrapper(workspace, action, opened: true);

    public static IDisposable RegisterTextDocumentClosedEventHandler(this Workspace workspace, Action<UnitTestingTextDocumentEventArgsWrapper> action)
        => new EventHandlerWrapper(workspace, action, opened: false);

    private sealed class EventHandlerWrapper : IDisposable
    {
        private readonly IDisposable _textDocumentOperationDisposer;

        internal EventHandlerWrapper(Workspace workspace, Action<UnitTestingTextDocumentEventArgsWrapper> action, bool opened)
        {
            _textDocumentOperationDisposer = opened
                ? workspace.RegisterTextDocumentOpenedHandler(HandleAsync)
                : workspace.RegisterTextDocumentClosedHandler(HandleAsync);

            Task HandleAsync(TextDocumentEventArgs args, CancellationToken cancellationToken)
            {
                action(new UnitTestingTextDocumentEventArgsWrapper(args));

                return Task.CompletedTask;
            }
        }

        public void Dispose()
            => _textDocumentOperationDisposer.Dispose();
    }
}
