// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

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
        /// Last created snapshot of our data.  We hand this out instead of exposing our raw
        /// data so that <see cref="ObjectReader"/> and <see cref="ObjectWriter"/> do not need to
        /// take any locks while processing.
        /// </summary>
        private static ObjectBinderSnapshot? s_lastSnapshot = null;

        /// <summary>
        /// Map from a <see cref="Type"/> to the corresponding index in <see cref="s_types"/> and
        /// <see cref="s_typeReaders"/>.  <see cref="ObjectWriter"/> will write out the index into
        /// the stream, and <see cref="ObjectReader"/> will use that index to get the reader used
        /// for deserialization.
        /// </summary>
        private static readonly Dictionary<Type, int> s_typeToIndex = new Dictionary<Type, int>();
        private static readonly List<Type> s_types = new List<Type>();
        private static readonly List<Func<ObjectReader, IObjectWritable>> s_typeReaders = new List<Func<ObjectReader, IObjectWritable>>();

        /// <summary>
        /// Gets an immutable copy of the state of this binder.  This copy does not need to be
        /// locked while it is used.
        /// </summary>
        public static ObjectBinderSnapshot GetSnapshot()
        {
            lock (s_gate)
            {
                if (s_lastSnapshot == null)
                {
                    s_lastSnapshot = new ObjectBinderSnapshot(s_typeToIndex, s_types, s_typeReaders);
                }

                return s_lastSnapshot.Value;
            }
        }

        public static void RegisterTypeReader(Type type, Func<ObjectReader, IObjectWritable> typeReader)
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

                // Registering this type mutated state, clear the cached last snapshot as it
                // is no longer valid.
                s_lastSnapshot = null;
            }
        }
    }
}
