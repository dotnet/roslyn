// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Roslyn.Utilities
{
    /// <summary>
    /// A <see cref="ObjectBinder"/> that records runtime types and object readers during object writing so they
    /// can be used to read back objects later.
    /// </summary>
    /// <remarks>
    /// This binder records runtime types an object readers as a way to avoid needing to describe all serialization types up front
    /// or using reflection to determine them on demand.
    /// </remarks>
    internal static class ObjectBinder
    {
        private static object s_gate = new object();

        private static int s_version;

        private static readonly ObjectBinderState s_state = ObjectBinderState.Create(s_version);
        private static readonly Stack<ObjectBinderState> s_pool = new Stack<ObjectBinderState>();

        public static ObjectBinderState AllocateStateCopy()
        {
            lock (s_gate)
            {
                // If we have any pooled copies, then just return one of those.
                if (s_pool.Count > 0)
                {
                    return s_pool.Pop();
                }

                // Otherwise, create copy from our current state and return that.
                var state = ObjectBinderState.Create(s_version);
                state.CopyFrom(s_state);

                return state;
            }
        }

        public static void FreeStateCopy(ObjectBinderState state)
        {
            lock (s_gate)
            {
                // If our version changed between now and when we returned the state object,
                // then we don't want to keep around this verion in the pool.  
                if (state.Version == s_version)
                {
                    if (s_pool.Count < 128)
                    {
                        s_pool.Push(state);
                    }
                }
            }
        }

        public static void RegisterTypeReader(Type type, Func<ObjectReader, object> typeReader)
        {
            lock (s_gate)
            {
                if (s_state.RegisterTypeReader(type, typeReader))
                {
                    // Registering this type mutated state, clear any cached copies we have
                    // of ourselves.  Also increment the version number so that we don't attempt
                    // to cache any in-flight copies that are returned.
                    s_version++;
                    s_pool.Clear();
                }
            }
        }
    }
}