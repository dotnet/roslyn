// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using Microsoft.Cci;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Used to devirtualize ConcurrentDictionary for EqualityComparer{T}.Default and ReferenceEquals
    /// 
    /// This type is to enable fast-path devirtualization in the Jit. Dictionary{K, V}, HashTable{T}
    /// and ConcurrentDictionary{K, V} will devirtualize (and potentially inline) the IEquatable{T}.Equals
    /// method for a struct when the Comparer is unspecified in .NET Core, .NET 5; whereas specifying
    /// a Comparer will make .Equals and GetHashcode slower interface calls.
    /// </summary>
    internal readonly struct IReferenceOrISignature : IEquatable<IReferenceOrISignature>
    {
        private readonly object _item;

        public IReferenceOrISignature(IReference item) => _item = item;

        public IReferenceOrISignature(ISignature item) => _item = item;

        // Needed to resolve ambiguity for types that implement both IReference and ISignature
        public IReferenceOrISignature(IMethodReference item) => _item = item;

        public bool Equals(IReferenceOrISignature other) => ReferenceEquals(_item, other._item);

        public override bool Equals(object? obj) => false;

        public override int GetHashCode() => RuntimeHelpers.GetHashCode(_item);

        public override string ToString() => _item.ToString() ?? "null";

        internal object AsObject() => _item;
    }
}
