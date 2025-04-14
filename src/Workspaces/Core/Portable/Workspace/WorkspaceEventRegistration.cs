// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

internal abstract class WorkspaceEventRegistration : IDisposable
{
    public static WorkspaceEventRegistration Create<TEventArgs>(AsyncEventMap asyncEventMap, string eventName, Action<TEventArgs> handler, bool requiresMainThread)
        where TEventArgs : EventArgs
    {
        return new WorkspaceEventRegistrationImpl<TEventArgs>(asyncEventMap, eventName, handler, requiresMainThread);
    }

    public abstract void Dispose();

    private sealed class WorkspaceEventRegistrationImpl<TEventArgs>(AsyncEventMap asyncEventMap, string eventName, Action<TEventArgs> handler, bool requiresMainThread)
        : WorkspaceEventRegistration
        where TEventArgs : EventArgs
    {
        private readonly AsyncEventMap _asyncEventMap = asyncEventMap;
        private readonly string _eventName = eventName;
        private readonly Action<TEventArgs> _handler = handler;
        private readonly bool _requiresMainThread = requiresMainThread;

        public override void Dispose()
             => _asyncEventMap.RemoveHandler(_eventName, _handler, _requiresMainThread);
    }
}
