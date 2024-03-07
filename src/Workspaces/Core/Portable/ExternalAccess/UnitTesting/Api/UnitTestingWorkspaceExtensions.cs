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
        private readonly Workspace _workspace;
        private readonly EventHandler<TextDocumentEventArgs> _handler;
        private readonly bool _opened;

        internal EventHandlerWrapper(Workspace workspace, Action<UnitTestingTextDocumentEventArgsWrapper> action, bool opened)
        {
            _workspace = workspace;
            _handler = (sender, args) => action(new UnitTestingTextDocumentEventArgsWrapper(args));
            _opened = opened;

            if (_opened)
                _workspace.TextDocumentOpened += _handler;
            else
                _workspace.TextDocumentClosed += _handler;
        }

        public void Dispose()
        {
            if (_opened)
                _workspace.TextDocumentOpened -= _handler;
            else
                _workspace.TextDocumentClosed -= _handler;
        }
    }
}
