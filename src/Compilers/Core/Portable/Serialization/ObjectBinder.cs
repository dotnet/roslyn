// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Roslyn.Utilities
{
    /// <summary>
    /// <see cref="ObjectBinder"/> is a registry that maps between arbitrary <see cref="Type"/>s and 
    /// the 'reader' function used to deserialize serialized instances of those types.  Registration
    /// must happen ahead of time using the <see cref="RegisterTypeReader"/> method.
    /// </summary>
    internal static class ObjectBinder
    {
        /// <summary>
        /// Lock for all data in this type.
        /// </summary>
        private static object s_gate = new object();

        /// <summary>
        /// A monotonically increasing counter we have that gets changed every time a new deserialization
        /// function is registered with us.  We use this to determine if we can still use our pooled
        /// snapshots that we hand out.
        /// </summary>
        private static int s_version;

        /// <summary>
        /// Pool of immutable snapshots of our data.  We hand these out instead of exposing our raw
        /// data so that <see cref="ObjectReader"/> and <see cref="ObjectWriter"/> do not need to
        /// take any locks while processing.
        /// </summary>
        private static readonly Stack<ObjectBinderSnapshot> s_pool = new Stack<ObjectBinderSnapshot>();

        /// <summary>
        /// Map from a <see cref="Type"/> to the corresponding index in <see cref="s_types"/> and
        /// <see cref="s_typeReaders"/>.  <see cref="ObjectWriter"/> will write out the index into
        /// the stream, and <see cref="ObjectReader"/> will use that index to get the reader used
        /// for deserialization.
        /// </summary>
        private static readonly Dictionary<Type, int> s_typeToIndex = new Dictionary<Type, int>();
        private static readonly List<Type> s_types = new List<Type>();
        private static readonly List<Func<ObjectReader, object>> s_typeReaders = new List<Func<ObjectReader, object>>();

        /// <summary>
        /// Gets an immutable copy of the state of this binder.  This copy does not need to be
        /// locked while it is used.
        /// </summary>
        public static ObjectBinderSnapshot AllocateStateCopy()
        {
            lock (s_gate)
            {
                // If we have any pooled copies, then just return one of those.
                if (s_pool.Count > 0)
                {
                    return s_pool.Pop();
                }

                // Otherwise, create copy from our current state and return that.
                var state = new ObjectBinderSnapshot(
                    s_version, 
                    s_typeToIndex.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                    s_types.ToImmutableArray(), s_typeReaders.ToImmutableArray());

                return state;
            }
        }

        public static void FreeStateCopy(ObjectBinderSnapshot state)
        {
            lock (s_gate)
            {
                // If our version changed between now and when we returned the state object,
                // then we don't want to keep around this version in the pool.  
                if (state.Version == s_version)
                {
                    if (s_pool.Count < 32)
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
                if (s_typeToIndex.ContainsKey(type))
                {
                    // We already knew about this type, nothing to register.
                    return;
                }

                int index = s_typeReaders.Count;
                s_types.Add(type);
                s_typeReaders.Add(typeReader);
                s_typeToIndex.Add(type, index);

                // Registering this type mutated state, clear any cached copies we have
                // of ourselves.  Also increment the version number so that we don't attempt
                // to cache any in-flight copies that are returned.
                s_version++;
                s_pool.Clear();
            }
        }
    }
}