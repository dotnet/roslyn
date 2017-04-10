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

        private static readonly ObjectBinderState s_state = ObjectBinderState.Create();

        private static readonly Stack<ObjectBinderState> s_pool = new Stack<ObjectBinderState>();

        public static ObjectBinderState AllocateStateCopy()
        {
            lock (s_gate)
            {
                ObjectBinderState state;
                if (s_pool.Count > 0)
                {
                    state = s_pool.Pop();
                }
                else
                {
                    state = ObjectBinderState.Create();
                }

                state.CopyFrom(s_state);

                return state;
            }
        }

        public static void FreeStateCopy(ObjectBinderState state)
        {
            state.Clear();
            lock (s_gate)
            {
                if (s_pool.Count < 128)
                {
                    s_pool.Push(state);
                }
            }
        }

        public static int GetTypeId(Type type)
        {
            lock (s_gate)
            {
                return s_state.GetTypeId(type);
            }
        }

        public static int GetOrAddTypeId(Type type)
        {
            lock (s_gate)
            {
                return s_state.GetOrAddTypeId(type);
            }
        }

        public static void RegisterTypeReader(Type type, Func<ObjectReader, object> typeReader)
        {
            lock (s_gate)
            {
                s_state.RegisterTypeReader(type, typeReader);
            }
        }

        public static Type GetTypeFromId(int typeId)
        {
            lock (s_gate)
            {
                return s_state.GetTypeFromId(typeId);
            }
        }

        public static (Type, Func<ObjectReader, object>) GetTypeAndReaderFromId(int typeId)
        {
            lock (s_gate)
            {
                return s_state.GetTypeAndReaderFromId(typeId);
            }
        }

        public static Func<ObjectReader, object> GetTypeReader(int index)
        {
            lock (s_gate)
            {
                return s_state.GetTypeReader(index);
            }
        }
    }
}