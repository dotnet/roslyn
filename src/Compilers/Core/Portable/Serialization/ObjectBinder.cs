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
        private static object s_typesGate = new object();
        private static readonly Dictionary<Type, int> s_typeToIndex = new Dictionary<Type, int>();
        private static readonly List<Type> s_types = new List<Type>();

        private static object s_readerGate = new object();
        private static Dictionary<Type, Func<ObjectReader, object>> s_typeToReader = new Dictionary<Type, Func<ObjectReader, object>>();

        public static int GetTypeId(Type type)
        {
            lock (s_typesGate)
            {
                if (!s_typeToIndex.TryGetValue(type, out var index))
                {
                    index = s_types.Count;
                    s_types.Add(type);
                    s_typeToIndex.Add(type, index);
                }

                return index;
            }
        }

        public static Type GetTypeFromId(int typeId)
        {
            lock (s_typesGate)
            {
                return s_types[typeId];
            }
        }

        public static Func<ObjectReader, object> GetTypeReader(Type type)
        {
            lock (s_readerGate)
            {
                return s_typeToReader[type];
            }
        }

        public static void RegisterTypeReader(Type type, Func<ObjectReader, object> typeReader)
        {
            lock (s_readerGate)
            {
                s_typeToReader[type] = typeReader;
            }
        }
    }
}