// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

internal abstract class WorkspaceEventRegistration : IDisposable
{
    public static WorkspaceEventRegistration Create<TEventArgs>(AsyncEventMap asyncEventMap, string eventName, Func<TEventArgs, Task> handler)
        where TEventArgs : EventArgs
    {
        return new WorkspaceEventRegistrationImpl<TEventArgs>(asyncEventMap, eventName, handler);
    }

    public abstract void Dispose();

    private sealed class WorkspaceEventRegistrationImpl<TEventArgs>(AsyncEventMap asyncEventMap, string eventName, Func<TEventArgs, Task> handler)
        : WorkspaceEventRegistration
        where TEventArgs : EventArgs
    {
        private readonly AsyncEventMap _asyncEventMap = asyncEventMap;
        private readonly string _eventName = eventName;
        private readonly Func<TEventArgs, Task> _handler = handler;

        public override void Dispose()
             => _asyncEventMap.RemoveAsyncEventHandler(_eventName, _handler);
    }
}
