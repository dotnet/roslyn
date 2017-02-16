// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

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
        private static readonly Dictionary<Type, int> s_typeToIndex = new Dictionary<Type, int>();

        private static readonly List<Type> s_types = new List<Type>();
        private static readonly List<Func<ObjectReader, object>> s_typeReaders = new List<Func<ObjectReader, object>>();

        public static int GetTypeId(Type type)
        {
            lock (s_gate)
            {
                return s_typeToIndex[type];
            }
        }

        public static int GetOrAddTypeId(Type type)
        {
            lock (s_gate)
            {
                if (!s_typeToIndex.TryGetValue(type, out var index))
                {
                    RegisterTypeReader(type, typeReader: null);
                    index = s_typeToIndex[type];
                }

                return index;
            }
        }

        public static Type GetTypeFromId(int typeId)
        {
            lock (s_gate)
            {
                return s_types[typeId];
            }
        }

        public static (Type, Func<ObjectReader, object>) GetTypeAndReaderFromId(int typeId)
        {
            lock (s_gate)
            {
                return (s_types[typeId], s_typeReaders[typeId]);
            }
        }

        public static Func<ObjectReader, object> GetTypeReader(int index)
        {
            lock (s_gate)
            {
                return s_typeReaders[index];
            }
        }

        public static void RegisterTypeReader(Type type, Func<ObjectReader, object> typeReader)
        {
            lock (s_gate)
            {
                int index = s_typeReaders.Count;
                s_types.Add(type);
                s_typeReaders.Add(typeReader);
                s_typeToIndex.Add(type, index);
            }
        }
    }
}