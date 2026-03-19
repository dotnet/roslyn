// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Roslyn.Utilities;

internal static class EventHandlerFactory<TArgs>
{
    /// <summary>
    /// Creates an event handler that holds onto the target weakly.
    /// </summary>
    /// <param name="target">The target that is held weakly, and passed as an argument to the invoker.</param>
    /// <param name="invoker">An action that will receive the event arguments as well as the target instance. 
    /// The invoker itself must not capture any state.</param>
    public static EventHandler<TArgs> CreateWeakHandler<TTarget>(TTarget target, Action<TTarget, object?, TArgs> invoker)
        where TTarget : class
    {
        var weakTarget = new WeakReference<TTarget>(target);

        return (sender, args) =>
        {
            if (weakTarget.TryGetTarget(out var targ))
            {
                invoker(targ, sender, args);
            }
        };
    }
}
