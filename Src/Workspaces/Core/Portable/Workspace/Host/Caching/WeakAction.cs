// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.Host
{
    internal class WeakAction<TAnchor, TArg> : IWeakAction<TArg> where TAnchor : class
    {
        private readonly WeakReference<TAnchor> owner;
        private readonly Action<TAnchor, TArg> weakAction;

        public WeakAction(TAnchor owner, Action<TAnchor, TArg> weakAction)
        {
            this.owner = new WeakReference<TAnchor>(owner);
            this.weakAction = weakAction;
        }

        public void Invoke(TArg value)
        {
            // invoke action if the anchor is still alive.
            TAnchor anchor;
            if (this.owner.TryGetTarget(out anchor))
            {
                this.weakAction(anchor, value);
            }
            else
            {
#if DEBUG
                //// Trace.WriteLine(typeof(TData));
#endif
            }
        }
    }
}
