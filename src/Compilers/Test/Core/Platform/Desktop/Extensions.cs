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
            // we will copy the content into an array and serialize the copy
            // we could serialize element-wise, but that would require serializing
            // name and type for every serialized element which seems worse than creating a copy.
            info.AddValue(name, value.IsDefault ? null : value.ToArray(), typeof(T[]));
        }

        public static ImmutableArray<T> GetArray<T>(this SerializationInfo info, string name) where T : class
        {
            var arr = (T[])info.GetValue(name, typeof(T[]));
            return ImmutableArray.Create<T>(arr);
        }

        public static void AddByteArray(this SerializationInfo info, string name, ImmutableArray<byte> value)
        {
            // we will copy the content into an array and serialize the copy
            // we could serialize element-wise, but that would require serializing
            // name and type for every serialized element which seems worse than creating a copy.
            info.AddValue(name, value.IsDefault ? null : value.ToArray(), typeof(byte[]));
        }

        public static ImmutableArray<byte> GetByteArray(this SerializationInfo info, string name)
        {
            var arr = (byte[])info.GetValue(name, typeof(byte[]));
            return ImmutableArray.Create<byte>(arr);
        }
    }
}

#endif
