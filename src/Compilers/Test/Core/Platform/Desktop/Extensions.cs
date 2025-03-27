// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

#if NET472
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.Emit;
using Roslyn.Utilities;

namespace Roslyn.Test.Utilities.Desktop
{
    internal static class SerializationInfoExtensions
    {
        public static void AddArray<T>(this SerializationInfo info, string name, ImmutableArray<T> value) where T : class
        {
            // This will store the underlying T[] directly into the SerializationInfo. That is safe because it
            // only ever reads from the array. This is done instead of creating a copy because it is a 
            // significant source of allocations in our unit tests.
            info.AddValue(name, value.IsDefault ? null : ImmutableCollectionsMarshal.AsArray(value), typeof(T[]));
        }

        public static ImmutableArray<T> GetArray<T>(this SerializationInfo info, string name) where T : class
        {
            var array = (T[])info.GetValue(name, typeof(T[]));
            return ImmutableCollectionsMarshal.AsImmutableArray(array);
        }

        public static void AddByteArray(this SerializationInfo info, string name, ImmutableArray<byte> value)
        {
            // This will store the underlying byte[] directly into the SerializationInfo. That is safe because it
            // only ever reads from the array. This is done instead of creating a copy because it is a 
            // significant source of allocations in our unit tests.
            info.AddValue(name, value.IsDefault ? null : ImmutableCollectionsMarshal.AsArray(value), typeof(byte[]));
        }

        public static ImmutableArray<byte> GetByteArray(this SerializationInfo info, string name)
        {
            var array = (byte[])info.GetValue(name, typeof(byte[]));
            return ImmutableCollectionsMarshal.AsImmutableArray(array);
        }
    }
}

#endif
