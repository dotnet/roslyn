// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Used by Hashtable and Dictionary's SeralizationInfo .ctor's to store the SeralizationInfo
// object until OnDeserialization is called.

using System.Threading;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Collections
{
    internal static partial class HashHelpers
    {
        private static ConditionalWeakTable<object, SerializationInfo>? s_serializationInfoTable;

        public static ConditionalWeakTable<object, SerializationInfo> SerializationInfoTable
        {
            get
            {
                if (s_serializationInfoTable == null)
                    Interlocked.CompareExchange(ref s_serializationInfoTable, new ConditionalWeakTable<object, SerializationInfo>(), null);

                return s_serializationInfoTable;
            }
        }
    }
}
